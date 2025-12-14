# CspReport IIS Deployment Guide

This guide provides step-by-step instructions for deploying the CspReport application to IIS.

---

## Prerequisites

Before deploying, ensure you have:

1. **Windows Server 2016+ or Windows 10/11** with IIS installed
2. **.NET 10 Hosting Bundle** installed on the target server
3. **Administrator privileges** on the server
4. **PowerShell 5.1 or later**

### Installing IIS (if not already installed)

1. Open **Server Manager** (Windows Server) or **Control Panel** (Windows 10/11)
2. Navigate to **Add Roles and Features** (Server) or **Turn Windows features on or off** (Desktop)
3. Enable the following:
   - Internet Information Services
   - Web Management Tools → IIS Management Console
   - World Wide Web Services → Application Development Features → WebSocket Protocol
   - World Wide Web Services → Common HTTP Features (all)
   - World Wide Web Services → Health and Diagnostics → HTTP Logging

4. Click **OK/Install** and restart if prompted

### Installing .NET 10 Hosting Bundle

1. Download the **ASP.NET Core 10.0 Runtime - Windows Hosting Bundle** from:
   ```
   https://dotnet.microsoft.com/download/dotnet/10.0
   ```

2. Run the installer: `dotnet-hosting-10.0.x-win.exe`

3. After installation, restart IIS:
   ```powershell
   iisreset
   ```

4. Verify installation:
   ```powershell
   dotnet --list-runtimes
   ```
   You should see `Microsoft.AspNetCore.App 10.0.x` in the list.

---

## Deployment Methods

### Method 1: Automated Deployment (Recommended)

This is the easiest method using the provided PowerShell script.

#### Step 1: Open PowerShell as Administrator

Right-click **PowerShell** and select **Run as Administrator**

#### Step 2: Navigate to Project Directory

```powershell
cd C:\path\to\CspReport
```

#### Step 3: Run Deployment Script

**Basic deployment** (default settings):
```powershell
.\deploy-iis.ps1
```

This will:
- Build and publish the application
- Create app pool "CspReportPool"
- Create website "CspReport" on port 80
- Deploy to `C:\inetpub\wwwroot\CspReport`
- Set proper permissions
- Start the site

#### Step 4: Verify Deployment

Open a browser and navigate to:
- Dashboard: `http://localhost/`
- Health check: `http://localhost/health`
- Report count: `http://localhost/csp/count`

---

### Method 2: Manual Deployment

If you prefer manual control or the script doesn't work for your environment.

#### Step 1: Build and Publish

Open PowerShell in the project directory:

```powershell
dotnet publish -c Release -o publish
```

#### Step 2: Create Deployment Directory

```powershell
New-Item -Path "C:\inetpub\wwwroot\CspReport" -ItemType Directory -Force
```

#### Step 3: Copy Files

```powershell
Copy-Item -Path "publish\*" -Destination "C:\inetpub\wwwroot\CspReport" -Recurse -Force
```

#### Step 4: Create Application Pool

Open **IIS Manager** (run `inetmgr`) or use PowerShell:

```powershell
Import-Module WebAdministration

# Create app pool
New-WebAppPool -Name "CspReportPool"

# Configure for .NET Core (no managed runtime)
Set-ItemProperty "IIS:\AppPools\CspReportPool" -Name "managedRuntimeVersion" -Value ""
Set-ItemProperty "IIS:\AppPools\CspReportPool" -Name "startMode" -Value "AlwaysRunning"
```

#### Step 5: Create Website

```powershell
New-Website -Name "CspReport" `
    -PhysicalPath "C:\inetpub\wwwroot\CspReport" `
    -ApplicationPool "CspReportPool" `
    -Port 80
```

#### Step 6: Set Permissions

```powershell
# Grant read/execute to IIS_IUSRS
$path = "C:\inetpub\wwwroot\CspReport"
$acl = Get-Acl $path
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
    "IIS_IUSRS", "ReadAndExecute", "ContainerInherit,ObjectInherit", "None", "Allow"
)
$acl.SetAccessRule($rule)
Set-Acl $path $acl

