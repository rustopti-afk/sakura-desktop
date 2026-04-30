using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Threading;

namespace Sakura.App.Services;

public enum ToastKind { Info, Success, Warning, Error }

public sealed partial class ToastService : ObservableObject
{
    public static ToastService Instance { get; } = new();

    [ObservableProperty] private string    _message    = "";
    [ObservableProperty] private bool      _isVisible  = false;
    [ObservableProperty] private ToastKind _kind       = ToastKind.Info;

    private DispatcherTimer? _timer;

    private ToastService() { }

    public void Show(string message, ToastKind kind = ToastKind.Success, int durationMs = 3000)
    {
        Message   = message;
        Kind      = kind;
        IsVisible = true;

        _timer?.Stop();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(durationMs) };
        _timer.Tick += (_, _) =>
        {
            _timer.Stop();
            IsVisible = false;
        };
        _timer.Start();
    }

    public void Dismiss()
    {
        _timer?.Stop();
        IsVisible = false;
    }
}
