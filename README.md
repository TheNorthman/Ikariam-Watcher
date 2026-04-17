# Ikariam Watcher

Lightweight Windows tray application that watches window titles for countdown timers (e.g. `38m 55s`, `01h 39m 46s`) and notifies the user shortly before they reach a threshold.

Features
- Runs in the system tray (no main window).
- Scans visible windows every 5 seconds and parses countdowns from window titles using the pattern `(?:(\d{1,2})h\s*)?(\d{1,2})m(?:\s*(\d{1,2})s)?`.
- Anchors alarms to window handles (HWND) so each window is tracked independently.
- Fires an alarm exactly once when the remaining time crosses the 5-minute threshold; alarms do not repeatedly fire as titles update.
- Right‑click tray menu exposes three toggles and Exit:
  - Enable (default: enabled)
  - Play sound (default: enabled)
  - Show notification (default: disabled)

Requirements
- Windows
- .NET 10 SDK (desktop workload / Windows Forms enabled)
- Visual Studio 2022+ or `dotnet` CLI

Build and run
1. Open the solution `Ikariam Watcher.sln` in Visual Studio and build/run, or
2. From the repository root run:

```powershell
dotnet build "Ikariam Watcher\Ikariam Watcher.csproj"
dotnet run --project "Ikariam Watcher\Ikariam Watcher.csproj"
```

Usage
- The app lives in the system tray. Right-click the tray icon to toggle `Enable`, `Play sound`, and `Show notification`, or to `Exit` the app.
- When an alarm fires and `Show notification` is enabled the app shows a balloon tip. When `Play sound` is enabled the system exclamation sound is played.
- The tray tooltip lines show the world and city when available in the format: `{world} ({city}) - {alarm info}`. If no city is present the format falls back to `{world} - {alarm info}`.

Notes for developers
- Project language: C# 14 targeting .NET 10 (WPF/WinForms enabled).
- The tray icon is loaded from the embedded resource `favicon.ico` (ApplicationIcon is set in the project file).
- Alarm logic lives in `AlarmManager.cs`. Tray behavior and user toggles are handled in `TrayApplicationContext.cs`.

Ikariam v17 title handling
- Recent Ikariam (v17) titles may include hidden Unicode formatting/control characters (for example bidi markers) which could hide or alter the visible text. To avoid false positives the watcher now:
  - Normalizes window titles by stripping Unicode control/format characters and trimming whitespace before any parsing or display.
  - Only attempts to parse countdowns from titles that contain the word "Ikariam" after normalization.
  - Continues to use the existing countdown regex `(?:(\d{1,2})h\s*)?(\d{1,2})m(?:\s*(\d{1,2})s)?` but applied to the normalized title.

Developer verification
- Build the solution and run the app.
- Open an Ikariam v17 browser/game window with a title like: `Ikariam - 01h 38m 16s (Rayong) - World Theseus` and confirm it is detected.
- Open a non-Ikariam window that happens to include text matching the countdown pattern and confirm it is ignored.
- Tray tooltips and notifications should show normalized text with no hidden formatting characters.

Contributing
- Fork, create a branch, add changes and open a pull request.

License
- MIT (choose your preferred license and update this file as needed).
