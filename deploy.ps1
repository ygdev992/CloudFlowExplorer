# XrmToolBox Plugin Build & Deploy Script
# Builds and deploys Cloud Flow Explorer to XrmToolBox

param(
    [switch]$SkipBuild,
    [switch]$Force,
    [switch]$WhatIf
)

$ErrorActionPreference = "Stop"

# File list - plugin DLL/PDB
$pluginFiles = @(
    "CloudFlowExplorer.dll",
    "CloudFlowExplorer.pdb"
)

Write-Host "`n=== Cloud Flow Explorer Deployment ===" -ForegroundColor Cyan

# Check if XrmToolBox is running
Write-Host "`nChecking for running XrmToolBox processes..." -ForegroundColor Yellow
$xrmProcesses = Get-Process -Name "XrmToolBox" -ErrorAction SilentlyContinue

if ($xrmProcesses) {
    Write-Host "  WARNING: XrmToolBox is currently running!" -ForegroundColor Red
    Write-Host "  Found $($xrmProcesses.Count) process(es) with PID(s): $($xrmProcesses.Id -join ', ')" -ForegroundColor Yellow

    if ($Force) {
        Write-Host "  -Force specified, attempting to close XrmToolBox..." -ForegroundColor Yellow
        foreach ($proc in $xrmProcesses) {
            try {
                $proc.CloseMainWindow() | Out-Null
                Start-Sleep -Milliseconds 500
                if (!$proc.HasExited) {
                    $proc.Kill()
                }
                Write-Host "  Closed process $($proc.Id)" -ForegroundColor Green
            }
            catch {
                Write-Host "  Failed to close process $($proc.Id): $($_.Exception.Message)" -ForegroundColor Red
            }
        }
        Start-Sleep -Seconds 2
    }
    else {
        Write-Host "`n  Please close XrmToolBox before deploying, or use -Force to close automatically." -ForegroundColor Yellow
        Write-Host "  Press Ctrl+C to cancel, or press Enter to continue anyway..." -ForegroundColor Gray
        Read-Host
    }
}
else {
    Write-Host "  No running XrmToolBox processes found." -ForegroundColor Green
}

# Build
if (-not $SkipBuild) {
    Write-Host "`nBuilding plugin..." -ForegroundColor Green

    if ($WhatIf) {
        Write-Host "  [WhatIf] Would run: dotnet clean" -ForegroundColor Gray
        Write-Host "  [WhatIf] Would run: dotnet build" -ForegroundColor Gray
    }
    else {
        dotnet clean CloudFlowExplorer.sln --configuration Release --verbosity quiet
        dotnet build CloudFlowExplorer.sln --configuration Release

        if ($LASTEXITCODE -ne 0) {
            Write-Host "`nBuild failed!" -ForegroundColor Red
            exit 1
        }
        Write-Host "Build successful!" -ForegroundColor Green
    }
}
else {
    Write-Host "`nSkipping build (using existing binaries)..." -ForegroundColor Yellow
}

# Verify build output exists
$buildPath = "bin\Release\net48"
if (-not $WhatIf -and -not (Test-Path "$buildPath\CloudFlowExplorer.dll")) {
    Write-Host "`nError: Build output not found at $buildPath\CloudFlowExplorer.dll" -ForegroundColor Red
    exit 1
}

# Deployment path
Write-Host "`nDetecting deployment path..." -ForegroundColor Green
$pluginsPath = "$env:APPDATA\MscrmTools\XrmToolBox\Plugins"
Write-Host "  Target: $pluginsPath" -ForegroundColor White

if ($WhatIf) {
    Write-Host "`n[WhatIf] Would ensure folder exists: $pluginsPath" -ForegroundColor Gray
    foreach ($file in $pluginFiles) {
        Write-Host "  [WhatIf] Would copy: $buildPath\$file -> $pluginsPath\$file" -ForegroundColor Gray
    }
}
else {
    New-Item -ItemType Directory -Force -Path $pluginsPath | Out-Null

    foreach ($file in $pluginFiles) {
        $source = Join-Path $buildPath $file
        if (Test-Path $source) {
            Copy-Item -Path $source -Destination $pluginsPath -Force
            Write-Host "  Copied $file" -ForegroundColor Green
        }
        else {
            Write-Host "  Skipped $file (not found)" -ForegroundColor Yellow
        }
    }

    Write-Host "`nDeployment complete." -ForegroundColor Cyan
}

if ($Force -and $xrmProcesses -and -not $WhatIf) {
    Write-Host "`nRelaunching XrmToolBox..." -ForegroundColor Green
    $xrmExe = Get-Command "XrmToolBox.exe" -ErrorAction SilentlyContinue
    if ($xrmExe) {
        Start-Process $xrmExe.Source
    }
    else {
        Write-Host "  Could not auto-locate XrmToolBox.exe; please relaunch manually." -ForegroundColor Yellow
    }
}
