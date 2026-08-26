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

        if (DatePickerFieldControl.Date == DateTime.Today)
        {
            DatePickerFieldControl.Date = DateTime.Today.AddDays(1);
            DatePickerFieldControl.Date = DateTime.Today;
        }
    }

    protected override bool OnBackButtonPressed()
    {
        _viewModel.OnBackAsync();
        return true;
    }
}
