using Sakura.App.Services;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shell;

namespace Sakura.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        StateChanged += OnStateChanged;
    }

    private void OnStateChanged(object? sender, EventArgs e)
    {
        var chrome = WindowChrome.GetWindowChrome(this);
        if (WindowState == WindowState.Maximized)
        {
            chrome.ResizeBorderThickness = new Thickness(0);
            BorderThickness = new Thickness(0);
        }
        else
        {
            chrome.ResizeBorderThickness = new Thickness(8);
            BorderThickness = new Thickness(0);
        }
    }

    private void OnMinimize(object sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void OnMaximizeRestore(object sender, RoutedEventArgs e)
        => WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;

    private void OnClose(object sender, RoutedEventArgs e)
        => Close();

    private void OnDismissToast(object sender, RoutedEventArgs e)
        => ToastService.Instance.Dismiss();

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (e.ButtonState == MouseButtonState.Pressed
            && e.GetPosition(this).Y < 48
            && WindowState != WindowState.Maximized)
        {
            DragMove();
        }
    }
}
