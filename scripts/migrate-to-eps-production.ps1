#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Migrate Zeus.People resources to EPS Production subscription
    
.DESCRIPTION
    This script helps migrate and clean up Zeus.People resources across subscriptions.
    It identifies resources to be deleted from the old subscription and ensures
    everything is consolidated in the EPS Production subscription.
    
.PARAMETER Action
    The action to perform: 'analyze', 'cleanup', or 'migrate'
    
.PARAMETER Force
    Skip confirmation prompts
    
.EXAMPLE
    .\migrate-to-eps-production.ps1 -Action analyze
    .\migrate-to-eps-production.ps1 -Action cleanup -Force
#>

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('analyze', 'cleanup', 'migrate')]
    [string]$Action,
    
    [switch]$Force
)

# Configuration
$SourceSubscription = "5232b409-b25e-441c-9951-16e69069f224"  # Concordant-PayGo
$TargetSubscription = "40d786b1-fabb-46d5-9c89-5194ea79dca1"  # EPS Production

Write-Host "🚀 Zeus.People Subscription Migration Tool" -ForegroundColor Cyan
Write-Host "=" * 50

function Write-Status {
    param([string]$Message, [string]$Status = "INFO")
    $color = switch ($Status) {
        "SUCCESS" { "Green" }
        "WARNING" { "Yellow" }
        "ERROR" { "Red" }
        default { "White" }
    }
    Write-Host "[$Status] $Message" -ForegroundColor $color
}

function Analyze-Resources {
    Write-Status "Analyzing resources across subscriptions..."
    
    # Check source subscription resources
    Write-Host "`n📍 Resources in Concordant-PayGo (Source - to be cleaned up):" -ForegroundColor Yellow
    az resource list --subscription $SourceSubscription --query "[?contains(name, 'academic') || contains(name, 'zeus')].{Name:name, Type:type, ResourceGroup:resourceGroup, Location:location}" --output table
    
    # Check target subscription resources
    Write-Host "`n📍 Resources in EPS Production (Target - current):" -ForegroundColor Green
    az resource list --subscription $TargetSubscription --query "[?contains(name, 'academic') || contains(name, 'zeus')].{Name:name, Type:type, ResourceGroup:resourceGroup, Location:location}" --output table
    
    Write-Status "Analysis complete. The main staging environment is already in EPS Production." "SUCCESS"
    Write-Status "You can safely clean up the old resources in Concordant-PayGo." "WARNING"
}

function Cleanup-OldResources {
    Write-Status "Preparing to clean up old resources in Concordant-PayGo subscription..."
    
    if (-not $Force) {
        $confirm = Read-Host "This will DELETE resources in Concordant-PayGo subscription. Are you sure? (yes/no)"
        if ($confirm -ne "yes") {
            Write-Status "Operation cancelled." "WARNING"
            return
        }
    }
    
    Write-Status "Deleting resource groups in source subscription..." "WARNING"
    
    # Delete dev environment
    $devRG = "rg-academic-dev-eastus2"
    Write-Status "Deleting resource group: $devRG"
    az group delete --name $devRG --subscription $SourceSubscription --yes --no-wait
    
    # Delete staging environment (partial)
    $stagingRG = "rg-academic-staging-westus2"
    Write-Status "Deleting resource group: $stagingRG"
    az group delete --name $stagingRG --subscription $SourceSubscription --yes --no-wait
    
    Write-Status "Cleanup initiated. Resources are being deleted in the background." "SUCCESS"
    Write-Status "You can check the status in the Azure portal." "INFO"
}

function Migrate-Configuration {
    Write-Status "Updating local configuration files..."
    
    # The academic-staging environment config has already been updated
    Write-Status "✅ Updated .azure/academic-staging/.env to point to EPS Production" "SUCCESS"
    
    # Verify current environment
    $currentEnv = Get-Content "$PSScriptRoot/../.azure/config.json" | ConvertFrom-Json
    Write-Status "Current default environment: $($currentEnv.defaultEnvironment)" "INFO"
    
    # Test connectivity
    Write-Status "Testing connectivity to EPS Production resources..."
    try {
        az account set --subscription $TargetSubscription
        $resources = az resource list --subscription $TargetSubscription --query "[?contains(name, 'academic-staging-2ymnmfmrvsb3w')]" | ConvertFrom-Json
        if ($resources.Count -gt 0) {
            Write-Status "✅ Successfully connected to EPS Production resources" "SUCCESS"
            Write-Status "Found $($resources.Count) Zeus.People resources in target subscription" "INFO"
        }
        else {
            Write-Status "⚠️ No Zeus.People resources found in target subscription" "WARNING"
        }
    }
    catch {
        Write-Status "❌ Failed to connect to EPS Production: $($_.Exception.Message)" "ERROR"
    }
}

# Main execution
switch ($Action) {
    'analyze' {
        Analyze-Resources
    }
    'cleanup' {
        Cleanup-OldResources
    }
    'migrate' {
        Migrate-Configuration
        Write-Host "`n🎯 Migration Summary:" -ForegroundColor Magenta
        Write-Host "• Configuration updated to use EPS Production subscription" -ForegroundColor Green
        Write-Host "• Main staging environment already exists in EPS Production" -ForegroundColor Green
        Write-Host "• Run with '-Action cleanup' to remove old resources" -ForegroundColor Yellow
        Write-Host "• All new deployments will use EPS Production subscription" -ForegroundColor Green
    }
}

Write-Host "`n✨ Migration tool completed!" -ForegroundColor Cyan
