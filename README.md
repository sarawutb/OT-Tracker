# OT Tracker

OT Tracker is a .NET MAUI 10 mobile overtime tracking app for Android and iOS, with a Windows debug build target.

## Features

- PIN unlock with SecureStorage-backed PIN hash.
- Dashboard with monthly OT hours, estimated earnings, weekly summary, and recent entries.
- Log OT entry form with real-time salary-based earnings calculation.
- History calendar with month totals, entry edit, and delete confirmation.
- Settings for salary assumptions, OT multipliers, security, CSV export, and clear data.
- Offline-first SQLite persistence.

## Verification

Windows target build:

```powershell
dotnet build "src\OTTracker\OTTracker.csproj" -f net8.0-windows10.0.19041.0 -p:TargetFrameworks=net8.0-windows10.0.19041.0
```

Calculation test runner:

```powershell
dotnet run --project "tests\OTTracker.Tests\OTTracker.Tests.csproj"
```
