# Changelog

All notable changes to UsbWatch, newest first. Releases follow [Semantic Versioning](https://semver.org/).

This file is embedded in the exe and shown in Settings > About, and `Build.bat` reads the release
version from it. Keep the format: a `## [x.y.z] - date` heading per release followed by `- ` bullets,
with the newest release first (do not add an `[Unreleased]` heading above it). The build date/time is
appended to the version automatically at build time.

## [1.0.0] - 2026-11-11
- First public release
- Live table of USB arrivals, removals and status changes, color-coded, with Windows error codes explained in plain English
- Optional columns: Time, Event, Device, Class, VID:PID, Port, Status (pick them in Settings > Columns)
- Toast notifications: the first change shows instantly and the rest of a burst collapses into one summary; clicking a toast brings the window forward
- Runs in the tray: close to tray, right-click the tray icon to exit
- Gear settings menu, plus Pause, List current and Clear on the toolbar
- Settings, columns and window size/position are remembered in UsbWatch.ini
- Optional log file that records everything shown in the list, including List current snapshots
- About dialog with this changelog, the release version plus exact build date/time, a link to the project page and a support link

## Development history (before 1.0.0)
- 2026-10-30: PowerShell command-line prototype
- 2026-11-03: first GUI version with live table, toasts and a one-click build script
- 2026-11-06: Dracula dark theme, auto-scroll, error 45/21 handling and a device-query fix
- 2026-11-10: notification batching and click-to-focus
- 2026-11-11: tray mode, saved settings, gear menu, column picker, app icon, About dialog and embedded changelog
