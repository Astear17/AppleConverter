param(
    [string]$Destination = (Join-Path $PSScriptRoot '..\tools\ffmpeg'),
    [string]$Url = 'https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip',
    [switch]$Force
)

$ErrorActionPreference = 'Stop'

$destinationPath = Resolve-Path -LiteralPath (New-Item -ItemType Directory -Path $Destination -Force).FullName
$ffmpegExe = Join-Path $destinationPath 'ffmpeg.exe'
if ((Test-Path -LiteralPath $ffmpegExe) -and -not $Force) {
    Write-Host "FFmpeg already exists at $ffmpegExe"
    exit 0
}

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("AppleConverter-FFmpeg-" + [Guid]::NewGuid().ToString('N'))
$archivePath = Join-Path $tempRoot 'ffmpeg.zip'
$extractPath = Join-Path $tempRoot 'extract'
New-Item -ItemType Directory -Path $tempRoot, $extractPath -Force | Out-Null

try {
    Write-Host "Downloading FFmpeg from $Url"
    & curl.exe --fail --location --retry 3 --output $archivePath $Url
    if ($LASTEXITCODE -ne 0) {
        throw "curl failed with exit code $LASTEXITCODE"
    }

    Expand-Archive -LiteralPath $archivePath -DestinationPath $extractPath -Force
    $binFolder = Get-ChildItem -Path $extractPath -Recurse -Directory |
        Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName 'ffmpeg.exe') } |
        Select-Object -First 1

    if (-not $binFolder) {
        throw "The FFmpeg archive did not contain ffmpeg.exe."
    }

    Get-ChildItem -LiteralPath $binFolder.FullName -File |
        Where-Object { $_.Name -in @('ffmpeg.exe', 'ffprobe.exe') -or $_.Extension -eq '.dll' -or $_.Name -like 'LICENSE*' -or $_.Name -like 'README*' } |
        ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $destinationPath $_.Name) -Force
    }

    Write-Host "Installed FFmpeg to $destinationPath"
}
finally {
    Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}
