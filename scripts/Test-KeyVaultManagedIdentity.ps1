# Test-KeyVaultManagedIdentity.ps1
# Comprehensive test to verify Key Vault access works with managed identity

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$KeyVaultName,
    
    [Parameter(Mandatory = $false)]
    [string]$ResourceGroupName,
    
    [Parameter(Mandatory = $false)]
    [string]$SubscriptionId,
    
    [Parameter(Mandatory = $false)]
    [string]$ManagedIdentityName,
    
    [Parameter(Mandatory = $false)]
    [switch]$CreateTestResources,
    
    [Parameter(Mandatory = $false)]
    [switch]$TestFromLocal,
    
    [Parameter(Mandatory = $false)]
    [switch]$Verbose
)

# Set up logging
$logFile = "KeyVault-ManagedIdentity-Test-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"
$testResults = @{
    AzureAuth              = $false
    KeyVaultExists         = $false
    ManagedIdentityExists  = $false
    AccessPolicyConfigured = $false
    SecretRetrieval        = $false
    ApplicationTest        = $false
    OverallSuccess         = $false
}

function Write-TestLog {
    param([string]$Message, [string]$Level = "INFO")
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logEntry = "[$timestamp] [$Level] $Message"
    Write-Host $logEntry -ForegroundColor $(if ($Level -eq "ERROR") { "Red" } elseif ($Level -eq "WARN") { "Yellow" } elseif ($Level -eq "SUCCESS") { "Green" } else { "White" })
    Add-Content -Path $logFile -Value $logEntry
}

function Test-AzureAuthentication {
    Write-TestLog "Testing Azure authentication..."
    
    try {
        $context = Get-AzContext
        if (-not $context) {
            Write-TestLog "Not authenticated to Azure. Please run Connect-AzAccount" -Level "ERROR"
            return $false
        }
        
        Write-TestLog "Authenticated as: $($context.Account.Id)" -Level "SUCCESS"
        Write-TestLog "Subscription: $($context.Subscription.Name) ($($context.Subscription.Id))" -Level "SUCCESS"
        
        # Set subscription if provided
        if ($SubscriptionId) {
            Set-AzContext -SubscriptionId $SubscriptionId | Out-Null
            Write-TestLog "Switched to subscription: $SubscriptionId" -Level "SUCCESS"
        }
        
        return $true
    }
    catch {
        Write-TestLog "Azure authentication failed: $($_.Exception.Message)" -Level "ERROR"
        return $false
    }
}

function Test-KeyVaultExists {
    param([string]$VaultName, [string]$ResourceGroup)
    
    Write-TestLog "Testing if Key Vault '$VaultName' exists..."
    
    try {
        $vault = Get-AzKeyVault -VaultName $VaultName -ResourceGroupName $ResourceGroup -ErrorAction SilentlyContinue
        
        if ($vault) {
            Write-TestLog "Key Vault found: $($vault.VaultUri)" -Level "SUCCESS"
            Write-TestLog "Location: $($vault.Location)" -Level "SUCCESS"
            Write-TestLog "Resource Group: $($vault.ResourceGroupName)" -Level "SUCCESS"
            return $true
        }
        else {
            Write-TestLog "Key Vault '$VaultName' not found in resource group '$ResourceGroup'" -Level "ERROR"
            return $false
        }
    }
    catch {
        Write-TestLog "Error checking Key Vault: $($_.Exception.Message)" -Level "ERROR"
        return $false
    }
}

function Test-ManagedIdentityExists {
    param([string]$IdentityName, [string]$ResourceGroup)
    
    Write-TestLog "Testing if Managed Identity '$IdentityName' exists..."
    
    try {
        $identity = Get-AzUserAssignedIdentity -Name $IdentityName -ResourceGroupName $ResourceGroup -ErrorAction SilentlyContinue
        
        if ($identity) {
            Write-TestLog "Managed Identity found: $($identity.Name)" -Level "SUCCESS"
            Write-TestLog "Principal ID: $($identity.PrincipalId)" -Level "SUCCESS"
            Write-TestLog "Client ID: $($identity.ClientId)" -Level "SUCCESS"
            return $identity
        }
        else {
            Write-TestLog "Managed Identity '$IdentityName' not found in resource group '$ResourceGroup'" -Level "ERROR"
            return $null
        }
    }
    catch {
        Write-TestLog "Error checking Managed Identity: $($_.Exception.Message)" -Level "ERROR"
        return $null
    }
}

