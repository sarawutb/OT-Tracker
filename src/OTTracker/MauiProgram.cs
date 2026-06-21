using Android.Graphics.Drawables;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;
using OTTracker.Controls;
using OTTracker.Data;
using OTTracker.Services;
using OTTracker.ViewModels;
using OTTracker.Views;

namespace OTTracker;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

#if ANDROID
        EntryHandler.Mapper.AppendToMapping("HideUnderline", (handler, view) =>
        {
            handler.PlatformView.BackgroundTintList =
                Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
        });

        DatePickerHandler.Mapper.AppendToMapping("HideUnderline", (handler, view) =>
        {
            handler.PlatformView.BackgroundTintList =
                Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
        });

        TimePickerHandler.Mapper.AppendToMapping("HideUnderline", (handler, view) =>
        {
            handler.PlatformView.BackgroundTintList =
                Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
        });

        DatePickerHandler.Mapper.AppendToMapping("NoUnderline", (handler, view) =>
        {
            handler.PlatformView.Background = new ColorDrawable(Android.Graphics.Color.Transparent);
        });

        DatePickerHandler.Mapper.AppendToMapping(nameof(BorderlessDatePicker), (handler, view) =>
        {
            if (view is BorderlessDatePicker)
            {
                handler.PlatformView.Background = null;
            }
        });
#endif

        builder
            .UseMauiApp<App>()
#if DEBUG
            .EnableHotReload()
#endif
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
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
        return builder.Build();
    }
}
