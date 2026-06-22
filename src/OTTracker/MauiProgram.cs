using System.Globalization;
using Android.Content.Res;
using Android.Graphics.Drawables;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;
using OTTracker.Controls;
using OTTracker.Data;
using OTTracker.Services;
using OTTracker.ViewModels;
using OTTracker.Views;
using UraniumUI;

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

#if DEBUG
            //.EnableHotReload()
#endif
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddMaterialIconFonts();
                fonts.AddMaterialSymbolsFonts();
            })
            .UseMauiCommunityToolkit();

        builder.Services.AddSingleton<AppDatabase>();
        builder.Services.AddSingleton<AppEvents>();
        builder.Services.AddSingleton<IOtCalculationService, OtCalculationService>();
        builder.Services.AddSingleton<IOtEntryRepository, OtEntryRepository>();
        builder.Services.AddSingleton<ISettingsService, SettingsService>();
        builder.Services.AddSingleton<IAuthService, AuthService>();
        builder.Services.AddSingleton<IBiometricService, BiometricService>();
        builder.Services.AddSingleton<ICsvExportService, CsvExportService>();

        builder.Services.AddTransient<PinViewModel>();
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<LogEntryViewModel>();
        builder.Services.AddTransient<HistoryViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();

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

        System.Diagnostics.Debug.WriteLine(
            $"CurrentCulture: {CultureInfo.CurrentCulture.Name}");

        System.Diagnostics.Debug.WriteLine(
            $"CurrentUICulture: {CultureInfo.CurrentUICulture.Name}");

        return builder.Build();
    }
}
