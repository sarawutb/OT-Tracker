using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using OTTracker_Avalonia.Domain.Entities;
using OTTracker_Avalonia.AppServices.Interfaces.Repositories;
using OTTracker_Avalonia.AppServices.Interfaces.Services;
using OTTracker_Avalonia.AppServices.Services;
using OTTracker_Avalonia.Infrastructure.Repositories;
using OTTracker_Avalonia.Infrastructure.Services;
using OTTracker_Avalonia.ViewModels;
using OTTracker_Avalonia.Views;

namespace OTTracker_Avalonia;

public partial class App : Avalonia.Application
{
    public static IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);
        Services = serviceCollection.BuildServiceProvider();

        var mainWindowViewModel = Services.GetRequiredService<MainWindowViewModel>();

        // Navigation initialization based on auth and lock settings
        var settingsService = Services.GetRequiredService<ISettingsService>();
        var authService = Services.GetRequiredService<IAuthService>();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainWindowViewModel
            };

            // Start up checking (runs in background thread or async task safely)
            _ = Task.Run(async () =>
            {
                var clientProvider = Services.GetRequiredService<ISupabaseClientProvider>();
                var supabase = clientProvider.Client;
                var sessionService = Services.GetRequiredService<ISupabaseSessionService>();

                var sessionRestored = false;
                try
                {
                    var savedSession = await sessionService.LoadSessionAsync();
                    if (savedSession != null)
                    {
                        await supabase.Auth.SetSession(savedSession.Value.AccessToken, savedSession.Value.RefreshToken);
                        sessionRestored = supabase.Auth.CurrentUser != null;
                    }
                }
                catch
                {
                    try
                    {
                        await sessionService.ClearSessionAsync();
                    }
                    catch
                    {
                        // Ignore clear errors
                    }
                }

                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    if (!sessionRestored)
                    {
                        var loginViewModel = Services.GetRequiredService<LoginViewModel>();
                        mainWindowViewModel.NavigateTo(loginViewModel);
                    }
                    else
                    {
                        AppSettings settings;
                        try
                        {
                            settings = await settingsService.GetAsync();
                        }
                        catch
                        {
                            settings = new AppSettings();
                        }
                        var hasPin = await authService.HasPinAsync();

                        if (settings.PinLockEnabled && hasPin)
                        {
                            var pinViewModel = Services.GetRequiredService<PinViewModel>();
                            pinViewModel.Unlocked = async () =>
                            {
                                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                                {
                                    var dashboard = Services.GetRequiredService<DashboardViewModel>();
                                    mainWindowViewModel.NavigateTo(dashboard);
                                    return Task.CompletedTask;
                                });
                            };
                            mainWindowViewModel.NavigateTo(pinViewModel);
                        }
                        else
                        {
                            var dashboard = Services.GetRequiredService<DashboardViewModel>();
                            mainWindowViewModel.NavigateTo(dashboard);
                        }
                    }
                });
            });
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Supabase Client & Config Services
        services.AddSingleton<ISupabaseConfigService, SupabaseConfigService>();
        services.AddSingleton<ISupabaseSessionService, SupabaseSessionService>();
        services.AddSingleton<ISupabaseClientProvider, SupabaseClientProvider>();
        services.AddTransient(provider => provider.GetRequiredService<ISupabaseClientProvider>().Client);

        // Core Services
        services.AddSingleton<AppEvents>();
        services.AddSingleton<IOtCalculationService, OtCalculationService>();
        services.AddSingleton<IOtEntryRepository, OtEntryRepository>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<ICsvExportService, CsvExportService>();

        // ViewModels
        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<PinViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<LogEntryViewModel>();
        services.AddTransient<HistoryViewModel>();
        services.AddTransient<SettingsViewModel>();
    }
}