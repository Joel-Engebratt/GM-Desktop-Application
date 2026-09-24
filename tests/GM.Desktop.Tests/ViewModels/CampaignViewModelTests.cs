using System.Globalization;
using GM_Desktop_Application.Models;
using GM_Desktop_Application.Services;
using GM_Desktop_Application.ViewModels;

namespace GM.Desktop.Tests.ViewModels
{
    [TestClass]
    public sealed class CampaignViewModelTests
    {
        private static Campaign Campaign(string name, string system, int day, int id) => new()
        {
            Id = new Guid($"00000000-0000-0000-0000-{id:D12}"), Name = name, SystemId = "custom", CustomSystemName = system,
            CreatedAt = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero), UpdatedAt = new DateTimeOffset(2026, 9, day, 0, 0, 0, TimeSpan.Zero)
        };

        private sealed class FakeStore : ICampaignStore
        {
            public List<Campaign> Campaigns { get; } = [];
            public IReadOnlyList<CampaignLoadIssue> Issues { get; set; } = [];
            public Exception? LoadError { get; set; }
            public Exception? SaveError { get; set; }
            public TaskCompletionSource<CampaignLibrary>? PendingLoad { get; set; }
            public TaskCompletionSource<Campaign>? PendingSave { get; set; }
            public int Creates { get; private set; }
            public Task<CampaignLibrary> ListAsync() => LoadError is not null ? Task.FromException<CampaignLibrary>(LoadError) : PendingLoad?.Task ?? Task.FromResult(new CampaignLibrary(Campaigns.ToArray(), Issues));
            public Task<Campaign> CreateAsync(CampaignDraft draft)
            {
                Creates++;
                if (SaveError is not null) return Task.FromException<Campaign>(SaveError);
                if (PendingSave is not null) return PendingSave.Task;
                var campaign = Campaign(draft.Name.Trim(), draft.CustomSystemName.Trim(), 24, Creates + 100);
                Campaigns.Add(campaign);
                return Task.FromResult(campaign);
            }
        }

