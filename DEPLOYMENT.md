# CspReport IIS Deployment - Quick Reference

## Basic Deployment

Run as Administrator:

```powershell
.\deploy-iis.ps1
```

This will:
- Build and publish the application
- Create app pool "CspReportPool"
- Create website "CspReport" on port 80
- Deploy to `C:\inetpub\wwwroot\CspReport`
- Set proper permissions

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

## Prerequisites

- Windows Server or Windows 10/11 with IIS installed
- .NET 10 Hosting Bundle installed
- Administrator privileges
- PowerShell 5.1 or later

## Security Notes

- The script sets minimum required permissions (IIS_IUSRS)
- Logs folder gets write permissions for the app to function
- Consider using HTTPS in production
- Review and adjust permissions based on your security requirements
