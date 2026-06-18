using OTTracker.ViewModels;

namespace OTTracker.Views;

public partial class LogEntryPage : ContentPage
{
    private readonly LogEntryViewModel _viewModel;

    public LogEntryPage(LogEntryViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }
}