# Grant write permission on Logs folder
$logsPath = Join-Path $path "Logs"
New-Item -Path $logsPath -ItemType Directory -Force
$logsAcl = Get-Acl $logsPath
$logsRule = New-Object System.Security.AccessControl.FileSystemAccessRule(
    "IIS_IUSRS", "Modify", "ContainerInherit,ObjectInherit", "None", "Allow"
)
$logsAcl.SetAccessRule($logsRule)
Set-Acl $logsPath $logsAcl
```

#### Step 7: Start the Site

```powershell
Start-WebAppPool -Name "CspReportPool"
Start-Website -Name "CspReport"
```

#### Step 8: Verify

Browse to `http://localhost/health` - you should see `"ok"`

---

## Custom Deployment Options

## Custom Deployment Options

### Deploy to custom path:
```powershell
.\deploy-iis.ps1 -PhysicalPath "D:\websites\CspReport"
```

### Deploy on custom port:
```powershell
.\deploy-iis.ps1 -Port 8080
```

### Deploy with custom names:
```powershell
.\deploy-iis.ps1 -SiteName "MyCSPCollector" -AppPoolName "MyCSPPool"
```

### Deploy with HTTPS:
```powershell
.\deploy-iis.ps1 -UseHttps -HttpsPort 443 -CertThumbprint "YOUR_CERT_THUMBPRINT"
```

### Deploy with host header:
```powershell
.\deploy-iis.ps1 -HostHeader "csp.example.com" -Port 80
```

## Complete Example with All Options

```powershell
.\deploy-iis.ps1 `
    -SiteName "CspReport" `
    -AppPoolName "CspReportPool" `
    -PhysicalPath "C:\websites\CspReport" `
    -Port 80 `
    -HostHeader "csp.example.com" `
    -UseHttps `
    -HttpsPort 443 `
    -CertThumbprint "ABC123..."
```

## Post-Deployment

### Test the deployment:
```powershell
Invoke-RestMethod http://localhost/health
Invoke-RestMethod http://localhost/csp/count
```

### View IIS logs:
```powershell
Get-Content "C:\inetpub\wwwroot\CspReport\Logs\csp-report-uri.jsonl"
```

### Restart the site:
```powershell
Restart-WebAppPool -Name "CspReportPool"
Restart-Website -Name "CspReport"
```

## Troubleshooting

### Check app pool status:
```powershell
Get-WebAppPoolState -Name "CspReportPool"
```

### Check website status:
```powershell
Get-Website -Name "CspReport"
```

### View IIS logs (if enabled):
```powershell
Get-Content "C:\inetpub\wwwroot\CspReport\logs\stdout*.log"
```

### Enable detailed logging in web.config:
Change `stdoutLogEnabled="false"` to `stdoutLogEnabled="true"`

---

## Configuring HTTPS (Production)

For production deployments, you should use HTTPS.

### Option 1: Using the Deployment Script

```powershell
.\deploy-iis.ps1 -UseHttps -HttpsPort 443 -CertThumbprint "YOUR_CERT_THUMBPRINT"
```

To find your certificate thumbprint:
```powershell
Get-ChildItem -Path Cert:\LocalMachine\My | Format-List
```

### Option 2: Manual HTTPS Configuration

1. In IIS Manager, select your site
2. Click **Bindings** in the Actions panel
3. Click **Add**
4. Select **https** as type
5. Choose port **443**
6. Select your SSL certificate
7. Click **OK**

---

## Updating an Existing Deployment

When you make code changes and need to redeploy:

### Using the Script

Simply run the deployment script again - it will stop the site, update files, and restart:

```powershell
.\deploy-iis.ps1
```

### Manual Update

```powershell
# Stop the app pool
Stop-WebAppPool -Name "CspReportPool"

# Publish and copy new files
dotnet publish -c Release -o publish
Copy-Item -Path "publish\*" -Destination "C:\inetpub\wwwroot\CspReport" -Recurse -Force

# Start the app pool
Start-WebAppPool -Name "CspReportPool"
```

---

## Monitoring and Maintenance

### Viewing CSP Reports

**Dashboard (Web UI):**
```
http://your-server/
```

**API Endpoints:**
```powershell
# Get total count
Invoke-RestMethod http://your-server/csp/count

# Get latest 20 reports
Invoke-RestMethod http://your-server/csp/reports

# Get specific range
Invoke-RestMethod "http://your-server/csp/reports?skip=10&take=5"
```

**Direct Log File Access:**
```powershell
Get-Content "C:\inetpub\wwwroot\CspReport\Logs\csp-report-uri.jsonl" -Tail 20
```

### Restarting the Application

```powershell
Restart-WebAppPool -Name "CspReportPool"
```

### Checking Application Status

