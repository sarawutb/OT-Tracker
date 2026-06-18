# OT Tracker Checklist

Checked date: 2026-06-18

Verification references:

- App source: `D:\AI 2026\OT Tracker`
- Build: Windows target build passed with 0 warnings and 0 errors.
- Test runner: calculation tests passed.
- Verification doc: `D:\AI 2026\OT Tracker\docs\Checklist Verification.md`

## 1. Planning and Decisions

- [x] Confirm version 1 target platform: Android and iOS project targets, with Windows debug target.
- [x] Confirm whether optional Windows debug build is required.
- [x] Confirm whether overnight OT is required in version 1: not supported in version 1.
- [x] Confirm public holiday handling: manual day type selection.
- [x] Confirm salary calculation formula is company-standard for this implementation.
- [ ] Confirm CSV currency format with payroll/user policy.
- [ ] Confirm biometric acceptance test devices.
- [x] Confirm app display name and package identifier.

## 2. Project Setup

- [x] Create .NET MAUI 8 solution.
- [x] Create main app project.
- [x] Add CommunityToolkit.Mvvm.
- [x] Add SQLite package.
- [x] Configure dependency injection.
- [x] Configure Shell navigation.
- [x] Add project folders: `Models`, `ViewModels`, `Views`, `Services`, `Data`.
- [ ] Add `Controls` folder: not needed yet because no reusable custom controls were extracted.
- [x] Add resource dictionaries for colors, typography, spacing, and components.
- [x] Add purple accent and light theme styles.

## 3. Models and Data Contracts

- [x] Create `DayType` enum for regular workday, weekend, and public holiday.
- [x] Create `OtEntry` model.
- [x] Create `AppSettings` model.
- [x] Create monthly summary model.
- [x] Create weekly summary model.
- [x] Define default settings.
- [x] Confirm sensitive values are excluded from plain SQLite storage.

## 4. Calculation Service

- [x] Implement hourly rate calculation.
- [x] Implement net OT hours calculation.
- [x] Implement estimated earnings calculation.
- [x] Apply regular workday multiplier.
- [x] Apply weekend multiplier.
- [x] Apply public holiday multiplier.
- [x] Prevent negative net OT hours.
- [x] Add formatting helpers for hours and earnings where used by view models.

## 5. Local Persistence

- [x] Initialize SQLite database.
- [x] Create OT entries table.
- [x] Create settings table or Preferences-backed settings storage.
- [x] Implement create entry.
- [x] Implement update entry.
- [x] Implement delete entry.
- [x] Implement get entry by id.
- [x] Implement monthly query.
- [x] Implement recent entries query.
- [x] Implement clear all data.
- [x] Recalculate summaries after create, update, and delete.

## 6. Security

- [x] Implement PIN enabled setting.
- [x] Implement 4-digit PIN setup.
- [x] Store PIN hash in SecureStorage.
- [x] Implement PIN unlock.
- [x] Implement invalid PIN feedback.
- [x] Implement change PIN flow.
- [x] Implement disable PIN flow through settings toggle.
- [x] Detect biometric availability.
- [x] Implement biometric unlock hook when available.
- [x] Route locked launch to PIN screen.
- [x] Route unlocked state to main app shell.

## 7. Dashboard Screen

- [x] Show current month and greeting.
- [x] Show month total OT hours.
- [x] Show month estimated earnings.
- [x] Add earnings privacy toggle.
- [x] Mask earnings when privacy is enabled.
- [x] Show this-week OT hours.
- [x] Show this-week entry count.
- [x] Show weekly bar chart by weekday.
- [ ] Visually distinguish weekday and weekend OT in chart bars.
- [x] Show recent OT entries.
- [x] Add quick action to log today's OT.
- [x] Add quick action to open history.
- [x] Refresh dashboard after entry changes.

## 8. Log OT Entry Screen

