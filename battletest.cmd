@echo off
setlocal

cd /d "%~dp0"

echo.
echo Launching Joseon Murim Tactics BattleTest...
echo This builds the BattleTest Windows player, then starts it.
echo.

call "%~dp0play-windows-player-preview.cmd" %*
exit /b %ERRORLEVEL%
