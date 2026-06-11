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

start "" "%PLAYER_EXE%" -screen-fullscreen 0 -screen-width 1280 -screen-height 720 -force-d3d11 %*
exit /b %ERRORLEVEL%
