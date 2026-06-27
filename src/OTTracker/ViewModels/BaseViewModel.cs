using CommunityToolkit.Mvvm.ComponentModel;
using OTTracker.Services.GlobalExceptions;

namespace OTTracker.ViewModels;

public abstract partial class BaseViewModel : ObservableObject
{
    public Page? CurrentPage => MauiUserExceptionNotifier.GetVisiblePage(Application.Current?.Windows.FirstOrDefault()?.Page);

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string errorMessage = string.Empty;
}
