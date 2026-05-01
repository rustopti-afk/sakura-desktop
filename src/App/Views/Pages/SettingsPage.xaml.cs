using Sakura.App.ViewModels;
using System.Windows.Controls;

namespace Sakura.App.Views.Pages;

public partial class SettingsPage : UserControl
{
    public SettingsPage(SettingsViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}
