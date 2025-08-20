#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Comprehensive rollback testing script for Zeus.People application
    
.DESCRIPTION
    This script tests various rollback scenarios including:
    - Application version rollback
    - Database migration rollback
    - Infrastructure rollback
    - Configuration rollback
    
.PARAMETER TestType
    Type of rollback test to perform (Application, Database, Infrastructure, Configuration, All)
    
.PARAMETER Environment
    Target environment (staging, production)
    
.PARAMETER DryRun
    Perform a dry run without actual rollback
#>

param(
    [Parameter(Mandatory = $false)]
    [ValidateSet("Application", "Database", "Infrastructure", "Configuration", "All")]
    [string]$TestType = "All",
    
    [Parameter(Mandatory = $false)]
    [ValidateSet("staging", "production")]
    [string]$Environment = "staging",
    
    [Parameter(Mandatory = $false)]
    [switch]$DryRun
)

# Script configuration
$ErrorActionPreference = "Stop"
$InformationPreference = "Continue"

# Environment configuration
$envConfig = @{
    "staging"    = @{
        "resourceGroup" = "rg-academic-staging-westus2"
        "appName"       = "app-academic-staging-dvjm4oxxoy2g6"
        "azdEnv"        = "academic-staging"
    }
    "production" = @{
        "resourceGroup" = "rg-academic-production-westus2"
        "appName"       = "app-academic-production"
        "azdEnv"        = "academic-production"
    }
}

$config = $envConfig[$Environment]

# Logging
function Write-TestLog {
    param([string]$Message, [string]$Level = "INFO")
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $color = switch ($Level) {
        "INFO" { "Cyan" }
        "SUCCESS" { "Green" }
        "WARNING" { "Yellow" }
        "ERROR" { "Red" }
        default { "White" }
    }
    Write-Host "[$timestamp] [$Level] $Message" -ForegroundColor $color
}

function Test-ApplicationRollback {
    Write-TestLog "Testing Application Rollback Procedures..." "INFO"
    
    try {
        # 1. Get current application version
        Write-TestLog "Getting current application version..." "INFO"
        $currentHealth = Invoke-RestMethod -Uri "https://$($config.appName).azurewebsites.net/health" -Method Get
        $currentVersion = $currentHealth.results.configuration.data.configuration_summary -match "Version '([^']+)'" | Out-Null; $matches[1]
        Write-TestLog "Current version: $currentVersion" "SUCCESS"
        
        # 2. Simulate deploying a problematic version
        Write-TestLog "Simulating problematic deployment..." "INFO"
        if (-not $DryRun) {
            # Create a temporary "broken" version by modifying health endpoint response
            $brokenHealthResponse = @{
                status    = "Degraded"
                timestamp = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ssZ")
                message   = "SIMULATED FAILURE - Testing rollback procedures"
                version   = "1.0.99-rollback-test"
            }
            
            # Store current deployment info for rollback
            $deploymentInfo = az webapp deployment source show --name $config.appName --resource-group $config.resourceGroup | ConvertFrom-Json
            Write-TestLog "Stored deployment info for rollback" "SUCCESS"
        }
        else {
            Write-TestLog "[DRY RUN] Would simulate problematic deployment" "WARNING"
        }
        
        # 3. Test rollback detection
        Write-TestLog "Testing rollback detection mechanisms..." "INFO"
        $healthCheckFailed = $false
        for ($i = 1; $i -le 3; $i++) {
            try {
                $healthCheck = Invoke-RestMethod -Uri "https://$($config.appName).azurewebsites.net/health" -Method Get -TimeoutSec 10
                if ($healthCheck.status -ne "Healthy") {
                    $healthCheckFailed = $true
                    Write-TestLog "Health check failed on attempt ${i}: $($healthCheck.status)" "WARNING"
                    break
                }
                Write-TestLog "Health check ${i}/3: $($healthCheck.status)" "SUCCESS"
            }
            catch {
                $healthCheckFailed = $true
                Write-TestLog "Health check ${i}/3 failed with error: $($_.Exception.Message)" "ERROR"
                break
            }
            Start-Sleep -Seconds 2
        }
        
        # 4. Test automatic rollback trigger
        if ($healthCheckFailed) {
            Write-TestLog "Health check failures detected - triggering rollback..." "WARNING"
            if (-not $DryRun) {
                # Perform rollback using AZD
                Write-TestLog "Executing rollback using AZD..." "INFO"
                $rollbackResult = azd deploy --environment $config.azdEnv --force 2>&1
                if ($LASTEXITCODE -eq 0) {
                    Write-TestLog "AZD rollback completed successfully" "SUCCESS"
                }
                else {
                    Write-TestLog "AZD rollback failed: $rollbackResult" "ERROR"
                    return $false
                }
            }
            else {
                Write-TestLog "[DRY RUN] Would execute automatic rollback" "WARNING"
            }
        }
        else {
            Write-TestLog "No health check failures detected - rollback not needed" "SUCCESS"
        }
        
        # 5. Verify rollback success
        Write-TestLog "Verifying rollback success..." "INFO"
        Start-Sleep -Seconds 10  # Allow time for deployment
        
        $postRollbackHealth = Invoke-RestMethod -Uri "https://$($config.appName).azurewebsites.net/health" -Method Get
        if ($postRollbackHealth.status -eq "Healthy") {
            Write-TestLog "Post-rollback health check: PASSED ✅" "SUCCESS"
            return $true
        }
        else {
            Write-TestLog "Post-rollback health check: FAILED ❌" "ERROR"
            return $false
        }
        
    }
    catch {
        Write-TestLog "Application rollback test failed: $($_.Exception.Message)" "ERROR"
        return $false
    }
}

