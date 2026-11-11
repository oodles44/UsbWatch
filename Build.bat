@echo off
rem Builds UsbWatch.exe using the C# compiler that ships with Windows (no SDK needed),
rem then launches it.
rem   - Release version: read from the newest "## [x.y.z]" heading in CHANGELOG.md
rem   - Build stamp:     the current date/time, shown next to the version in the app
rem   - CHANGELOG.md is embedded in the exe and shown in Settings > About
setlocal
cd /d "%~dp0"

if not exist CHANGELOG.md (
  echo ERROR: CHANGELOG.md not found next to Build.bat.
  pause
  exit /b 1
)

set VERSION=
for /f "tokens=2 delims=[]" %%v in ('findstr /b /l /c:"## [" CHANGELOG.md') do if not defined VERSION set VERSION=%%v
if not defined VERSION (
  echo ERROR: no release heading like "## [1.0.0]" found in CHANGELOG.md.
  pause
  exit /b 1
)

rem Stop a running copy first: Windows locks a running exe, which would fail the build.
taskkill /f /im UsbWatch.exe >nul 2>&1
if not errorlevel 1 timeout /t 1 /nobreak >nul

set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe

set STAMP=
for /f "usebackq delims=" %%i in (`powershell -NoProfile -Command "Get-Date -Format 'yyyy.MM.dd-HHmm'"`) do set STAMP=%%i
if "%STAMP%"=="" set STAMP=unknown

rem Generated file: version constants for the UI, plus the exe's file-properties metadata.
> BuildInfo.cs echo [assembly: System.Reflection.AssemblyVersion("%VERSION%.0")]
>> BuildInfo.cs echo [assembly: System.Reflection.AssemblyFileVersion("%VERSION%.0")]
>> BuildInfo.cs echo [assembly: System.Reflection.AssemblyInformationalVersion("%VERSION%+%STAMP%")]
>> BuildInfo.cs echo [assembly: System.Reflection.AssemblyTitle("UsbWatch")]
>> BuildInfo.cs echo [assembly: System.Reflection.AssemblyProduct("UsbWatch")]
>> BuildInfo.cs echo [assembly: System.Reflection.AssemblyDescription("Live USB device connect/disconnect monitor")]
>> BuildInfo.cs echo [assembly: System.Reflection.AssemblyCopyright("Copyright (c) 2026 Dakota McGilton")]
>> BuildInfo.cs echo static class BuildInfo { public const string Version = "%VERSION%"; public const string Stamp = "%STAMP%"; }

rem Embed UsbWatch.ico as the exe's file icon (Explorer, taskbar pin, shortcuts).
set ICON=
if exist UsbWatch.ico set ICON=/win32icon:UsbWatch.ico
if not defined ICON echo NOTE: UsbWatch.ico not found next to Build.bat, building without an embedded icon.

"%CSC%" /nologo /target:winexe /optimize+ /out:UsbWatch.exe %ICON% ^
  /resource:CHANGELOG.md,UsbWatch.CHANGELOG.md ^
  /r:System.Management.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll ^
  UsbWatch.cs BuildInfo.cs

if errorlevel 1 (
  echo.
  echo Build FAILED.
  pause
  exit /b 1
)

echo.
echo Built UsbWatch.exe  version %VERSION%+%STAMP%
start "" "%~dp0UsbWatch.exe"
echo Launched. Closing in 3 seconds...
timeout /t 3 /nobreak >nul
