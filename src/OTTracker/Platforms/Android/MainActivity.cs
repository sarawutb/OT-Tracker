using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Color = Android.Graphics.Color;

namespace OTTracker;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        ApplyStatusBarColor();
    }

    private void ApplyStatusBarColor()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.Lollipop)
            return;

        var isDark = App.Current?.RequestedTheme == AppTheme.Dark;

        Window?.SetStatusBarColor(isDark
            ? Color.ParseColor("#5B4FE8")
            : Color.ParseColor("#AFA9EC"));

        if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
        {
            Window.DecorView.SystemUiVisibility = isDark
                ? 0
                : (StatusBarVisibility)SystemUiFlags.LightStatusBar;
        }
    }
}
