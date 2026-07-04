using AtomUI.Desktop.Controls;

namespace OutlineUi.Views;

public partial class UploadListDialog : Window
{
    public UploadListDialog()
    {
        InitializeComponent();
    }

    public void SelectAll()
    {
        if (UploadDataGrid.ItemsSource != null)
        {
            foreach (var item in UploadDataGrid.ItemsSource)
            {
                UploadDataGrid.SelectedItems.Add(item);
            }
        }
    }

    public void DeselectAll()
    {
        UploadDataGrid.SelectedItems.Clear();
    }
}
