using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using OD.Planner.Data;
using OD.Planner.Models;
using OD.Planner.Services;
using OD.Planner.ViewModels;
using OD.Planner.Views;

namespace OD.Planner;

public partial class MainWindow : Window
{
    private const int WM_NCHITTEST = 0x84;
    private const int WM_GETMINMAXINFO = 0x24;

    private const int HTCLIENT = 1;
    private const int HTCAPTION = 2;
    private const int HTLEFT = 10;
    private const int HTRIGHT = 11;
    private const int HTTOP = 12;
    private const int HTTOPLEFT = 13;
    private const int HTTOPRIGHT = 14;
    private const int HTBOTTOM = 15;
    private const int HTBOTTOMLEFT = 16;
    private const int HTBOTTOMRIGHT = 17;

    private const int ResizeBorder = 8;

    private readonly MainViewModel _viewModel;
    private readonly AppSettings _settings;
    private readonly AlarmEngine _alarmEngine;
    private AlarmPopup? _alarmPopup;

    public MainWindow(MainViewModel viewModel, AppSettings settings, AlarmEngine alarmEngine)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _settings = settings;
        _alarmEngine = alarmEngine;
        DataContext = viewModel;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var handle = new WindowInteropHelper(this).Handle;
        HwndSource.FromHwnd(handle)?.AddHook(WndProc);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplySavedBounds();
        _alarmEngine.AlarmRaised += OnAlarmsRaised;
        _alarmEngine.MidnightPassed += OnMidnightPassed;
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        _alarmEngine.AlarmRaised -= OnAlarmsRaised;
        _alarmEngine.MidnightPassed -= OnMidnightPassed;

        var bounds = WindowState == WindowState.Maximized ? RestoreBounds : new Rect(Left, Top, ActualWidth, ActualHeight);
        _settings.WindowLeft = bounds.Left;
        _settings.WindowTop = bounds.Top;
        _settings.WindowWidth = bounds.Width;
        _settings.WindowHeight = bounds.Height;
        SettingsService.Save(_settings);

        // ShutdownMode is OnExplicitShutdown: the FirstRunDialog is the first window
        // shown, so WPF auto-promotes it to Application.MainWindow and closing it would
        // otherwise shut the app down (OnMainWindowClose). We exit explicitly here,
        // only when the real main window is actually being closed.
        if (!e.Cancel)
        {
            Application.Current.Shutdown();
        }
    }

    private void ApplySavedBounds()
    {
        var work = SystemParameters.WorkArea;
        if (_settings.WindowLeft is double left &&
            _settings.WindowTop is double top &&
            _settings.WindowWidth is double width &&
            _settings.WindowHeight is double height)
        {
            width = Math.Min(width, work.Width);
            height = Math.Min(height, work.Height);
            left = Math.Clamp(left, work.Left, work.Right - width);
            top = Math.Clamp(top, work.Top, work.Bottom - height);
            Left = left;
            Top = top;
            Width = width;
            Height = height;
        }
        else
        {
            // First run: dock the window at the top-right of the primary screen.
            Width = Math.Min(Width, work.Width);
            Height = Math.Min(Height, work.Height);
            Left = work.Right - Width;
            Top = work.Top;
        }
    }

    // ----- Window buttons & drag -----

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void Maximize_Click(object sender, RoutedEventArgs e) => ToggleMaximize();

    private void CloseWindow_Click(object sender, RoutedEventArgs e) => Close();

    private void ToggleMaximize() =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleMaximize();
            return;
        }

        try
        {
            DragMove();
        }
        catch (InvalidOperationException)
        {
            // DragMove throws when the mouse button is not captured; ignore.
        }
    }

    private void OnStateChanged(object? sender, EventArgs e)
    {
        MaximizeButton.Content = WindowState == WindowState.Maximized ? "\uE923" : "\uE922";
        WindowBorder.CornerRadius = WindowState == WindowState.Maximized ? new CornerRadius(0) : new CornerRadius(10);
    }

    // ----- Resize & maximized bounds -----

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
                var work = SystemParameters.WorkArea;
                var mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);
                mmi.ptMaxPosition.X = (int)work.Left;
                mmi.ptMaxPosition.Y = (int)work.Top;
                mmi.ptMaxSize.X = (int)work.Width;
                mmi.ptMaxSize.Y = (int)work.Height;
                Marshal.StructureToPtr(mmi, lParam, false);
                handled = true;
                break;
            }
        }

        return IntPtr.Zero;
    }

    private int HitTestResize(Point pt)
    {
        if (WindowState == WindowState.Maximized || pt.X < 0 || pt.Y < 0 || pt.X > ActualWidth || pt.Y > ActualHeight)
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

    // ----- Alarms -----

    private void OnAlarmsRaised(IReadOnlyList<AlarmEntry> entries)
    {
        if (_alarmPopup is not null && _alarmPopup.IsVisible)
        {
            return;
        }

        var vm = new AlarmPopupViewModel(entries, _alarmEngine);
        var popup = new AlarmPopup { DataContext = vm, Owner = this };
        vm.AllResolved += popup.Close;
        PlacePopup(popup);
        popup.Show();
        _alarmPopup = popup;
    }

    private void PlacePopup(AlarmPopup popup)
    {
        popup.WindowStartupLocation = WindowStartupLocation.Manual;
        popup.UpdateLayout();
        var work = SystemParameters.WorkArea;
        popup.Left = work.Right - popup.ActualWidth - 16;
        popup.Top = work.Bottom - popup.ActualHeight - 16;
    }

    private void OnMidnightPassed() => _viewModel.RefreshDeadlines();

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
