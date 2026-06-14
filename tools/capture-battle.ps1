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
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class Win32Capture {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, UInt32 uFlags);
    [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr hWnd, ref POINT point);
    [DllImport("user32.dll")] public static extern void keybd_event(byte bVk, byte bScan, UInt32 dwFlags, UIntPtr dwExtraInfo);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int X, int Y);
    [DllImport("user32.dll")] public static extern void mouse_event(UInt32 dwFlags, UInt32 dx, UInt32 dy, UInt32 dwData, UIntPtr dwExtraInfo);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }
}
"@

$SW_RESTORE = 9
$SWP_NOMOVE = 0x0002
$SWP_NOSIZE = 0x0001
$SWP_SHOWWINDOW = 0x0040
$HWND_TOPMOST = [IntPtr](-1)
$HWND_NOTOPMOST = [IntPtr](-2)
$KEYEVENTF_KEYUP = 0x0002
$MOUSEEVENTF_LEFTDOWN = 0x0002
$MOUSEEVENTF_LEFTUP = 0x0004
$VK_SPACE = 0x20
$VK_TAB = 0x09
$VK_2 = 0x32

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

function Focus-PlayerWindow([IntPtr]$hwnd) {
    [Win32Capture]::ShowWindow($hwnd, $SW_RESTORE) | Out-Null
    [Win32Capture]::SetWindowPos($hwnd, $HWND_TOPMOST, 0, 0, 0, 0, $SWP_NOMOVE -bor $SWP_NOSIZE -bor $SWP_SHOWWINDOW) | Out-Null
    [Win32Capture]::SetForegroundWindow($hwnd) | Out-Null
    [Win32Capture+RECT]$rect = New-Object Win32Capture+RECT
    [Win32Capture]::GetClientRect($hwnd, [ref]$rect) | Out-Null
    [Win32Capture+POINT]$point = New-Object Win32Capture+POINT
    $point.X = [Math]::Max(8, [Math]::Min(48, ($rect.Right - $rect.Left) / 2))
    $point.Y = [Math]::Max(8, [Math]::Min(48, ($rect.Bottom - $rect.Top) / 2))
    [Win32Capture]::ClientToScreen($hwnd, [ref]$point) | Out-Null
    [Win32Capture]::SetCursorPos($point.X, $point.Y) | Out-Null
    Start-Sleep -Milliseconds 60
    [Win32Capture]::mouse_event($MOUSEEVENTF_LEFTDOWN, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 40
    [Win32Capture]::mouse_event($MOUSEEVENTF_LEFTUP, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 140
}

function Invoke-PlayerKey([byte]$virtualKey) {
    [Win32Capture]::keybd_event($virtualKey, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 90
    [Win32Capture]::keybd_event($virtualKey, 0, $KEYEVENTF_KEYUP, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 180
}

$proc = Start-Process -FilePath $ExePath -ArgumentList @("-screen-fullscreen","0","-screen-width","$Width","-screen-height","$Height") -PassThru
try {
    Start-Sleep -Seconds 14
    if ($proc.HasExited) { throw "Player exited early (code $($proc.ExitCode))" }
    $proc.Refresh()
    $hwnd = $proc.MainWindowHandle
    if ($hwnd -eq [IntPtr]::Zero) { throw "No main window handle" }
    Focus-PlayerWindow $hwnd
    Start-Sleep -Milliseconds 900

    Save-WindowShot $hwnd (Join-Path $OutDir "01-scout.png")

    # Confirm deployment -> player phase with move range of first ally
    Focus-PlayerWindow $hwnd
    Invoke-PlayerKey $VK_SPACE
    Start-Sleep -Seconds 3
    Save-WindowShot $hwnd (Join-Path $OutDir "02-player-phase-move.png")

    # Toggle threat overlay (Tab)
    Focus-PlayerWindow $hwnd
    Invoke-PlayerKey $VK_TAB
    Start-Sleep -Seconds 2
    Save-WindowShot $hwnd (Join-Path $OutDir "03-threat-overlay.png")
    Focus-PlayerWindow $hwnd
    Invoke-PlayerKey $VK_TAB

    # Attack command (2) to see attack range tiles
    Focus-PlayerWindow $hwnd
    Invoke-PlayerKey $VK_2
    Start-Sleep -Seconds 2
    Save-WindowShot $hwnd (Join-Path $OutDir "04-attack-mode.png")
    [Win32Capture]::SetWindowPos($hwnd, $HWND_NOTOPMOST, 0, 0, 0, 0, $SWP_NOMOVE -bor $SWP_NOSIZE) | Out-Null
}
finally {
    if (-not $proc.HasExited) { Stop-Process -Id $proc.Id -Force }
}
Write-Host "done"
