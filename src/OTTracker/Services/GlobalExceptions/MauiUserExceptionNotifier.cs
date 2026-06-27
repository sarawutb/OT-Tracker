using Microsoft.Maui.ApplicationModel;

namespace OTTracker.Services.GlobalExceptions;

public sealed class MauiUserExceptionNotifier : IUserExceptionNotifier
{
    public Task ShowAsync()
    {
        // Global events may be raised on arbitrary runtime threads.
        return MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var page = GetVisiblePage(Application.Current?.Windows.FirstOrDefault()?.Page);
            if (page is null)
            {
                return;
            }

            await page.DisplayAlertAsync(
                "Something went wrong",
                "An unexpected error occurred. You can continue, but please retry your last action.",
                "OK");
        });
    }

    public static Page? GetVisiblePage(Page? page) => page switch
    {
        Shell shell => GetVisiblePage(shell.CurrentPage) ?? shell,
        NavigationPage navigation => GetVisiblePage(navigation.CurrentPage) ?? navigation,
        TabbedPage tabs => GetVisiblePage(tabs.CurrentPage) ?? tabs,
        FlyoutPage flyout => GetVisiblePage(flyout.Detail) ?? flyout,
        _ => page
    };
}
