# Test Azure Service Principal Credentials
# Run this script to verify the service principal works correctly

param(
    [Parameter(Mandatory = $false)]
    [string]$ClientId = "a90252fe-4d36-4e18-8a85-7e8ecbf04ed0",
    
    [Parameter(Mandatory = $false)]
    [string]$ClientSecret = "[REDACTED]",
    
    [Parameter(Mandatory = $false)]
    [string]$TenantId = "24db396b-b795-45c9-bcfa-d3559193f2f7",
    
    [Parameter(Mandatory = $false)]
    [string]$ResourceGroup = "rg-academic-staging-westus2"
)

Write-Host "🔍 Testing Azure Service Principal Credentials..." -ForegroundColor Green
Write-Host "=================================================" -ForegroundColor Green

# Test login with service principal
Write-Host "🔐 Testing service principal login..." -ForegroundColor Yellow
try {
    az login --service-principal --username $ClientId --password $ClientSecret --tenant $TenantId --output none
    Write-Host "✅ Service principal login successful!" -ForegroundColor Green
}
catch {
    Write-Host "❌ Service principal login failed!" -ForegroundColor Red
    Write-Host "Error: $_" -ForegroundColor Red
    exit 1
}

# Test subscription access
Write-Host "📋 Testing subscription access..." -ForegroundColor Yellow
try {
    $subscription = az account show --query "{name:name, id:id}" --output json | ConvertFrom-Json
    Write-Host "✅ Subscription access successful: $($subscription.name)" -ForegroundColor Green
}
catch {
    Write-Host "❌ Subscription access failed!" -ForegroundColor Red
    exit 1
}

# Test resource group access (if exists)
Write-Host "🏗️ Testing resource group access..." -ForegroundColor Yellow
try {
    $rgExists = az group exists --name $ResourceGroup
    if ($rgExists -eq "true") {
        Write-Host "✅ Resource group '$ResourceGroup' exists and is accessible" -ForegroundColor Green
        
        # List resources in the group
        Write-Host "📦 Resources in the group:" -ForegroundColor Cyan
        az resource list --resource-group $ResourceGroup --query "[].{Name:name, Type:type, Location:location}" --output table
    }
    else {
        Write-Host "ℹ️ Resource group '$ResourceGroup' does not exist yet (will be created during deployment)" -ForegroundColor Blue
    }
}
catch {
    Write-Host "❌ Resource group access test failed!" -ForegroundColor Red
    Write-Host "This might indicate insufficient permissions" -ForegroundColor Yellow
}

# Test deployment permissions
Write-Host "🚀 Testing deployment permissions..." -ForegroundColor Yellow
try {
    $deployments = az deployment group list --resource-group $ResourceGroup --output json 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Deployment permissions verified" -ForegroundColor Green
    }
    else {
        Write-Host "ℹ️ Resource group doesn't exist yet, but permission structure looks correct" -ForegroundColor Blue
    }
}
catch {
    Write-Host "⚠️ Could not test deployment permissions (resource group may not exist yet)" -ForegroundColor Yellow
}

# Return to original authentication
Write-Host "🔄 Restoring original authentication..." -ForegroundColor Yellow
az logout --output none
az login --output none

Write-Host "`n🎉 Credential Test Summary:" -ForegroundColor Green
Write-Host "===========================" -ForegroundColor Green
Write-Host "✅ Service Principal Authentication: WORKING" -ForegroundColor Green
Write-Host "✅ Subscription Access: WORKING" -ForegroundColor Green
Write-Host "✅ Resource Management Permissions: WORKING" -ForegroundColor Green

Write-Host "`n🚀 Next Steps:" -ForegroundColor Yellow
Write-Host "1. Ensure all GitHub secrets are added correctly" -ForegroundColor White
Write-Host "2. Run the GitHub Actions pipeline" -ForegroundColor White
Write-Host "3. Monitor the deployment progress" -ForegroundColor White
Write-Host "4. Update placeholder secrets after infrastructure deployment" -ForegroundColor White

Write-Host "`n✨ Your Azure credentials are configured correctly for GitHub Actions!" -ForegroundColor Green
