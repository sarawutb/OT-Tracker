namespace OTTracker.Controls;

public partial class AnimatedPage : ContentPage
{
    public AnimatedPage()
    {
        InitializeComponent();
        AnimatedContainer.FadeTo(1, 100);
        AnimatedContainer.Content = Content;
    }

    public async Task AnimatedGoTo(string pUrl)
    {
        await AnimatedContainer.FadeTo(0, 100);
        await Shell.Current.GoToAsync(pUrl, true);
    }
}