```powershell
# Check app pool
Get-WebAppPoolState -Name "CspReportPool"

# Check website
Get-Website -Name "CspReport"

# Check bindings
Get-WebBinding -Name "CspReport"
```

---

## Troubleshooting

### Application Won't Start

**Check .NET Runtime:**
```powershell
dotnet --list-runtimes | Select-String "AspNetCore"
```
Ensure you have ASP.NET Core 10.0 installed.

**Check App Pool Status:**
```powershell
Get-WebAppPoolState -Name "CspReportPool"
```

**Enable Detailed Logging:**

Edit `C:\inetpub\wwwroot\CspReport\web.config`:
```xml
<aspNetCore processPath="dotnet" 
            arguments=".\CspReport.dll" 
            stdoutLogEnabled="true"    <!-- Change to true -->
            stdoutLogFile=".\logs\stdout" 
            hostingModel="inprocess" />
```

Then check logs:
```powershell
Get-Content "C:\inetpub\wwwroot\CspReport\logs\stdout*.log"
```

### Permission Errors

If you see "Access Denied" errors in logs:

```powershell
# Reset permissions on deployment folder
$path = "C:\inetpub\wwwroot\CspReport"
icacls $path /grant "IIS_IUSRS:(OI)(CI)RX" /T

# Ensure Logs folder has write permissions
icacls "$path\Logs" /grant "IIS_IUSRS:(OI)(CI)M" /T
```

### Port Already in Use

If port 80 is already in use:

```powershell
# Deploy on a different port
.\deploy-iis.ps1 -Port 8080

# Or check what's using port 80
netstat -ano | findstr :80
```

### Can't Access from Other Machines

**Check Windows Firewall:**
```powershell
# Allow inbound on port 80
New-NetFirewallRule -DisplayName "CspReport HTTP" -Direction Inbound -Protocol TCP -LocalPort 80 -Action Allow
```

**For HTTPS (port 443):**
```powershell
New-NetFirewallRule -DisplayName "CspReport HTTPS" -Direction Inbound -Protocol TCP -LocalPort 443 -Action Allow
```

### Application Returns 500 Errors

1. Enable detailed errors temporarily in `appsettings.json`:
   ```json
   {
     "Logging": {
       "LogLevel": {
         "Default": "Debug",
         "Microsoft.AspNetCore": "Debug"
       }
     }
   }
   ```

2. Check Application Event Log:
   ```powershell
   Get-EventLog -LogName Application -Source "IIS AspNetCore Module V2" -Newest 10
   ```

---

## Security Best Practices

### File Permissions
- **Application files**: Read/Execute for IIS_IUSRS (already configured)
- **Logs folder**: Modify for IIS_IUSRS (already configured)
- **Configuration files**: Consider encrypting sensitive settings

### Network Security
- Use HTTPS in production (certificates required)
- Configure firewall rules to limit access
- Consider IP restrictions in IIS if needed

### Application Security
- Keep .NET runtime updated
- Monitor CSP reports regularly for suspicious activity
- Review log file size and implement rotation if needed
- Set appropriate `MaxBodyBytes` in `appsettings.json` to prevent large payloads

---

## Configuration Reference

### appsettings.json

Located at: `C:\inetpub\wwwroot\CspReport\appsettings.json`

```json
{
  "CspReport": {
    "LogDirectory": "Logs",
    "FileName": "csp-report-uri.jsonl",
    "MaxBodyBytes": 262144
  }
}
```

**Settings:**
- `LogDirectory`: Where to store log files (relative to app root)
- `FileName`: Name of the JSONL log file
- `MaxBodyBytes`: Maximum size of incoming CSP reports (256KB default)

Changes to `appsettings.json` require an app pool restart:
```powershell
Restart-WebAppPool -Name "CspReportPool"
```

---

## Uninstalling

To completely remove the deployment:

```powershell
# Stop and remove website
Stop-Website -Name "CspReport"
Remove-Website -Name "CspReport"

# Stop and remove app pool
Stop-WebAppPool -Name "CspReportPool"
Remove-WebAppPool -Name "CspReportPool"

# Delete files
Remove-Item -Path "C:\inetpub\wwwroot\CspReport" -Recurse -Force
```

---

## Getting Help

- **GitHub Issues**: https://github.com/dstelow/CspReport/issues
- **Documentation**: See README.md in the repository
- **IIS Logs**: Check Application Event Viewer for detailed errors
