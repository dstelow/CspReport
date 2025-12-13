# CspReport IIS Deployment Script
# Run this script as Administrator

param(
    [string]$SiteName = "CspReport",
    [string]$AppPoolName = "CspReportPool",
    [string]$PhysicalPath = "C:\inetpub\wwwroot\CspReport",
    [int]$Port = 80,
    [string]$HostHeader = "",
    [switch]$UseHttps,
    [int]$HttpsPort = 443,
    [string]$CertThumbprint = ""
)

# Ensure running as administrator
if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Error "This script must be run as Administrator"
    exit 1
}

Write-Host "Starting CspReport deployment to IIS..." -ForegroundColor Cyan

# Import WebAdministration module
Import-Module WebAdministration -ErrorAction Stop

# Step 1: Build and publish the application
Write-Host "`n[1/7] Building and publishing application..." -ForegroundColor Yellow
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
Push-Location $scriptPath

try {
    dotnet publish -c Release -o publish --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw "Publish failed with exit code $LASTEXITCODE"
    }
    Write-Host "✓ Application published successfully" -ForegroundColor Green
}
catch {
    Write-Error "Failed to publish application: $_"
    Pop-Location
    exit 1
}
finally {
    Pop-Location
}

# Step 2: Create physical directory
Write-Host "`n[2/7] Creating physical directory..." -ForegroundColor Yellow
if (-not (Test-Path $PhysicalPath)) {
    New-Item -Path $PhysicalPath -ItemType Directory -Force | Out-Null
    Write-Host "✓ Created directory: $PhysicalPath" -ForegroundColor Green
}
else {
    Write-Host "✓ Directory already exists: $PhysicalPath" -ForegroundColor Green
}

# Step 3: Stop existing site and app pool if they exist
Write-Host "`n[3/7] Stopping existing site and app pool (if exists)..." -ForegroundColor Yellow
if (Test-Path "IIS:\Sites\$SiteName") {
    Stop-Website -Name $SiteName -ErrorAction SilentlyContinue
    Write-Host "✓ Stopped existing website" -ForegroundColor Green
}
if (Test-Path "IIS:\AppPools\$AppPoolName") {
    Stop-WebAppPool -Name $AppPoolName -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
    Write-Host "✓ Stopped existing app pool" -ForegroundColor Green
}

# Step 4: Copy files to IIS directory
Write-Host "`n[4/7] Copying files to IIS directory..." -ForegroundColor Yellow
Push-Location $scriptPath
try {
    Copy-Item -Path "publish\*" -Destination $PhysicalPath -Recurse -Force
    Write-Host "✓ Files copied successfully" -ForegroundColor Green
}
catch {
    Write-Error "Failed to copy files: $_"
    Pop-Location
    exit 1
}
finally {
    Pop-Location
}

# Step 5: Create/update application pool
Write-Host "`n[5/7] Configuring application pool..." -ForegroundColor Yellow
if (Test-Path "IIS:\AppPools\$AppPoolName") {
    Write-Host "✓ Application pool already exists" -ForegroundColor Green
}
else {
    New-WebAppPool -Name $AppPoolName | Out-Null
    Write-Host "✓ Created application pool: $AppPoolName" -ForegroundColor Green
}

# Configure app pool for .NET Core (no managed runtime)
Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name "managedRuntimeVersion" -Value ""
Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name "startMode" -Value "AlwaysRunning"
Write-Host "✓ Configured app pool settings" -ForegroundColor Green

# Step 6: Create/update website
Write-Host "`n[6/7] Configuring website..." -ForegroundColor Yellow
if (Test-Path "IIS:\Sites\$SiteName") {
    Remove-Website -Name $SiteName -ErrorAction Stop
    Write-Host "✓ Removed existing website" -ForegroundColor Green
}

$bindings = @()
if ($UseHttps -and $CertThumbprint) {
    $bindings += @{protocol='https'; bindingInformation="*:${HttpsPort}:${HostHeader}"; certificateThumbprint=$CertThumbprint; certificateStoreName='My'}
    Write-Host "✓ HTTPS binding configured with certificate" -ForegroundColor Green
}
else {
    $bindings += @{protocol='http'; bindingInformation="*:${Port}:${HostHeader}"}
    Write-Host "✓ HTTP binding configured" -ForegroundColor Green
}

New-Website -Name $SiteName `
    -PhysicalPath $PhysicalPath `
    -ApplicationPool $AppPoolName `
    -Port $Port `
    -HostHeader $HostHeader | Out-Null

Write-Host "✓ Created website: $SiteName" -ForegroundColor Green

# Add HTTPS binding if specified
if ($UseHttps -and $CertThumbprint) {
    New-WebBinding -Name $SiteName -Protocol https -Port $HttpsPort -HostHeader $HostHeader
    $binding = Get-WebBinding -Name $SiteName -Protocol https
    $binding.AddSslCertificate($CertThumbprint, "My")
    Write-Host "✓ HTTPS binding added" -ForegroundColor Green
}

# Step 7: Set permissions
Write-Host "`n[7/7] Setting permissions..." -ForegroundColor Yellow

# Grant IIS_IUSRS read/execute on root
$acl = Get-Acl $PhysicalPath
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule("IIS_IUSRS", "ReadAndExecute", "ContainerInherit,ObjectInherit", "None", "Allow")
$acl.SetAccessRule($rule)
Set-Acl $PhysicalPath $acl
Write-Host "✓ Set read/execute permissions for IIS_IUSRS" -ForegroundColor Green

# Grant IIS_IUSRS write permission on Logs folder
$logsPath = Join-Path $PhysicalPath "Logs"
if (-not (Test-Path $logsPath)) {
    New-Item -Path $logsPath -ItemType Directory -Force | Out-Null
}
$logsAcl = Get-Acl $logsPath
$logsRule = New-Object System.Security.AccessControl.FileSystemAccessRule("IIS_IUSRS", "Modify", "ContainerInherit,ObjectInherit", "None", "Allow")
$logsAcl.SetAccessRule($logsRule)
Set-Acl $logsPath $logsAcl
Write-Host "✓ Set write permissions for Logs folder" -ForegroundColor Green

# Start the site
Start-WebAppPool -Name $AppPoolName
Start-Website -Name $SiteName
Write-Host "`n✓ Website started successfully" -ForegroundColor Green

# Display summary
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Deployment Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Site Name:        $SiteName" -ForegroundColor White
Write-Host "App Pool:         $AppPoolName" -ForegroundColor White
Write-Host "Physical Path:    $PhysicalPath" -ForegroundColor White
Write-Host "HTTP URL:         http://localhost:$Port/" -ForegroundColor White
if ($UseHttps) {
    Write-Host "HTTPS URL:        https://localhost:$HttpsPort/" -ForegroundColor White
}
Write-Host "`nTest endpoints:" -ForegroundColor Cyan
Write-Host "  Health:         http://localhost:$Port/health" -ForegroundColor White
Write-Host "  Count:          http://localhost:$Port/csp/count" -ForegroundColor White
Write-Host "  Reports:        http://localhost:$Port/csp/reports" -ForegroundColor White
Write-Host "`nLogs location:    $logsPath" -ForegroundColor White
Write-Host "========================================`n" -ForegroundColor Cyan
