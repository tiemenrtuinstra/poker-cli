# Poker CLI Installer for Windows
# Usage: irm https://raw.githubusercontent.com/tiemenrtuinstra/poker-cli/main/install.ps1 | iex

$ErrorActionPreference = "Stop"

$Repo = "tiemenrtuinstra/poker-cli"
$BinaryName = "poker-cli.exe"
$InstallDir = "$env:LOCALAPPDATA\Programs\poker-cli"

function Write-Info { param($Message) Write-Host "[INFO] $Message" -ForegroundColor Cyan }
function Write-Success { param($Message) Write-Host "[SUCCESS] $Message" -ForegroundColor Green }
function Write-Warn { param($Message) Write-Host "[WARN] $Message" -ForegroundColor Yellow }
function Write-Err { param($Message) Write-Host "[ERROR] $Message" -ForegroundColor Red; exit 1 }

function Get-LatestVersion {
    Write-Info "Fetching latest version..."
    try {
        $release = Invoke-RestMethod -Uri "https://api.github.com/repos/$Repo/releases/latest"
        $version = $release.tag_name
        Write-Info "Latest version: $version"
        return $version
    }
    catch {
        Write-Err "Could not determine latest version. Check https://github.com/$Repo/releases"
    }
}

function Install-PokerCli {
    param($Version)

    $artifact = "poker-cli-windows-x64.exe"
    $downloadUrl = "https://github.com/$Repo/releases/download/$Version/$artifact"

    Write-Info "Downloading from: $downloadUrl"

    # Create install directory
    if (-not (Test-Path $InstallDir)) {
        New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
    }

    $destination = Join-Path $InstallDir $BinaryName

    # Download
    try {
        Invoke-WebRequest -Uri $downloadUrl -OutFile $destination -UseBasicParsing
    }
    catch {
        Write-Err "Download failed: $_"
    }

    Write-Success "Installed $BinaryName to $destination"
}

function Add-ToPath {
    $currentPath = [Environment]::GetEnvironmentVariable("Path", "User")

    if ($currentPath -notlike "*$InstallDir*") {
        Write-Info "Adding $InstallDir to user PATH..."
        $newPath = "$currentPath;$InstallDir"
        [Environment]::SetEnvironmentVariable("Path", $newPath, "User")
        $env:Path = "$env:Path;$InstallDir"
        Write-Success "Added to PATH"
    }
    else {
        Write-Info "$InstallDir is already in PATH"
    }
}

function Main {
    Write-Host ""
    Write-Host "  ♠ ♥ ♦ ♣  Poker CLI Installer  ♣ ♦ ♥ ♠" -ForegroundColor Magenta
    Write-Host ""

    $version = Get-LatestVersion
    Install-PokerCli -Version $version
    Add-ToPath

    Write-Host ""
    Write-Success "Installation complete!"
    Write-Host ""
    Write-Host "Restart your terminal, then run 'poker-cli' to start playing!" -ForegroundColor White
    Write-Host ""
}

Main
