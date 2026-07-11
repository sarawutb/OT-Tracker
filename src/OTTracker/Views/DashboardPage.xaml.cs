using OTTracker.Services.GlobalExceptions;
using OTTracker.ViewModels;

namespace OTTracker.Views;

public partial class DashboardPage : ContentPage
{
    private readonly DashboardViewModel _viewModel;

    public DashboardPage(DashboardViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        HeaderVersionLabel.Text = GetVersionText();
        _viewModel.LoadAsync().SafeFireAndForget(ex =>
        {
            DisplayAlert("Error", ex.Message, "OK");
        });
    }

    private static string GetVersionText()
    {
        return $"v{AppInfo.VersionString} ({AppInfo.BuildString})";
    }
}
