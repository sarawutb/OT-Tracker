using OTTracker.Services.GlobalExceptions;
using OTTracker.ViewModels;

namespace OTTracker.Views;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel _viewModel;

    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.LoadAsync().SafeFireAndForget(ex =>
        {
            DisplayAlert("Error", ex.Message, "OK");
        });
    }

    private void Switch_PinLock_Toggled(object sender, ToggledEventArgs e)
    {
        _viewModel.CheckPinLock().SafeFireAndForget(ex =>
        {
            DisplayAlert("Error", ex.Message, "OK");
        });
    }
}
