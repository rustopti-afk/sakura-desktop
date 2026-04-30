using Sakura.App.ViewModels;
using System.Windows.Controls;

namespace Sakura.App.Views.Pages;

public partial class IntegrationsPage : Page
{
    public IntegrationsPage(IntegrationsViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}
