#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Updates specific secrets in Azure Key Vault for Zeus.People application
.DESCRIPTION
    This script allows you to update individual secrets in an existing Key Vault
    without redeploying the entire infrastructure.
.PARAMETER KeyVaultName
    Name of the Azure Key Vault
.PARAMETER SecretName
    Name of the secret to update
.PARAMETER SecretValue
    New value for the secret
.PARAMETER SecretsFromFile
    Path to JSON file containing multiple secrets to update
.PARAMETER Environment
    Environment context for validation
.EXAMPLE
    .\Update-KeyVaultSecrets.ps1 -KeyVaultName "kv-zeus-people-prod-1234" -SecretName "Database--ConnectionString" -SecretValue "Server=..."
.EXAMPLE
    .\Update-KeyVaultSecrets.ps1 -KeyVaultName "kv-zeus-people-prod-1234" -SecretsFromFile "secrets-prod.json"
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$KeyVaultName,
    
    [Parameter(Mandatory = $false)]
    [string]$SecretName,
    
    [Parameter(Mandatory = $false)]
    [string]$SecretValue,
    
    [Parameter(Mandatory = $false)]
    [string]$SecretsFromFile,
    
    [Parameter(Mandatory = $false)]
    [ValidateSet("Development", "Staging", "Production")]
    [string]$Environment,
    
    [Parameter(Mandatory = $false)]
    [switch]$WhatIf
)

# Script start time for duration tracking
$scriptStartTime = Get-Date
Write-Host "🔄 Starting Key Vault secrets update at $scriptStartTime" -ForegroundColor Green

# Set error action preference
$ErrorActionPreference = "Stop"

# Import required modules
$requiredModules = @('Az.Accounts', 'Az.KeyVault')
foreach ($module in $requiredModules) {
    if (!(Get-Module -ListAvailable -Name $module)) {
        Write-Host "⚠️  Installing required module: $module" -ForegroundColor Yellow
        Install-Module -Name $module -Force -AllowClobber -Scope CurrentUser
    }
    Import-Module $module -Force
}

try {
    # Validate parameters
    if (-not $SecretName -and -not $SecretsFromFile) {
        throw "Either SecretName or SecretsFromFile must be specified"
    }

    if ($SecretName -and -not $SecretValue) {
        throw "SecretValue must be specified when SecretName is provided"
    }

    # Check Azure authentication
    $context = Get-AzContext
    if (-not $context) {
        throw "Not authenticated to Azure. Please run Connect-AzAccount first."
    }

    Write-Host "✅ Using subscription: $($context.Subscription.Name)" -ForegroundColor Green

    # Verify Key Vault exists and access
    Write-Host "🔍 Verifying Key Vault access..." -ForegroundColor Blue
    try {
        $keyVault = Get-AzKeyVault -VaultName $KeyVaultName -ErrorAction Stop
        Write-Host "   ✅ Key Vault found: $($keyVault.VaultUri)" -ForegroundColor Green
    }
    catch {
        throw "Key Vault '$KeyVaultName' not found or access denied: $($_.Exception.Message)"
    }

    # Prepare secrets to update
    $secretsToUpdate = @{}

    if ($SecretName) {
        # Single secret update
        $secretsToUpdate[$SecretName] = $SecretValue
        Write-Host "📝 Updating single secret: $SecretName" -ForegroundColor Blue
    }

    if ($SecretsFromFile) {
        # Multiple secrets from file
        if (-not (Test-Path $SecretsFromFile)) {
            throw "Secrets file not found: $SecretsFromFile"
        }

        Write-Host "📄 Loading secrets from file: $SecretsFromFile" -ForegroundColor Blue
        try {
            $fileSecrets = Get-Content -Path $SecretsFromFile -Raw | ConvertFrom-Json -AsHashtable
            foreach ($key in $fileSecrets.Keys) {
                $secretsToUpdate[$key] = $fileSecrets[$key]
            }
            Write-Host "   ✅ Loaded $($fileSecrets.Count) secrets from file" -ForegroundColor Green
        }
        catch {
            throw "Failed to parse secrets file: $($_.Exception.Message)"
        }
    }

    if ($secretsToUpdate.Count -eq 0) {
        throw "No secrets to update"
    }

    Write-Host "🔐 Updating $($secretsToUpdate.Count) secrets..." -ForegroundColor Blue

    if ($WhatIf) {
        Write-Host "🔍 WhatIf mode - showing secrets that would be updated:" -ForegroundColor Yellow
        foreach ($secretName in $secretsToUpdate.Keys) {
            $maskedValue = if ($secretsToUpdate[$secretName].Length -gt 10) {
                $secretsToUpdate[$secretName].Substring(0, 10) + "***"
            }
            else {
                "***"
            }
            Write-Host "   $secretName = $maskedValue" -ForegroundColor Gray
        }
        return
    }

    # Update secrets
    $updateCount = 0
    $successCount = 0
    $failureCount = 0

    foreach ($secretName in $secretsToUpdate.Keys) {
        $updateCount++
        Write-Host "   [$updateCount/$($secretsToUpdate.Count)] Updating: $secretName" -ForegroundColor Yellow
        
        try {
            # Check if secret already exists
            $existingSecret = Get-AzKeyVaultSecret -VaultName $KeyVaultName -Name $secretName -ErrorAction SilentlyContinue
            $isUpdate = $null -ne $existingSecret
            
            # Set the secret
            $secureString = ConvertTo-SecureString -String $secretsToUpdate[$secretName] -AsPlainText -Force
            $result = Set-AzKeyVaultSecret -VaultName $KeyVaultName -Name $secretName -SecretValue $secureString
            
            if ($result) {
                $successCount++
                $action = if ($isUpdate) { "Updated" } else { "Created" }
                Write-Host "       ✅ $action successfully" -ForegroundColor Green
            }
        }
        catch {
            $failureCount++
            Write-Host "       ❌ Failed: $($_.Exception.Message)" -ForegroundColor Red
        }
    }

    # Summary
    $totalDuration = (Get-Date) - $scriptStartTime
    
    Write-Host "`n📊 Update Summary:" -ForegroundColor Cyan
    Write-Host "   Total Secrets: $($secretsToUpdate.Count)" -ForegroundColor White
    Write-Host "   Successful: $successCount" -ForegroundColor Green
    Write-Host "   Failed: $failureCount" -ForegroundColor Red
    Write-Host "   Duration: $($totalDuration.TotalSeconds) seconds" -ForegroundColor Gray

    if ($failureCount -gt 0) {
        Write-Host "⚠️  Some secrets failed to update. Check the errors above." -ForegroundColor Yellow
    }
    else {
        Write-Host "🎉 All secrets updated successfully!" -ForegroundColor Green
    }

    # Generate update log
    $updateLog = @{
        KeyVaultName   = $KeyVaultName
        Environment    = $Environment
        UpdatedSecrets = @($secretsToUpdate.Keys)
        SuccessCount   = $successCount
        FailureCount   = $failureCount
        TotalCount     = $secretsToUpdate.Count
        UpdateTime     = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
        Duration       = $totalDuration.ToString("mm\:ss")
    }

    $logPath = "secrets-update-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
    $updateLog | ConvertTo-Json -Depth 10 | Out-File -FilePath $logPath -Encoding UTF8
    Write-Host "📄 Update log saved to: $logPath" -ForegroundColor Gray

    return $updateLog

}
catch {
    Write-Error "❌ Secret update failed: $($_.Exception.Message)"
    Write-Error "Stack trace: $($_.Exception.StackTrace)"
    throw
}
