# CLAUDE.md

This file guides Claude Code when working in this repository.

## Overview

OT Tracker is a .NET 10 MAUI app for tracking overtime hours and earnings. Android is the main target. iOS and MacCatalyst are also listed, and the Windows target framework is commented out in the csproj. The SDK version is pinned in `global.json` (10.0.301). Some UI text and changelogs are in Thai.

## Commands

```powershell
# Build Android
dotnet build src/OTTracker/OTTracker.csproj -f net10.0-android36.1

# Release build (produces a signed APK)
dotnet publish src/OTTracker/OTTracker.csproj -f net10.0-android36.1 -c Release

# Calculation tests: a plain console runner, not xUnit (exit code 1 = failure)
dotnet run --project tests/OTTracker.Tests/OTTracker.Tests.csproj
```

The `net8.0-windows…` build command in README.md is out of date.

## Solution layout

- `src/OTTracker-Domain`: entities, the `DayType` enum, and service interfaces. `OtEntry` and `AppSettings` are Supabase Postgrest `BaseModel`s mapped with `[Table]`/`[Column]` attributes, so renaming a property can break the database mapping.
- `src/OTTracker-Infrastructure`: Supabase repositories and services, plus `OtCalculationService` (all pay math) and `AppEvents`. `SupabaseConfigService` reads credentials from `%AppData%/OTTracker/db.dat` (DPAPI-encrypted on Windows) and falls back to defaults if the file is missing.
- `src/OTTracker`: the MAUI app. It uses MVVM with CommunityToolkit.Mvvm; ViewModels inherit `BaseViewModel`, which provides `IsBusy` and `ErrorMessage`. The UI uses UraniumUI Material. AppShell has the tab routes `Dashboard`, `History` and `Settings`. `LogEntryPage` is a pushed route named `Log`, registered in `AppShell.xaml.cs`. `PinPage` is shown as the main page before the Shell loads.
## Architecture: two data sources

- `DataSourceModeService` stores the `ot_tracker_use_supabase` flag in MAUI `Preferences`.
- `RoutedOtEntryRepository` and `RoutedSettingsService` are registered as `IOtEntryRepository` and `ISettingsService`. They send each call either to Supabase or to local SQLite (`LocalOtEntryRepository`, `LocalSettingsService`, and `Data/LocalAppDatabase`, which uses `ottracker-local.db3`).
  - Reads fall back to local SQLite if Supabase throws.
  - Entry writes and deletes do **not** fall back; Supabase errors reach the caller.
  - Settings saves always write to local SQLite first, then try Supabase and ignore any Supabase error.
- `DataSyncService` copies snapshots between the two sources when Supabase is turned on or off, or when the user syncs manually.
- At startup, `App.InitializeAsync` checks the Supabase connection with a 5-second timeout. If the check fails, it asks the user to switch to local SQLite or exit. It then shows `PinPage` if PIN lock is on, or `AppShell` otherwise, and schedules reminders.
- All dependency injection is set up in `src/OTTracker/MauiProgram.cs`. Register every new service, ViewModel and Page there.

## Conventions and gotchas

- Pay formulas:
  - hourly rate = salary / (working days × hours per day), **rounded** (AwayFromZero) to 2 decimals (28144 / 240 = 117.2667 → 117.27)
  - net hours = (end − start − break minutes) / 60, never below 0, **rounded** to 2 decimals
  - earnings = net hours × hourly rate × multiplier (regular, weekend or holiday), **truncated** to 2 decimals (263.8575 → 263.85, not 263.86)
- `tests/OTTracker.Tests/Program.cs` **copies** these formulas instead of referencing `OtCalculationService`. If you change one, update the other to match.
- `OtEntry.EntryDate` always holds a date with no time part, marked as UTC. Start and end times are stored as `"HH:mm:ss"` strings.
- Unhandled exceptions go through `GlobalExceptionHandler`. Run fire-and-forget tasks with `task.SafeFireAndForget(...)` (in `Services/GlobalExceptions/SafeExecutor.cs`).
- **Releasing:** change all of these in the same commit:
  - `ApplicationDisplayVersion` and `ApplicationVersion` in `OTTracker.csproj`
  - `versionName`, `versionCode`, `apkUrl` and `changeLog` in the root `update.json`

  For in-app updates, `UpdateService` downloads `update.json` from the GitHub `main` branch and compares `versionCode` with the installed app's build number.
- `appsettings.json`, `*.db*` and `releases/` are gitignored. Never commit Supabase keys.
