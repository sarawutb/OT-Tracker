using CommunityToolkit.Mvvm.ComponentModel;

namespace OTTracker.ViewModels;

public abstract class BaseViewModel : ObservableObject
{
    private bool _isBusy;
    private string _errorMessage = string.Empty;

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }
}
