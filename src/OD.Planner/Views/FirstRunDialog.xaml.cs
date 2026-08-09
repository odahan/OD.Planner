using System.IO;
using System.Windows;
using Microsoft.Win32;
using OD.Planner.ViewModels;

namespace OD.Planner.Views;

public partial class FirstRunDialog : Window
{
    public FirstRunDialog()
    {
        InitializeComponent();
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not FirstRunViewModel vm)
        {
            return;
        }

        var dialog = new OpenFolderDialog
        {
            Title = "Choisir le dossier de la base de données",
        };

        var currentDir = Path.GetDirectoryName(vm.DbPath);
        if (Directory.Exists(currentDir))
        {
            dialog.InitialDirectory = currentDir;
        }

        if (dialog.ShowDialog(this) == true)
        {
            vm.DbPath = Path.Combine(dialog.FolderName, "tasks.db");
        }
    }

    private void Create_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is FirstRunViewModel vm && vm.Validate())
        {
            DialogResult = true;
        }
    }

    private void CloseWindow_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
