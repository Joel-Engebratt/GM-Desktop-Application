using System.Diagnostics;
using System.Windows.Automation;

namespace GM.Development.Mcp
{
    public sealed record SessionInfo(int ProcessId, string SessionDirectory, string DataDirectory);
    public sealed record UiNode(string Id, string? ParentId, string AutomationId, string Name,
        string ControlType, bool Enabled, bool Offscreen, string? Value, string[] Patterns);
    public sealed record UiSnapshot(IReadOnlyList<UiNode> Nodes, bool Truncated);

    public sealed class DesktopSession(DevelopmentOptions options) : IDisposable
    {
        private readonly object gate = new();
        private readonly Dictionary<string, AutomationElement> elements = [];
        private Process? process;
        private string? directory;

        public SessionInfo Launch()
        {
            lock (gate)
            {
                if (process is { HasExited: false }) return Info();
                process?.Dispose();
                process = null;
                elements.Clear();
                var executable = Path.Combine(options.BuildDirectory, DevelopmentOptions.ExecutableName);
                if (!File.Exists(executable))
                    throw new InvalidOperationException("Build the solution first using scripts/verify.ps1.");
                directory = null;
                var newDirectory = Path.Combine(options.SessionsDirectory, Guid.NewGuid().ToString("N"));
                SessionFiles.CopyBuild(options.BuildDirectory, newDirectory);
                directory = newDirectory;
                return StartApp();
            }
        }

        public SessionInfo Restart()
        {
            lock (gate)
            {
                if (directory is null) throw new InvalidOperationException("Call gm_launch first.");
                CloseCore();
                return StartApp();
            }
        }

        private SessionInfo StartApp()
        {
            try
            {
                process = Process.Start(new ProcessStartInfo(Path.Combine(directory!, DevelopmentOptions.ExecutableName))
                {
                    WorkingDirectory = directory!,
                    UseShellExecute = false,
                    // Redirect inherited stdout so the application can never corrupt MCP framing.
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }) ?? throw new InvalidOperationException("Could not start GM Desktop.");
                process.OutputDataReceived += (_, _) => { };
                process.ErrorDataReceived += (_, _) => { };
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                var watch = Stopwatch.StartNew();
                while (watch.Elapsed < TimeSpan.FromSeconds(20))
                {
                    process.Refresh();
                    if (process.HasExited) throw new InvalidOperationException("GM Desktop exited before its window opened.");
                    if (process.MainWindowHandle != IntPtr.Zero) return Info();
                    Thread.Sleep(100);
                }
                throw new TimeoutException("GM Desktop did not open a window within 20 seconds. Check the desktop session.");
            }
            catch { CloseCore(); throw; }
        }

        public UiSnapshot Inspect(int maxNodes = 250, int maxDepth = 12)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(maxNodes, 1);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(maxNodes, 500);
            ArgumentOutOfRangeException.ThrowIfLessThan(maxDepth, 1);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(maxDepth, 20);
            lock (gate)
            {
                elements.Clear();
                var nodes = new List<UiNode>();
                var truncated = false;
                var prefix = Guid.NewGuid().ToString("N");
                void Visit(AutomationElement element, string? parent, int depth)
                {
                    if (nodes.Count >= maxNodes) { truncated = true; return; }
                    var current = element.Current;
                    if (current.ProcessId != process!.Id) return;
                    var id = $"{prefix}:{nodes.Count}";
                    var value = !current.IsPassword && element.TryGetCurrentPattern(ValuePattern.Pattern, out var valuePattern)
                        ? Limit(((ValuePattern)valuePattern).Current.Value) : null;
                    nodes.Add(new UiNode(id, parent, current.AutomationId, Limit(current.Name),
                        current.ControlType.ProgrammaticName, current.IsEnabled, current.IsOffscreen, value,
                        element.GetSupportedPatterns().Select(pattern => pattern.ProgrammaticName).ToArray()));
                    elements.Add(id, element);
                    var child = TreeWalker.ControlViewWalker.GetFirstChild(element);
                    if (depth == maxDepth) { truncated |= child is not null; return; }
                    while (child is not null)
                    {
                        if (nodes.Count >= maxNodes) { truncated = true; break; }
                        Visit(child, id, depth + 1);
                        child = TreeWalker.ControlViewWalker.GetNextSibling(child);
                    }
                }
                try { Visit(Root(), null, 0); }
                catch (ElementNotAvailableException)
                {
                    elements.Clear();
                    throw new InvalidOperationException("The UI changed during inspection. Inspect again.");
                }
                return new UiSnapshot(nodes, truncated);
            }
        }

