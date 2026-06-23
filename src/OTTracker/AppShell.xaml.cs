using AndroidX.Lifecycle;
using OTTracker.ViewModels;

namespace OTTracker;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
	}

    protected override bool OnBackButtonPressed()
    {
        if (CurrentPage is Views.HistoryPage)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await GoToAsync("//Dashboard");
            });

            return true;
        }
        else if (CurrentPage is Views.LogEntryPage)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                var _bindingContext = CurrentPage.BindingContext as LogEntryViewModel;
                _bindingContext?.OnBackAsync();
            });

            return true;
        }

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            bool confirm = await DisplayAlert(
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
