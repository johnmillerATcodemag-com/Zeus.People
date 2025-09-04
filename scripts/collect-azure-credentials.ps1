# Azure Credentials Collection Script for GitHub Actions
# Run this script after creating the service principal to collect all required values

param(
    [Parameter(Mandatory = $true)]
    [string]$SubscriptionId,
    
    [Parameter(Mandatory = $false)]
    [string]$ResourceGroupName = "rg-academic-staging-westus2",
    
    [Parameter(Mandatory = $false)]
    [string]$ManagedIdentityName = "managed-identity-academic-staging-2ymnmfmrvsb3w",
    
    [Parameter(Mandatory = $false)]
    [string]$AppInsightsName = "app-insights-academic-staging-2ymnmfmrvsb3w"
)

Write-Host "🔑 Collecting Azure Credentials for GitHub Actions..." -ForegroundColor Green
Write-Host "=====================================================" -ForegroundColor Green

# Check if logged in to Azure
try {
    $currentAccount = az account show --query "user.name" --output tsv 2>$null
    if (-not $currentAccount) {
        throw "Not logged in"
    }
    Write-Host "✅ Logged in as: $currentAccount" -ForegroundColor Green
}
catch {
    Write-Host "❌ Please login to Azure CLI first: az login" -ForegroundColor Red
    exit 1
}

# Set subscription
Write-Host "📋 Setting subscription: $SubscriptionId" -ForegroundColor Yellow
az account set --subscription $SubscriptionId

# Get tenant ID
$tenantId = az account show --query tenantId --output tsv
Write-Host "✅ Tenant ID: $tenantId" -ForegroundColor Green

Write-Host "`n🏗️ Creating Service Principal for GitHub Actions..." -ForegroundColor Yellow
Write-Host "Note: If you already created the service principal, you can skip this step." -ForegroundColor Gray

# Create service principal (uncomment if needed)
# $spOutput = az ad sp create-for-rbac --name "GitHub-Actions-Zeus-People" --role contributor --scopes "/subscriptions/$SubscriptionId" --sdk-auth
# $sp = $spOutput | ConvertFrom-Json

Write-Host "`n🔍 Collecting Application-Specific Values..." -ForegroundColor Yellow

# Get Managed Identity Client ID
try {
    $managedIdentityClientId = az identity show --resource-group $ResourceGroupName --name $ManagedIdentityName --query clientId --output tsv 2>$null
    if ($managedIdentityClientId) {
        Write-Host "✅ Managed Identity Client ID: $managedIdentityClientId" -ForegroundColor Green
    }
    else {
        Write-Host "⚠️ Managed Identity not found. You may need to deploy infrastructure first." -ForegroundColor Yellow
        $managedIdentityClientId = "DEPLOY_INFRASTRUCTURE_FIRST"
    }
}
catch {
    Write-Host "⚠️ Could not retrieve Managed Identity Client ID" -ForegroundColor Yellow
    $managedIdentityClientId = "DEPLOY_INFRASTRUCTURE_FIRST"
}

# Get Application Insights Connection String
try {
    $appInsightsConnectionString = az monitor app-insights component show --app $AppInsightsName --resource-group $ResourceGroupName --query connectionString --output tsv 2>$null
    if ($appInsightsConnectionString) {
        Write-Host "✅ Application Insights Connection String: $($appInsightsConnectionString.Substring(0,50))..." -ForegroundColor Green
    }
    else {
        Write-Host "⚠️ Application Insights not found. You may need to deploy infrastructure first." -ForegroundColor Yellow
        $appInsightsConnectionString = "DEPLOY_INFRASTRUCTURE_FIRST"
    }
}
catch {
    Write-Host "⚠️ Could not retrieve Application Insights Connection String" -ForegroundColor Yellow
    $appInsightsConnectionString = "DEPLOY_INFRASTRUCTURE_FIRST"
}

Write-Host "`n📝 GitHub Secrets Configuration" -ForegroundColor Green
Write-Host "=================================" -ForegroundColor Green

