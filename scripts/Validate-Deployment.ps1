# Validate-Deployment.ps1
# Validation script to verify successful deployment of Zeus People configuration

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("Development", "Staging", "Production")]
    [string]$Environment,
    
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroupName,
    
    [Parameter(Mandatory = $true)]
    [string]$SubscriptionId,
    
    [Parameter(Mandatory = $false)]
    [string]$KeyVaultName,
    
    [Parameter(Mandatory = $false)]
    [string]$AppServiceName,
    
    [Parameter(Mandatory = $false)]
    [string]$ManagedIdentityName
)

# Import required modules
$requiredModules = @('Az.Accounts', 'Az.Resources', 'Az.KeyVault', 'Az.Websites', 'Az.ManagedServiceIdentity')
foreach ($module in $requiredModules) {
    if (-not (Get-Module -Name $module -ListAvailable)) {
        Write-Error "Required module $module is not installed. Please install it using: Install-Module -Name $module"
        exit 1
    }
    Import-Module -Name $module -Force
}

# Set error action preference
$ErrorActionPreference = 'Stop'

# Initialize variables
$env = $Environment.ToLower()
if (-not $KeyVaultName) { $KeyVaultName = "kv-zeus-people-$env" }
if (-not $AppServiceName) { $AppServiceName = "app-zeus-people-$env" }
if (-not $ManagedIdentityName) { $ManagedIdentityName = "mi-zeus-people-$env" }

# Validation results
$validationResults = @()

function Write-ValidationResult {
    param(
        [string]$TestName,
        [bool]$Passed,
        [string]$Message,
        [string]$Details = ""
    )
    
    $result = [PSCustomObject]@{
        TestName  = $TestName
        Passed    = $Passed
        Message   = $Message
        Details   = $Details
        Timestamp = (Get-Date)
    }
    
    $validationResults += $result
    
    if ($Passed) {
        Write-Host "✅ $TestName - $Message" -ForegroundColor Green
    }
    else {
        Write-Host "❌ $TestName - $Message" -ForegroundColor Red
        if ($Details) {
            Write-Host "   Details: $Details" -ForegroundColor Yellow
        }
    }
}

