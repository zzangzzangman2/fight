param(
    [string]$ExePath = "$PSScriptRoot\..\UnityScaffold\Builds\BattleTest\JoseonMurimTacticsBattleTest.exe",
    [int]$Width = 1280,
    [int]$Height = 720,
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$ExtraArgs
)

. "$PSScriptRoot\Use-ProjectUtf8.ps1"

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class BattleTestWindowPlacement {
    [DllImport("user32.dll")] public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
}
"@

if (-not (Test-Path -LiteralPath $ExePath)) {
    throw "BattleTest player was not found: $ExePath"
}

$screen = [System.Windows.Forms.Screen]::FromPoint([System.Windows.Forms.Cursor]::Position)
$area = $screen.WorkingArea
$targetWidth = [Math]::Min($Width, $area.Width)
$targetHeight = [Math]::Min($Height, $area.Height)
$targetX = $area.X + [Math]::Max(0, [Math]::Floor(($area.Width - $targetWidth) / 2))
$targetY = $area.Y + [Math]::Max(0, [Math]::Floor(($area.Height - $targetHeight) / 2))

$arguments = @(
    '-screen-fullscreen', '0',
    '-screen-width', "$targetWidth",
    '-screen-height', "$targetHeight",
    '-force-d3d11'
)

if ($ExtraArgs -ne $null -and $ExtraArgs.Count -gt 0) {
    $arguments += @($ExtraArgs | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
}

$process = Start-Process -FilePath $ExePath -ArgumentList $arguments -PassThru

$deadline = (Get-Date).AddSeconds(15)
do {
    Start-Sleep -Milliseconds 100
    $process.Refresh()
} while (-not $process.HasExited -and $process.MainWindowHandle -eq [IntPtr]::Zero -and (Get-Date) -lt $deadline)

if ($process.HasExited -or $process.MainWindowHandle -eq [IntPtr]::Zero) {
    exit 0
}

for ($i = 0; $i -lt 12; $i++) {
    $process.Refresh()
    if ($process.HasExited) {
        break
    }

    [BattleTestWindowPlacement]::ShowWindow($process.MainWindowHandle, 1) | Out-Null
    [BattleTestWindowPlacement]::MoveWindow($process.MainWindowHandle, $targetX, $targetY, $targetWidth, $targetHeight, $true) | Out-Null
    Start-Sleep -Milliseconds 250
}
