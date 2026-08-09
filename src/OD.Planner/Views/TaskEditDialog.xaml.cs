using System.Windows;
using OD.Planner.Models;
using OD.Planner.ViewModels;

namespace OD.Planner.Views;

public partial class TaskEditDialog : Window
{
    private TaskEditViewModel? Vm => DataContext as TaskEditViewModel;

    public TaskEditDialog()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (Vm is null)
        {
            return;
        }

        switch (Vm.DeadlineType)
        {
            case DeadlineType.None:
                RbNone.IsChecked = true;
                break;
            case DeadlineType.DaysFromCreation:
                RbDays.IsChecked = true;
                break;
            case DeadlineType.FixedDate:
                RbDate.IsChecked = true;
                break;
        }

        UpdatePanels();
    }

    private void Rb_Checked(object sender, RoutedEventArgs e)
    {
        if (Vm is null)
        {
            return;
        }

        if ((sender as FrameworkElement)?.Tag is string tag && int.TryParse(tag, out var value))
        {
            Vm.DeadlineType = (DeadlineType)value;
        }

        UpdatePanels();
    }

    private void UpdatePanels()
    {
        if (Vm is null)
        {
            return;
        }

        DaysPanel.Visibility = Vm.DeadlineType == DeadlineType.DaysFromCreation ? Visibility.Visible : Visibility.Collapsed;
        DatePanel.Visibility = Vm.DeadlineType == DeadlineType.FixedDate ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (Vm is not null && Vm.Save())
        {
            DialogResult = true;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void CloseWindow_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
