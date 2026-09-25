using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace GM.Development.Mcp
{
    [McpServerToolType]
    public sealed class DesktopTools(DesktopSession session)
    {
        [McpServerTool(Name = "gm_launch", Destructive = false, OpenWorld = false)]
        [Description("Launch an isolated copy of the built GM Desktop app. Returns its PID and disposable data directory. Reuses an active session. No access to normal campaign data.")]
        public string Launch() => Run(() => JsonSerializer.Serialize(session.Launch()));

        [McpServerTool(Name = "gm_inspect", ReadOnly = true, OpenWorld = false)]
        [Description("Inspect the owned window's bounded UI Automation control tree. Returns element IDs, automation IDs, names, values and patterns. IDs expire after another inspection or any action. Reinspect after actions; UI work may finish asynchronously. Text is capped at 2048 characters; Truncated signals tree limits.")]
        public string Inspect(int maxNodes = 250, int maxDepth = 12) => Run(() => JsonSerializer.Serialize(session.Inspect(maxNodes, maxDepth)));

        [McpServerTool(Name = "gm_invoke", Destructive = false, OpenWorld = false)]
        [Description("Invoke an enabled visible button or other InvokePattern control from the latest inspection. Can create campaign files only in the isolated session. Inspect again to observe asynchronous completion.")]
        public string Invoke(string elementId) => Run(() => { session.Invoke(elementId); return "Invoked. Inspect again for the resulting UI."; });

        [McpServerTool(Name = "gm_set_text", Destructive = false, OpenWorld = false)]
        [Description("Replace text in an editable ValuePattern control from the latest inspection. Application validation still applies. Does not simulate physical typing or clipboard input.")]
        public string SetText(string elementId, string text) => Run(() => { session.SetText(elementId, text); return "Text set. Inspect again."; });

        [McpServerTool(Name = "gm_select", Destructive = false, OpenWorld = false)]
        [Description("Select a visible row or list item supporting SelectionItemPattern, using its latest inspection ID.")]
        public string Select(string elementId) => Run(() => { session.Select(elementId); return "Selected. Inspect again."; });

        [McpServerTool(Name = "gm_expand", Destructive = false, OpenWorld = false)]
        [Description("Expand or collapse a control supporting ExpandCollapsePattern, such as a combo box. Inspect again to find its items.")]
        public string Expand(string elementId, bool expanded) => Run(() => { session.Expand(elementId, expanded); return "Updated. Inspect again."; });

        [McpServerTool(Name = "gm_resize", Destructive = false, OpenWorld = false)]
        [Description("Resize the owned window in screen coordinates. Bounds: width 660–2560, height 480–1600. Inspect and capture to verify layout.")]
        public string Resize(int width, int height) => Run(() => { session.Resize(width, height); return "Resized. Inspect again."; });

        [McpServerTool(Name = "gm_capture", ReadOnly = true, OpenWorld = false)]
        [Description("Capture only the owned app window as PNG and save a diagnostic copy in its session directory. Returns an inline image and its path. Requires a restored window; popup windows are not included.")]
        public CallToolResult Capture() => Run(() =>
        {
            var (path, png) = session.Capture();
            return new CallToolResult
            {
                Content = [new TextContentBlock { Text = path }, ImageContentBlock.FromBytes(png, "image/png")]
            };
        });

        [McpServerTool(Name = "gm_close", Destructive = true, OpenWorld = false)]
        [Description("Close only the app instance owned by this MCP server. Force termination after 3 seconds if needed; unsaved form entries are lost. Retains disposable session files for diagnosis.")]
        public string Close() => Run(() => { session.Close(); return "Closed. Session files retained under artifacts/mcp."; });

        [McpServerTool(Name = "gm_restart", Destructive = true, OpenWorld = false)]
        [Description("Restart the owned app using the same disposable data directory to test persistence. Unsaved form entries are lost. Requires a previous launch in this server session.")]
        public string Restart() => Run(() => JsonSerializer.Serialize(session.Restart()));

        private static T Run<T>(Func<T> operation)
        {
            try { return operation(); }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException
                or IOException or TimeoutException or System.ComponentModel.Win32Exception
                or System.Windows.Automation.ElementNotAvailableException or System.Runtime.InteropServices.COMException)
            {
                // The SDK deliberately hides ordinary exception messages. These expected
                // local tool errors need actionable messages (stale IDs, blank desktop, etc.).
                throw new McpException(exception.Message, exception);
            }
        }
    }
}