        public void Invoke(string id) => Act(id, element => Pattern<InvokePattern>(element, InvokePattern.Pattern).Invoke());

        public void SetText(string id, string text)
        {
            if (text.Length > 4096) throw new ArgumentException("Text is limited to 4096 characters.");
            Act(id, element =>
            {
                if (element.Current.IsPassword) throw new InvalidOperationException("Password controls are not supported.");
                var value = Pattern<ValuePattern>(element, ValuePattern.Pattern);
                if (value.Current.IsReadOnly) throw new InvalidOperationException("The control is read-only.");
                value.SetValue(text);
            });
        }

        public void Select(string id) => Act(id, element => Pattern<SelectionItemPattern>(element, SelectionItemPattern.Pattern).Select());

        public void Expand(string id, bool expanded) => Act(id, element =>
        {
            var pattern = Pattern<ExpandCollapsePattern>(element, ExpandCollapsePattern.Pattern);
            if (expanded) pattern.Expand(); else pattern.Collapse();
        });

        public void Resize(int width, int height)
        {
            if (width is < 660 or > 2560 || height is < 480 or > 1600)
                throw new ArgumentException("Window dimensions must be 660–2560 by 480–1600.");
            lock (gate)
            {
                try { Pattern<TransformPattern>(Root(), TransformPattern.Pattern).Resize(width, height); }
                finally { elements.Clear(); }
            }
        }

        public (string Path, byte[] Png) Capture()
        {
            lock (gate)
            {
                Root();
                var png = WindowCapture.Capture(process!.MainWindowHandle);
                var path = Path.Combine(directory!, $"capture-{Guid.NewGuid():N}.png");
                File.WriteAllBytes(path, png);
                return (path, png);
            }
        }

        public void Close() { lock (gate) { CloseCore(); } }
        public void Dispose() => Close();

        private SessionInfo Info() => new(process!.Id, directory!, Path.Combine(directory!, "Data", "Campaigns"));

        private AutomationElement Root()
        {
            if (process is null || process.HasExited) throw new InvalidOperationException("No running session. Call gm_launch first.");
            process.Refresh();
            if (process.MainWindowHandle == IntPtr.Zero) throw new InvalidOperationException("The session window is unavailable.");
            var root = AutomationElement.FromHandle(process.MainWindowHandle);
            if (root.Current.ProcessId != process.Id) throw new InvalidOperationException("Window ownership changed.");
            return root;
        }

        private void Act(string id, Action<AutomationElement> action)
        {
            lock (gate)
            {
                Root();
                if (!elements.TryGetValue(id, out var element))
                    throw new InvalidOperationException("Unknown or stale element reference. Call gm_inspect again.");
                try
                {
                    if (element.Current.ProcessId != process!.Id) throw new InvalidOperationException("Element belongs to another process.");
                    if (!element.Current.IsEnabled) throw new InvalidOperationException("The control is disabled.");
                    if (element.Current.IsOffscreen) throw new InvalidOperationException("The control is offscreen.");
                    action(element);
                }
                finally { elements.Clear(); }
            }
        }

        private void CloseCore()
        {
            elements.Clear();
            if (process is null) return;
            try
            {
                if (!process.HasExited)
                {
                    process.CloseMainWindow();
                    if (!process.WaitForExit(3000))
                    {
                        process.Kill();
                        process.WaitForExit(3000);
                    }
                }
            }
            finally { process.Dispose(); process = null; }
        }

        private static T Pattern<T>(AutomationElement element, AutomationPattern pattern) where T : class =>
            element.TryGetCurrentPattern(pattern, out var value) ? (T)value :
                throw new InvalidOperationException($"This control does not support {pattern.ProgrammaticName}.");

        private static string Limit(string value) => value.Length <= 2048 ? value : value[..2048] + "…";
    }
}
