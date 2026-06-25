using OTTracker.ViewModels;

namespace OTTracker.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override bool OnBackButtonPressed()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            var confirm = await DisplayAlert(
                "Exit App",
                "Do you want to exit the app?",
                "Yes",
                "No");

            if (confirm)
            {
#if ANDROID
                Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
#endif
            }
        });

        return true;
    }
}
