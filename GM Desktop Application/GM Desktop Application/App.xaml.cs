using System.Windows;
using GM_Desktop_Application.Services;
using GM_Desktop_Application.ViewModels;

namespace GM_Desktop_Application
{
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            var shell = new ShellViewModel(new JsonCampaignStore());
            MainWindow = new MainWindow { DataContext = shell };
            MainWindow.Show();
            await shell.InitializeAsync();
        }
    }
}
