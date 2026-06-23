using System.Reflection;
using OTTracker.ViewModels;

namespace OTTracker.Views;

public partial class PinPage : ContentPage
{
    private readonly PinViewModel _viewModel;

    public PinPage(PinViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        viewModel.Unlocked = App.ShowMainAsync;
        BindingContext = viewModel;
        SetVersionText();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        SetVersionText();
        await _viewModel.LoadAsync();
    }

    private void SetVersionText()
    {
        AppVersionLabel.Text = GetVersionText();
    }

    private static string GetVersionText()
    {
        var version = AppInfo.Current.VersionString;
        var build = AppInfo.Current.BuildString;

        if (string.IsNullOrWhiteSpace(version))
        {
            version = typeof(App).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;
        }

        if (string.IsNullOrWhiteSpace(version))
        {
            version = typeof(App).Assembly.GetName().Version?.ToString(3) ?? "1.0.2";
        }

        return string.IsNullOrWhiteSpace(build) ? $"v{version}" : $"v{version} ({build})";
    }
}
