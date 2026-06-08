@echo off
setlocal

set "PROJECT_DIR=%~dp0UnityScaffold"
set "UNITY_EXE=C:\Program Files\Unity\Hub\Editor\6000.3.0f1\Editor\Unity.exe"

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
  echo Install Unity 6000.3.0f1 or edit UNITY_EXE in this file.
  pause
  exit /b 1
)

if not exist "%PROJECT_DIR%\Assets" (
  echo Unity project folder was not found:
  echo %PROJECT_DIR%
  pause
  exit /b 1
)

echo Opening Unity character asset preview...
echo Project: %PROJECT_DIR%
echo Unity:   %UNITY_EXE%

echo Preparing preview scene. This can take a minute on first launch...
"%UNITY_EXE%" -batchmode -quit -projectPath "%PROJECT_DIR%" -executeMethod JoseonMurimTactics.Editor.CharacterPreviewLauncher.RebuildPreviewScene -logFile "%TEMP%\joseon-murim-character-preview-1.log"
"%UNITY_EXE%" -batchmode -quit -projectPath "%PROJECT_DIR%" -executeMethod JoseonMurimTactics.Editor.CharacterPreviewLauncher.RebuildPreviewScene -logFile "%TEMP%\joseon-murim-character-preview-2.log"

if errorlevel 1 (
  echo Unity preview preparation failed.
  echo Check logs:
  echo %TEMP%\joseon-murim-character-preview-1.log
  echo %TEMP%\joseon-murim-character-preview-2.log
  pause
  exit /b 1
)

start "" "%UNITY_EXE%" -projectPath "%PROJECT_DIR%" -executeMethod JoseonMurimTactics.Editor.CharacterPreviewLauncher.OpenAndPlay
endlocal
