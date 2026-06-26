using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using OTTracker.Domain.Interfaces;

namespace OTTracker_Avalonia.ViewModels;

public sealed partial class LoginViewModel : ViewModelBase
{
    private readonly Supabase.Client _client;
    private readonly IServiceProvider _services;
    private readonly ISettingsService _settingsService;
    private readonly IAuthService _auth;

    [ObservableProperty]
    private string _email = "rang7754@gmail.com";

    [ObservableProperty]
    private string _password = "Sarawut7754*";

    public LoginViewModel(
        Supabase.Client client,
        IServiceProvider services,
        ISettingsService settingsService,
        IAuthService auth)
    {
        _client = client;
        _services = services;
        _settingsService = settingsService;
        _auth = auth;

        SignInCommand = new AsyncRelayCommand(SignInAsync);
    }

    public IAsyncRelayCommand SignInCommand { get; }

    private async Task SignInAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Email and Password are required.";
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            await _client.Auth.SignIn(Email, Password);
            await HandlePostLoginAsync();
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

    private async Task HandlePostLoginAsync()
    {
        var settings = await _settingsService.GetAsync();
        var hasPin = await _auth.HasPinAsync();

        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var mainViewModel = _services.GetRequiredService<MainWindowViewModel>();
            if (settings.PinLockEnabled && hasPin)
            {
                var pinViewModel = _services.GetRequiredService<PinViewModel>();
                pinViewModel.Unlocked = async () =>
                {
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        var dashboard = _services.GetRequiredService<DashboardViewModel>();
                        mainViewModel.NavigateTo(dashboard);
                        return Task.CompletedTask;
                    });
                };
                mainViewModel.NavigateTo(pinViewModel);
            }
            else
            {
                var dashboard = _services.GetRequiredService<DashboardViewModel>();
                mainViewModel.NavigateTo(dashboard);
            }
        });
    }
}
