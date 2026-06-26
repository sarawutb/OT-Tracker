using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OTTracker.Domain.Interfaces;
using OTTracker.Services;
using OTTracker.Views;

namespace OTTracker.ViewModels;

public sealed partial class LoginViewModel : BaseViewModel
{
    private readonly ISupabaseClientProvider _clientProvider;
    private readonly ISupabaseConfigService _configService;
    private readonly IServiceProvider _services;
    private readonly ISettingsService _settingsService;
    private readonly IAuthService _auth;
    private readonly IDataSourceModeService _modeService;
    private readonly IDataSyncService _syncService;

    [ObservableProperty]
    private string email = "rang7754@gmail.com";

    [ObservableProperty]
    private string password = "Sarawut7754*";

    [ObservableProperty]
    private string appVersion = string.Empty;

    //[ObservableProperty]
    //private string supabaseUrl = "https://qeoauyussturlgjjysqe.supabase.co";

    //[ObservableProperty]
    //private string supabaseAnonKey = "sb_secret_MY2ISglTP9jTYLH5yXrhAg_OuR6uc0r";

    public LoginViewModel(
        ISupabaseClientProvider clientProvider,
        ISupabaseConfigService configService,
        IServiceProvider services,
        ISettingsService settingsService,
        IAuthService auth,
        IDataSourceModeService modeService,
        IDataSyncService syncService)
    {
        _clientProvider = clientProvider;
        _configService = configService;
        _services = services;
        _settingsService = settingsService;
        _auth = auth;
        _modeService = modeService;
        _syncService = syncService;

        SignInCommand = new AsyncRelayCommand(SignInAsync);
        SetVersionText();
    }

    public IAsyncRelayCommand SignInCommand { get; }

    private async Task SignInAsync()
    {
        if (!ValidateCredentials())
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            await _clientProvider.Client.Auth.SignIn(Email.Trim(), Password);
            if (_modeService.PendingSupabaseSync)
            {
                await _syncService.EnableSupabaseAsync();
            }
            else
            {
                await _modeService.SetUseSupabaseAsync(true);
            }

            await NavigateAfterLoginAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Login failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            if (!string.IsNullOrEmpty(ErrorMessage))
                await App.Current.MainPage.DisplayAlert("Error Message", ErrorMessage, "OK");
        }
    }

    private bool ValidateCredentials()
    {
        ErrorMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Email and password are required.";
            return false;
        }

        return true;
    }

    private async Task NavigateAfterLoginAsync()
    {
        var settings = await _settingsService.GetAsync();
        if (settings.PinLockEnabled && await _auth.HasPinAsync())
        {
            Application.Current!.MainPage = _services.GetRequiredService<PinPage>();
            return;
        }

        await App.ShowMainAsync();
    }

    private void SetVersionText()
    {
        AppVersion = GetVersionText();
    }

    private static string GetVersionText()
    {
        var version = AppInfo.Current.VersionString;
        var build = AppInfo.Current.BuildString;

        if (string.IsNullOrWhiteSpace(version))
        {
            version = typeof(App).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;
        }

        if (string.IsNullOrWhiteSpace(version))
        {
            version = typeof(App).Assembly.GetName().Version?.ToString(3);
        }

        return string.IsNullOrWhiteSpace(build) ? $"version {version}" : $"version {version} ({build})";
    }
}