        [TestMethod]
        public async Task AllSortFieldsToggleAndPreserveSelectionWithStableTies()
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            var store = new FakeStore();
            store.Campaigns.AddRange([Campaign("zebra", "Alpha", 22, 4), Campaign("alpha", "Beta", 23, 2), Campaign("Alpha", "beta", 23, 1), Campaign("Beta", "alpha", 24, 3)]);
            var vm = new LibraryViewModel(store, () => { }, _ => { });
            await vm.LoadAsync();
            void Expect(params int[] ids) => CollectionAssert.AreEqual(ids, vm.Campaigns.Select(c => int.Parse(c.Id.ToString("N")[20..], CultureInfo.InvariantCulture)).ToArray());
            Expect(3, 1, 2, 4);
            vm.SelectedCampaign = store.Campaigns[1];
            vm.SortBy(CampaignSortField.Name);
            Expect(1, 2, 3, 4);
            vm.SortBy(CampaignSortField.Name);
            Expect(4, 3, 1, 2);
            vm.SortBy(CampaignSortField.RulesSystem);
            Expect(3, 4, 1, 2);
            vm.SortBy(CampaignSortField.RulesSystem);
            Expect(1, 2, 3, 4);
            vm.SortBy(CampaignSortField.LastUpdated);
            Expect(3, 1, 2, 4);
            vm.SortBy(CampaignSortField.LastUpdated);
            Expect(4, 1, 2, 3);
            Assert.AreEqual(store.Campaigns[1].Id, vm.SelectedCampaign!.Id);
        }

        [TestMethod]
        public async Task OpenRequiresSelectionAndDoesNotChangeTimestamp()
        {
            var campaign = Campaign("Name", "Homebrew", 24, 1);
            var store = new FakeStore();
            store.Campaigns.Add(campaign);
            Campaign? opened = null;
            var vm = new LibraryViewModel(store, () => { }, value => opened = value);
            Assert.IsFalse(vm.OpenCommand.CanExecute(null));
            await vm.LoadAsync();
            vm.SelectedCampaign = vm.Campaigns.Single();
            Assert.IsTrue(vm.OpenCommand.CanExecute(null));
            vm.OpenCommand.Execute(null);
            Assert.AreSame(campaign, opened);
            Assert.AreEqual(campaign.UpdatedAt, opened!.UpdatedAt);
            Assert.AreEqual(0, store.Creates);
        }

        [TestMethod]
        public async Task LoadFailureIsNotAnEmptyStateAndRetryRecovers()
        {
            var store = new FakeStore { LoadError = new UnauthorizedAccessException() };
            var vm = new LibraryViewModel(store, () => { }, _ => { });
            await vm.LoadCommand.ExecuteAsync();
            Assert.IsNotNull(vm.Error);
            Assert.IsFalse(vm.IsEmpty);
            Assert.IsFalse(vm.NewCommand.CanExecute(null));
            store.LoadError = null;
            await vm.LoadCommand.ExecuteAsync();
            Assert.IsNull(vm.Error);
            Assert.IsTrue(vm.IsEmpty);
            Assert.IsTrue(vm.NewCommand.CanExecute(null));
        }

        [TestMethod]
        public async Task LoadingDisablesConflictingActionsAndReportsPartialIssues()
        {
            var store = new FakeStore { PendingLoad = new(TaskCreationOptions.RunContinuationsAsynchronously) };
            var vm = new LibraryViewModel(store, () => { }, _ => { });
            var load = vm.LoadCommand.ExecuteAsync();
            Assert.IsTrue(vm.IsBusy);
            Assert.IsFalse(vm.IsEmpty);
            Assert.IsFalse(vm.NewCommand.CanExecute(null));
            Assert.IsFalse(vm.OpenCommand.CanExecute(null));
            Assert.IsFalse(vm.LoadCommand.CanExecute(null));
            store.PendingLoad.SetResult(new([Campaign("Good", "Custom", 24, 1)], [new("bad-folder", "Invalid metadata")]));
            await load;
            Assert.HasCount(1, vm.Campaigns);
            StringAssert.Contains(vm.Warning!, "bad-folder");
            Assert.IsFalse(vm.IsBusy);
        }

        [TestMethod]
        public async Task ValidationIsInlineAndNeverCallsStoreForBlankOrMissingFields()
        {
            var store = new FakeStore();
            var vm = new CreateCampaignViewModel(store, () => { }, _ => Assert.Fail("Must not navigate"));
            Assert.AreEqual("custom", vm.SelectedSystem!.Id);
            await vm.SaveCommand.ExecuteAsync();
            Assert.IsNotNull(vm.NameError);
            Assert.IsNotNull(vm.CustomSystemError);
            vm.Name = "Valid";
            vm.CustomSystemName = "Homebrew";
            vm.SelectedSystem = null;
            await vm.SaveCommand.ExecuteAsync();
            Assert.IsNotNull(vm.SystemError);
            Assert.AreEqual(0, store.Creates);
        }

        [TestMethod]
        public async Task SaveFailureRetainsEntriesAndAllowsRetry()
        {
            var store = new FakeStore { SaveError = new UnauthorizedAccessException() };
            Campaign? saved = null;
            var vm = new CreateCampaignViewModel(store, () => { }, c => saved = c) { Name = " Name ", CustomSystemName = " System " };
            await vm.SaveCommand.ExecuteAsync();
            Assert.IsNull(saved);
            Assert.AreEqual(" Name ", vm.Name);
            Assert.AreEqual(" System ", vm.CustomSystemName);
            StringAssert.Contains(vm.Error!, "writable location");
            Assert.IsTrue(vm.SaveCommand.CanExecute(null));
            store.SaveError = null;
            await vm.SaveCommand.ExecuteAsync();
            Assert.IsNotNull(saved);
            Assert.AreEqual("Name", saved.Name);
            Assert.AreEqual("System", saved.CustomSystemName);
            Assert.IsNull(vm.Error);
        }

        [TestMethod]
        public async Task SaveInProgressPreventsDuplicateSubmissionAndCancellation()
        {
            var store = new FakeStore { PendingSave = new(TaskCreationOptions.RunContinuationsAsynchronously) };
            var cancelled = false;
            Campaign? saved = null;
            var vm = new CreateCampaignViewModel(store, () => cancelled = true, c => saved = c) { Name = "Name", CustomSystemName = "Custom" };
            var saving = vm.SaveCommand.ExecuteAsync();
            Assert.IsTrue(vm.IsBusy);
            Assert.IsFalse(vm.IsEditable);
            Assert.IsFalse(vm.CancelCommand.CanExecute(null));
            vm.CancelCommand.Execute(null);
            await vm.SaveCommand.ExecuteAsync();
            Assert.AreEqual(1, store.Creates);
            Assert.IsFalse(cancelled);
            Assert.IsNull(saved);
            store.PendingSave.SetResult(Campaign("Name", "Custom", 24, 1));
            await saving;
            Assert.IsNotNull(saved);
            Assert.IsFalse(vm.IsBusy);
        }

        [TestMethod]
        public async Task ShellCreationAndBackRetainLibrarySortAndSelectNewCampaign()
        {
            var store = new FakeStore();
            var shell = new ShellViewModel(store);
            await shell.InitializeAsync();
            shell.Library.SortBy(CampaignSortField.Name);
            shell.Library.NewCommand.Execute(null);
            var create = (CreateCampaignViewModel)shell.CurrentView;
            create.Name = "New";
            create.CustomSystemName = "Homebrew";
            await create.SaveCommand.ExecuteAsync();
            var page = (CampaignViewModel)shell.CurrentView;
            Assert.AreEqual(page.Campaign.Id, shell.Library.SelectedCampaign!.Id);
            page.BackCommand.Execute(null);
            Assert.AreSame(shell.Library, shell.CurrentView);
            Assert.AreEqual(CampaignSortField.Name, shell.Library.SortField);
            Assert.IsFalse(shell.Library.SortDescending);
            Assert.HasCount(1, shell.Library.Campaigns);
        }

        [TestMethod]
        public async Task InvalidTextShowsInlineErrorsRetainsInputAndDoesNotSave()
        {
            var store = new FakeStore();
            var vm = new CreateCampaignViewModel(store, () => { }, _ => Assert.Fail("Must not navigate"))
            {
                Name = new string('a', 201), CustomSystemName = "Hidden\u202Etext"
            };
            await vm.SaveCommand.ExecuteAsync();
            Assert.IsNotNull(vm.NameError);
            Assert.IsNotNull(vm.CustomSystemError);
            Assert.AreEqual(201, vm.Name.Length);
            Assert.AreEqual("Hidden\u202Etext", vm.CustomSystemName);
            Assert.AreEqual(0, store.Creates);
            vm.Name = "Valid";
            vm.CustomSystemName = "Homebrew";
            Assert.IsNull(vm.NameError);
            Assert.IsNull(vm.CustomSystemError);
        }
        [TestMethod]
        public void CancellingDiscardsFormAndDoesNotSave()
        {
            var store = new FakeStore();
            var shell = new ShellViewModel(store);
            shell.Library.NewCommand.Execute(null);
            var first = (CreateCampaignViewModel)shell.CurrentView;
            first.Name = "Unsaved";
            first.CancelCommand.Execute(null);
            Assert.AreSame(shell.Library, shell.CurrentView);
            shell.Library.NewCommand.Execute(null);
            Assert.AreEqual("", ((CreateCampaignViewModel)shell.CurrentView).Name);
            Assert.AreEqual(0, store.Creates);
        }
    }
}
