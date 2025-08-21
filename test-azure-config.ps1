# Test Azure configuration validation
param(
    [string]$Environment = "staging",
    [string]$KeyVaultName = "kv2ymnmfmrvsb3w"
)

Write-Host "[INFO] Testing Azure configuration for environment: $Environment"
Write-Host "[INFO] Key Vault: $KeyVaultName"

# Test Azure CLI
try {
    $azVersion = & az --version 2>&1
    Write-Host "[SUCCESS] Azure CLI is available"
    
    # Test account
    $account = & az account show --query "name" -o tsv 2>&1
    Write-Host "[SUCCESS] Logged in as: $account"
    
    # Test Key Vault access
    Write-Host "[INFO] Testing Key Vault access..."
    $keyVaultTest = & az keyvault show --name $KeyVaultName --query "name" -o tsv 2>$null
    if ($keyVaultTest) {
        Write-Host "[SUCCESS] Key Vault '$KeyVaultName' is accessible"
        
        # List secrets
        Write-Host "[INFO] Listing Key Vault secrets..."
        $secrets = & az keyvault secret list --vault-name $KeyVaultName --query "[].name" -o tsv
        Write-Host "[INFO] Found $($secrets.Count) secrets in Key Vault"
        
        # Test retrieving a sample secret (without showing value)
        $testSecret = & az keyvault secret show --vault-name $KeyVaultName --name "ApplicationSettings--Environment" --query "value" -o tsv 2>$null
        if ($testSecret) {
            Write-Host "[SUCCESS] Successfully retrieved secret from Key Vault"
        }
        else {
            Write-Host "[WARN] Could not retrieve test secret"
        }
    }
    else {
        Write-Host "[ERROR] Key Vault '$KeyVaultName' is not accessible"
    }
    
    # Test resource group
    Write-Host "[INFO] Testing resource group access..."
    $rg = & az group show --name "rg-academic-staging-westus2" --query "name" -o tsv 2>$null
    if ($rg) {
        Write-Host "[SUCCESS] Resource group 'rg-academic-staging-westus2' is accessible"
    }
    else {
        Write-Host "[ERROR] Resource group 'rg-academic-staging-westus2' is not accessible"
    }
    
}
catch {
    Write-Host "[ERROR] Azure CLI test failed: $($_.Exception.Message)"
}

Write-Host "[INFO] Configuration test completed"
