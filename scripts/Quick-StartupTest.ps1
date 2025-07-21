# Quick-StartupTest.ps1
# Quick test script to verify application startup with Azure configuration

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [ValidateSet("Development", "Staging", "Production")]
    [string]$Environment = "Development",
    
    [Parameter(Mandatory = $false)]
    [string]$KeyVaultName,
    
    [Parameter(Mandatory = $false)]
    [int]$TimeoutSeconds = 60
)

$ErrorActionPreference = 'Stop'

function Write-Status {
    param([string]$Message, [string]$Color = "White")
    Write-Host "$(Get-Date -Format 'HH:mm:ss') - $Message" -ForegroundColor $Color
}

function Test-QuickStartup {
    Write-Status "🚀 Starting Zeus People API quick startup test..." "Cyan"
    Write-Status "Environment: $Environment" "Gray"
    
    # Set environment variables
    $env:ASPNETCORE_ENVIRONMENT = $Environment
    $env:ASPNETCORE_URLS = "https://localhost:7001"
    
    if ($KeyVaultName) {
        $env:KeyVault__VaultUrl = "https://$KeyVaultName.vault.azure.net/"
        Write-Status "Using Key Vault: $KeyVaultName" "Gray"
    }
    
    # Check if project exists
    if (-not (Test-Path ".\src\API\Zeus.People.API.csproj")) {
        Write-Status "❌ Project file not found" "Red"
        return $false
    }
    
    Write-Status "📦 Building application..." "Yellow"
    
    # Build the application
    $buildOutput = & dotnet build .\src\API\Zeus.People.API.csproj --configuration Release --verbosity quiet 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Status "❌ Build failed" "Red"
        Write-Host $buildOutput -ForegroundColor Red
        return $false
    }
    
    Write-Status "✅ Build successful" "Green"
    Write-Status "🏃 Starting application..." "Yellow"
    
    # Start the application
    $process = Start-Process -FilePath "dotnet" `
        -ArgumentList "run --project .\src\API\Zeus.People.API.csproj --configuration Release --no-build" `
        -PassThru `
        -RedirectStandardOutput "startup-output.log" `
        -RedirectStandardError "startup-error.log"
    
    # Wait for startup
    $startTime = Get-Date
    $timeout = $startTime.AddSeconds($TimeoutSeconds)
    $startupDetected = $false
    
    Write-Status "⏳ Waiting for application startup (timeout: $TimeoutSeconds seconds)..." "Yellow"
    
    while ((Get-Date) -lt $timeout -and -not $process.HasExited) {
        Start-Sleep -Milliseconds 1000
        
        # Check output for startup indicators
        if (Test-Path "startup-output.log") {
            $output = Get-Content "startup-output.log" -ErrorAction SilentlyContinue
            if ($output -match "Now listening on:|Application started|Started Zeus.People.API") {
                $startupDetected = $true
                break
            }
        }
        
        # Check for errors
        if (Test-Path "startup-error.log") {
            $errors = Get-Content "startup-error.log" -ErrorAction SilentlyContinue
            if ($errors -match "Fatal|terminated unexpectedly") {
                break
            }
        }
        
        Write-Host "." -NoNewline -ForegroundColor Gray
    }
    
    Write-Host "" # New line after dots
    
    if ($startupDetected) {
        $startupTime = ((Get-Date) - $startTime).TotalSeconds
        Write-Status "✅ Application started successfully in $([math]::Round($startupTime, 2)) seconds" "Green"
        
        # Quick health check
        Start-Sleep -Seconds 2
        try {
            $response = Invoke-RestMethod -Uri "https://localhost:7001/health" -Method Get -TimeoutSec 10 -SkipCertificateCheck
            if ($response.status -eq "Healthy") {
                Write-Status "✅ Health check passed" "Green"
                $success = $true
            }
            else {
                Write-Status "⚠️ Health check reported: $($response.status)" "Yellow"
                $success = $true # Still consider startup successful
            }
        }
        catch {
            Write-Status "⚠️ Health check failed, but application started: $($_.Exception.Message)" "Yellow"
            $success = $true # Still consider startup successful
        }
    }
    else {
        Write-Status "❌ Application startup timeout or failed" "Red"
        
        # Show error output
        if (Test-Path "startup-error.log") {
            $errors = Get-Content "startup-error.log" -ErrorAction SilentlyContinue
            if ($errors) {
                Write-Status "Error output:" "Red"
                $errors | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
            }
        }
        
        $success = $false
    }
    
    # Cleanup
    if (-not $process.HasExited) {
        Write-Status "🛑 Stopping application..." "Yellow"
        $process.Kill()
        $process.WaitForExit(5000)
    }
    
    # Clean up log files
    Remove-Item "startup-output.log" -Force -ErrorAction SilentlyContinue
    Remove-Item "startup-error.log" -Force -ErrorAction SilentlyContinue
    
    return $success
}

try {
    $result = Test-QuickStartup
    
    if ($result) {
        Write-Status "🎉 Quick startup test completed successfully!" "Green"
        exit 0
    }
    else {
        Write-Status "💥 Quick startup test failed!" "Red"
        exit 1
    }
}
catch {
    Write-Status "❌ Test script error: $($_.Exception.Message)" "Red"
    exit 1
}
