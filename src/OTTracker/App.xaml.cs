using Microsoft.Maui.Controls;
using OTTracker.Domain.Entities;
using OTTracker.Domain.Interfaces;
using OTTracker.Views;

namespace OTTracker;

public partial class App : Application
{
    private readonly IServiceProvider _services;
    private readonly ISettingsService _settings;
    private readonly IAuthService _auth;
    private readonly ISupabaseClientProvider _clientProvider;
    private readonly ISupabaseSessionService _sessionService;

    public App(
        IServiceProvider services,
        ISettingsService settings,
        IAuthService auth,
        ISupabaseClientProvider clientProvider,
        ISupabaseSessionService sessionService)
    {
        _services = services;
        _settings = settings;
        _auth = auth;
        _clientProvider = clientProvider;
        _sessionService = sessionService;
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
        var sessionRestored = await RestoreSessionAsync();
        if (!sessionRestored)
        {
            MainPage = _services.GetRequiredService<LoginPage>();
            return;
        }

        AppSettings settings;
        try
        {
            settings = await _settings.GetAsync();
        }
        catch
        {
            MainPage = _services.GetRequiredService<LoginPage>();
            return;
        }

        if (settings.PinLockEnabled && await _auth.HasPinAsync())
        {
            MainPage = _services.GetRequiredService<PinPage>();
            return;
        }

        MainPage = new AppShell();
    }

    private async Task<bool> RestoreSessionAsync()
    {
        try
        {
            var savedSession = await _sessionService.LoadSessionAsync();
            if (savedSession is null)
            {
                return false;
            }

            var supabase = _clientProvider.Client;
            await supabase.Auth.SetSession(savedSession.Value.AccessToken, savedSession.Value.RefreshToken);
            return supabase.Auth.CurrentUser is not null;
        }
        catch
        {
            try
            {
                await _sessionService.ClearSessionAsync();
            }
            catch
            {
                // Ignore cleanup errors during startup.
            }

            return false;
        }
    }
}
