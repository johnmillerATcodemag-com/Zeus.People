#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Batch setup of Key Vault secrets for staging environment

.DESCRIPTION
    Sets up the required Key Vault secrets for Zeus.People Academic Management System
    staging environment using known configuration values.
#>

# Configuration values
$keyVaultName = "kv2ymnmfmrvsb3w"
$tenantId = "24db396b-b795-45c9-bcfa-d3559193f2f7"
$appInsightsConnectionString = "InstrumentationKey=8b53209c-bc3e-49f7-9e7f-1e54a399f2fd;IngestionEndpoint=https://westus2-2.in.applicationinsights.azure.com/;LiveEndpoint=https://westus2.livediagnostics.monitor.azure.com/;ApplicationId=aa74245f-8e8f-4489-9131-bf7f4904dab6"
$appInsightsInstrumentationKey = "8b53209c-bc3e-49f7-9e7f-1e54a399f2fd"

Write-Host "🔐 Setting up Key Vault secrets for staging environment..." -ForegroundColor Cyan
Write-Host "Key Vault: $keyVaultName" -ForegroundColor Yellow
Write-Host ""

# Function to set secret safely
function Set-KeyVaultSecret {
    param(
        [string]$SecretName,
        [string]$SecretValue,
        [string]$Description
    )
    
    Write-Host "Setting $Description..." -ForegroundColor Yellow
    
    if ([string]::IsNullOrEmpty($SecretValue)) {
        Write-Host "  ⏭️ Skipping $SecretName (no value provided)" -ForegroundColor Gray
        return $false
    }
    
    try {
        $result = az keyvault secret set --vault-name $keyVaultName --name $SecretName --value $SecretValue --query "id" -o tsv 2>$null
        
        if ($LASTEXITCODE -eq 0 -and $result) {
            Write-Host "  ✅ Successfully set $SecretName" -ForegroundColor Green
            return $true
        }
        else {
            Write-Host "  ❌ Failed to set $SecretName" -ForegroundColor Red
            return $false
        }
    }
    catch {
        Write-Host "  ❌ Error setting $SecretName`: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Generate secure JWT secret
$jwtSecret = [System.Web.Security.Membership]::GeneratePassword(64, 16)
Write-Host "✅ Generated secure JWT secret" -ForegroundColor Green

# Set the secrets
$secrets = @{
    "JwtSettings--SecretKey"                  = @{ Value = $jwtSecret; Description = "JWT Secret Key" }
    "AzureAd--TenantId"                       = @{ Value = $tenantId; Description = "Azure AD Tenant ID" }
    "ApplicationInsights--ConnectionString"   = @{ Value = $appInsightsConnectionString; Description = "Application Insights Connection String" }
    "ApplicationInsights--InstrumentationKey" = @{ Value = $appInsightsInstrumentationKey; Description = "Application Insights Instrumentation Key" }
}

Write-Host ""
$successCount = 0
$totalCount = $secrets.Count

foreach ($secretName in $secrets.Keys) {
    $secretInfo = $secrets[$secretName]
    if (Set-KeyVaultSecret -SecretName $secretName -SecretValue $secretInfo.Value -Description $secretInfo.Description) {
        $successCount++
    }
}

Write-Host ""
Write-Host "📊 Results: $successCount/$totalCount secrets configured successfully" -ForegroundColor $(if ($successCount -eq $totalCount) { "Green" } else { "Yellow" })

if ($successCount -eq $totalCount) {
    Write-Host "🎉 All secrets configured successfully!" -ForegroundColor Green
}
elseif ($successCount -gt 0) {
    Write-Host "⚠️ Some secrets were configured. Please review any failures above." -ForegroundColor Yellow
}
else {
    Write-Host "❌ No secrets were configured. Please check Key Vault permissions." -ForegroundColor Red
}

Write-Host ""
Write-Host "📋 Configured secrets:" -ForegroundColor Cyan
foreach ($secretName in $secrets.Keys) {
    $displayName = $secretName -replace '--', ':'
    Write-Host "  • $displayName" -ForegroundColor Gray
}

Write-Host ""
Write-Host "🔍 Note: Database connection strings and Azure AD client credentials" -ForegroundColor Yellow
Write-Host "need to be configured separately with your actual values." -ForegroundColor Yellow
