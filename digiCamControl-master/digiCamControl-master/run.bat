@echo off
set BINDIR=%~dp0CameraControl\bin\Debug
set APPDIR=%~dp0CameraControl.Application

if not exist "%BINDIR%\Tools\exiv2.exe" (
    echo Copying Tools...
    xcopy "%APPDIR%\Tools" "%BINDIR%\Tools\" /Y /E /I /Q
)

start "" "%BINDIR%\CameraControl.exe"
