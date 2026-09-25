using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;
using System.Xml.Linq;

namespace GM.Desktop.Tests.Views
{
    // WPF permits one Application per process. Share only its dispatcher/resources;
    // each test creates its own views and ViewModels on that dispatcher.
    internal sealed class WpfTestDispatcher : IDisposable
    {
        private readonly Thread thread;
        private readonly Dispatcher dispatcher;

        public WpfTestDispatcher()
        {
            var ready = new TaskCompletionSource<Dispatcher>(TaskCreationOptions.RunContinuationsAsynchronously);
            thread = new Thread(() =>
            {
                try
                {
                    var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                    application.Resources = LoadApplicationResources();
                    ready.SetResult(Dispatcher.CurrentDispatcher);
                    application.Run();
                }
                catch (Exception exception) { ready.TrySetException(exception); }
            }) { IsBackground = true, Name = "WPF rendering tests" };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            dispatcher = ready.Task.WaitAsync(TimeSpan.FromSeconds(30)).GetAwaiter().GetResult();
        }

        public void Run(Action action) => dispatcher.Invoke(action, DispatcherPriority.Normal,
            CancellationToken.None, TimeSpan.FromSeconds(30));

        private static ResourceDictionary LoadApplicationResources()
        {
            // Read a build-copied production XAML file, retaining its real templates and
            // theme, without invoking App.OnStartup or touching normal campaign storage.
            var document = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "TestAssets", "App.xaml"));
            XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
            var resources = new XElement(document.Root!.Element(presentation + "Application.Resources")!.Elements().Single());
            foreach (var attribute in document.Root.Attributes().Where(attribute => attribute.IsNamespaceDeclaration))
            {
                var value = attribute.Value;
                if (value.StartsWith("clr-namespace:", StringComparison.Ordinal) && !value.Contains(";assembly=", StringComparison.Ordinal))
                    value += ";assembly=GM Desktop Application";
                resources.SetAttributeValue(attribute.Name, value);
            }
            foreach (var element in resources.Descendants())
            {
                var namespaceName = element.Name.NamespaceName;
                if (namespaceName.StartsWith("clr-namespace:", StringComparison.Ordinal) && !namespaceName.Contains(";assembly=", StringComparison.Ordinal))
                    element.Name = XName.Get(element.Name.LocalName, namespaceName + ";assembly=GM Desktop Application");
            }
            return (ResourceDictionary)XamlReader.Parse(resources.ToString(), new ParserContext
            {
                BaseUri = new Uri("pack://application:,,,/GM Desktop Application;component/App.xaml")
            });
        }

        public void Dispose()
        {
            Run(() => Application.Current.Shutdown());
            if (!thread.Join(TimeSpan.FromSeconds(30))) throw new TimeoutException("WPF dispatcher did not stop.");
        }
    }
}
