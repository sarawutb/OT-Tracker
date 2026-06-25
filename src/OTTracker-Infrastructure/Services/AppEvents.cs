using System;

namespace OTTracker.Infrastructure.Services;

public sealed class AppEvents
{
    public event EventHandler? EntriesChanged;

    public event EventHandler? SettingsChanged;

    public void NotifyEntriesChanged() => EntriesChanged?.Invoke(this, EventArgs.Empty);

    public void NotifySettingsChanged() => SettingsChanged?.Invoke(this, EventArgs.Empty);
}
