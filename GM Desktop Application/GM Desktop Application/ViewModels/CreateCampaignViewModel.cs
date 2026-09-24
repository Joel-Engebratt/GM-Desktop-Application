using System.IO;
using GM_Desktop_Application.Models;
using GM_Desktop_Application.Services;

namespace GM_Desktop_Application.ViewModels
{
    public sealed class CreateCampaignViewModel : ObservableObject
    {
        private string name = "";
        private string customSystemName = "";
        private RulesSystem? selectedSystem = RulesSystem.Supported[0];
        private bool validated;
        private bool isBusy;
        private string? error;
        public string Name { get => name; set { if (Set(ref name, value)) ValidateFields(); } }
        public string CustomSystemName { get => customSystemName; set { if (Set(ref customSystemName, value)) ValidateFields(); } }
        public IReadOnlyList<RulesSystem> Systems => RulesSystem.Supported;
        public RulesSystem? SelectedSystem
        {
            get => selectedSystem;
            set { if (Set(ref selectedSystem, value)) { Notify(nameof(IsCustom)); ValidateFields(); } }
        }
        public bool IsCustom => SelectedSystem?.Id == "custom";
        public bool IsBusy => isBusy;
        public bool IsEditable => !IsBusy;
        public string? Error => error;
        public string? NameError => validated ? Draft.NameError : null;
        public string? SystemError => validated ? Draft.SystemError : null;
        public string? CustomSystemError => validated ? Draft.CustomSystemError : null;
        private CampaignDraft Draft => new(Name, SelectedSystem?.Id ?? "", CustomSystemName);
        public RelayCommand CancelCommand { get; }
        public AsyncCommand SaveCommand { get; }

        public CreateCampaignViewModel(ICampaignStore store, Action cancel, Action<Campaign> saved)
        {
            CancelCommand = new RelayCommand(cancel, () => !IsBusy);
            SaveCommand = new AsyncCommand(async () =>
            {
                validated = true;
                ValidateFields();
                var draft = Draft;
                if (!draft.IsValid) return;
                Set(ref isBusy, true, nameof(IsBusy));
                Notify(nameof(IsEditable));
                CancelCommand.Refresh();
                Set(ref error, null, nameof(Error));
                try
                {
                    var campaign = await store.CreateAsync(draft);
                    saved(campaign);
                }
                catch (UnauthorizedAccessException)
                {
                    Set(ref error, "The campaign could not be saved. Move the application folder to a writable location, then try again. Your entries have been kept.", nameof(Error));
                }
                catch (IOException exception)
                {
                    Set(ref error, $"The campaign could not be saved. Check free space and that the application folder is writable, then try again. Your entries have been kept.\n{exception.Message}", nameof(Error));
                }
                finally
                {
                    Set(ref isBusy, false, nameof(IsBusy));
                    Notify(nameof(IsEditable));
                    CancelCommand.Refresh();
                }
            }, () => !IsBusy);
        }

        private void ValidateFields()
        {
            Notify(nameof(NameError));
            Notify(nameof(SystemError));
            Notify(nameof(CustomSystemError));
        }
    }
}
