using Microsoft.Maui.Controls;
using OTTracker.Services;
using OTTracker.Views;

namespace OTTracker;

public partial class App : Application
{
    private readonly IServiceProvider _services;
    private readonly ISettingsService _settings;
    private readonly IAuthService _auth;

    public App(IServiceProvider services, ISettingsService settings, IAuthService auth)
    {
        _services = services;
        _settings = settings;
        _auth = auth;
        InitializeComponent();
        MainPage = new ContentPage
        {
            BackgroundColor = Color.FromArgb("#F7F6FB"),
            Content = new ActivityIndicator
            {
                IsRunning = true,
                Color = Color.FromArgb("#5B4FE8"),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            }
        };
        _ = InitializeAsync();
    }

    public static Task ShowMainAsync()
    {
        Current!.MainPage = new AppShell();
        return Task.CompletedTask;
    }

    private async Task InitializeAsync()
    {
        var settings = await _settings.GetAsync();
        if (settings.PinLockEnabled && await _auth.HasPinAsync())
        {
            MainPage = _services.GetRequiredService<PinPage>();
            return;
        }

        MainPage = new AppShell();
    }
}
