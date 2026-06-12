@echo off
setlocal
chcp 65001 >nul
set "PYTHONUTF8=1"
set "PYTHONIOENCODING=utf-8"
powershell -NoLogo -NoExit -NoProfile -ExecutionPolicy Bypass -Command "& { . '%~dp0tools\Use-ProjectUtf8.ps1'; Set-Location -LiteralPath '%~dp0' }"
