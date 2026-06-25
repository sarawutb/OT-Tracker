using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OTTracker.Domain.Interfaces;
using OTTracker.Views;

namespace OTTracker.ViewModels;

public sealed partial class LoginViewModel : BaseViewModel
{
    private readonly ISupabaseClientProvider _clientProvider;
    private readonly ISupabaseConfigService _configService;
    private readonly IServiceProvider _services;
    private readonly ISettingsService _settingsService;
    private readonly IAuthService _auth;

    [ObservableProperty]
    private string email = "rang7754@gmail.com";

    [ObservableProperty]
    private string password = "Sarawut7754*";

    [ObservableProperty]
    private string supabaseUrl = "https://qeoauyussturlgjjysqe.supabase.co";

    [ObservableProperty]
    private string supabaseAnonKey = "sb_secret_MY2ISglTP9jTYLH5yXrhAg_OuR6uc0r";

    public LoginViewModel(
        ISupabaseClientProvider clientProvider,
        ISupabaseConfigService configService,
        IServiceProvider services,
        ISettingsService settingsService,
        IAuthService auth)
    {
        _clientProvider = clientProvider;
        _configService = configService;
        _services = services;
        _settingsService = settingsService;
        _auth = auth;

        var credentials = _configService.GetCredentials();
        SupabaseUrl = "https://qeoauyussturlgjjysqe.supabase.co";
        SupabaseAnonKey = "sb_secret_MY2ISglTP9jTYLH5yXrhAg_OuR6uc0r";

        SignInCommand = new AsyncRelayCommand(SignInAsync);
        SignUpCommand = new AsyncRelayCommand(SignUpAsync);
        SaveSupabaseConfigCommand = new AsyncRelayCommand(SaveSupabaseConfigAsync);
    }

    public IAsyncRelayCommand SignInCommand { get; }

    public IAsyncRelayCommand SignUpCommand { get; }

    public IAsyncRelayCommand SaveSupabaseConfigCommand { get; }

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
            await NavigateAfterLoginAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Login failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SignUpAsync()
    {
        if (!ValidateCredentials())
        {
            return;
        }

        if (Password.Length < 6)
        {
            ErrorMessage = "Password must be at least 6 characters.";
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            await _clientProvider.Client.Auth.SignUp(Email.Trim(), Password);
            ErrorMessage = "Registration successful. Please sign in.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Sign up failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
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

    private async Task SaveSupabaseConfigAsync()
    {
        ErrorMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(SupabaseUrl) || string.IsNullOrWhiteSpace(SupabaseAnonKey))
        {
            ErrorMessage = "Supabase URL and anon key are required.";
            return;
        }

        IsBusy = true;
        try
        {
            await _configService.SaveCredentialsAsync(SupabaseUrl.Trim(), SupabaseAnonKey.Trim());
            _clientProvider.RecreateClient(SupabaseUrl.Trim(), SupabaseAnonKey.Trim());
            ErrorMessage = "Connection saved. You can sign in now.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to save connection: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
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
}
