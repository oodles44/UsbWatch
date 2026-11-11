@echo off
rem Builds UsbWatch.exe using the C# compiler that ships with Windows (no SDK needed),
rem then launches it. Stamps the build date/time into BuildInfo.cs (shown as the version)
rem and embeds CHANGELOG.md (shown in Settings > About).
setlocal
cd /d "%~dp0"

if not exist CHANGELOG.md (
  echo ERROR: CHANGELOG.md not found next to Build.bat.
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

> BuildInfo.cs echo static class BuildInfo { public const string Stamp = "%STAMP%"; }

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
echo Built UsbWatch.exe  version %STAMP%
start "" "%~dp0UsbWatch.exe"
echo Launched. Closing in 3 seconds...
timeout /t 3 /nobreak >nul
