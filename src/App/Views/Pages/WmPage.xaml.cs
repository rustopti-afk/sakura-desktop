using Sakura.App.ViewModels;
using System.Windows.Controls;

namespace Sakura.App.Views.Pages;

public partial class WmPage : UserControl
{
    public WmPage(WmViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}