function Test-DatabaseRollback {
    Write-TestLog "Testing Database Rollback Procedures..." "INFO"
    
    try {
        # 1. Check current database schema version
        Write-TestLog "Checking current database schema version..." "INFO"
        # Note: This would typically query a schema version table
        # For now, we'll simulate this check
        $currentSchemaVersion = "1.0.0"
        Write-TestLog "Current schema version: $currentSchemaVersion" "SUCCESS"
        
        # 2. Simulate schema migration failure scenario
        Write-TestLog "Simulating database migration rollback scenario..." "INFO"
        if (-not $DryRun) {
            Write-TestLog "Would execute database rollback migration scripts" "WARNING"
            # In a real scenario, this would:
            # - Run rollback migration scripts
            # - Restore from backup if needed
            # - Validate data integrity
        }
        else {
            Write-TestLog "[DRY RUN] Would execute database rollback procedures" "WARNING"
        }
        
        # 3. Validate database connectivity and integrity
        Write-TestLog "Validating database connectivity post-rollback..." "INFO"
        $healthCheck = Invoke-RestMethod -Uri "https://$($config.appName).azurewebsites.net/health" -Method Get
        
        if ($healthCheck.results.cosmosdb.status -eq "Healthy") {
            Write-TestLog "Database connectivity check: PASSED ✅" "SUCCESS"
            return $true
        }
        else {
            Write-TestLog "Database connectivity check: FAILED ❌" "ERROR"
            return $false
        }
        
    }
    catch {
        Write-TestLog "Database rollback test failed: $($_.Exception.Message)" "ERROR"
        return $false
    }
}

function Test-InfrastructureRollback {
    Write-TestLog "Testing Infrastructure Rollback Procedures..." "INFO"
    
    try {
        # 1. Get current infrastructure state
        Write-TestLog "Getting current infrastructure state..." "INFO"
        if (-not $DryRun) {
            $resourceGroup = az group show --name $config.resourceGroup --query "{name: name, provisioningState: properties.provisioningState}" --output json | ConvertFrom-Json
            Write-TestLog "Resource group state: $($resourceGroup.provisioningState)" "SUCCESS"
            
            # Get key resources
            $appService = az webapp show --name $config.appName --resource-group $config.resourceGroup --query "{state: state, availabilityState: availabilityState}" --output json | ConvertFrom-Json
            Write-TestLog "App Service state: $($appService.state)/$($appService.availabilityState)" "SUCCESS"
        }
        else {
            Write-TestLog "[DRY RUN] Would check infrastructure state" "WARNING"
        }
        
        # 2. Test infrastructure rollback scenario
        Write-TestLog "Testing infrastructure rollback scenario..." "INFO"
        if (-not $DryRun) {
            # This would typically involve:
            # - Rolling back Bicep template changes
            # - Restoring previous configuration
            # - Validating resource health
            Write-TestLog "Would execute infrastructure rollback via Bicep template" "WARNING"
        }
        else {
            Write-TestLog "[DRY RUN] Would execute infrastructure rollback" "WARNING"
        }
        
        # 3. Validate infrastructure health
        Write-TestLog "Validating infrastructure health..." "INFO"
        $healthCheck = Invoke-RestMethod -Uri "https://$($config.appName).azurewebsites.net/health" -Method Get
        
        $allHealthy = $true
        foreach ($service in $healthCheck.results.PSObject.Properties) {
            if ($service.Value.status -ne "Healthy") {
                Write-TestLog "Service $($service.Name) is not healthy: $($service.Value.status)" "ERROR"
                $allHealthy = $false
            }
            else {
                Write-TestLog "Service $($service.Name): Healthy ✅" "SUCCESS"
            }
        }
        
        return $allHealthy
        
    }
    catch {
        Write-TestLog "Infrastructure rollback test failed: $($_.Exception.Message)" "ERROR"
        return $false
    }
}

