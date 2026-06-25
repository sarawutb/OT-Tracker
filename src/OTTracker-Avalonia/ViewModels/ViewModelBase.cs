using CommunityToolkit.Mvvm.ComponentModel;

namespace OTTracker_Avalonia.ViewModels;

public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _errorMessage = string.Empty;
}
