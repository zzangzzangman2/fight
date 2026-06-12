param(
    [string]$Root = (Resolve-Path -LiteralPath "$PSScriptRoot\..").Path
)

. "$PSScriptRoot\Use-ProjectUtf8.ps1"

$ErrorActionPreference = 'Stop'

$textExtensions = @(
    '.asmdef',
    '.asmref',
    '.asset',
    '.cmd',
    '.cs',
    '.css',
    '.csv',
    '.html',
    '.js',
    '.json',
    '.mat',
    '.md',
    '.meta',
    '.prefab',
    '.ps1',
    '.py',
    '.txt',
    '.unity'
)

$skipPathPattern = '\\(\.git|Library|Temp|Builds|Logs|obj|bin|work)\\'
function Join-CodePoints([int[]]$CodePoints) {
    -join ($CodePoints | ForEach-Object { [char]$_ })
}

$mojibakePatterns = @(
    [string][char]0xFFFD,
    (Join-CodePoints @(0x8B70, 0xACD7, 0xAFA9)),
    [string][char]0x00C3,
    [string][char]0x00C2
)
$strictUtf8 = New-Object System.Text.UTF8Encoding($false, $true)
$failures = New-Object System.Collections.Generic.List[object]

Get-ChildItem -LiteralPath $Root -Recurse -File | Where-Object {
    $textExtensions -contains $_.Extension.ToLowerInvariant() -and
    $_.FullName -notmatch $skipPathPattern -and
    $_.Length -lt 5MB
} | ForEach-Object {
    $path = $_.FullName
    try {
        $text = $strictUtf8.GetString([IO.File]::ReadAllBytes($path))
    }
    catch {
        $failures.Add([pscustomobject]@{
            Kind = 'InvalidUtf8'
            Path = $path
            Detail = $_.Exception.Message
        })
        return
    }

    foreach ($pattern in $mojibakePatterns) {
        if ($text.Contains($pattern)) {
            $failures.Add([pscustomobject]@{
                Kind = 'MojibakePattern'
                Path = $path
                Detail = $pattern
            })
            break
        }
    }
}

if ($failures.Count -gt 0) {
    $failures | Sort-Object Path | Format-Table -AutoSize
    throw "UTF-8 text validation failed: $($failures.Count) issue(s)."
}

Write-Host "UTF-8 text validation passed."
