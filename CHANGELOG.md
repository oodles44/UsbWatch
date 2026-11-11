# Changelog

All notable changes to UsbWatch. Versions are build date/times in `yyyy.MM.dd-HHmm` form, newest first.
This file is embedded into the exe and shown in Settings > About, so keep the format: a `## version`
line per release followed by `- ` bullets.

## 2026.11.11-1549
- Changelog now lives in CHANGELOG.md, embedded in the exe and shared with the About dialog
- "List current" rows are now written to the log file too, tagged PRESENT

## 2026.11.11-1535
- About dialog with this changelog

## 2026.11.11-1523
- Settings moved into a gear menu; toolbar slimmed down and no longer wraps or overlaps
- Choose which columns to show: Time, Event, Device, Class, VID:PID, Port, Status
- Window size, position and maximized state are remembered

## 2026.11.11-1514
- Settings persist across runs in UsbWatch.ini
- Build.bat launches the app after building and closes itself

## 2026.11.11-1511
- Close to tray: X hides the window, right-click the tray icon to exit

## 2026.11.11-1213
- App icon embedded in the exe

## 2026.11.11-1209
- Notifications: first change shows instantly, the rest of a burst collapses into one summary toast

## 2026.11.11-1129
- Build date/time shown as the version number in the status bar

## 2026.11.10-1027
- Wider Time column
- Notifications batched for docks and hubs; clicking a toast brings the window forward

## 2026.11.06-1738
- Fixed clipped button text and an empty device list
- Error 45 / 21 (already unplugged) now shows as a clean removal
- More plain-English explanations for Windows device error codes

## 2026.11.06-1729
- Auto-scroll toggle

## 2026.11.06-1727
- Dracula dark theme with color-coded rows

## 2026.11.03-1544
- First GUI version: live device table, toast notifications and a one-click build script, no Python needed

## 2026.10.30-0806
- First version: PowerShell command-line USB monitor
