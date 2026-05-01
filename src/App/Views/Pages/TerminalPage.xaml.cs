using Sakura.App.ViewModels;
using System.Windows.Controls;

namespace Sakura.App.Views.Pages;

public partial class TerminalPage : UserControl
{
    public TerminalPage(TerminalViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}
