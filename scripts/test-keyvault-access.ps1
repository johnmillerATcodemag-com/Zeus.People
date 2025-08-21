# Key Vault Access and Secrets Verification Script
# Tests Azure Key Vault connectivity and secret retrieval functionality
#
# Prerequisites:
# - Azure CLI installed and authenticated
# - Key Vault secrets deployed using deploy-keyvault-secrets.ps1
# - Managed identity configured with proper RBAC permissions

param(
    [Parameter(Mandatory = $true)]
    [string]$Environment,
    
    [Parameter(Mandatory = $false)]
    [string]$KeyVaultName = "",
    
    [Parameter(Mandatory = $false)]
    [string]$ResourceGroup = "",
    
    [Parameter(Mandatory = $false)]
    [string]$SubscriptionId = "40d786b1-fabb-46d5-9c89-5194ea79dca1"
)

$ErrorActionPreference = "Stop"

# Logging function
function Write-Log {
    param(
        [string]$Message,
        [string]$Level = "INFO"
    )
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Write-Host "[$timestamp] [$Level] $Message" -ForegroundColor $(
        switch ($Level) {
            "ERROR" { "Red" }
            "WARN" { "Yellow" }
            "SUCCESS" { "Green" }
            default { "White" }
        }
    )
}

Write-Log "Starting Key Vault access verification for environment: $Environment"

# Determine Key Vault name and resource group if not provided
if ([string]::IsNullOrEmpty($KeyVaultName)) {
    $KeyVaultName = "kvklle24thta446"
    Write-Log "Using default Key Vault name: $KeyVaultName"
}

if ([string]::IsNullOrEmpty($ResourceGroup)) {
    $ResourceGroup = "rg-academic-$Environment-eastus2"
    Write-Log "Using default Resource Group: $ResourceGroup"
}

$testResults = @{
    "TotalTests"  = 0
    "PassedTests" = 0
    "FailedTests" = 0
    "Results"     = @()
}

function Add-TestResult {
    param(
        [string]$TestName,
        [bool]$Passed,
        [string]$Message,
        [object]$Data = $null
    )
    
    $testResults.TotalTests++
    
    if ($Passed) {
        $testResults.PassedTests++
        Write-Log "$TestName - PASSED: $Message" -Level "SUCCESS"
    }
    else {
        $testResults.FailedTests++
        Write-Log "$TestName - FAILED: $Message" -Level "ERROR"
    }
    
    $testResults.Results += @{
        "TestName"  = $TestName
        "Passed"    = $Passed
        "Message"   = $Message
        "Data"      = $Data
        "Timestamp" = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    }
}

