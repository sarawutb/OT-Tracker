using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using OTTracker.Domain.Entities;
using OTTracker.Domain.Interfaces;
using OTTracker.Services;
using OTTracker.Services.GlobalExceptions;
using OTTracker.Views;

namespace OTTracker;

public partial class App : Application
{
    private const string RetryChoice = "Retry";
    private const string UseLocalChoice = "Use local SQLite";
    private const string ExitAppChoice = "Exit App";

    // Covers session restore + the test query, so a hanging token refresh cannot freeze startup.
    private static readonly TimeSpan ConnectionTimeout = TimeSpan.FromSeconds(8);

    private enum ConnectionResult { Connected, Failed, NoSession }

    private readonly IServiceProvider _services;
    private readonly LocalSettingsService _localSettings;
    private readonly IAuthService _auth;
    private readonly ISupabaseClientProvider _clientProvider;
    private readonly ISupabaseSessionService _sessionService;
    private readonly IDataSourceModeService _modeService;
    private readonly ISupabaseConfigService _configService;

    public App(
        IServiceProvider services,
        LocalSettingsService localSettings,
        IAuthService auth,
        ISupabaseClientProvider clientProvider,
        ISupabaseSessionService sessionService,
        IDataSourceModeService modeService,
        ISupabaseConfigService configService)
    {
        _services = services;
        _localSettings = localSettings;
        _auth = auth;
        _clientProvider = clientProvider;
        _sessionService = sessionService;
        _modeService = modeService;
        _configService = configService;
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
        InitializeAsync().SafeFireAndForget(_ =>
            MainThread.BeginInvokeOnMainThread(() => MainPage = new AppShell()));
    }

    public static Task ShowMainAsync()
    {
        Current!.MainPage = new AppShell();
        return Task.CompletedTask;
    }

    private async Task InitializeAsync()
    {
        if (_modeService.UseSupabase)
        {
            try
            {
                if (!await EnsureSupabaseConnectedAsync())
                {
                    return; // user chose Exit App
                }
            }
            catch
            {
                // Never leave the user stuck on the loading spinner.
                await _modeService.SetUseSupabaseAsync(false);
            }
        }

        AppSettings settings;
        try
        {
            settings = await _localSettings.GetAsync();
        }
        catch
        {
            settings = new AppSettings();
        }

        if (settings.PinLockEnabled && await _auth.HasPinAsync())
        {
            MainPage = _services.GetRequiredService<PinPage>();
            _ = ApplyRemindersOnStartupAsync(settings);
            return;
        }

        MainPage = new AppShell();
        _ = ApplyRemindersOnStartupAsync(settings);
    }

    /// <returns>false if the user chose to exit the app.</returns>
    private async Task<bool> EnsureSupabaseConnectedAsync()
    {
        var result = await VerifySupabaseConnectionAsync(recreateClient: false);
        while (result != ConnectionResult.Connected)
        {
            var message = result == ConnectionResult.NoSession
                ? "Supabase session expired\nUse local SQLite, then reconnect in Settings."
                : "Supabase Connection Failed\nCheck your internet connection and tap Retry.";

            var choice = await ShowStartupChoiceAsync(message);

            if (choice == UseLocalChoice)
            {
                await _modeService.SetUseSupabaseAsync(false);
                return true;
            }

            if (choice == ExitAppChoice)
            {
                QuitApp();
                return false;
            }

            // Retry (or sheet dismissed with Back): fresh client + saved session, no email/password needed.
            result = await VerifySupabaseConnectionAsync(recreateClient: true);
        }

        return true;
    }

    private Task<string> ShowStartupChoiceAsync(string message)
    {
        return MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var page = MainPage;
            // On a cold start the loading page may not be attached to a window yet; an action sheet
            // requested before that never completes, which left the spinner running forever.
            if (page is null || !await WaitForPageReadyAsync(page))
            {
                return UseLocalChoice;
            }

            return await page.DisplayActionSheet(message, null, ExitAppChoice, RetryChoice, UseLocalChoice)
                ?? RetryChoice;
        });
    }

    private static async Task<bool> WaitForPageReadyAsync(Page page)
    {
        for (var i = 0; i < 100; i++) // up to ~10 seconds
        {
            if (page.IsLoaded && page.Window is not null && page.Handler is not null)
            {
                return true;
            }

            await Task.Delay(100);
        }

        return false;
    }

    private async Task ApplyRemindersOnStartupAsync(AppSettings settings)
    {
        try
        {
            var reminderService = _services.GetRequiredService<IReminderService>();
            await reminderService.ApplyRemindersAsync(settings.RemindersEnabled, settings.ReminderTime);
        }
        catch
        {
            // Ignore notification scheduling failures during startup.
        }
    }

    private async Task<ConnectionResult> VerifySupabaseConnectionAsync(bool recreateClient)
    {
        var savedSession = await _sessionService.LoadSessionAsync();
        if (savedSession is null)
        {
            return ConnectionResult.NoSession;
        }

        try
        {
            if (recreateClient)
            {
                // Drop any client left in an offline/refreshing state by the previous attempt.
                var creds = _configService.GetCredentials();
                if (!string.IsNullOrWhiteSpace(creds.Url) && !string.IsNullOrWhiteSpace(creds.AnonKey))
                {
                    _clientProvider.RecreateClient(creds.Url, creds.AnonKey);
                }
            }

            await CheckConnectionAsync(savedSession.Value.AccessToken, savedSession.Value.RefreshToken)
                .WaitAsync(ConnectionTimeout);
            return ConnectionResult.Connected;
        }
        catch
        {
            // Network/timeout failures keep the saved session so Retry can reuse it.
            return ConnectionResult.Failed;
        }
    }

    private async Task CheckConnectionAsync(string accessToken, string refreshToken)
    {
        var supabase = _clientProvider.Client
            ?? throw new InvalidOperationException("Supabase client unavailable.");
        await supabase.Auth.SetSession(accessToken, refreshToken);

        var userId = supabase.Auth.CurrentUser?.Id
            ?? throw new InvalidOperationException("Supabase session has no user.");

        await supabase.From<AppSettings>()
            .Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals, userId)
            .Limit(1)
            .Get();
    }

    private void QuitApp()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
#if ANDROID
            Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
#elif IOS
            System.Environment.Exit(0);
#elif MACCATALYST
            Application.Current?.Quit();
            System.Environment.Exit(0);
#elif WINDOWS
            Application.Current?.Quit();
            System.Environment.Exit(0);
#else
            Application.Current?.Quit();
            System.Environment.Exit(0);
#endif
        });
    }
}
