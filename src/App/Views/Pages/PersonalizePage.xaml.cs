using Sakura.App.ViewModels;
using System.Windows.Controls;

namespace Sakura.App.Views.Pages;

public partial class PersonalizePage : UserControl
{
    public PersonalizePage(PersonalizeViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}
