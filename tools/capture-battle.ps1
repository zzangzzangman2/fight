# Launches the BattleTest player, captures battle screenshots, then closes it.
# Usage: powershell -ExecutionPolicy Bypass -File tools\capture-battle.ps1
param(
    [string]$ExePath = "$PSScriptRoot\..\UnityScaffold\Builds\BattleTest\JoseonMurimTacticsBattleTest.exe",
    [string]$OutDir = "$PSScriptRoot\..\work\battle-shots",
    [int]$Width = 1600,
    [int]$Height = 900
)

. "$PSScriptRoot\Use-ProjectUtf8.ps1"

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class Win32Capture {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr hWnd, ref POINT point);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }
}
"@

if (-not (Test-Path $ExePath)) { throw "Player exe not found: $ExePath" }
New-Item -ItemType Directory -Force $OutDir | Out-Null

function Save-WindowShot([IntPtr]$hwnd, [string]$path) {
    [Win32Capture+RECT]$rect = New-Object Win32Capture+RECT
    [Win32Capture]::GetClientRect($hwnd, [ref]$rect) | Out-Null
    $w = $rect.Right - $rect.Left
    $h = $rect.Bottom - $rect.Top
    if ($w -le 0 -or $h -le 0) { throw "Window has no client area" }
    [Win32Capture+POINT]$origin = New-Object Win32Capture+POINT
    [Win32Capture]::ClientToScreen($hwnd, [ref]$origin) | Out-Null
    $bmp = New-Object System.Drawing.Bitmap($w, $h)
    $gfx = [System.Drawing.Graphics]::FromImage($bmp)
    $gfx.CopyFromScreen($origin.X, $origin.Y, 0, 0, (New-Object System.Drawing.Size($w, $h)))
    $gfx.Dispose()
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Host "saved $path"
}

$proc = Start-Process -FilePath $ExePath -ArgumentList @("-screen-fullscreen","0","-screen-width","$Width","-screen-height","$Height") -PassThru
try {
    Start-Sleep -Seconds 14
    if ($proc.HasExited) { throw "Player exited early (code $($proc.ExitCode))" }
    $proc.Refresh()
    $hwnd = $proc.MainWindowHandle
    if ($hwnd -eq [IntPtr]::Zero) { throw "No main window handle" }
    [Win32Capture]::SetForegroundWindow($hwnd) | Out-Null
    Start-Sleep -Milliseconds 900

    Save-WindowShot $hwnd (Join-Path $OutDir "01-scout.png")

    # End scout mode (S) -> player phase with move range of first ally
    [System.Windows.Forms.SendKeys]::SendWait("s")
    Start-Sleep -Seconds 3
    Save-WindowShot $hwnd (Join-Path $OutDir "02-player-phase-move.png")

    # Toggle threat overlay (Tab)
    [System.Windows.Forms.SendKeys]::SendWait("{TAB}")
    Start-Sleep -Seconds 2
    Save-WindowShot $hwnd (Join-Path $OutDir "03-threat-overlay.png")
    [System.Windows.Forms.SendKeys]::SendWait("{TAB}")

    # Attack command (2) to see attack range tiles
    [System.Windows.Forms.SendKeys]::SendWait("2")
    Start-Sleep -Seconds 2
    Save-WindowShot $hwnd (Join-Path $OutDir "04-attack-mode.png")
}
finally {
    if (-not $proc.HasExited) { Stop-Process -Id $proc.Id -Force }
}
Write-Host "done"
