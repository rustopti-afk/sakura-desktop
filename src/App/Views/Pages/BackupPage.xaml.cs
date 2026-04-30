using Sakura.App.ViewModels;
using System.Windows.Controls;

namespace Sakura.App.Views.Pages;

public partial class BackupPage : Page
{
    public BackupPage(BackupViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}
