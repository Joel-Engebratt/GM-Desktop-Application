using GM_Desktop_Application.Models;
using GM_Desktop_Application.ViewModels;

namespace GM.Desktop.Tests.ViewModels
{
    [TestClass]
    public sealed class CreateCampaignViewModelTests
    {
        private FakeCampaignStore store = null!;
        private CreateCampaignViewModel form = null!;
        private Campaign? saved;
        private bool cancelled;

        [TestInitialize]
        public void Initialize()
        {
            store = new FakeCampaignStore();
            form = new CreateCampaignViewModel(store, () => cancelled = true, campaign => saved = campaign);
        }

        private void FillValidForm()
        {
            form.Name = " Name ";
            form.CustomSystemName = " System ";
        }

        private async Task ArrangeSaveFailure()
        {
            FillValidForm();
            store.SaveError = new UnauthorizedAccessException();
            await form.SaveCommand.ExecuteAsync();
        }

        [TestMethod]
        public void CustomSystemIsSelectedInitially() => Assert.AreEqual("custom", form.SelectedSystem!.Id);

        [TestMethod]
        public async Task BlankFieldsReportInlineErrorsWithoutSaving()
        {
            await form.SaveCommand.ExecuteAsync();

            Assert.IsNotNull(form.NameError);
            Assert.IsNotNull(form.CustomSystemError);
            Assert.AreEqual(0, store.Creates);
            Assert.IsNull(saved);
        }

        [TestMethod]
        public async Task MissingSystemReportsInlineErrorWithoutSaving()
        {
            FillValidForm();
            form.SelectedSystem = null;

            await form.SaveCommand.ExecuteAsync();

            Assert.IsNotNull(form.SystemError);
            Assert.AreEqual(0, store.Creates);
            Assert.IsNull(saved);
        }

        [TestMethod]
        public async Task SaveFailureRetainsEditableEntries()
        {
            await ArrangeSaveFailure();

            Assert.IsNull(saved);
            Assert.AreEqual(" Name ", form.Name);
            Assert.AreEqual(" System ", form.CustomSystemName);
            StringAssert.Contains(form.Error!, "writable location");
            Assert.IsTrue(form.SaveCommand.CanExecute(null));
        }

        [TestMethod]
        public async Task RetrySavesAfterFailureIsRemoved()
        {
            await ArrangeSaveFailure();
            store.SaveError = null;

            await form.SaveCommand.ExecuteAsync();

            Assert.IsNotNull(saved);
            Assert.AreEqual("Name", saved.Name);
            Assert.AreEqual("System", saved.CustomSystemName);
            Assert.IsNull(form.Error);
        }

        private Task StartPendingSave()
        {
            FillValidForm();
            store.PendingSave = new(TaskCreationOptions.RunContinuationsAsynchronously);
            return form.SaveCommand.ExecuteAsync();
        }

        private async Task FinishPendingSave(Task saving)
        {
            store.PendingSave!.SetResult(CampaignTestData.Campaign());
            await saving;
        }

        [TestMethod]
        public async Task PendingSaveDisablesEditing()
        {
            var saving = StartPendingSave();
            try
            {
                Assert.IsTrue(form.IsBusy);
                Assert.IsFalse(form.IsEditable);
            }
            finally { await FinishPendingSave(saving); }
        }

        [TestMethod]
        public async Task PendingSaveIgnoresDuplicateSubmission()
        {
            var saving = StartPendingSave();
            try
            {
                await form.SaveCommand.ExecuteAsync();

                Assert.AreEqual(1, store.Creates);
            }
            finally { await FinishPendingSave(saving); }
        }

        [TestMethod]
        public async Task PendingSavePreventsCancellation()
        {
            var saving = StartPendingSave();
            try
            {
                form.CancelCommand.Execute(null);

                Assert.IsFalse(form.CancelCommand.CanExecute(null));
                Assert.IsFalse(cancelled);
            }
            finally { await FinishPendingSave(saving); }
        }

        [TestMethod]
        public async Task SaveNotifiesOnlyAfterStorageCompletes()
        {
            var saving = StartPendingSave();
            try { Assert.IsNull(saved); }
            finally { await FinishPendingSave(saving); }

            Assert.IsNotNull(saved);
            Assert.IsFalse(form.IsBusy);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task InvalidTextIsRetainedWithoutSaving(bool systemName)
        {
            FillValidForm();
            var invalid = systemName ? "Hidden\u202Etext" : new string('a', 201);
            if (systemName) form.CustomSystemName = invalid; else form.Name = invalid;

            await form.SaveCommand.ExecuteAsync();

            Assert.IsNotNull(systemName ? form.CustomSystemError : form.NameError);
            Assert.AreEqual(invalid, systemName ? form.CustomSystemName : form.Name);
            Assert.AreEqual(0, store.Creates);
            Assert.IsNull(saved);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task CorrectingTextClearsInlineError(bool systemName)
        {
            FillValidForm();
            if (systemName) form.CustomSystemName = "Hidden\u202Etext"; else form.Name = new string('a', 201);
            await form.SaveCommand.ExecuteAsync();

            if (systemName) form.CustomSystemName = "Homebrew"; else form.Name = "Valid";

            Assert.IsNull(systemName ? form.CustomSystemError : form.NameError);
        }
    }
}
