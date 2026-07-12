using System;
using System.Threading.Tasks;
using Plugin.LocalNotification;

namespace OTTracker.Services;

public sealed class ReminderService : IReminderService
{
    private const int ReminderNotificationId = 9999;

    public async Task<bool> RequestPermissionAsync()
    {
        if (await LocalNotificationCenter.Current.AreNotificationsEnabled())
        {
            return true;
        }

        return await LocalNotificationCenter.Current.RequestNotificationPermission();
    }

    public async Task ApplyRemindersAsync(bool enabled, TimeSpan reminderTime)
    {
        LocalNotificationCenter.Current.Cancel(ReminderNotificationId);

        if (!enabled)
        {
            return;
        }

        var notifyTime = DateTime.Today.Add(reminderTime);
        if (notifyTime < DateTime.Now)
        {
            notifyTime = notifyTime.AddDays(1);
        }

        var request = new NotificationRequest
        {
            NotificationId = ReminderNotificationId,
            Title = "OT Tracker",
            Description = "Did you work OT today? Don't forget to log your hours!",
            Schedule = new NotificationRequestSchedule
            {
                NotifyTime = notifyTime,
                RepeatType = NotificationRepeat.Daily,
                
            }
        };

        await LocalNotificationCenter.Current.Show(request);
    }
}
