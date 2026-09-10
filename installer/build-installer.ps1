# PowerShell Skript zum Erstellen des Installations-Assistenten
param (
    [switch]$SkipAppBuild
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$ProjectDir = Split-Path -Parent $ScriptDir

Write-Host ""
Write-Host "=======================================================" -ForegroundColor Cyan
Write-Host "   SmartHome Kiosk - Installer Builder" -ForegroundColor Cyan
Write-Host "=======================================================" -ForegroundColor Cyan
Write-Host ""

# 1. Kiosk App kompilieren (falls nicht uebersprungen)
if (-not $SkipAppBuild) {
    Write-Host "[1/3] Kompiliere neueste SmartHomeKiosk.exe Release..." -ForegroundColor Yellow
    & "$ProjectDir\build.ps1"
} else {
    Write-Host "[1/3] Ueberspringe App-Build (bereits aktuell)..." -ForegroundColor Gray
}

# 2. Inno Setup Compiler (ISCC.exe) suchen
Write-Host "`n[2/3] Suche Inno Setup Compiler (ISCC.exe)..." -ForegroundColor Yellow
$isccPaths = @(
    "$env:LocalAppData\Programs\Inno Setup 6\ISCC.exe",
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe"
)

$iscc = $null
foreach ($path in $isccPaths) {
    if (Test-Path $path) {
        $iscc = $path
        break
    }
}

if (-not $iscc) {
    $cmd = Get-Command "iscc" -ErrorAction SilentlyContinue
    if ($cmd) { $iscc = $cmd.Source }
}

if (-not $iscc) {
    Write-Error "Inno Setup Compiler (ISCC.exe) nicht gefunden!"
    exit 1
}

Write-Host "ISCC gefunden: $iscc" -ForegroundColor Green

# 3. WebView2 Bootstrapper & Icon pruefen
$bootstrapper = Join-Path $ScriptDir "MicrosoftEdgeWebview2Setup.exe"
if (-not (Test-Path $bootstrapper)) {
    Write-Host "Lade WebView2 Evergreen Bootstrapper herunter..." -ForegroundColor Yellow
    Invoke-WebRequest -Uri "https://go.microsoft.com/fwlink/p/?LinkId=2124703" -OutFile $bootstrapper
}

$iconPath = Join-Path $ScriptDir "app.ico"
if (-not (Test-Path $iconPath)) {
    Copy-Item "$ProjectDir\app.ico" $iconPath -Force
}

# 4. Installer kompilieren
$issFile = Join-Path $ScriptDir "SmartHomeKioskSetup.iss"
Write-Host "`n[3/3] Erstelle SmartHomeKiosk-Setup.exe..." -ForegroundColor Yellow

& $iscc $issFile

$distSetup = Join-Path $ProjectDir "dist\SmartHomeKiosk-Setup.exe"
if (Test-Path $distSetup) {
    $sizeMb = [math]::Round(((Get-Item $distSetup).Length / 1MB), 2)
    Write-Host ""
    Write-Host "=======================================================" -ForegroundColor Green
    Write-Host " [ERFOLG] Installationsassistent fertiggestellt!" -ForegroundColor Green
    Write-Host " Datei: $distSetup ($sizeMb MB)" -ForegroundColor Green
    Write-Host "=======================================================" -ForegroundColor Green
    Write-Host ""
} else {
    Write-Error "Installer konnte nicht erstellt werden."
    exit 1
}
