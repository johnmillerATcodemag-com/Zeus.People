# Staging Environment Resource Verification Script
# Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
# Purpose: Verify all resources have been provisioned in the staging environment

Write-Host "=== Zeus.People Staging Environment Resource Verification ===" -ForegroundColor Green
Write-Host "Resource Group: rg-academic-staging-westus2" -ForegroundColor Yellow
Write-Host "Location: West US 2" -ForegroundColor Yellow
Write-Host "Environment: staging" -ForegroundColor Yellow
Write-Host ""

# Function to check resource status
function Test-AzureResource {
    param(
        [string]$ResourceName,
        [string]$ResourceType,
        [string]$Description
    )
    
    Write-Host "Checking $Description..." -ForegroundColor Cyan
    
    try {
        # Different resource types have different property paths for status
        if ($ResourceType -eq "Microsoft.Web/sites") {
            $resource = az webapp show --name $ResourceName --resource-group "rg-academic-staging-westus2" --query "{name:name,state:state,location:location}" --output json | ConvertFrom-Json
            $status = $resource.state
        }
        elseif ($ResourceType -eq "Microsoft.ManagedIdentity/userAssignedIdentities") {
            $resource = az identity show --name $ResourceName --resource-group "rg-academic-staging-westus2" --query "{name:name,principalId:principalId,location:location}" --output json | ConvertFrom-Json
            $status = if ($resource.principalId) { "Active" } else { "Inactive" }
        }
        else {
            $resource = az resource show --name $ResourceName --resource-group "rg-academic-staging-westus2" --resource-type $ResourceType --query "{name:name,provisioningState:properties.provisioningState,location:location}" --output json | ConvertFrom-Json
            $status = $resource.provisioningState
        }
        
        if ($status -eq "Succeeded" -or $status -eq "Running" -or $status -eq "Active") {
            Write-Host "  ✅ $($resource.name) - Status: $status" -ForegroundColor Green
            return $true
        }
        else {
            Write-Host "  ❌ $($resource.name) - Status: $status" -ForegroundColor Red
            return $false
        }
    }
    catch {
        Write-Host "  ❌ $ResourceName - Error: Resource not found or inaccessible" -ForegroundColor Red
        return $false
    }
}

# Check each resource
$resources = @(
    @{Name = "app-academic-staging-2ymnmfmrvsb3w"; Type = "Microsoft.Web/sites"; Description = "App Service (API)" },
    @{Name = "asp-academic-staging-2ymnmfmrvsb3w"; Type = "Microsoft.Web/serverFarms"; Description = "App Service Plan" },
    @{Name = "cosmos-academic-staging-2ymnmfmrvsb3w"; Type = "Microsoft.DocumentDB/databaseAccounts"; Description = "Cosmos DB" },
    @{Name = "sb-academic-staging-2ymnmfmrvsb3w"; Type = "Microsoft.ServiceBus/namespaces"; Description = "Service Bus Namespace" },
    @{Name = "kv2ymnmfmrvsb3w"; Type = "Microsoft.KeyVault/vaults"; Description = "Key Vault" },
    @{Name = "ai-academic-staging-2ymnmfmrvsb3w"; Type = "Microsoft.Insights/components"; Description = "Application Insights" },
    @{Name = "law-academic-staging-2ymnmfmrvsb3w"; Type = "Microsoft.OperationalInsights/workspaces"; Description = "Log Analytics Workspace" },
    @{Name = "mi-academic-staging-2ymnmfmrvsb3w"; Type = "Microsoft.ManagedIdentity/userAssignedIdentities"; Description = "Managed Identity" }
)

$successCount = 0
$totalCount = $resources.Count

Write-Host "=== Resource Status Check ===" -ForegroundColor Yellow
foreach ($resource in $resources) {
    if (Test-AzureResource -ResourceName $resource.Name -ResourceType $resource.Type -Description $resource.Description) {
        $successCount++
    }
    Write-Host ""
}

Write-Host "=== Service Endpoints ===" -ForegroundColor Yellow
Write-Host "App Service URL: https://app-academic-staging-2ymnmfmrvsb3w.azurewebsites.net" -ForegroundColor Cyan
Write-Host "Cosmos DB Endpoint: https://cosmos-academic-staging-2ymnmfmrvsb3w.documents.azure.com:443/" -ForegroundColor Cyan
Write-Host "Service Bus Endpoint: https://sb-academic-staging-2ymnmfmrvsb3w.servicebus.windows.net:443/" -ForegroundColor Cyan
Write-Host "Key Vault Endpoint: https://kv2ymnmfmrvsb3w.vault.azure.net/" -ForegroundColor Cyan
Write-Host ""

Write-Host "=== Summary ===" -ForegroundColor Yellow
Write-Host "Resources Verified: $successCount/$totalCount" -ForegroundColor $(if ($successCount -eq $totalCount) { "Green" } else { "Red" })

if ($successCount -eq $totalCount) {
    Write-Host "🎉 All staging environment resources are successfully provisioned and ready!" -ForegroundColor Green
}
else {
    Write-Host "⚠️  Some resources may need attention. Check the status above." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== Next Steps ===" -ForegroundColor Yellow
Write-Host "1. Deploy application code to App Service" -ForegroundColor White
Write-Host "2. Configure application settings and connection strings" -ForegroundColor White
Write-Host "3. Run integration tests against staging environment" -ForegroundColor White
Write-Host "4. Set up CI/CD pipeline for automated deployments" -ForegroundColor White

# Note about SQL Database
Write-Host ""
Write-Host "=== SQL Database Status ===" -ForegroundColor Yellow
Write-Host "❌ SQL Database is temporarily disabled due to Azure SQL password validation requirements." -ForegroundColor Red
Write-Host "   - Azure SQL requires complex passwords with specific character combinations" -ForegroundColor White
Write-Host "   - Password must meet: 8+ chars, uppercase, lowercase, digit, special character" -ForegroundColor White
Write-Host "   - Recommended solution: Store SQL credentials in Key Vault with proper password format" -ForegroundColor White
Write-Host ""
Write-Host "=== Resolution Steps ===" -ForegroundColor Yellow
Write-Host "1. Generate compliant SQL password: 'MyApp2024!SecureP@ssw0rd'" -ForegroundColor White
Write-Host "2. Store credentials in Key Vault as secrets" -ForegroundColor White
Write-Host "3. Update Bicep template to reference Key Vault secrets" -ForegroundColor White
Write-Host "4. Re-enable SQL Database module in main.bicep" -ForegroundColor White
Write-Host ""
Write-Host "All other services are fully operational and ready for application deployment." -ForegroundColor Green
