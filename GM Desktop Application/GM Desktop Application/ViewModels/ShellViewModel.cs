using GM_Desktop_Application.Models;
using GM_Desktop_Application.Services;

namespace GM_Desktop_Application.ViewModels
{
    public sealed class CampaignViewModel(Campaign campaign, Action back)
    {
        public Campaign Campaign { get; } = campaign;
        public RelayCommand BackCommand { get; } = new(back);
    }

    public sealed class ShellViewModel : ObservableObject
    {
        private object currentView = null!;
        public object CurrentView { get => currentView; private set => Set(ref currentView, value); }
        public LibraryViewModel Library { get; }

        public ShellViewModel(ICampaignStore store)
        {
            Library = new LibraryViewModel(store, () =>
                CurrentView = new CreateCampaignViewModel(store, ShowLibrary, campaign =>
                {
                    Library!.AddAndSelect(campaign);
                    Open(campaign);
                }), Open);
            CurrentView = Library;
        }

        public Task InitializeAsync() => Library.LoadAsync();
        private void ShowLibrary() => CurrentView = Library;
        private void Open(Campaign campaign) => CurrentView = new CampaignViewModel(campaign, ShowLibrary);
    }
}
