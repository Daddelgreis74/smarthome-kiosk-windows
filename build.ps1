# Build- und Publish-Skript fuer SmartHome Kiosk Windows
param (
    [switch]$Run
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition

Write-Host ""
Write-Host "=======================================================" -ForegroundColor Cyan
Write-Host "   SmartHome Kiosk (Windows) - Build & Publish" -ForegroundColor Cyan
Write-Host "=======================================================" -ForegroundColor Cyan
Write-Host ""

# 1. Pruefen, ob dotnet verfuegbar ist
$dotnetCmd = Get-Command "dotnet" -ErrorAction SilentlyContinue
if (-not $dotnetCmd) {
    if (Test-Path "$env:LocalAppData\Microsoft\dotnet\dotnet.exe") {
        $env:Path = "$env:LocalAppData\Microsoft\dotnet;$env:Path"
    } elseif (Test-Path "C:\Program Files\dotnet\dotnet.exe") {
        $env:Path = "C:\Program Files\dotnet;$env:Path"
    } else {
        Write-Error ".NET SDK nicht gefunden! Bitte installiere .NET 8 SDK."
        exit 1
    }
}

# 2. Veroeffentlichen als eigenstaendige Single-File-EXE
$PublishDir = Join-Path $ScriptDir "publish"
Write-Host "Erstelle Single-File-Release in: $PublishDir ..." -ForegroundColor Yellow

dotnet publish "$ScriptDir\SmartHomeKiosk.csproj" `
    -c Release `
    -r win-x64 `
    --self-contained `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o "$PublishDir"

Write-Host ""
Write-Host "[ERFOLG] Die Kiosk-App wurde erfolgreich gebaut!" -ForegroundColor Green
Write-Host "Ausfuehrbare Datei: $PublishDir\SmartHomeKiosk.exe" -ForegroundColor Green
Write-Host ""

if ($Run) {
    Write-Host "Starte Kiosk-App..." -ForegroundColor Cyan
    Start-Process (Join-Path $PublishDir "SmartHomeKiosk.exe")
}
