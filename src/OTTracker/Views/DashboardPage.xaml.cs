using OTTracker.ViewModels;

namespace OTTracker.Views;

public partial class DashboardPage : ContentPage
{
    private readonly DashboardViewModel _viewModel;

    public DashboardPage(DashboardViewModel viewModel)
    {
        InitializeComponent();
        HeaderVersionLabel.Text = GetVersionText();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }

    private static string GetVersionText()
    {
        return $"v{AppInfo.VersionString} ({AppInfo.BuildString})";
    }
}