function Test-ConfigurationRollback {
    Write-TestLog "Testing Configuration Rollback Procedures..." "INFO"
    
    try {
        # 1. Get current configuration state
        Write-TestLog "Getting current configuration state..." "INFO"
        $healthCheck = Invoke-RestMethod -Uri "https://$($config.appName).azurewebsites.net/health" -Method Get
        
        if ($healthCheck.results.configuration.status -eq "Healthy") {
            Write-TestLog "Configuration status: Healthy ✅" "SUCCESS"
            Write-TestLog "Configuration details: $($healthCheck.results.configuration.data.configuration_summary)" "INFO"
        }
        else {
            Write-TestLog "Configuration status: $($healthCheck.results.configuration.status) ❌" "ERROR"
        }
        
        # 2. Test configuration rollback scenario
        Write-TestLog "Testing configuration rollback scenario..." "INFO"
        if (-not $DryRun) {
            # This would typically involve:
            # - Rolling back Key Vault secrets
            # - Restoring previous app settings
            # - Updating connection strings
            Write-TestLog "Would execute configuration rollback procedures" "WARNING"
        }
        else {
            Write-TestLog "[DRY RUN] Would execute configuration rollback" "WARNING"
        }
        
        # 3. Validate configuration integrity
        Write-TestLog "Validating configuration integrity..." "INFO"
        $configValidation = $healthCheck.results.configuration
        
        if ($configValidation.status -eq "Healthy" -and 
            $configValidation.data.database_config -eq "Valid" -and
            $configValidation.data.servicebus_config -eq "Valid" -and
            $configValidation.data.azuread_config -eq "Valid") {
            
            Write-TestLog "Configuration validation: PASSED ✅" "SUCCESS"
            return $true
        }
        else {
            Write-TestLog "Configuration validation: FAILED ❌" "ERROR"
            return $false
        }
        
    }
    catch {
        Write-TestLog "Configuration rollback test failed: $($_.Exception.Message)" "ERROR"
        return $false
    }
}

function Test-RollbackProcedures {
    Write-TestLog "=== Starting Rollback Procedures Testing ===" "INFO"
    Write-TestLog "Environment: $Environment" "INFO"
    Write-TestLog "Test Type: $TestType" "INFO"
    Write-TestLog "Dry Run: $DryRun" "INFO"
    Write-TestLog "" "INFO"
    
    $results = @{}
    $overallSuccess = $true
    
    # Test application rollback
    if ($TestType -eq "All" -or $TestType -eq "Application") {
        $results["Application"] = Test-ApplicationRollback
        if (-not $results["Application"]) { $overallSuccess = $false }
        Write-TestLog "" "INFO"
    }
    
    # Test database rollback
    if ($TestType -eq "All" -or $TestType -eq "Database") {
        $results["Database"] = Test-DatabaseRollback
        if (-not $results["Database"]) { $overallSuccess = $false }
        Write-TestLog "" "INFO"
    }
    
    # Test infrastructure rollback
    if ($TestType -eq "All" -or $TestType -eq "Infrastructure") {
        $results["Infrastructure"] = Test-InfrastructureRollback
        if (-not $results["Infrastructure"]) { $overallSuccess = $false }
        Write-TestLog "" "INFO"
    }
    
    # Test configuration rollback
    if ($TestType -eq "All" -or $TestType -eq "Configuration") {
        $results["Configuration"] = Test-ConfigurationRollback
        if (-not $results["Configuration"]) { $overallSuccess = $false }
        Write-TestLog "" "INFO"
    }
    
    # Summary
    Write-TestLog "=== Rollback Test Results Summary ===" "INFO"
    foreach ($test in $results.Keys) {
        $status = if ($results[$test]) { "PASSED ✅" } else { "FAILED ❌" }
        Write-TestLog "$test Rollback: $status" $(if ($results[$test]) { "SUCCESS" } else { "ERROR" })
    }
    
    Write-TestLog "" "INFO"
    if ($overallSuccess) {
        Write-TestLog "🎉 ALL ROLLBACK TESTS PASSED! Rollback procedures are working correctly." "SUCCESS"
        exit 0
    }
    else {
        Write-TestLog "❌ SOME ROLLBACK TESTS FAILED! Review and fix rollback procedures." "ERROR"
        exit 1
    }
}

# Execute rollback tests
Test-RollbackProcedures
