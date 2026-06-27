using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OTTracker_Avalonia.ViewModels;

public abstract partial class ViewModelBase : ObservableObject
{
    public string? AppVersion => Assembly.GetExecutingAssembly().GetName().Version?.ToString(3);

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _errorMessage = string.Empty;
}
