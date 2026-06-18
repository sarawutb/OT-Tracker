using OTTracker.ViewModels;

namespace OTTracker.Views;

public partial class PinPage : ContentPage
{
    public PinPage(PinViewModel viewModel)
    {
        InitializeComponent();
        viewModel.Unlocked = App.ShowMainAsync;
        BindingContext = viewModel;
    }
}
