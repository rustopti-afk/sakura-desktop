using Sakura.App.Services;
using Sakura.App.ViewModels;
using System.ComponentModel;
using System.Media;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Shell;

namespace Sakura.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        StateChanged      += OnStateChanged;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is MainViewModel old)
            old.PropertyChanged -= OnVmPropertyChanged;
        if (e.NewValue is MainViewModel vm)
            vm.PropertyChanged += OnVmPropertyChanged;
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MainViewModel.CurrentPage)) return;

        // Fade-in animation when switching pages
        PageContent.Opacity = 0;
        var anim = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(180)))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        PageContent.BeginAnimation(OpacityProperty, anim);

        // Soft navigation click sound (respects Windows sound scheme)
        try { SystemSounds.Asterisk.Play(); } catch { }
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
