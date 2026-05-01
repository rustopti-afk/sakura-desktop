using Sakura.App.ViewModels;
using System.Windows.Controls;

namespace Sakura.App.Views.Pages;

public partial class ProfileEditorPage : UserControl
{
    public ProfileEditorPage(ProfileEditorViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}
