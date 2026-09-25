using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using GM_Desktop_Application.Models;
using GM_Desktop_Application.Services;
using GM_Desktop_Application.ViewModels;

namespace GM.Desktop.Tests.Views
{
    internal sealed class WpfRenderFixture : IDisposable
    {
        private sealed class BindingErrorListener : TraceListener
        {
            public List<string> Messages { get; } = [];
            public override void Write(string? message) { if (message is not null) Messages.Add(message); }
            public override void WriteLine(string? message) { if (message is not null) Messages.Add(message); }
        }

        private sealed class EmptyStore : ICampaignStore
        {
            public Task<CampaignLibrary> ListAsync() => Task.FromResult(new CampaignLibrary([], []));
            public Task<Campaign> CreateAsync(CampaignDraft draft) => throw new NotSupportedException();
        }

        private readonly BindingErrorListener errors = new();
        private readonly SourceLevels previousLevel = PresentationTraceSources.DataBindingSource.Switch.Level;
        private readonly Border canvas;
        public ShellViewModel Shell { get; } = new(new EmptyStore());
        public ContentControl Host { get; }
        public IReadOnlyList<string> BindingErrors => errors.Messages;

        public WpfRenderFixture()
        {
            PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;
            PresentationTraceSources.DataBindingSource.Listeners.Add(errors);
            var resources = Application.Current.Resources;
            Host = new ContentControl
            {
                Content = Shell.Library, Width = 928, Height = 585,
                FontFamily = new FontFamily("Segoe UI"), FontSize = 14,
                Foreground = (Brush)resources["TextBrush"], Background = (Brush)resources["BackgroundBrush"]
            };
            canvas = new Border
            {
                Background = (Brush)resources["BackgroundBrush"], Padding = new Thickness(36, 28, 36, 28), Child = Host
            };
        }

        public void PopulateLibrary()
        {
            var now = new DateTimeOffset(2026, 9, 24, 12, 32, 0, TimeSpan.Zero);
            Campaign Campaign(string name, string system, int days) => new()
            {
                Id = Guid.NewGuid(), Name = name, SystemId = "custom", CustomSystemName = system,
                CreatedAt = now.AddDays(-10), UpdatedAt = now.AddDays(-days)
            };
            Shell.Library.AddAndSelect(Campaign("The Ashen Crown", "Pathfinder 2e", 0));
            Shell.Library.AddAndSelect(Campaign("Lanterns of Blackwater", "Call of Cthulhu", 1));
            Shell.Library.AddAndSelect(Campaign("Beyond the Pale", "Homebrew", 6));
        }

        public void Show(string state)
        {
            switch (state)
            {
                case "empty-library": break;
                case "campaign-library": PopulateLibrary(); break;
                case "new-campaign": Shell.Library.NewCommand.Execute(null); break;
                case "validation":
                    Shell.Library.NewCommand.Execute(null);
                    ((CreateCampaignViewModel)Shell.CurrentView).SaveCommand.Execute(null);
                    break;
                case "campaign-page":
                    PopulateLibrary();
                    Shell.Library.OpenCommand.Execute(null);
                    break;
                default: throw new ArgumentException($"Unknown view state: {state}");
            }
            Host.Content = Shell.CurrentView;
        }

        public void Render(string name, bool minimumSize = false)
        {
            if (minimumSize) { Host.Width = 588; Host.Height = 385; }
            canvas.Measure(new Size(Host.Width + 72, Host.Height + 56));
            canvas.Arrange(new Rect(0, 0, Host.Width + 72, Host.Height + 56));
            canvas.UpdateLayout();
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => frame.Continue = false));
            Dispatcher.PushFrame(frame);
            Find<UserControl>()?.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
            canvas.UpdateLayout();
            var bitmap = new RenderTargetBitmap((int)Host.Width + 72, (int)Host.Height + 56, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(canvas);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            var output = Path.Combine(AppContext.BaseDirectory, "UiSmoke");
            Directory.CreateDirectory(output);
            using var stream = File.Create(Path.Combine(output, name + ".png"));
            encoder.Save(stream);
        }

        public T? Find<T>() where T : DependencyObject => Find<T>(Host);

        private static T? Find<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent is T match) return match;
            for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
            {
                var child = Find<T>(VisualTreeHelper.GetChild(parent, index));
                if (child is not null) return child;
            }
            return null;
        }

        public void Dispose()
        {
            Host.Content = null;
            PresentationTraceSources.DataBindingSource.Listeners.Remove(errors);
            PresentationTraceSources.DataBindingSource.Switch.Level = previousLevel;
            errors.Dispose();
        }
    }
}
