namespace OTTracker.Controls;

using Microsoft.Maui.Controls.Shapes;

public partial class AppTabBar : ContentView
{
    public static readonly BindableProperty ActiveRouteProperty = BindableProperty.Create(
        nameof(ActiveRoute),
        typeof(string),
        typeof(AppTabBar),
        "Dashboard",
        propertyChanged: OnActiveRouteChanged);

    public AppTabBar()
    {
        InitializeComponent();
        ApplyState();
        Loaded += AppTabBar_Loaded;
    }

    [Obsolete]
    private async void AppTabBar_Loaded(object? sender, EventArgs e)
    {
        await AnimatedContainer.TranslateTo(0, 50, 100);
        await AnimatedContainer.TranslateTo(0, 25, 100);
        await AnimatedContainer.TranslateTo(0, 0, 100);
    }

    public string ActiveRoute
    {
        get => (string)GetValue(ActiveRouteProperty);
        set => SetValue(ActiveRouteProperty, value);
    }

    private static void OnActiveRouteChanged(BindableObject bindable, object oldValue, object newValue)
    {
        ((AppTabBar)bindable).ApplyState();
    }

    private void ApplyState()
    {
        SetTabState(DashboardIcon, DashboardLabel, DashboardDot, ActiveRoute == "Dashboard");
        SetTabState(LogIcon, LogLabel, LogDot, ActiveRoute == "Log");
        SetTabState(SettingsIcon, SettingsLabel, SettingsDot, ActiveRoute == "Settings");
    }

    private void SetTabState(Path icon, Label label, Ellipse dot, bool isActive)
    {
        var surface = GetColor("Surface");
        var brush = new SolidColorBrush(surface);

        icon.Stroke = brush;
        label.TextColor = surface;
        label.IsVisible = isActive;
        dot.Fill = brush;
        dot.IsVisible = isActive;
    }

    private static Color GetColor(string resourceKey)
    {
        return Application.Current?.Resources.TryGetValue(resourceKey, out var value) == true && value is Color color
            ? color
            : Colors.Transparent;
    }

    private async void DashboardTapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("//Dashboard");
    }

    private async void LogTapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("//Log");
    }

    private async void SettingsTapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("//Settings");
    }
}
