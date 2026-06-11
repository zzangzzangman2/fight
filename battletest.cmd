@echo off
setlocal

cd /d "%~dp0"

set "PLAYER_EXE=%~dp0UnityScaffold\Builds\BattleTest\JoseonMurimTacticsBattleTest.exe"

if not exist "%PLAYER_EXE%" (
  echo BattleTest player was not found.
  echo Run play-windows-player-preview.cmd once to build it.
  pause
  exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\launch-battletest-window.ps1" -ExePath "%PLAYER_EXE%" %*
exit /b %ERRORLEVEL%
