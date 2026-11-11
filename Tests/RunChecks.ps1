param([switch]$NativeAssets)

$ErrorActionPreference = 'Stop'
$projectPath = Split-Path -Parent $PSScriptRoot
$unityCli = Join-Path $env:LOCALAPPDATA 'Unity/bin/unity.exe'
if (-not (Test-Path -LiteralPath $unityCli)) { throw 'Unity CLI was not found. Install/configure it before running the checks.' }

if ($NativeAssets) {
    & $unityCli command --project-path $projectPath test run --mode editor --filter Swg.Client.Tests.Editor --filter-type assembly --timeout 180
} else {
    & $unityCli command --project-path $projectPath test run --mode editor --filter Core --filter-type category --timeout 180
}
if ($LASTEXITCODE -ne 0) { throw 'Unity Edit Mode checks failed.' }
