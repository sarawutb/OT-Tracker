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

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.LoadAsync();
    }
}
