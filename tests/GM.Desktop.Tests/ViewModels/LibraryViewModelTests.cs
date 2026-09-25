using System.Globalization;
using GM_Desktop_Application.Models;
using GM_Desktop_Application.ViewModels;

namespace GM.Desktop.Tests.ViewModels
{
    [TestClass]
    public sealed class LibraryViewModelTests
    {
        private FakeCampaignStore store = null!;
        private LibraryViewModel library = null!;
        private CultureInfo previousCulture = null!;

        [TestInitialize]
        public void Initialize()
        {
            previousCulture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            store = new FakeCampaignStore();
            library = new LibraryViewModel(store, () => { }, _ => { });
        }

        [TestCleanup]
        public void Cleanup() => CultureInfo.CurrentCulture = previousCulture;

        private async Task LoadSortingExamples()
        {
            store.Campaigns.AddRange([
                CampaignTestData.Campaign("zebra", "Alpha", 22, 4),
                CampaignTestData.Campaign("alpha", "Beta", 23, 2),
                CampaignTestData.Campaign("Alpha", "beta", 23, 1),
                CampaignTestData.Campaign("Beta", "alpha", 24, 3)]);
            await library.LoadAsync();
        }

        private int[] CampaignIds() => library.Campaigns.Select(campaign =>
            int.Parse(campaign.Id.ToString("N")[20..], CultureInfo.InvariantCulture)).ToArray();

        [TestMethod]
        public async Task DefaultSortUsesLatestUpdateWithStableTies()
        {
            await LoadSortingExamples();

            CollectionAssert.AreEqual(new[] { 3, 1, 2, 4 }, CampaignIds());
        }

        [TestMethod]
        [DataRow(CampaignSortField.Name, 1, new[] { 1, 2, 3, 4 })]
        [DataRow(CampaignSortField.Name, 2, new[] { 4, 3, 1, 2 })]
        [DataRow(CampaignSortField.RulesSystem, 1, new[] { 3, 4, 1, 2 })]
        [DataRow(CampaignSortField.RulesSystem, 2, new[] { 1, 2, 3, 4 })]
        [DataRow(CampaignSortField.LastUpdated, 1, new[] { 4, 1, 2, 3 })]
        [DataRow(CampaignSortField.LastUpdated, 2, new[] { 3, 1, 2, 4 })]
        public async Task SortFieldOrdersCampaignsWithStableTies(CampaignSortField field, int clicks, int[] expected)
        {
            await LoadSortingExamples();

            for (var click = 0; click < clicks; click++) library.SortBy(field);

            CollectionAssert.AreEqual(expected, CampaignIds());
        }

        [TestMethod]
        [DataRow(CampaignSortField.Name)]
        [DataRow(CampaignSortField.RulesSystem)]
        [DataRow(CampaignSortField.LastUpdated)]
        public async Task SortingPreservesSelectedCampaign(CampaignSortField field)
        {
            await LoadSortingExamples();
            library.SelectedCampaign = store.Campaigns[1];

            library.SortBy(field);

            Assert.AreSame(store.Campaigns[1], library.SelectedCampaign);
        }

        [TestMethod]
        public void OpenRequiresSelection() => Assert.IsFalse(library.OpenCommand.CanExecute(null));

        [TestMethod]
        public async Task OpenPassesSelectedCampaignWithoutSaving()
        {
            var campaign = CampaignTestData.Campaign();
            store.Campaigns.Add(campaign);
            Campaign? opened = null;
            library = new LibraryViewModel(store, () => { }, value => opened = value);
            await library.LoadAsync();
            library.SelectedCampaign = library.Campaigns.Single();

            library.OpenCommand.Execute(null);

            Assert.IsTrue(library.OpenCommand.CanExecute(null));
            Assert.AreSame(campaign, opened);
            Assert.AreEqual(campaign.UpdatedAt, opened!.UpdatedAt);
            Assert.AreEqual(0, store.Creates);
        }

        [TestMethod]
        public async Task LoadFailureReportsErrorInsteadOfEmptyState()
        {
            store.LoadError = new UnauthorizedAccessException();

            await library.LoadCommand.ExecuteAsync();

            Assert.IsNotNull(library.Error);
            Assert.IsFalse(library.IsEmpty);
            Assert.IsFalse(library.NewCommand.CanExecute(null));
        }

        [TestMethod]
        public async Task RetryRecoversFromLoadFailure()
        {
            store.LoadError = new UnauthorizedAccessException();
            await library.LoadCommand.ExecuteAsync();
            store.LoadError = null;

            await library.LoadCommand.ExecuteAsync();

            Assert.IsNull(library.Error);
            Assert.IsTrue(library.IsEmpty);
            Assert.IsTrue(library.NewCommand.CanExecute(null));
        }

        [TestMethod]
        public async Task LoadingDisablesConflictingCommands()
        {
            store.PendingLoad = new(TaskCreationOptions.RunContinuationsAsynchronously);
            var loading = library.LoadCommand.ExecuteAsync();
            try
            {
                Assert.IsTrue(library.IsBusy);
                Assert.IsFalse(library.IsEmpty);
                Assert.IsFalse(library.NewCommand.CanExecute(null));
                Assert.IsFalse(library.OpenCommand.CanExecute(null));
                Assert.IsFalse(library.LoadCommand.CanExecute(null));
            }
            finally
            {
                store.PendingLoad.SetResult(new([], []));
                await loading;
            }
        }

        [TestMethod]
        public async Task PartialLoadShowsValidCampaignsWithWarning()
        {
            store.Campaigns.Add(CampaignTestData.Campaign());
            store.Issues = [new("bad-folder", "Invalid metadata")];

            await library.LoadCommand.ExecuteAsync();

            Assert.HasCount(1, library.Campaigns);
            StringAssert.Contains(library.Warning!, "bad-folder");
            Assert.IsFalse(library.IsBusy);
        }
    }
}
