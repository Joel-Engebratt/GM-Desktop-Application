using System.Windows.Controls;
using GM_Desktop_Application.Models;
using GM_Desktop_Application.ViewModels;

namespace GM.Desktop.Tests.Views
{
    [TestClass]
    [DoNotParallelize]
    public sealed class WpfRenderingTests
    {
        private static WpfTestDispatcher dispatcher = null!;

        [ClassInitialize]
        public static void Initialize(TestContext _) => dispatcher = new WpfTestDispatcher();

        [ClassCleanup]
        public static void Cleanup() => dispatcher?.Dispose();

        [TestMethod]
        [DataRow("empty-library", false)]
        [DataRow("campaign-library", false)]
        [DataRow("new-campaign", false)]
        [DataRow("validation", false)]
        [DataRow("new-campaign", true)]
        [DataRow("campaign-library", true)]
        [DataRow("campaign-page", true)]
        public void ViewRendersWithoutBindingErrors(string state, bool minimumSize)
        {
            dispatcher.Run(() =>
            {
                using var fixture = new WpfRenderFixture();
                fixture.Show(state);

                fixture.Render(state + (minimumSize ? "-minimum-size" : ""), minimumSize);

                Assert.IsEmpty(fixture.BindingErrors, string.Join(Environment.NewLine, fixture.BindingErrors));
            });
        }

        [TestMethod]
        public void SortingKeepsRealTableSelection()
        {
            dispatcher.Run(() =>
            {
                using var fixture = new WpfRenderFixture();
                fixture.PopulateLibrary();
                fixture.Render("selection-before-sort");
                var selection = fixture.Shell.Library.SelectedCampaign;

                fixture.Shell.Library.SortBy(CampaignSortField.Name);
                fixture.Render("sorted-library");

                Assert.AreSame(selection, fixture.Shell.Library.SelectedCampaign);
                Assert.AreSame(selection, fixture.Find<DataGrid>()!.SelectedItem);
            });
        }

        [TestMethod]
        public void LibraryTableDisplaysAllCampaigns()
        {
            dispatcher.Run(() =>
            {
                using var fixture = new WpfRenderFixture();
                fixture.PopulateLibrary();

                fixture.Render("table-items");

                Assert.AreEqual(3, fixture.Find<DataGrid>()!.Items.Count);
            });
        }

        [TestMethod]
        public void CreationViewDisplaysDefaultCustomSystem()
        {
            dispatcher.Run(() =>
            {
                using var fixture = new WpfRenderFixture();
                fixture.Show("new-campaign");

                fixture.Render("default-system");

                Assert.AreEqual("Other / custom", fixture.Find<ComboBox>()!.Text);
            });
        }

        [TestMethod]
        [DataRow("CampaignName", CampaignText.NameMaxLength)]
        [DataRow("SystemName", CampaignText.SystemNameMaxLength)]
        public void TextFieldUsesSharedLengthLimit(string field, int expectedLimit)
        {
            dispatcher.Run(() =>
            {
                using var fixture = new WpfRenderFixture();
                fixture.Show("new-campaign");

                fixture.Render("field-limit-" + field);

                var textBox = (TextBox)fixture.Find<UserControl>()!.FindName(field);
                Assert.AreEqual(expectedLimit, textBox.MaxLength);
            });
        }
    }
}
