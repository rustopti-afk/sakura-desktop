using Sakura.App.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Sakura.App.Views.Pages;

public partial class ProfilesPage : Page
{
    public ProfilesPage(ProfilesViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] files
            && DataContext is ProfilesViewModel vm)
        {
            vm.LoadFilesFromPaths(files);
        }
        e.Handled = true;
    }
}
