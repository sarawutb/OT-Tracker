using CommunityToolkit.Mvvm.ComponentModel;

namespace OTTracker.ViewModels;

public abstract partial class BaseViewModel : ObservableObject
{
    public Page? CurrentPage => App.Current?.MainPage;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string errorMessage = string.Empty;
}
