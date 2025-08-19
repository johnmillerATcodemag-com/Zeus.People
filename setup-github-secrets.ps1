# Quick GitHub Secrets Setup Script
# This script will help you quickly add the required secrets to GitHub

Write-Host "🚀 GitHub Secrets Quick Setup" -ForegroundColor Green
Write-Host "=============================" -ForegroundColor Green

Write-Host "`n📋 Required GitHub Secrets:" -ForegroundColor Yellow

$secrets = @{
    "AZURE_CREDENTIALS"              = @'
{
  "clientId": "a90252fe-4d36-4e18-8a85-7e8ecbf04ed0",
  "clientSecret": "[REDACTED]",
  "subscriptionId": "5232b409-b25e-441c-9951-16e69069f224",
  "tenantId": "24db396b-b795-45c9-bcfa-d3559193f2f7",
  "activeDirectoryEndpointUrl": "https://login.microsoftonline.com",
  "resourceManagerEndpointUrl": "https://management.azure.com/",
  "activeDirectoryGraphResourceId": "https://graph.windows.net/",
  "sqlManagementEndpointUrl": "https://management.core.windows.net:8443/",
  "galleryEndpointUrl": "https://gallery.azure.com/",
  "managementEndpointUrl": "https://management.core.windows.net/"
}
'@
    "AZURE_CLIENT_ID"                = "a90252fe-4d36-4e18-8a85-7e8ecbf04ed0"
    "AZURE_CLIENT_SECRET"            = "[REDACTED]"
    "AZURE_TENANT_ID"                = "24db396b-b795-45c9-bcfa-d3559193f2f7"
    "MANAGED_IDENTITY_CLIENT_ID"     = "PLACEHOLDER_UPDATE_AFTER_INFRASTRUCTURE_DEPLOYMENT"
    "APP_INSIGHTS_CONNECTION_STRING" = "PLACEHOLDER_UPDATE_AFTER_INFRASTRUCTURE_DEPLOYMENT"
}

foreach ($secretName in $secrets.Keys) {
    $secretValue = $secrets[$secretName]
    Write-Host "`n🔐 $secretName" -ForegroundColor Cyan
    Write-Host "Value to copy:" -ForegroundColor Gray
    
    if ($secretName -eq "AZURE_CREDENTIALS") {
        Write-Host $secretValue -ForegroundColor DarkGray
    }
    else {
        Write-Host $secretValue -ForegroundColor DarkGray
    }
    
    # Copy to clipboard for convenience
    if ($secretName -eq "AZURE_CLIENT_ID") {
        Write-Host "✅ Copied to clipboard!" -ForegroundColor Green
        $secretValue | Set-Clipboard
        Read-Host "Press Enter to continue to next secret"
    }
}

Write-Host "`n🌐 Opening GitHub Secrets Page..." -ForegroundColor Yellow
Start-Process "https://github.com/johnmillerATcodemag-com/Zeus.People/settings/secrets/actions"

Write-Host "`n📝 Instructions:" -ForegroundColor Green
Write-Host "1. The GitHub secrets page should now be open in your browser" -ForegroundColor White
Write-Host "2. Click 'New repository secret' for each secret above" -ForegroundColor White
Write-Host "3. Copy the Name and Value exactly as shown" -ForegroundColor White
Write-Host "4. Use 'PLACEHOLDER...' values for the last two secrets" -ForegroundColor White
Write-Host "5. After infrastructure deploys, update the placeholder values" -ForegroundColor White

Write-Host "`n🎯 Priority Order:" -ForegroundColor Yellow
Write-Host "1. Add the 4 core Azure secrets first" -ForegroundColor White
Write-Host "2. Add the 2 placeholder secrets" -ForegroundColor White
Write-Host "3. Run the pipeline (it should now work)" -ForegroundColor White
Write-Host "4. Update placeholder values after infrastructure deployment" -ForegroundColor White

Write-Host "`n✨ After adding secrets, your pipeline should complete successfully!" -ForegroundColor Green
