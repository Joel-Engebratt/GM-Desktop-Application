using GM_Desktop_Application.ViewModels;

namespace GM.Desktop.Tests.ViewModels
{
    [TestClass]
    public sealed class ShellViewModelTests
    {
        private FakeCampaignStore store = null!;
        private ShellViewModel shell = null!;

        [TestInitialize]
        public async Task Initialize()
        {
            store = new FakeCampaignStore();
            shell = new ShellViewModel(store);
            await shell.InitializeAsync();
        }

        private CreateCampaignViewModel OpenForm()
        {
            shell.Library.NewCommand.Execute(null);
            return (CreateCampaignViewModel)shell.CurrentView;
        }

        private async Task CreateCampaign()
        {
            var form = OpenForm();
            form.Name = "New";
            form.CustomSystemName = "Homebrew";
            await form.SaveCommand.ExecuteAsync();
        }

        [TestMethod]
        public async Task CreationOpensCampaignPage()
        {
            await CreateCampaign();

            Assert.IsInstanceOfType<CampaignViewModel>(shell.CurrentView);
            Assert.AreEqual("New", ((CampaignViewModel)shell.CurrentView).Campaign.Name);
        }

        [TestMethod]
        public async Task CreationAddsAndSelectsNewCampaign()
        {
            await CreateCampaign();

            Assert.HasCount(1, shell.Library.Campaigns);
            Assert.AreSame(shell.Library.Campaigns.Single(), shell.Library.SelectedCampaign);
        }

        [TestMethod]
        public async Task BackReturnsToSameLibraryWithSortPreserved()
        {
            shell.Library.SortBy(CampaignSortField.Name);
            await CreateCampaign();

            ((CampaignViewModel)shell.CurrentView).BackCommand.Execute(null);

            Assert.AreSame(shell.Library, shell.CurrentView);
            Assert.AreEqual(CampaignSortField.Name, shell.Library.SortField);
            Assert.IsFalse(shell.Library.SortDescending);
        }

        [TestMethod]
        public void CancelReturnsToLibraryWithoutSaving()
        {
            var form = OpenForm();
            form.Name = "Unsaved";

            form.CancelCommand.Execute(null);

            Assert.AreSame(shell.Library, shell.CurrentView);
            Assert.AreEqual(0, store.Creates);
        }

        [TestMethod]
        public void ReopeningCancelledFormStartsBlank()
        {
            var form = OpenForm();
            form.Name = "Unsaved";
            form.CancelCommand.Execute(null);

            var reopened = OpenForm();

            Assert.AreEqual("", reopened.Name);
        }
    }
}
