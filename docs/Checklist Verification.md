# OT Tracker Checklist Verification

Verification date: 2026-06-18

Source documents:

- `PGM/Project/2026/OT Tracker/Plans/Implementation Plan.md`
- `PGM/Project/2026/OT Tracker/CheckList/Checklist.md`
- `Project/2026/Program/OT Tracker/Design/ot_tracker_all_screens_refresh.html`

## Verified by Build

- .NET MAUI 8 project created.
- SDK pinned with `global.json`.
- App project targets Android, iOS, Mac Catalyst, and Windows debug build.
- Shell bottom navigation configured for Dashboard, Log OT, History, and Settings.
- Purple-accent light theme and compact card/button/input styles added from the design reference.
- Models created for `OtEntry`, `AppSettings`, `DayType`, calendar days, and summaries.
- MVVM view models created for PIN, Dashboard, Log Entry, History, and Settings.
- Services created for calculation, SQLite data access, settings, authentication, biometric hook, CSV export, and app refresh events.
- SQLite persistence implemented for entries and non-sensitive settings.
- SecureStorage PIN hashing implemented.
- CSV export creates a local CSV file and opens the platform share sheet.
- Create, edit, delete, clear-data, export, and summary refresh flows are implemented in the app layer.

Build command:

```powershell
dotnet build "src\OTTracker\OTTracker.csproj" -f net8.0-windows10.0.19041.0 -p:TargetFrameworks=net8.0-windows10.0.19041.0 -o "$env:TEMP\ottracker-build"
```

Result:

```text
Build succeeded.
0 Warning(s)
0 Error(s)
```

## Verified by Automated Test Runner

Test command:

```powershell
dotnet run --project "tests\OTTracker.Tests\OTTracker.Tests.csproj"
```

Result:

```text
OT Tracker calculation tests passed.
```

Covered checks:

- Default hourly rate calculation.
- Default net OT hours calculation.
- Regular workday estimated earnings.
- Weekend estimated earnings.
- Public holiday estimated earnings.
- Negative net hours clamped to zero.

## Implemented Functional Checklist

- Dashboard monthly totals, weekly summary, weekly chart, privacy toggle, recent entries, and quick actions.
- Log OT create/edit form with date, day type, start/end time, break minutes, note, real-time net hours, rate, multiplier, and earnings.
- Validation for required time rules, break minutes, and non-negative net hours.
- History month navigation, calendar marks, monthly stats, entry edit, and delete confirmation.
- Settings salary assumptions, multipliers, PIN lock, change PIN, biometric toggle, CSV export, and clear all data.
- Local offline-first storage.
- Summary refresh after create, update, delete, and settings changes.

## Requires Device Manual Verification

- Android launch and navigation on emulator/device.
- iOS launch and navigation on simulator/device.
- Platform biometric prompt integration and hardware behavior.
- Platform share sheet behavior for exported CSV.
- Exact phone-size visual polish against the HTML mockup.
- Clear-data confirmation UX on each platform.

## Known Constraints

- The biometric service includes the app-level availability/authentication hook, but true native biometric prompt behavior should be validated or replaced with a platform biometric package during device testing.
- Formal xUnit/MSTest packages were not used because remote NuGet restore was blocked in this environment. A no-dependency console runner verifies deterministic calculation rules.
- Full Android build verification was blocked by the local .NET 8 Android workload manifest, which reported missing Android pack metadata. The Windows-target MAUI build verified the shared app code and XAML.
