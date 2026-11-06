$ErrorActionPreference = 'Stop'
$tejaRepository = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$tejaDirectory = Join-Path $tejaRepository 'game/Assets/StreamingAssets/Networking'
$tejaDestination = [System.IO.Path]::GetFullPath((Join-Path $tejaDirectory 'cloudflared.exe'))
$tejaExpected = '411b16d40c8d872243efeb52c550053d00af0a8a97fbb32e730fb8aba429eaa8'
$tejaUri = 'https://github.com/cloudflare/cloudflared/releases/download/2026.10.4/cloudflared-windows-amd64.exe'

if (-not $tejaDestination.StartsWith($tejaRepository + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw 'The helper destination must stay within this repository.'
}
if (-not (Test-Path -LiteralPath (Join-Path $tejaRepository 'game/ProjectSettings/ProjectVersion.txt'))) {
    throw 'Run this script from a repository containing the game/ Unity project.'
}
if (Test-Path -LiteralPath $tejaDestination) {
    if ((Get-FileHash -LiteralPath $tejaDestination -Algorithm SHA256).Hash -eq $tejaExpected) {
        Write-Output 'The verified cloudflared 2026.10.4 helper is already installed.'
        return
    }
}
New-Item -ItemType Directory -Path $tejaDirectory -Force | Out-Null
$tejaTemporary = Join-Path $tejaDirectory ('cloudflared-download-' + [Guid]::NewGuid().ToString('N') + '.tmp')
try {
    Invoke-WebRequest -Uri $tejaUri -OutFile $tejaTemporary -UseBasicParsing -TimeoutSec 180
    $tejaActual = (Get-FileHash -LiteralPath $tejaTemporary -Algorithm SHA256).Hash
    if ($tejaActual -ne $tejaExpected) {
        throw 'The downloaded helper failed its SHA-256 check. The existing helper was not changed.'
    }
    Copy-Item -LiteralPath $tejaTemporary -Destination $tejaDestination -Force
    Write-Output ('Installed verified cloudflared 2026.10.4 at ' + $tejaDestination)
} finally {
    if (Test-Path -LiteralPath $tejaTemporary) {
        Remove-Item -LiteralPath $tejaTemporary -Force
    }
}
