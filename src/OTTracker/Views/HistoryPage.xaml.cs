using OTTracker.Services.GlobalExceptions;
using OTTracker.ViewModels;

namespace OTTracker.Views;

public partial class HistoryPage : ContentPage
{
    private readonly HistoryViewModel _viewModel;

    public HistoryPage(HistoryViewModel viewModel)
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
}
