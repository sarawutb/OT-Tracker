using Microsoft.Maui.Controls;
using OTTracker.Services.GlobalExceptions;
using OTTracker.ViewModels;

namespace OTTracker.Views;

public partial class HistoryPage : ContentPage
{
    private readonly HistoryViewModel _viewModel;
    private double _panX;
    private bool _hasSwiped;

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

    private void OnCalendarPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _panX = 0;
                _hasSwiped = false;
                break;
            case GestureStatus.Running:
                _panX = e.TotalX;
                if (!_hasSwiped)
                {
                    if (_panX < -60)
                    {
                        _hasSwiped = true;
                        _viewModel.NextMonthCommand.Execute(null);
                    }
                    else if (_panX > 60)
                    {
                        _hasSwiped = true;
                        _viewModel.PreviousMonthCommand.Execute(null);
                    }
                }
                break;
            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                _panX = 0;
                _hasSwiped = false;
                break;
        }
    }
}
