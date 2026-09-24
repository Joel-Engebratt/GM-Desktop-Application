using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GM_Desktop_Application.ViewModels;

namespace GM_Desktop_Application.Views
{
    public partial class LibraryView : UserControl
    {
        public LibraryView() => InitializeComponent();
        private void OnLoaded(object sender, RoutedEventArgs e) => UpdateSortIndicator();
        private void OnSorting(object sender, DataGridSortingEventArgs e)
        {
            e.Handled = true;
            if (DataContext is LibraryViewModel model && Enum.TryParse<CampaignSortField>(e.Column.SortMemberPath, out var field))
            {
                model.SortBy(field);
                UpdateSortIndicator();
            }
        }
        private void UpdateSortIndicator()
        {
            if (DataContext is not LibraryViewModel model) return;
            foreach (var column in CampaignTable.Columns)
            {
                column.SortDirection = column.SortMemberPath == model.SortField.ToString()
                    ? (model.SortDescending ? ListSortDirection.Descending : ListSortDirection.Ascending) : null;
            }
        }
        private void OnDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is DependencyObject source && ItemsControl.ContainerFromElement(CampaignTable, source) is DataGridRow) Open();
        }
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && e.OriginalSource is DependencyObject source && ItemsControl.ContainerFromElement(CampaignTable, source) is DataGridRow)
            {
                Open();
                e.Handled = true;
            }
        }
        private void Open()
        {
            if (DataContext is LibraryViewModel model) model.OpenCommand.Execute(null);
        }
    }
}
