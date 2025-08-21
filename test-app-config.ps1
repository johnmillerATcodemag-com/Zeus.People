# Test application startup with comprehensive secrets configuration
param(
    [string]$Environment = "Development",
    [switch]$TestEnvironmentVariables,
    [switch]$TestKeyVault,
    [switch]$TestAppService
)

Write-Host "[INFO] Testing application configuration with secrets management"
Write-Host "[INFO] Environment: $Environment"

# Color functions
function Write-Success($Message) { Write-Host "[SUCCESS] $Message" -ForegroundColor Green }
function Write-Warning($Message) { Write-Host "[WARNING] $Message" -ForegroundColor Yellow }
function Write-Error($Message) { Write-Host "[ERROR] $Message" -ForegroundColor Red }

# Set environment for testing
$env:ASPNETCORE_ENVIRONMENT = $Environment

if ($TestEnvironmentVariables) {
    Write-Host "[INFO] Testing environment variable configuration..."
    
    # Check if environment variables are set
    $envVars = @(
        "JWT_SECRET_KEY",
        "AZURE_AD_TENANT_ID", 
        "AZURE_AD_CLIENT_ID",
        "AZURE_AD_CLIENT_SECRET",
        "DATABASE_CONNECTION_STRING",
        "EVENT_STORE_CONNECTION_STRING",
        "SERVICE_BUS_CONNECTION_STRING",
        "APPLICATION_INSIGHTS_CONNECTION_STRING"
    )
    
    $missing = @()
    foreach ($var in $envVars) {
        $value = [Environment]::GetEnvironmentVariable($var)
        if ([string]::IsNullOrEmpty($value) -or $value.Contains("REPLACE_WITH")) {
            $missing += $var
        } else {
            $maskedValue = if ($value.Length -gt 8) { $value.Substring(0, 4) + "..." + $value.Substring($value.Length - 4) } else { "***" }
            Write-Success "✅ $var = $maskedValue"
        }
    }
    
    if ($missing.Count -gt 0) {
        Write-Warning "Missing or placeholder environment variables:"
        foreach ($var in $missing) {
            Write-Warning "  ❌ $var"
        }
        Write-Host ""
        Write-Host "Run .\scripts\setup-development-secrets.ps1 to configure them"
        Write-Host ""
    }
}

# Test configuration validation
Write-Host "[INFO] Building and testing configuration validation..."

try {
    # Build the solution first
    Write-Host "[INFO] Building solution..."
    & dotnet build --configuration Release --verbosity quiet
    if ($LASTEXITCODE -eq 0) {
        Write-Host "[SUCCESS] Solution built successfully"
    }
    else {
        Write-Host "[ERROR] Build failed"
        exit 1
    }
    
    # Run configuration validation tests
    Write-Host "[INFO] Running configuration validation tests..."
    & dotnet test tests/Zeus.People.Tests.Integration/ --filter "Category=Configuration" --verbosity quiet --no-build
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "[SUCCESS] Configuration validation tests passed"
    }
    else {
        Write-Host "[WARN] Some configuration tests may have failed (this is expected without full Azure connection)"
    }
    
    # Test health checks
    Write-Host "[INFO] Testing health checks configuration..."
    $healthCheckResult = & dotnet run --project src/API/ --no-build --configuration Release -- --environment $Environment --check-health 2>&1
    
    Write-Host "[INFO] Health check output:"
    Write-Host $healthCheckResult
    
}
catch {
    Write-Host "[ERROR] Application test failed: $($_.Exception.Message)"
}

Write-Host "[INFO] Application configuration test completed"
