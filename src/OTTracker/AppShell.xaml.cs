namespace OTTracker;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
	}

    protected override bool OnBackButtonPressed()
    {
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