- [x] Support create mode.
- [x] Support edit mode by entry id.
- [x] Add date input.
- [x] Add day type input.
- [x] Add start time input.
- [x] Add end time input.
- [x] Add break minutes input.
- [x] Add optional note input.
- [x] Calculate net hours in real time.
- [x] Calculate hourly rate in real time.
- [x] Calculate multiplier in real time.
- [x] Calculate estimated earnings in real time.
- [x] Validate required fields through required controls and save rules.
- [x] Validate break minutes is 0 or greater.
- [x] Validate end time is later than start time.
- [x] Block save when invalid.
- [x] Save new entry.
- [x] Update existing entry.
- [x] Navigate back or refresh destination after save.

## 9. History Screen

- [x] Show month navigator.
- [x] Show calendar grid for selected month.
- [x] Mark dates that have OT entries.
- [ ] Highlight selected day visually.
- [ ] Highlight today's date visually.
- [x] Show month total hours.
- [x] Show month estimated earnings.
- [x] List entries for the selected month.
- [x] Support edit action.
- [x] Support delete action.
- [x] Ask for confirmation before delete.
- [x] Refresh history after create, update, and delete.

## 10. Settings Screen

- [x] Edit base monthly salary.
- [x] Edit working days per month.
- [x] Edit hours per day.
- [x] Edit regular workday multiplier.
- [x] Edit weekend multiplier.
- [x] Edit public holiday multiplier.
- [x] Validate salary is greater than 0.
- [x] Validate working days is greater than 0.
- [x] Validate hours per day is greater than 0.
- [x] Validate multipliers are greater than 0.
- [x] Toggle PIN lock.
- [x] Open change PIN flow.
- [x] Toggle biometric unlock.
- [x] Trigger CSV export.
- [x] Trigger clear all data flow.
- [x] Save settings and apply to new calculations.

## 11. CSV Export

- [x] Implement CSV export service.
- [x] Include date.
- [x] Include day type.
- [x] Include start time.
- [x] Include end time.
- [x] Include break minutes.
- [x] Include net OT hours.
- [x] Include hourly rate.
- [x] Include multiplier.
- [x] Include estimated earnings.
- [x] Include note.
- [x] Escape CSV fields correctly.
- [x] Save CSV file locally.
- [x] Open platform share sheet.
- [ ] Verify exported file content on device.

## 12. Automated Tests

- [x] Test hourly rate calculation.
- [x] Test net hour calculation.
- [x] Test estimated earnings for regular workday.
- [x] Test estimated earnings for weekend.
- [x] Test estimated earnings for public holiday.
- [ ] Test settings changes affecting new calculations.
- [ ] Test create repository operation.
- [ ] Test update repository operation.
- [ ] Test delete repository operation.
- [ ] Test monthly query repository operation.
- [ ] Test CSV output columns.

## 13. Manual UI Tests

- [ ] App launches successfully on Android/iOS device or emulator.
- [ ] Bottom navigation works on device or emulator.
- [ ] PIN unlock works when enabled on device or emulator.
- [ ] Biometric unlock works on supported device.
- [ ] Dashboard shows correct monthly summary on device or emulator.
- [ ] Earnings privacy toggle masks and reveals amount on device or emulator.
- [ ] Log entry save works on device or emulator.
- [ ] Log entry validation blocks invalid data on device or emulator.
- [ ] History calendar marks OT dates on device or emulator.
- [ ] Edit from history works on device or emulator.
- [ ] Delete from history asks for confirmation on device or emulator.
- [ ] Settings save correctly on device or emulator.
- [ ] CSV export shares a readable file on device or emulator.
- [ ] Clear all data asks for confirmation and removes entries on device or emulator.
- [x] App is designed for offline operation.
- [ ] UI is usable on common Android phone size.
- [ ] UI is usable on common iPhone size if iOS is in scope.

## 14. Release Readiness

- [x] All version 1 implementation decisions are documented.
- [x] All required screens are implemented.
- [x] All required services are implemented.
- [x] Automated calculation tests pass.
- [ ] Full automated repository and CSV tests pass.
- [ ] Manual UI test pass is complete.
- [x] No sensitive values are stored in plain database rows.
- [ ] CSV export is verified on device.
- [x] Known limitations are documented.
- [ ] Android/iOS build artifact is ready for selected platform.
- [x] Windows debug target build passes.