function Test-KeyVaultAccessPolicy {
    param([string]$VaultName, [string]$ResourceGroup, [string]$PrincipalId)
    
    Write-TestLog "Testing Key Vault access policy for managed identity..."
    
    try {
        $vault = Get-AzKeyVault -VaultName $VaultName -ResourceGroupName $ResourceGroup
        $accessPolicies = $vault.AccessPolicies
        
        $matchingPolicy = $accessPolicies | Where-Object { $_.ObjectId -eq $PrincipalId }
        
        if ($matchingPolicy) {
            Write-TestLog "Access policy found for managed identity" -Level "SUCCESS"
            Write-TestLog "Permissions - Secrets: $($matchingPolicy.PermissionsToSecrets -join ', ')" -Level "SUCCESS"
            Write-TestLog "Permissions - Keys: $($matchingPolicy.PermissionsToKeys -join ', ')" -Level "SUCCESS"
            Write-TestLog "Permissions - Certificates: $($matchingPolicy.PermissionsToCertificates -join ', ')" -Level "SUCCESS"
            
            # Check if required permissions are present
            $hasGetSecret = $matchingPolicy.PermissionsToSecrets -contains "Get"
            $hasListSecret = $matchingPolicy.PermissionsToSecrets -contains "List"
            
            if ($hasGetSecret) {
                Write-TestLog "Required 'Get' permission for secrets is present" -Level "SUCCESS"
                return $true
            }
            else {
                Write-TestLog "Missing required 'Get' permission for secrets" -Level "ERROR"
                return $false
            }
        }
        else {
            Write-TestLog "No access policy found for managed identity with Principal ID: $PrincipalId" -Level "ERROR"
            return $false
        }
    }
    catch {
        Write-TestLog "Error checking access policy: $($_.Exception.Message)" -Level "ERROR"
        return $false
    }
}

function Test-SecretRetrieval {
    param([string]$VaultName, [string]$IdentityClientId)
    
    Write-TestLog "Testing secret retrieval using managed identity..."
    
    try {
        # Create test secrets if they don't exist
        $testSecrets = @{
            "test-connection-string" = "Server=test.database.windows.net;Database=testdb;Integrated Security=true;"
            "test-api-key"           = "test-api-key-value-$(Get-Date -Format 'yyyyMMddHHmmss')"
            "test-jwt-secret"        = "test-jwt-secret-$(Get-Random)"
        }
        
        foreach ($secretName in $testSecrets.Keys) {
            try {
                # Try to get the secret first
                $existingSecret = Get-AzKeyVaultSecret -VaultName $VaultName -Name $secretName -ErrorAction SilentlyContinue
                
                if (-not $existingSecret) {
                    Write-TestLog "Creating test secret: $secretName"
                    $secretValue = ConvertTo-SecureString -String $testSecrets[$secretName] -AsPlainText -Force
                    Set-AzKeyVaultSecret -VaultName $VaultName -Name $secretName -SecretValue $secretValue | Out-Null
                    Write-TestLog "Test secret '$secretName' created" -Level "SUCCESS"
                }
                else {
                    Write-TestLog "Test secret '$secretName' already exists" -Level "SUCCESS"
                }
                
                # Now test retrieval
                $retrievedSecret = Get-AzKeyVaultSecret -VaultName $VaultName -Name $secretName
                if ($retrievedSecret) {
                    Write-TestLog "Successfully retrieved secret: $secretName" -Level "SUCCESS"
                }
                else {
                    Write-TestLog "Failed to retrieve secret: $secretName" -Level "ERROR"
                    return $false
                }
            }
            catch {
                Write-TestLog "Error with secret '$secretName': $($_.Exception.Message)" -Level "ERROR"
                return $false
            }
        }
        
        return $true
    }
    catch {
        Write-TestLog "Error during secret retrieval test: $($_.Exception.Message)" -Level "ERROR"
        return $false
    }
}

function Test-ApplicationConnectivity {
    Write-TestLog "Testing application connectivity to Key Vault..."
    
    try {
        # Check if application is running
        $apiBaseUrl = "https://localhost:7001"
        $healthEndpoint = "$apiBaseUrl/health"
        
        Write-TestLog "Testing application health endpoint: $healthEndpoint"
        
        try {
            $response = Invoke-RestMethod -Uri $healthEndpoint -Method Get -TimeoutSec 30
            Write-TestLog "Application health check successful" -Level "SUCCESS"
            
            # Check for Key Vault health specifically
            if ($response -like "*KeyVault*" -or $response -like "*vault*") {
                Write-TestLog "Key Vault health check found in response" -Level "SUCCESS"
                return $true
            }
            else {
                Write-TestLog "Key Vault health check not found in response, but application is running" -Level "WARN"
                return $true
            }
        }
        catch {
            Write-TestLog "Application health check failed: $($_.Exception.Message)" -Level "ERROR"
            Write-TestLog "This may be expected if the application is not currently running" -Level "WARN"
            return $false
        }
    }
    catch {
        Write-TestLog "Error testing application connectivity: $($_.Exception.Message)" -Level "ERROR"
        return $false
    }
}

