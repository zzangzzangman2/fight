# Project-local UTF-8 defaults for Windows PowerShell 5.1 and newer.

$script:ProjectUtf8NoBom = New-Object System.Text.UTF8Encoding($false)

[Console]::InputEncoding = $script:ProjectUtf8NoBom
[Console]::OutputEncoding = $script:ProjectUtf8NoBom
$OutputEncoding = $script:ProjectUtf8NoBom

$env:PYTHONUTF8 = '1'
$env:PYTHONIOENCODING = 'utf-8'

if ($PSVersionTable.PSVersion.Major -le 5) {
    $PSDefaultParameterValues['Get-Content:Encoding'] = 'UTF8'
    $PSDefaultParameterValues['Set-Content:Encoding'] = 'UTF8'
    $PSDefaultParameterValues['Add-Content:Encoding'] = 'UTF8'
    $PSDefaultParameterValues['Import-Csv:Encoding'] = 'UTF8'
    $PSDefaultParameterValues['Export-Csv:Encoding'] = 'UTF8'
    $PSDefaultParameterValues['Out-File:Encoding'] = 'UTF8'
}
