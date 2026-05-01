using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace Sakura.App.Views.Controls;

public partial class NavigationControl : UserControl
{
    public static readonly DependencyProperty ItemsProperty =
        DependencyProperty.Register(nameof(Items), typeof(IEnumerable), typeof(NavigationControl));

    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(NavigationControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public IEnumerable? Items
    {
        get => (IEnumerable?)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public NavigationControl() => InitializeComponent();
}

public sealed class NavItem
{
    public string Key         { get; set; } = string.Empty;
    public string Label       { get; set; } = string.Empty;
    public string IconGlyph   { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
