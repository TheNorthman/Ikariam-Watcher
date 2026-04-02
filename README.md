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

Notes for developers
- Project language: C# 14 targeting .NET 10 (WPF/WinForms enabled).
- The tray icon is loaded from the embedded resource `favicon.ico` (ApplicationIcon is set in the project file).
- Alarm logic lives in `AlarmManager.cs`. Tray behavior and user toggles are handled in `TrayApplicationContext.cs`.

Contributing
- Fork, create a branch, add changes and open a pull request.

License
- MIT (choose your preferred license and update this file as needed).
