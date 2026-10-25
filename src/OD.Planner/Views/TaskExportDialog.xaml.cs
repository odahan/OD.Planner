using System.Windows;

namespace OD.Planner.Views;

/// <summary>
/// Displays the task export mode selection.
/// </summary>
public partial class TaskExportDialog : Window
{
    /// <summary>
    /// Initializes the export dialog.
    /// </summary>
    public TaskExportDialog()
    {
        InitializeComponent();
    }

    private void Export_Click(object sender, RoutedEventArgs e) => DialogResult = true;

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
