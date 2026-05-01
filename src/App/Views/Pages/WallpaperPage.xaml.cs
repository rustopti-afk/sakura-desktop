using Sakura.App.ViewModels;
using System.Windows.Controls;

namespace Sakura.App.Views.Pages;

public partial class WallpaperPage : UserControl
{
    public WallpaperPage(WallpaperViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}
