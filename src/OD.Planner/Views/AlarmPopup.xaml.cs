using System.Windows;
using System.Windows.Input;

namespace OD.Planner.Views;

public partial class AlarmPopup : Window
{
    public AlarmPopup()
    {
        InitializeComponent();
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        try
        {
            DragMove();
        }
        catch (InvalidOperationException)
        {
            // DragMove throws when the mouse button is released before capture completes.
        }
    }
}
