param(
    [string]$Configuration = 'Release',
    [string]$Runtime = 'win-x64',
    [switch]$SkipFFmpegDownload,
    [switch]$InstallInno,
    [switch]$SkipInstaller
)

$ErrorActionPreference = 'Stop'

function Find-InnoCompiler {
    $command = Get-Command iscc.exe -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    $candidates = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
    )

    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate) {
            return $candidate
        }
    }

    return $null
}

$repoRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')
$publishDir = Join-Path $repoRoot 'artifacts\publish\AppleConverter'
$installerDir = Join-Path $repoRoot 'artifacts\installer'
$project = Join-Path $repoRoot 'src\AppleLegacyMediaConverter\AppleLegacyMediaConverter.csproj'
$iss = Join-Path $repoRoot 'installer\AppleConverter.iss'

if (-not $SkipFFmpegDownload) {
    & (Join-Path $PSScriptRoot 'Get-FFmpeg.ps1')
}

Remove-Item -LiteralPath $publishDir -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $publishDir, $installerDir -Force | Out-Null

dotnet publish $project `
    --configuration $Configuration `
    --runtime $Runtime `
    --self-contained true `
    -p:Platform=x64 `
    -p:WindowsAppSDKSelfContained=true `
    -o $publishDir

Copy-Item -LiteralPath (Join-Path $repoRoot 'README.md') -Destination (Join-Path $publishDir 'README.md') -Force
Copy-Item -LiteralPath (Join-Path $repoRoot 'LICENSE') -Destination (Join-Path $publishDir 'LICENSE.txt') -Force

if ($SkipInstaller) {
    Write-Host "Published self-contained app to $publishDir"
    exit 0
}

$iscc = Find-InnoCompiler
if (-not $iscc -and $InstallInno) {
    $wingetCommand = Get-Command winget.exe -ErrorAction SilentlyContinue
    if ($wingetCommand) {
        & $wingetCommand.Source install --id JRSoftware.InnoSetup --scope user --silent --accept-package-agreements --accept-source-agreements
    }

    $iscc = Find-InnoCompiler
    if (-not $iscc) {
        $chocoCommand = Get-Command choco.exe -ErrorAction SilentlyContinue
        $choco = if ($chocoCommand) { $chocoCommand.Source } else { $null }
        if ($choco) {
            & $choco install innosetup --yes --no-progress --limit-output
            $iscc = Find-InnoCompiler
        }
    }
}

if (-not $iscc) {
    throw "Inno Setup compiler (iscc.exe) was not found. Install Inno Setup or run this script with -SkipInstaller."
}

$env:APPLECONVERTER_REPO_ROOT = $repoRoot
$env:APPLECONVERTER_PUBLISH_DIR = $publishDir
$env:APPLECONVERTER_INSTALLER_DIR = $installerDir
& $iscc $iss

if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup failed with exit code $LASTEXITCODE"
}

Write-Host "Installer output:"
Get-ChildItem -LiteralPath $installerDir -Filter '*.exe' | Select-Object FullName, Length
