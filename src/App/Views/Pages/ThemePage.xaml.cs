using Sakura.App.ViewModels;
using System.Windows.Controls;

namespace Sakura.App.Views.Pages;

public partial class ThemePage : UserControl
{
    public ThemePage(ThemeViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}
