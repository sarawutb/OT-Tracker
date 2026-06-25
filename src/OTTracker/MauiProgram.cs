using System.Globalization;
using CommunityToolkit.Maui;
using DotNet.Meteor.HotReload.Plugin;
using MauiIcons.FontAwesome;
using Microsoft.Extensions.Logging;
using OTTracker.Domain.Interfaces;
using OTTracker.Infrastructure.Repositories;
using OTTracker.Infrastructure.Services;
using OTTracker.ViewModels;
using OTTracker.Views;
using UraniumUI;
using MauiCsvExportService = OTTracker.Services.CsvExportService;

namespace OTTracker;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseUraniumUI()
            .UseUraniumUIMaterial()
            .UseFontAwesomeMauiIcons()
#if DEBUG
            .EnableHotReload()
#endif
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddMaterialIconFonts();
                fonts.AddMaterialSymbolsFonts();
            })
            .UseMauiCommunityToolkit();

        builder.Services.AddSingleton<ISupabaseConfigService, SupabaseConfigService>();
        builder.Services.AddSingleton<ISupabaseSessionService, SupabaseSessionService>();
        builder.Services.AddSingleton<ISupabaseClientProvider, SupabaseClientProvider>();
        builder.Services.AddTransient(provider => provider.GetRequiredService<ISupabaseClientProvider>().Client);

        builder.Services.AddSingleton<AppEvents>();
        builder.Services.AddSingleton<IOtCalculationService, OtCalculationService>();
        builder.Services.AddSingleton<IOtEntryRepository, OtEntryRepository>();
        builder.Services.AddSingleton<ISettingsService, SettingsService>();
        builder.Services.AddSingleton<IAuthService, AuthService>();
        builder.Services.AddSingleton<Services.IBiometricService, Services.BiometricService>();
        builder.Services.AddSingleton<ICsvExportService, MauiCsvExportService>();

        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<PinViewModel>();
        builder.Services.AddScoped<DashboardViewModel>();
        builder.Services.AddTransient<LogEntryViewModel>();
        builder.Services.AddTransient<HistoryViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();

        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<PinPage>();
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<LogEntryPage>();
        builder.Services.AddTransient<HistoryPage>();
        builder.Services.AddTransient<SettingsPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif
        var culture = CultureInfo.CurrentCulture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        return builder.Build();
    }
}
