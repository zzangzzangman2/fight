@echo off
setlocal

chcp 65001 >nul
set "PYTHONUTF8=1"
set "PYTHONIOENCODING=utf-8"

cd /d "%~dp0"

set "PROJECT_DIR=%~dp0UnityScaffold"
set "UNITY_EXE=C:\Program Files\Unity\Hub\Editor\6000.4.9f1\Editor\Unity.exe"
set "PLAYER_EXE=%~dp0UnityScaffold\Builds\BattleTest\JoseonMurimTacticsBattleTest.exe"
set "BUILD_LOG=%TEMP%\joseon-murim-battletest-build.log"
set "BUILD_ONLY="

if /I "%~1"=="--build-only" (
  set "BUILD_ONLY=1"
  shift /1
)

if not exist "%UNITY_EXE%" (
  for /f "delims=" %%D in ('dir /b /ad "%ProgramFiles%\Unity\Hub\Editor" 2^>nul ^| sort /r') do (
    if exist "%ProgramFiles%\Unity\Hub\Editor\%%D\Editor\Unity.exe" (
      set "UNITY_EXE=%ProgramFiles%\Unity\Hub\Editor\%%D\Editor\Unity.exe"
      goto :found_unity
    )
  )
)

:found_unity
if not exist "%UNITY_EXE%" (
  echo Unity.exe was not found.
  echo Install Unity Editor with Windows Build Support, or edit UNITY_EXE in this cmd.
  pause
  exit /b 1
)

if not exist "%PROJECT_DIR%\Assets" (
  echo Unity project was not found: %PROJECT_DIR%
  pause
  exit /b 1
)

echo.
echo [1/2] Building latest BattleTest player...
"%UNITY_EXE%" -batchmode -quit -projectPath "%PROJECT_DIR%" -buildTarget Win64 -executeMethod JoseonMurimTactics.Editor.CharacterPlayerBuild.BuildWindowsBattleTest -logFile "%BUILD_LOG%"
if errorlevel 1 (
  echo BattleTest player build failed.
  echo Log: %BUILD_LOG%
  pause
  exit /b 1
)

if not exist "%PLAYER_EXE%" (
  echo BattleTest player executable was not created:
  echo %PLAYER_EXE%
  echo Log: %BUILD_LOG%
  pause
  exit /b 1
)

if defined BUILD_ONLY (
  echo.
  echo BattleTest build complete:
  echo %PLAYER_EXE%
  endlocal
  exit /b 0
)

echo.
echo [2/2] Launching BattleTest player...
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\launch-battletest-window.ps1" -ExePath "%PLAYER_EXE%" %*
exit /b %ERRORLEVEL%
