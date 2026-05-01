using Sakura.App.ViewModels;
using System.Windows.Controls;

namespace Sakura.App.Views.Pages;

public partial class IconsPage : UserControl
{
    public IconsPage(IconsViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}
