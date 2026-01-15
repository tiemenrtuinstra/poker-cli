# Build All Platforms Script for Poker CLI
# Creates self-contained executables for all supported platforms

param(
    [string]$Version = "1.2.7",
    [string]$OutputDir = "./release"
)

$ErrorActionPreference = "Stop"

$platforms = @(
    "win-x64",
    "linux-x64",
    "linux-arm64",
    "osx-x64",
    "osx-arm64"
)

Write-Host "Building Poker CLI v$Version for all platforms..." -ForegroundColor Cyan
Write-Host ""

# Create output directory
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir | Out-Null
}

# Restore once
Write-Host "Restoring dependencies..." -ForegroundColor Yellow
dotnet restore TexasHoldem/TexasHoldem.csproj

$successful = @()
$failed = @()

foreach ($runtime in $platforms) {
    Write-Host ""
    Write-Host "Building for $runtime..." -ForegroundColor Cyan

    try {
        dotnet publish TexasHoldem/TexasHoldem.csproj `
            --configuration Release `
            --runtime $runtime `
            --output "$OutputDir/$runtime" `
            -p:Version=$Version `
            -p:InformationalVersion=$Version `
            --no-restore

        $ext = if ($runtime -like "win-*") { ".exe" } else { "" }
        $sourceFile = "$OutputDir/$runtime/TexasHoldem$ext"
        $destFile = "$OutputDir/poker-cli-$runtime$ext"

        if (Test-Path $sourceFile) {
            Move-Item -Path $sourceFile -Destination $destFile -Force
            Remove-Item -Path "$OutputDir/$runtime" -Recurse -Force -ErrorAction SilentlyContinue
            $successful += $runtime
            Write-Host "  Success: $destFile" -ForegroundColor Green
        } else {
            $failed += $runtime
            Write-Host "  Failed: Output not found" -ForegroundColor Red
        }
    }
    catch {
        $failed += $runtime
        Write-Host "  Failed: $_" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Build Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Successful: $($successful.Count)" -ForegroundColor Green
foreach ($p in $successful) {
    Write-Host "  - $p" -ForegroundColor Green
}

if ($failed.Count -gt 0) {
    Write-Host ""
    Write-Host "Failed: $($failed.Count)" -ForegroundColor Red
    foreach ($p in $failed) {
        Write-Host "  - $p" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "Output directory: $OutputDir" -ForegroundColor Cyan
Get-ChildItem $OutputDir -File | ForEach-Object { Write-Host "  $($_.Name)" -ForegroundColor White }
