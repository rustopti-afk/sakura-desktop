using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sakura.App.ViewModels;
using Sakura.App.Views;
using Sakura.App.Views.Pages;
using Sakura.Core.Backup;
using Sakura.Core.Native;
using Sakura.Core.Profile;
using Sakura.Core.Theme;
using System.Windows;

namespace Sakura.App;

public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = Host.CreateDefaultBuilder(e.Args)
            .ConfigureLogging(logging => logging.AddDebug())
            .ConfigureServices(services =>
            {
                // Core services
                services.AddSingleton<ThemeEngine>();
                services.AddSingleton<ProfileApplicator>();

                // ViewModels
                services.AddSingleton<MainViewModel>();
                services.AddSingleton<PersonalizeViewModel>();
                services.AddSingleton<ProfilesViewModel>();
                services.AddSingleton<BackupViewModel>();
                services.AddSingleton<IntegrationsViewModel>();
                services.AddSingleton<TerminalViewModel>();
                services.AddSingleton<SettingsViewModel>();
                services.AddSingleton<ProfileEditorViewModel>();

                // Views
                services.AddSingleton<MainWindow>();
                services.AddTransient<PersonalizePage>();
                services.AddTransient<ProfilesPage>();
                services.AddTransient<BackupPage>();
                services.AddTransient<IntegrationsPage>();
                services.AddTransient<TerminalPage>();
                services.AddTransient<SettingsPage>();
                services.AddTransient<ProfileEditorPage>();
                services.AddSingleton<ComingSoonPage>();
            })
            .Build();

        await _host.StartAsync();

        var mainVm = _host.Services.GetRequiredService<MainViewModel>();

        // Wire preview: ProfilesViewModel notifies MainViewModel when selection changes
        _host.Services.GetRequiredService<ProfilesViewModel>().SetMainViewModel(mainVm);

        var window = _host.Services.GetRequiredService<MainWindow>();
        window.DataContext = mainVm;
        window.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(5));
            _host.Dispose();
        }
        base.OnExit(e);
    }
}
