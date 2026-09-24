using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using GM_Desktop_Application;
using GM_Desktop_Application.Models;
using GM_Desktop_Application.Services;
using GM_Desktop_Application.ViewModels;

namespace GM.Desktop.Tests.Views
{
    [TestClass]
    [DoNotParallelize]
    public sealed class WpfRenderingTests
    {
        private sealed class BindingErrors : TraceListener
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

        [TestMethod]
        public void ViewsRenderOnStaWithoutBindingErrorsAndKeepSelectionWhenSorting()
        {
            Exception? failure = null;
            var thread = new Thread(() =>
            {
                var errors = new BindingErrors();
                var previousLevel = PresentationTraceSources.DataBindingSource.Switch.Level;
                PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;
                PresentationTraceSources.DataBindingSource.Listeners.Add(errors);
                try
                {
                    var app = new App();
                    app.InitializeComponent();
                    var shell = new ShellViewModel(new EmptyStore());
                    var host = new ContentControl
                    {
                        Content = shell.Library, Width = 928, Height = 585,
                        FontFamily = new FontFamily("Segoe UI"), FontSize = 14,
                        Foreground = (Brush)app.Resources["TextBrush"], Background = (Brush)app.Resources["BackgroundBrush"]
                    };
                    var canvas = new Border { Background = (Brush)app.Resources["BackgroundBrush"], Padding = new Thickness(36, 28, 36, 28), Child = host };
                    var output = Path.Combine(AppContext.BaseDirectory, "UiSmoke");
                    Directory.CreateDirectory(output);
                    void Render(string name)
                    {
                        canvas.Measure(new Size(host.Width + 72, host.Height + 56));
                        canvas.Arrange(new Rect(0, 0, host.Width + 72, host.Height + 56));
                        canvas.UpdateLayout();
                        var frame = new DispatcherFrame();
                        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => frame.Continue = false));
                        Dispatcher.PushFrame(frame);
                        Find<UserControl>(host)?.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                        canvas.UpdateLayout();
                        var bitmap = new RenderTargetBitmap((int)host.Width + 72, (int)host.Height + 56, 96, 96, PixelFormats.Pbgra32);
                        bitmap.Render(canvas);
                        var encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(bitmap));
                        using var stream = File.Create(Path.Combine(output, name + ".png"));
                        encoder.Save(stream);
                    }
                    Render("empty-library");
                    var now = new DateTimeOffset(2026, 9, 24, 12, 32, 0, TimeSpan.Zero);
                    Campaign New(string name, string system, int days) => new()
                    {
                        Id = Guid.NewGuid(), Name = name, SystemId = "custom", CustomSystemName = system,
                        CreatedAt = now.AddDays(-10), UpdatedAt = now.AddDays(-days)
                    };
                    shell.Library.AddAndSelect(New("The Ashen Crown", "Pathfinder 2e", 0));
                    shell.Library.AddAndSelect(New("Lanterns of Blackwater", "Call of Cthulhu", 1));
                    shell.Library.AddAndSelect(New("Beyond the Pale", "Homebrew", 6));
                    Render("campaign-library");
                    var selection = shell.Library.SelectedCampaign;
                    shell.Library.SortBy(CampaignSortField.Name);
                    Render("sorted-library");
                    Assert.AreSame(selection, shell.Library.SelectedCampaign);
                    var grid = Find<DataGrid>(host)!;
                    Assert.IsNotNull(grid);
                    Assert.AreSame(selection, grid.SelectedItem);
                    Assert.AreEqual(3, grid.Items.Count);
                    shell.Library.NewCommand.Execute(null);
                    host.Content = shell.CurrentView;
                    Render("new-campaign");
                    Assert.AreEqual("Other / custom", Find<ComboBox>(host)!.Text);
                    var creationView = Find<UserControl>(host)!;
                    Assert.AreEqual(CampaignText.NameMaxLength, ((TextBox)creationView.FindName("CampaignName")).MaxLength);
                    Assert.AreEqual(CampaignText.SystemNameMaxLength, ((TextBox)creationView.FindName("SystemName")).MaxLength);
                    var form = (CreateCampaignViewModel)shell.CurrentView;
                    form.SaveCommand.Execute(null);
                    Render("validation");
                    Assert.IsNotNull(form.NameError);
                    Assert.IsNotNull(form.CustomSystemError);
                    host.Width = 588;
                    host.Height = 385;
                    Render("creation-minimum-size");
                    form.CancelCommand.Execute(null);
                    host.Content = shell.CurrentView;
                    Render("library-minimum-size");
                    shell.Library.OpenCommand.Execute(null);
                    host.Content = shell.CurrentView;
                    Render("campaign-page");
                    Assert.IsInstanceOfType<CampaignViewModel>(shell.CurrentView);
                    Assert.IsEmpty(errors.Messages, string.Join(Environment.NewLine, errors.Messages));
                    app.Shutdown();
                }
                catch (Exception exception) { failure = exception; }
                finally
                {
                    PresentationTraceSources.DataBindingSource.Listeners.Remove(errors);
                    PresentationTraceSources.DataBindingSource.Switch.Level = previousLevel;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            Assert.IsTrue(thread.Join(TimeSpan.FromSeconds(30)), "STA rendering timed out.");
            if (failure is not null) throw new AssertFailedException("WPF rendering failed.", failure);
        }

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
    }
}