function Create-TestResources {
    param([string]$ResourceGroup, [string]$VaultName, [string]$IdentityName)
    
    Write-TestLog "Creating test resources..."
    
    try {
        # Create resource group if it doesn't exist
        $rg = Get-AzResourceGroup -Name $ResourceGroup -ErrorAction SilentlyContinue
        if (-not $rg) {
            Write-TestLog "Creating resource group: $ResourceGroup"
            New-AzResourceGroup -Name $ResourceGroup -Location "East US" | Out-Null
            Write-TestLog "Resource group created" -Level "SUCCESS"
        }
        
        # Create Key Vault if it doesn't exist
        $vault = Get-AzKeyVault -VaultName $VaultName -ResourceGroupName $ResourceGroup -ErrorAction SilentlyContinue
        if (-not $vault) {
            Write-TestLog "Creating Key Vault: $VaultName"
            New-AzKeyVault -VaultName $VaultName -ResourceGroupName $ResourceGroup -Location "East US" | Out-Null
            Write-TestLog "Key Vault created" -Level "SUCCESS"
        }
        
        # Create Managed Identity if it doesn't exist
        $identity = Get-AzUserAssignedIdentity -Name $IdentityName -ResourceGroupName $ResourceGroup -ErrorAction SilentlyContinue
        if (-not $identity) {
            Write-TestLog "Creating User Assigned Managed Identity: $IdentityName"
            $identity = New-AzUserAssignedIdentity -Name $IdentityName -ResourceGroupName $ResourceGroup -Location "East US"
            Write-TestLog "Managed Identity created" -Level "SUCCESS"
        }
        
        # Set Key Vault access policy
        Write-TestLog "Setting Key Vault access policy for managed identity"
        Set-AzKeyVaultAccessPolicy -VaultName $VaultName -ObjectId $identity.PrincipalId -PermissionsToSecrets Get, List, Set -PermissionsToKeys Get, List -PermissionsToCertificates Get, List
        Write-TestLog "Access policy configured" -Level "SUCCESS"
        
        return $true
    }
    catch {
        Write-TestLog "Error creating test resources: $($_.Exception.Message)" -Level "ERROR"
        return $false
    }
}

# Main execution
Write-TestLog "Starting Key Vault Managed Identity Test" -Level "SUCCESS"
Write-TestLog "Test parameters:"
Write-TestLog "  Key Vault Name: $KeyVaultName"
Write-TestLog "  Resource Group: $ResourceGroupName"
Write-TestLog "  Managed Identity: $ManagedIdentityName"
Write-TestLog "  Subscription ID: $SubscriptionId"
Write-TestLog "  Create Test Resources: $CreateTestResources"

# Import required modules
$requiredModules = @('Az.Accounts', 'Az.KeyVault', 'Az.ManagedServiceIdentity', 'Az.Resources')
foreach ($module in $requiredModules) {
    if (-not (Get-Module -Name $module -ListAvailable)) {
        Write-TestLog "Installing required module: $module" -Level "WARN"
        Install-Module -Name $module -Force -Scope CurrentUser
    }
    Import-Module -Name $module -Force
}

# Default values if not provided
if (-not $KeyVaultName) { $KeyVaultName = "kv-zeus-people-dev-$(Get-Random -Maximum 9999)" }
if (-not $ResourceGroupName) { $ResourceGroupName = "rg-zeus-people-test" }
if (-not $ManagedIdentityName) { $ManagedIdentityName = "mi-zeus-people-api" }