try {
    Write-Host "🚀 Starting deployment validation for $Environment environment..." -ForegroundColor Cyan
    Write-Host "Resource Group: $ResourceGroupName" -ForegroundColor Gray
    Write-Host "Subscription: $SubscriptionId" -ForegroundColor Gray
    Write-Host "Timestamp: $(Get-Date)" -ForegroundColor Gray
    Write-Host ""

    # Set Azure context
    Set-AzContext -SubscriptionId $SubscriptionId | Out-Null
    
    # Test 1: Resource Group Exists
    try {
        $resourceGroup = Get-AzResourceGroup -Name $ResourceGroupName -ErrorAction Stop
        Write-ValidationResult -TestName "Resource Group" -Passed $true -Message "Resource group exists" -Details "Location: $($resourceGroup.Location)"
    }
    catch {
        Write-ValidationResult -TestName "Resource Group" -Passed $false -Message "Resource group not found" -Details $_.Exception.Message
        return
    }

    # Test 2: Key Vault Exists and Accessible
    try {
        $keyVault = Get-AzKeyVault -VaultName $KeyVaultName -ResourceGroupName $ResourceGroupName -ErrorAction Stop
        Write-ValidationResult -TestName "Key Vault Existence" -Passed $true -Message "Key Vault exists" -Details "URI: $($keyVault.VaultUri)"
        
        # Test Key Vault accessibility
        try {
            $secrets = Get-AzKeyVaultSecret -VaultName $KeyVaultName -ErrorAction Stop
            Write-ValidationResult -TestName "Key Vault Access" -Passed $true -Message "Key Vault is accessible" -Details "Found $($secrets.Count) secrets"
        }
        catch {
            Write-ValidationResult -TestName "Key Vault Access" -Passed $false -Message "Cannot access Key Vault secrets" -Details $_.Exception.Message
        }
    }
    catch {
        Write-ValidationResult -TestName "Key Vault Existence" -Passed $false -Message "Key Vault not found" -Details $_.Exception.Message
    }

    # Test 3: Required Secrets Exist
    $requiredSecrets = @(
        "Database--ConnectionString",
        "Database--ReadOnlyConnectionString", 
        "ServiceBus--ConnectionString",
        "ServiceBus--Namespace",
        "AzureAd--ClientSecret",
        "JwtSettings--SecretKey",
        "ApplicationInsights--ConnectionString"
    )

    $missingSecrets = @()
    $foundSecrets = @()

    foreach ($secretName in $requiredSecrets) {
        try {
            $secret = Get-AzKeyVaultSecret -VaultName $KeyVaultName -Name $secretName -ErrorAction Stop
            $foundSecrets += $secretName
        }
        catch {
            $missingSecrets += $secretName
        }
    }

    if ($missingSecrets.Count -eq 0) {
        Write-ValidationResult -TestName "Required Secrets" -Passed $true -Message "All required secrets exist" -Details "Found: $($foundSecrets -join ', ')"
    }
    else {
        Write-ValidationResult -TestName "Required Secrets" -Passed $false -Message "Missing required secrets" -Details "Missing: $($missingSecrets -join ', ')"
    }

    # Test 4: Managed Identity Exists
    try {
        $managedIdentity = Get-AzUserAssignedIdentity -ResourceGroupName $ResourceGroupName -Name $ManagedIdentityName -ErrorAction Stop
        Write-ValidationResult -TestName "Managed Identity" -Passed $true -Message "Managed identity exists" -Details "Client ID: $($managedIdentity.ClientId)"
    }
    catch {
        Write-ValidationResult -TestName "Managed Identity" -Passed $false -Message "Managed identity not found" -Details $_.Exception.Message
    }

    # Test 5: App Service Exists (if specified)
    if ($AppServiceName) {
        try {
            $appService = Get-AzWebApp -ResourceGroupName $ResourceGroupName -Name $AppServiceName -ErrorAction Stop
            Write-ValidationResult -TestName "App Service" -Passed $true -Message "App Service exists" -Details "State: $($appService.State), URL: $($appService.DefaultHostName)"
            
            # Test 6: App Service Managed Identity Configuration
            if ($appService.Identity -and $appService.Identity.UserAssignedIdentities) {
                $userIdentities = $appService.Identity.UserAssignedIdentities.Keys
                if ($userIdentities -contains $managedIdentity.Id) {
                    Write-ValidationResult -TestName "App Service Identity" -Passed $true -Message "Managed identity is assigned to App Service"
                }
                else {
                    Write-ValidationResult -TestName "App Service Identity" -Passed $false -Message "Managed identity not assigned to App Service"
                }
            }
            else {
                Write-ValidationResult -TestName "App Service Identity" -Passed $false -Message "No user-assigned identities found on App Service"
            }
        }
        catch {
            Write-ValidationResult -TestName "App Service" -Passed $false -Message "App Service not found" -Details $_.Exception.Message
        }
    }

    # Test 7: Key Vault Access Policies
    if ($keyVault) {
        try {
            $accessPolicies = $keyVault.AccessPolicies
            $managedIdentityPolicy = $accessPolicies | Where-Object { $_.ObjectId -eq $managedIdentity.PrincipalId }
            
            if ($managedIdentityPolicy) {
                $hasGetSecret = $managedIdentityPolicy.PermissionsToSecrets -contains "Get"
                $hasListSecret = $managedIdentityPolicy.PermissionsToSecrets -contains "List"
                
                if ($hasGetSecret -and $hasListSecret) {
                    Write-ValidationResult -TestName "Key Vault Access Policy" -Passed $true -Message "Managed identity has proper Key Vault permissions"
                }
                else {
                    Write-ValidationResult -TestName "Key Vault Access Policy" -Passed $false -Message "Managed identity missing required permissions" -Details "Has Get: $hasGetSecret, Has List: $hasListSecret"
                }
            }
            else {
                Write-ValidationResult -TestName "Key Vault Access Policy" -Passed $false -Message "No access policy found for managed identity"
            }
        }
        catch {
            Write-ValidationResult -TestName "Key Vault Access Policy" -Passed $false -Message "Error checking access policies" -Details $_.Exception.Message
        }
    }

    # Test 8: Secret Values Validation
    $secretValidationTests = @(
        @{ Name = "Database--ConnectionString"; Pattern = "Server=.*Authentication=Active Directory Managed Identity" },
        @{ Name = "ServiceBus--ConnectionString"; Pattern = "Endpoint=sb://.*Authentication=Managed Identity" },
        @{ Name = "JwtSettings--SecretKey"; MinLength = 32 }
    )

    foreach ($test in $secretValidationTests) {
        try {
            $secret = Get-AzKeyVaultSecret -VaultName $KeyVaultName -Name $test.Name -AsPlainText -ErrorAction Stop
            
            $isValid = $true
            $validationDetails = ""
            
            if ($test.Pattern) {
                if ($secret -notmatch $test.Pattern) {
                    $isValid = $false
                    $validationDetails = "Does not match expected pattern: $($test.Pattern)"
                }
            }
            
            if ($test.MinLength) {
                if ($secret.Length -lt $test.MinLength) {
                    $isValid = $false
                    $validationDetails = "Length $($secret.Length) is less than required minimum $($test.MinLength)"
                }
            }
            
            if ($isValid) {
                Write-ValidationResult -TestName "Secret Validation: $($test.Name)" -Passed $true -Message "Secret value is valid"
            }
            else {
                Write-ValidationResult -TestName "Secret Validation: $($test.Name)" -Passed $false -Message "Secret value validation failed" -Details $validationDetails
            }
        }
        catch {
            Write-ValidationResult -TestName "Secret Validation: $($test.Name)" -Passed $false -Message "Cannot retrieve secret for validation" -Details $_.Exception.Message
        }
    }

    # Test 9: Environment-Specific Configuration
    try {
        $envSecret = Get-AzKeyVaultSecret -VaultName $KeyVaultName -Name "Application--Environment" -AsPlainText -ErrorAction Stop
        if ($envSecret -eq $Environment) {
            Write-ValidationResult -TestName "Environment Configuration" -Passed $true -Message "Environment setting matches deployment target"
        }
        else {
            Write-ValidationResult -TestName "Environment Configuration" -Passed $false -Message "Environment mismatch" -Details "Expected: $Environment, Found: $envSecret"
        }
    }
    catch {
        Write-ValidationResult -TestName "Environment Configuration" -Passed $false -Message "Environment secret not found" -Details $_.Exception.Message
    }

    # Summary
    Write-Host ""
    Write-Host "📊 Validation Summary" -ForegroundColor Cyan
    Write-Host "===================" -ForegroundColor Cyan
    
    $totalTests = $validationResults.Count
    $passedTests = ($validationResults | Where-Object { $_.Passed }).Count
    $failedTests = $totalTests - $passedTests
    
    Write-Host "Total Tests: $totalTests" -ForegroundColor Gray
    Write-Host "Passed: $passedTests" -ForegroundColor Green
    Write-Host "Failed: $failedTests" -ForegroundColor Red
    Write-Host "Success Rate: $([math]::Round(($passedTests / $totalTests) * 100, 2))%" -ForegroundColor $(if ($failedTests -eq 0) { 'Green' } else { 'Yellow' })

    # Generate validation report
    $reportPath = ".\validation-report-$env-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
    $validationResults | ConvertTo-Json -Depth 5 | Out-File -FilePath $reportPath -Encoding UTF8
    Write-Host ""
    Write-Host "📄 Detailed report saved to: $reportPath" -ForegroundColor Gray

    if ($failedTests -eq 0) {
        Write-Host ""
        Write-Host "🎉 All validation tests passed! Deployment is successful." -ForegroundColor Green
        exit 0
    }
    else {
        Write-Host ""
        Write-Host "⚠️  Some validation tests failed. Please review the issues above." -ForegroundColor Yellow
        exit 1
    }
}
catch {
    Write-Error "❌ Validation script failed: $($_.Exception.Message)"
    Write-Host "Stack trace: $($_.ScriptStackTrace)" -ForegroundColor Red
    exit 1
}