Write-Host "`nTo configure GitHub secrets, go to:" -ForegroundColor Yellow
Write-Host "https://github.com/johnmillerATcodemag-com/Zeus.People/settings/secrets/actions" -ForegroundColor Cyan

Write-Host "`n🔐 Required GitHub Secrets:" -ForegroundColor Yellow

Write-Host "`nAZURE_TENANT_ID:" -ForegroundColor White
Write-Host $tenantId -ForegroundColor Gray

if ($managedIdentityClientId -ne "DEPLOY_INFRASTRUCTURE_FIRST") {
    Write-Host "`nMANAGED_IDENTITY_CLIENT_ID:" -ForegroundColor White
    Write-Host $managedIdentityClientId -ForegroundColor Gray
}
else {
    Write-Host "`nMANAGED_IDENTITY_CLIENT_ID:" -ForegroundColor White
    Write-Host "⚠️ Deploy infrastructure first, then run this script again" -ForegroundColor Yellow
}

if ($appInsightsConnectionString -ne "DEPLOY_INFRASTRUCTURE_FIRST") {
    Write-Host "`nAPP_INSIGHTS_CONNECTION_STRING:" -ForegroundColor White
    Write-Host $appInsightsConnectionString -ForegroundColor Gray
}
else {
    Write-Host "`nAPP_INSIGHTS_CONNECTION_STRING:" -ForegroundColor White
    Write-Host "⚠️ Deploy infrastructure first, then run this script again" -ForegroundColor Yellow
}

Write-Host "`n⚠️ MANUAL STEP REQUIRED:" -ForegroundColor Red
Write-Host "You need to create the service principal manually and add these secrets:" -ForegroundColor Red
Write-Host "- AZURE_CREDENTIALS (JSON output from service principal creation)" -ForegroundColor Red
Write-Host "- AZURE_CLIENT_ID (from service principal)" -ForegroundColor Red
Write-Host "- AZURE_CLIENT_SECRET (from service principal)" -ForegroundColor Red

Write-Host "`n🚀 Service Principal Creation Command:" -ForegroundColor Yellow
Write-Host "az ad sp create-for-rbac --name `"GitHub-Actions-Zeus-People`" --role contributor --scopes `"/subscriptions/$SubscriptionId`" --sdk-auth" -ForegroundColor Cyan

Write-Host "`n📋 Summary of values collected:" -ForegroundColor Green
Write-Host "- Tenant ID: ✅" -ForegroundColor Green
Write-Host "- Managed Identity Client ID: $(if($managedIdentityClientId -ne 'DEPLOY_INFRASTRUCTURE_FIRST'){'✅'}else{'⚠️'})" -ForegroundColor $(if ($managedIdentityClientId -ne 'DEPLOY_INFRASTRUCTURE_FIRST') { 'Green' }else { 'Yellow' })
Write-Host "- App Insights Connection String: $(if($appInsightsConnectionString -ne 'DEPLOY_INFRASTRUCTURE_FIRST'){'✅'}else{'⚠️'})" -ForegroundColor $(if ($appInsightsConnectionString -ne 'DEPLOY_INFRASTRUCTURE_FIRST') { 'Green' }else { 'Yellow' })

# Save to file for reference
$outputFile = "azure-credentials-$(Get-Date -Format 'yyyyMMdd-HHmmss').txt"
@"
Azure Credentials for GitHub Actions - $(Get-Date)
================================================

AZURE_TENANT_ID: $tenantId
MANAGED_IDENTITY_CLIENT_ID: $managedIdentityClientId
APP_INSIGHTS_CONNECTION_STRING: $appInsightsConnectionString

Next Steps:
1. Create service principal: az ad sp create-for-rbac --name "GitHub-Actions-Zeus-People" --role contributor --scopes "/subscriptions/$SubscriptionId" --sdk-auth
2. Add all secrets to GitHub repository
3. Run pipeline to test

"@ | Out-File -FilePath $outputFile -Encoding UTF8

Write-Host "`n💾 Values saved to: $outputFile" -ForegroundColor Green
Write-Host "`n✨ Once you've created the service principal and added all secrets to GitHub, your pipeline should work!" -ForegroundColor Green