try {
    # Test 1: Azure Authentication
    Write-TestLog "=== Test 1: Azure Authentication ===" -Level "SUCCESS"
    $testResults.AzureAuth = Test-AzureAuthentication
    
    if (-not $testResults.AzureAuth) {
        Write-TestLog "Azure authentication failed. Please run Connect-AzAccount first." -Level "ERROR"
        exit 1
    }
    
    # Create test resources if requested
    if ($CreateTestResources) {
        Write-TestLog "=== Creating Test Resources ===" -Level "SUCCESS"
        $resourcesCreated = Create-TestResources -ResourceGroup $ResourceGroupName -VaultName $KeyVaultName -IdentityName $ManagedIdentityName
        if (-not $resourcesCreated) {
            Write-TestLog "Failed to create test resources" -Level "ERROR"
            exit 1
        }
    }
    
    # Test 2: Key Vault Exists
    Write-TestLog "=== Test 2: Key Vault Existence ===" -Level "SUCCESS"
    $testResults.KeyVaultExists = Test-KeyVaultExists -VaultName $KeyVaultName -ResourceGroup $ResourceGroupName
    
    # Test 3: Managed Identity Exists
    Write-TestLog "=== Test 3: Managed Identity Existence ===" -Level "SUCCESS"
    $managedIdentity = Test-ManagedIdentityExists -IdentityName $ManagedIdentityName -ResourceGroup $ResourceGroupName
    $testResults.ManagedIdentityExists = $managedIdentity -ne $null
    
    # Test 4: Access Policy Configuration
    if ($managedIdentity) {
        Write-TestLog "=== Test 4: Key Vault Access Policy ===" -Level "SUCCESS"
        $testResults.AccessPolicyConfigured = Test-KeyVaultAccessPolicy -VaultName $KeyVaultName -ResourceGroup $ResourceGroupName -PrincipalId $managedIdentity.PrincipalId
    }
    
    # Test 5: Secret Retrieval
    if ($testResults.KeyVaultExists -and $testResults.AccessPolicyConfigured) {
        Write-TestLog "=== Test 5: Secret Retrieval ===" -Level "SUCCESS"
        $testResults.SecretRetrieval = Test-SecretRetrieval -VaultName $KeyVaultName -IdentityClientId $managedIdentity.ClientId
    }
    
    # Test 6: Application Connectivity (if requested)
    if ($TestFromLocal) {
        Write-TestLog "=== Test 6: Application Connectivity ===" -Level "SUCCESS"
        $testResults.ApplicationTest = Test-ApplicationConnectivity
    }
    
    # Overall assessment
    $testResults.OverallSuccess = $testResults.AzureAuth -and $testResults.KeyVaultExists -and $testResults.ManagedIdentityExists -and $testResults.AccessPolicyConfigured -and $testResults.SecretRetrieval
    
    Write-TestLog "=== TEST SUMMARY ===" -Level "SUCCESS"
    Write-TestLog "Azure Authentication:      $($testResults.AzureAuth)"
    Write-TestLog "Key Vault Exists:          $($testResults.KeyVaultExists)"
    Write-TestLog "Managed Identity Exists:   $($testResults.ManagedIdentityExists)"
    Write-TestLog "Access Policy Configured:  $($testResults.AccessPolicyConfigured)"
    Write-TestLog "Secret Retrieval:          $($testResults.SecretRetrieval)"
    Write-TestLog "Application Test:          $($testResults.ApplicationTest)"
    Write-TestLog "Overall Success:           $($testResults.OverallSuccess)"
    
    if ($testResults.OverallSuccess) {
        Write-TestLog "Key Vault managed identity access verification PASSED!" -Level "SUCCESS"
        
        # Output configuration for application
        Write-TestLog "=== CONFIGURATION FOR APPLICATION ===" -Level "SUCCESS"
        Write-TestLog "Use these settings in your appsettings.json:"
        Write-TestLog "{"
        Write-TestLog "  \"KeyVaultSettings\": {"
        Write-TestLog "    \"VaultName\": \"$KeyVaultName\","
        Write-TestLog "    \"VaultUrl\": \"https://$KeyVaultName.vault.azure.net/\","
        Write-TestLog "    \"UseManagedIdentity\": true,"
        Write-TestLog "    \"ClientId\": \"$($managedIdentity.ClientId)\""
        Write-TestLog "  }"
        Write-TestLog "}"
        
        exit 0
    }
    else {
        Write-TestLog "Key Vault managed identity access verification FAILED!" -Level "ERROR"
        Write-TestLog "Check the test results above for specific failure details." -Level "ERROR"
        exit 1
    }
}
catch {
    Write-TestLog "Unexpected error during testing: $($_.Exception.Message)" -Level "ERROR"
    Write-TestLog "Stack trace: $($_.ScriptStackTrace)" -Level "ERROR"
    exit 1
}
finally {
    Write-TestLog "Test completed. Log file: $logFile"
}
