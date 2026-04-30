using Sakura.App.Services;

namespace Sakura.App.Tests;

public sealed class ToastServiceTests
{
    [Fact]
    public void Instance_IsSingleton()
    {
        ToastService.Instance.Should().BeSameAs(ToastService.Instance);
    }

    [Fact]
    public void Show_SetsMessageAndIsVisible()
    {
        ToastService.Instance.Show("Hello", ToastKind.Info, durationMs: 60_000);
        ToastService.Instance.Message.Should().Be("Hello");
        ToastService.Instance.IsVisible.Should().BeTrue();
        ToastService.Instance.Dismiss();
    }

    [Fact]
    public void Show_SetsKind()
    {
        ToastService.Instance.Show("msg", ToastKind.Error, durationMs: 60_000);
        ToastService.Instance.Kind.Should().Be(ToastKind.Error);
        ToastService.Instance.Dismiss();
    }

    [Fact]
    public void Dismiss_SetsIsVisibleFalse()
    {
        ToastService.Instance.Show("msg", ToastKind.Success, durationMs: 60_000);
        ToastService.Instance.Dismiss();
        ToastService.Instance.IsVisible.Should().BeFalse();
    }

    [Fact]
    public void Show_OverwritesPreviousMessage()
    {
        ToastService.Instance.Show("First",  ToastKind.Info,    durationMs: 60_000);
        ToastService.Instance.Show("Second", ToastKind.Success, durationMs: 60_000);
        ToastService.Instance.Message.Should().Be("Second");
        ToastService.Instance.Kind.Should().Be(ToastKind.Success);
        ToastService.Instance.Dismiss();
    }

    [Fact]
    public void Show_FiresPropertyChangedForIsVisible()
    {
        ToastService.Instance.Dismiss();
        bool fired = false;
        ToastService.Instance.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ToastService.Instance.IsVisible))
                fired = true;
        };

        ToastService.Instance.Show("test", durationMs: 60_000);
        fired.Should().BeTrue();
        ToastService.Instance.Dismiss();
    }
}
