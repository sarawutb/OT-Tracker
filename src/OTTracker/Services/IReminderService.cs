using System;
using System.Threading.Tasks;

namespace OTTracker.Services;

public interface IReminderService
{
    Task<bool> RequestPermissionAsync();
    Task ApplyRemindersAsync(bool enabled, TimeSpan reminderTime);
}
