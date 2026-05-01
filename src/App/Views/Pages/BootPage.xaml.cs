using Sakura.App.ViewModels;
using System.Windows.Controls;

namespace Sakura.App.Views.Pages;

public partial class BootPage : UserControl
{
    public BootPage(BootViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}
