@echo off
setlocal enabledelayedexpansion

echo.
echo  ==========================================
echo    CoolifeVO - AI Assistant  Windows Setup
echo  ==========================================
echo.

set "APPDIR=%LOCALAPPDATA%\CoolifeVO"
set "EXE=CoolifeVO.exe"

tasklist /fi "imagename eq %EXE%" 2>nul | find /i "%EXE%" >nul
if !errorlevel! equ 0 (
    echo  [INFO] CoolifeVO is running, closing...
    taskkill /f /im %EXE% >nul 2>&1
    timeout /t 2 /nobreak >nul
)

echo  [1/3] Installing files to: %APPDIR%
if not exist "%APPDIR%" mkdir "%APPDIR%"
copy /y "%~dp0CoolifeVO.exe" "%APPDIR%\" >nul
if errorlevel 1 (
    echo  [ERROR] Failed to copy CoolifeVO.exe
    pause
    exit /b 1
)
if exist "%~dp0app.ico" copy /y "%~dp0app.ico" "%APPDIR%\" >nul

echo  [2/3] Creating desktop shortcut...
set "LNK=%USERPROFILE%\Desktop\CoolifeVO Smart Assistant.lnk"
if exist "%LNK%" del /f /q "%LNK%" >nul 2>&1
powershell -NoProfile -ExecutionPolicy Bypass -Command "$ws=New-Object -ComObject WScript.Shell; $s=$ws.CreateShortcut('%LNK%'); $s.TargetPath='%APPDIR%\%EXE%'; $s.WorkingDirectory='%APPDIR%'; $s.IconLocation='%APPDIR%\app.ico,0'; $s.Description='CoolifeVO - AI Assistant'; $s.Save()" >nul 2>&1
if exist "%LNK%" (
    echo  [OK] Desktop shortcut created
) else (
    echo  [WARNING] Shortcut creation failed
)

echo  [3/3] Setup complete!
echo.
echo  ==========================================
echo    Launching CoolifeVO - AI Assistant...
echo  ==========================================
start "" "%APPDIR%\%EXE%" >nul 2>&1
exit /b 0