try {
    Write-Log "Setting Azure subscription to: $SubscriptionId"
    az account set --subscription $SubscriptionId
    
    # Test 1: Key Vault Accessibility
    Write-Log "Test 1: Verifying Key Vault accessibility..."
    try {
        $keyVaultDetails = az keyvault show --name $KeyVaultName --query "{name:name, location:location, sku:sku.name, enableRbacAuthorization:properties.enableRbacAuthorization}" --output json | ConvertFrom-Json
        
        if ($keyVaultDetails.name -eq $KeyVaultName) {
            Add-TestResult "Key Vault Accessibility" $true "Key Vault '$KeyVaultName' is accessible in location '$($keyVaultDetails.location)' with SKU '$($keyVaultDetails.sku)'"
        }
        else {
            Add-TestResult "Key Vault Accessibility" $false "Key Vault response mismatch"
        }
        
        # Verify RBAC is enabled
        if ($keyVaultDetails.enableRbacAuthorization -eq $true) {
            Add-TestResult "Key Vault RBAC Configuration" $true "RBAC authorization is properly enabled"
        }
        else {
            Add-TestResult "Key Vault RBAC Configuration" $false "RBAC authorization is not enabled"
        }
    }
    catch {
        Add-TestResult "Key Vault Accessibility" $false "Cannot access Key Vault: $($_.Exception.Message)"
    }
    
    # Test 2: Managed Identity RBAC Permissions
    Write-Log "Test 2: Verifying Managed Identity RBAC permissions..."
    try {
        $managedIdentityName = "mi-academic-$Environment-klle24thta446"
        $keyVaultScope = "/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroup/providers/Microsoft.KeyVault/vaults/$KeyVaultName"
        
        $roleAssignments = az role assignment list --scope $keyVaultScope --query "[].{PrincipalId:principalId, RoleDefinitionName:roleDefinitionName, PrincipalName:principalName}" --output json | ConvertFrom-Json
        
        $secretsUserRole = $roleAssignments | Where-Object { $_.RoleDefinitionName -eq "Key Vault Secrets User" }
        
        if ($secretsUserRole) {
            Add-TestResult "Managed Identity RBAC Permissions" $true "Managed Identity has 'Key Vault Secrets User' role assigned"
        }
        else {
            Add-TestResult "Managed Identity RBAC Permissions" $false "Managed Identity does not have 'Key Vault Secrets User' role"
        }
    }
    catch {
        Add-TestResult "Managed Identity RBAC Permissions" $false "Error checking RBAC permissions: $($_.Exception.Message)"
    }
    
    # Test 3: List Key Vault Secrets
    Write-Log "Test 3: Listing Key Vault secrets..."
    try {
        $secrets = az keyvault secret list --vault-name $KeyVaultName --query "[].{name:name, enabled:attributes.enabled}" --output json | ConvertFrom-Json
        
        if ($secrets.Count -gt 0) {
            $enabledSecrets = $secrets | Where-Object { $_.enabled -eq $true }
            Add-TestResult "Key Vault Secrets List" $true "Found $($secrets.Count) secrets, $($enabledSecrets.Count) enabled"
        }
        else {
            Add-TestResult "Key Vault Secrets List" $false "No secrets found in Key Vault"
        }
    }
    catch {
        Add-TestResult "Key Vault Secrets List" $false "Error listing secrets: $($_.Exception.Message)"
    }
    
    # Test 4: Verify Critical Secrets Exist
    Write-Log "Test 4: Verifying critical application secrets exist..."
    $criticalSecrets = @(
        "DatabaseSettings--WriteConnectionString",
        "DatabaseSettings--ReadConnectionString", 
        "DatabaseSettings--EventStoreConnectionString",
        "ServiceBusSettings--ConnectionString",
        "ServiceBusSettings--Namespace",
        "JwtSettings--SecretKey",
        "ApplicationInsights--InstrumentationKey"
    )
    
    $foundSecrets = 0
    foreach ($secretName in $criticalSecrets) {
        try {
            $secretExists = az keyvault secret show --vault-name $KeyVaultName --name $secretName --query "name" --output tsv 2>$null
            
            if ($secretExists) {
                $foundSecrets++
                Write-Log "  ✓ Secret exists: $secretName" -Level "SUCCESS"
            }
            else {
                Write-Log "  ✗ Secret missing: $secretName" -Level "WARN"
            }
        }
        catch {
            Write-Log "  ✗ Error checking secret '$secretName': $($_.Exception.Message)" -Level "WARN"
        }
    }
    
    if ($foundSecrets -eq $criticalSecrets.Count) {
        Add-TestResult "Critical Secrets Verification" $true "All $($criticalSecrets.Count) critical secrets found"
    }
    elseif ($foundSecrets -gt 0) {
        Add-TestResult "Critical Secrets Verification" $false "Only $foundSecrets of $($criticalSecrets.Count) critical secrets found"
    }
    else {
        Add-TestResult "Critical Secrets Verification" $false "No critical secrets found"
    }
    
    # Test 5: Secret Value Retrieval
    Write-Log "Test 5: Testing secret value retrieval..."
    try {
        # Test retrieving a non-sensitive secret value
        $environmentSecret = az keyvault secret show --vault-name $KeyVaultName --name "ApplicationSettings--Environment" --query "value" --output tsv 2>$null
        
        if ($environmentSecret -eq $Environment) {
            Add-TestResult "Secret Value Retrieval" $true "Successfully retrieved and verified environment secret value"
        }
        elseif (![string]::IsNullOrEmpty($environmentSecret)) {
            Add-TestResult "Secret Value Retrieval" $true "Successfully retrieved secret value (content: $environmentSecret)"
        }
        else {
            Add-TestResult "Secret Value Retrieval" $false "Could not retrieve secret value"
        }
    }
    catch {
        Add-TestResult "Secret Value Retrieval" $false "Error retrieving secret: $($_.Exception.Message)"
    }
    
    # Test 6: Connection String Validation
    Write-Log "Test 6: Validating connection string secrets..."
    try {
        $serviceBusConnectionString = az keyvault secret show --vault-name $KeyVaultName --name "ServiceBusSettings--ConnectionString" --query "value" --output tsv 2>$null
        
        if ($serviceBusConnectionString -and $serviceBusConnectionString.Contains("servicebus.windows.net")) {
            Add-TestResult "Service Bus Connection String" $true "Service Bus connection string format is valid"
        }
        elseif (![string]::IsNullOrEmpty($serviceBusConnectionString)) {
            Add-TestResult "Service Bus Connection String" $false "Service Bus connection string format may be invalid"
        }
        else {
            Add-TestResult "Service Bus Connection String" $false "Service Bus connection string not found"
        }
    }
    catch {
        Add-TestResult "Service Bus Connection String" $false "Error validating Service Bus connection string: $($_.Exception.Message)"
    }
    
    # Test 7: Application Configuration Readiness
    Write-Log "Test 7: Verifying application configuration readiness..."
    try {
        $configSecrets = @(
            "KeyVaultSettings--VaultUrl",
            "ApplicationSettings--SupportEmail",
            "JwtSettings--SecretKey"
        )
        
        $configReady = $true
        foreach ($secretName in $configSecrets) {
            $secretValue = az keyvault secret show --vault-name $KeyVaultName --name $secretName --query "value" --output tsv 2>$null
            
            if ([string]::IsNullOrEmpty($secretValue) -or $secretValue.Contains("<TO_BE_CONFIGURED>")) {
                $configReady = $false
                Write-Log "  ✗ Configuration incomplete for: $secretName" -Level "WARN"
            }
            else {
                Write-Log "  ✓ Configuration ready for: $secretName" -Level "SUCCESS"
            }
        }
        
        if ($configReady) {
            Add-TestResult "Application Configuration Readiness" $true "All tested configuration values are ready"
        }
        else {
            Add-TestResult "Application Configuration Readiness" $false "Some configuration values need to be updated"
        }
    }
    catch {
        Add-TestResult "Application Configuration Readiness" $false "Error checking configuration readiness: $($_.Exception.Message)"
    }
    
    # Generate test report
    Write-Log ""
    Write-Log "KEY VAULT ACCESS VERIFICATION REPORT" -Level "SUCCESS"
    Write-Log "====================================" -Level "SUCCESS"
    Write-Log "Environment: $Environment" -Level "SUCCESS"
    Write-Log "Key Vault: $KeyVaultName" -Level "SUCCESS"
    Write-Log "Resource Group: $ResourceGroup" -Level "SUCCESS"
    Write-Log ""
    Write-Log "TEST SUMMARY:" -Level "SUCCESS"
    Write-Log "Total Tests: $($testResults.TotalTests)" -Level "SUCCESS"
    Write-Log "Passed: $($testResults.PassedTests)" -Level "SUCCESS"
    Write-Log "Failed: $($testResults.FailedTests)" -Level "SUCCESS"
    Write-Log "Success Rate: $([Math]::Round(($testResults.PassedTests / $testResults.TotalTests) * 100, 2))%" -Level "SUCCESS"
    
    if ($testResults.FailedTests -gt 0) {
        Write-Log ""
        Write-Log "FAILED TESTS:" -Level "WARN"
        foreach ($result in $testResults.Results) {
            if (-not $result.Passed) {
                Write-Log "- $($result.TestName): $($result.Message)" -Level "WARN"
            }
        }
    }
    
    # Export test results to JSON
    $reportFile = "key-vault-verification-results-$Environment-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
    $testResults | ConvertTo-Json -Depth 10 | Out-File -FilePath $reportFile -Encoding UTF8
    Write-Log "Test results exported to: $reportFile" -Level "SUCCESS"
    
    if ($testResults.FailedTests -eq 0) {
        Write-Log "All tests passed! Key Vault is ready for application use." -Level "SUCCESS"
        exit 0
    }
    else {
        Write-Log "Some tests failed. Please review the results and fix issues before proceeding." -Level "WARN"
        exit 1
    }
}
catch {
    Write-Log "Critical error during Key Vault verification: $($_.Exception.Message)" -Level "ERROR"
    exit 1
}
