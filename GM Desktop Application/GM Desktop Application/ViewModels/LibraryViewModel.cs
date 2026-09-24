using System.Collections.ObjectModel;
using System.IO;
using GM_Desktop_Application.Models;
using GM_Desktop_Application.Services;

namespace GM_Desktop_Application.ViewModels
{
    public enum CampaignSortField { Name, RulesSystem, LastUpdated }

    public sealed class LibraryViewModel : ObservableObject
    {
        private readonly ICampaignStore store;
        private readonly List<Campaign> campaigns = [];
        private Campaign? selectedCampaign;
        private bool isBusy;
        private string? error;
        private string? warning;
        public ObservableCollection<Campaign> Campaigns { get; } = [];
        public CampaignSortField SortField { get; private set; } = CampaignSortField.LastUpdated;
        public bool SortDescending { get; private set; } = true;
        public Campaign? SelectedCampaign
        {
            get => selectedCampaign;
            set { if (Set(ref selectedCampaign, value)) OpenCommand.Refresh(); }
        }
        public bool IsBusy => isBusy;
        public bool IsEmpty => !IsBusy && Error is null && Campaigns.Count == 0;
        public string CountDisplay => $"{Campaigns.Count} campaign{(Campaigns.Count == 1 ? "" : "s")}";
        public string? Error => error;
        public string? Warning => warning;
        public RelayCommand NewCommand { get; }
        public RelayCommand OpenCommand { get; }
        public AsyncCommand LoadCommand { get; }

        public LibraryViewModel(ICampaignStore store, Action create, Action<Campaign> open)
        {
            this.store = store;
            NewCommand = new RelayCommand(create, () => !IsBusy && Error is null);
            OpenCommand = new RelayCommand(() => open(SelectedCampaign!), () => !IsBusy && Error is null && SelectedCampaign is not null);
            LoadCommand = new AsyncCommand(LoadAsync, () => !IsBusy);
        }


        public async Task LoadAsync()
        {
            if (IsBusy) return;
            Set(ref isBusy, true, nameof(IsBusy));
            Set(ref error, null, nameof(Error));
            RefreshState();
            try
            {
                var library = await store.ListAsync();
                campaigns.Clear();
                campaigns.AddRange(library.Campaigns);
                Set(ref warning, library.Issues.Count == 0 ? null : "Some campaigns could not be loaded:\n" +
                    string.Join("\n", library.Issues.Select(issue => $"{issue.Folder}: {issue.Message}")), nameof(Warning));
                ApplySort();
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                Set(ref error, $"The campaign library could not be read. Check access to the application's Data folder and retry.\n{exception.Message}", nameof(Error));
            }
            finally
            {
                Set(ref isBusy, false, nameof(IsBusy));
                RefreshState();
            }
        }

        public void SortBy(CampaignSortField field)
        {
            if (IsBusy) return;
            SortDescending = SortField == field ? !SortDescending : field == CampaignSortField.LastUpdated;
            SortField = field;
            ApplySort();
        }

        public void AddAndSelect(Campaign campaign)
        {
            campaigns.Add(campaign);
            ApplySort();
            SelectedCampaign = Campaigns.Single(item => item.Id == campaign.Id);
        }

        private void ApplySort()
        {
            var selectedId = SelectedCampaign?.Id;
            var comparer = StringComparer.CurrentCultureIgnoreCase;
            IOrderedEnumerable<Campaign> ordered = SortField switch
            {
                CampaignSortField.Name => SortDescending ? campaigns.OrderByDescending(c => c.Name, comparer) : campaigns.OrderBy(c => c.Name, comparer),
                CampaignSortField.RulesSystem => SortDescending ? campaigns.OrderByDescending(c => c.CustomSystemName, comparer) : campaigns.OrderBy(c => c.CustomSystemName, comparer),
                _ => SortDescending ? campaigns.OrderByDescending(c => c.UpdatedAt) : campaigns.OrderBy(c => c.UpdatedAt)
            };
            var sorted = ordered.ThenBy(c => c.Name, comparer).ThenBy(c => c.Id).ToArray();
            Campaigns.Clear();
            foreach (var campaign in sorted) Campaigns.Add(campaign);
            SelectedCampaign = Campaigns.FirstOrDefault(c => c.Id == selectedId);
            RefreshState();
        }

        private void RefreshState()
        {
            Notify(nameof(IsEmpty));
            Notify(nameof(CountDisplay));
            NewCommand.Refresh();
            OpenCommand.Refresh();
            LoadCommand.Refresh();
        }
    }
}
