using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Win32;
using OD.Planner.Localization;
using OD.Planner.ViewModels;

namespace OD.Planner.Views;

/// <summary>
/// Settings dialog for configuring application preferences.
/// </summary>
public partial class SettingsDialog : Window
{
    private const int WM_NCHITTEST = 0x84;
    private const int WM_GETMINMAXINFO = 0x24;

    private const int HTCLIENT = 1;
    private const int HTLEFT = 10;
    private const int HTRIGHT = 11;
    private const int HTTOP = 12;
    private const int HTTOPLEFT = 13;
    private const int HTTOPRIGHT = 14;
    private const int HTBOTTOM = 15;
    private const int HTBOTTOMLEFT = 16;
    private const int HTBOTTOMRIGHT = 17;

    private const int ResizeBorder = 8;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsDialog"/> class.
    /// </summary>
    public SettingsDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Called when the window source is initialized.
    /// </summary>
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var handle = new WindowInteropHelper(this).Handle;
        HwndSource.FromHwnd(handle)?.AddHook(WndProc);
    }

    /// <summary>
    /// Closes the settings dialog.
    /// </summary>
    private void CloseWindow_Click(object sender, RoutedEventArgs e) => Close();

    /// <summary>
    /// Handles mouse left button down on the header for dragging.
    /// </summary>
    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        try
        {
            DragMove();
        }
        catch (InvalidOperationException)
        {
            // DragMove throws when the mouse button is not captured; ignore.
        }
    }

    /// <summary>
    /// Processes Windows messages for resize handling.
    /// </summary>
    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        switch (msg)
        {
            case WM_NCHITTEST:
            {
                var x = (short)(lParam.ToInt64() & 0xFFFF);
                var y = (short)((lParam.ToInt64() >> 16) & 0xFFFF);
                var pt = PointFromScreen(new Point(x, y));
                var result = HitTestResize(pt);
                if (result != HTCLIENT)
                {
                    handled = true;
                    return (IntPtr)result;
                }

                break;
            }

            case WM_GETMINMAXINFO:
            {
                var dpi = VisualTreeHelper.GetDpi(this);
                var mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);
                mmi.ptMinTrackSize.X = (int)(MinWidth * dpi.DpiScaleX);
                mmi.ptMinTrackSize.Y = (int)(MinHeight * dpi.DpiScaleY);
                Marshal.StructureToPtr(mmi, lParam, false);
                handled = true;
                break;
            }
        }

        return IntPtr.Zero;
    }

    /// <summary>
    /// Performs a hit test for resize borders.
    /// </summary>
    private int HitTestResize(Point pt)
    {
        if (pt.X < 0 || pt.Y < 0 || pt.X > ActualWidth || pt.Y > ActualHeight)
        {
            return HTCLIENT;
        }

        var left = pt.X < ResizeBorder;
        var right = pt.X > ActualWidth - ResizeBorder;
        var top = pt.Y < ResizeBorder;
        var bottom = pt.Y > ActualHeight - ResizeBorder;

        if (left && top)
        {
            return HTTOPLEFT;
        }

        if (right && top)
        {
            return HTTOPRIGHT;
        }

        if (left && bottom)
        {
            return HTBOTTOMLEFT;
        }

        if (right && bottom)
        {
            return HTBOTTOMRIGHT;
        }

        if (left)
        {
            return HTLEFT;
        }

        if (right)
        {
            return HTRIGHT;
        }

        if (top)
        {
            return HTTOP;
        }

        if (bottom)
        {
            return HTBOTTOM;
        }

        return HTCLIENT;
    }

    /// <summary>
    /// Opens a folder browser dialog to change the database location.
    /// </summary>
    private void ChangeDb_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not SettingsViewModel vm)
        {
            return;
        }

        var dialog = new OpenFolderDialog
        {
            Title = LocalizationService.Instance["ChooseDbFolder"],
        };

        var currentDir = Path.GetDirectoryName(vm.DbPath);
        if (Directory.Exists(currentDir))
        {
            dialog.InitialDirectory = currentDir;
        }

        if (dialog.ShowDialog(this) == true)
        {
            vm.ChangeDatabase(Path.Combine(dialog.FolderName, "tasks.db"));
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MINMAXINFO
    {
        public POINT ptReserved;
        public POINT ptMaxSize;
        public POINT ptMaxPosition;
        public POINT ptMinTrackSize;
        public POINT ptMaxTrackSize;
    }
}
