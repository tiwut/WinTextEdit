@echo off
setlocal enabledelayedexpansion

if /i "%1"=="clean" goto CLEAN_ARG
if /i "%1"=="dist" goto BUILD_ARG
if not "%1"=="" (
    echo Invalid argument: %1
    echo Usage: %0 [clean ^| dist]
    exit /b 1
)

:MENU
cls
echo ==============================================
echo  WinTextEdit Build Script
echo ==============================================
echo  1. Clean Project (dotnet clean + delete bin/obj)
echo  2. Build to dist (Release mode publish)
echo  3. Exit
echo ==============================================
set /p choice="Select an option (1-3): "

if "%choice%"=="1" goto CLEAN
if "%choice%"=="2" goto BUILD
if "%choice%"=="3" goto EXIT
echo Invalid choice, try again.
pause
goto MENU

:CLEAN
echo.
echo Cleaning project...
dotnet clean
if exist bin rmdir /s /q bin
if exist obj rmdir /s /q obj
echo.
echo Clean complete.
pause
goto MENU

:CLEAN_ARG
echo Cleaning project...
dotnet clean
if exist bin rmdir /s /q bin
if exist obj rmdir /s /q obj
echo Clean complete.
exit /b 0

:BUILD
echo.
echo Building and publishing to .\dist...
dotnet publish -c Release -o .\dist --no-self-contained
if %errorlevel% equ 0 (
    echo.
    echo Build successful! Outputs copied to .\dist
) else (
    echo.
    echo Build failed with error code %errorlevel%
)
pause
goto MENU

:BUILD_ARG
echo Building and publishing to .\dist...
dotnet publish -c Release -o .\dist --no-self-contained
if %errorlevel% equ 0 (
    echo.
    echo Build successful! Outputs copied to .\dist
    exit /b 0
) else (
    echo.
    echo Build failed with error code %errorlevel%
    exit /b %errorlevel%
)

:EXIT
exit /b 0
