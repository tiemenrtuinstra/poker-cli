# Build Release Script for Poker CLI
# Creates self-contained executables for distribution

param(
    [string]$Runtime = "win-x64",
    [string]$Version = "1.2.7",
    [string]$OutputDir = "./release"
)

$ErrorActionPreference = "Stop"

Write-Host "Building Poker CLI v$Version for $Runtime..." -ForegroundColor Cyan

# Validate runtime
$validRuntimes = @("win-x64", "win-arm64", "linux-x64", "linux-arm64", "osx-x64", "osx-arm64")
if ($Runtime -notin $validRuntimes) {
    Write-Host "Invalid runtime. Valid options: $($validRuntimes -join ', ')" -ForegroundColor Red
    exit 1
}

# Create output directory
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir | Out-Null
}

# Build
Write-Host "Restoring dependencies..." -ForegroundColor Yellow
dotnet restore TexasHoldem/TexasHoldem.csproj

Write-Host "Publishing self-contained executable..." -ForegroundColor Yellow
dotnet publish TexasHoldem/TexasHoldem.csproj `
    --configuration Release `
    --runtime $Runtime `
    --output "$OutputDir/$Runtime" `
    -p:Version=$Version `
    -p:InformationalVersion=$Version

# Determine output file name
$ext = if ($Runtime -like "win-*") { ".exe" } else { "" }
$sourceFile = "$OutputDir/$Runtime/TexasHoldem$ext"
$destFile = "$OutputDir/poker-cli-$Runtime$ext"

if (Test-Path $sourceFile) {
    Move-Item -Path $sourceFile -Destination $destFile -Force
    Write-Host "Created: $destFile" -ForegroundColor Green
} else {
    Write-Host "Build output not found at: $sourceFile" -ForegroundColor Red
    exit 1
}

# Clean up intermediate files
Remove-Item -Path "$OutputDir/$Runtime" -Recurse -Force -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "Build complete!" -ForegroundColor Green
Write-Host "Executable: $destFile" -ForegroundColor Cyan
Write-Host ""
Write-Host "To build for other platforms:" -ForegroundColor Yellow
Write-Host "  .\build-release.ps1 -Runtime linux-x64"
Write-Host "  .\build-release.ps1 -Runtime osx-arm64"
