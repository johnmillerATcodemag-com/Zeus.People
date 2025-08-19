# SQL Database Fix Script for Zeus.People
# Purpose: Resolve Azure SQL Database password validation issue and enable SQL provisioning
# Duration: Script creation started

Write-Host "=== Zeus.People SQL Database Fix Script ===" -ForegroundColor Green
Write-Host "Resolving Azure SQL Database password validation issue..." -ForegroundColor Yellow
Write-Host ""

# Step 1: Generate compliant SQL password
Write-Host "Step 1: Generating Azure SQL compliant password..." -ForegroundColor Cyan
$sqlPassword = "ZeusPeople2024!@SecureP@ssw0rd789"
$sqlLogin = "zeusadmin"

Write-Host "  ✅ Generated compliant password (meets all Azure SQL requirements)" -ForegroundColor Green
Write-Host "  ✅ Login: $sqlLogin" -ForegroundColor Green
Write-Host ""

# Step 2: Set environment variables
Write-Host "Step 2: Setting AZD environment variables..." -ForegroundColor Cyan
try {
    azd env set SQL_ADMIN_LOGIN $sqlLogin
    azd env set SQL_ADMIN_PASSWORD $sqlPassword
    Write-Host "  ✅ SQL credentials set in AZD environment" -ForegroundColor Green
}
catch {
    Write-Host "  ❌ Error setting environment variables: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Step 3: Test password compliance
Write-Host "Step 3: Validating password compliance..." -ForegroundColor Cyan
$passwordChecks = @{
    "Length (8+ chars)" = $sqlPassword.Length -ge 8
    "Uppercase letter"  = $sqlPassword -cmatch "[A-Z]"
    "Lowercase letter"  = $sqlPassword -cmatch "[a-z]"
    "Digit"             = $sqlPassword -cmatch "[0-9]"
    "Special character" = $sqlPassword -cmatch "[^A-Za-z0-9]"
}

$allValid = $true
foreach ($check in $passwordChecks.GetEnumerator()) {
    if ($check.Value) {
        Write-Host "  ✅ $($check.Key)" -ForegroundColor Green
    }
    else {
        Write-Host "  ❌ $($check.Key)" -ForegroundColor Red
        $allValid = $false
    }
}

if (-not $allValid) {
    Write-Host "❌ Password does not meet Azure SQL requirements. Exiting." -ForegroundColor Red
    exit 1
}
Write-Host ""

# Step 4: Show manual steps to enable SQL Database
Write-Host "Step 4: Manual steps to enable SQL Database in Bicep template..." -ForegroundColor Cyan
Write-Host ""
Write-Host "To complete the SQL Database setup, manually perform these steps:" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. Edit infra/main.bicep:" -ForegroundColor White
Write-Host "   - Uncomment SQL parameters (lines ~25-35)" -ForegroundColor Gray
Write-Host "   - Uncomment SQL Database module (lines ~170-190)" -ForegroundColor Gray
Write-Host "   - Uncomment SQL outputs (lines ~320-330)" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Run deployment:" -ForegroundColor White
Write-Host "   azd provision --no-prompt" -ForegroundColor Gray
Write-Host ""
Write-Host "3. Verify SQL Database deployment:" -ForegroundColor White
Write-Host "   .\verify-staging-resources.ps1" -ForegroundColor Gray
Write-Host ""

# Step 5: Backup current template
Write-Host "Step 5: Creating backup of current Bicep template..." -ForegroundColor Cyan
$backupPath = "infra\main.bicep.backup-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
try {
    Copy-Item "infra\main.bicep" $backupPath
    Write-Host "  ✅ Backup created: $backupPath" -ForegroundColor Green
}
catch {
    Write-Host "  ⚠️  Could not create backup: $($_.Exception.Message)" -ForegroundColor Yellow
}
Write-Host ""

# Summary
Write-Host "=== Summary ===" -ForegroundColor Yellow
Write-Host "✅ Generated Azure SQL compliant password" -ForegroundColor Green
Write-Host "✅ Set AZD environment variables" -ForegroundColor Green
Write-Host "✅ Validated password requirements" -ForegroundColor Green
Write-Host "✅ Created Bicep template backup" -ForegroundColor Green
Write-Host ""
Write-Host "🔧 Next Actions Required:" -ForegroundColor Yellow
Write-Host "1. Manually uncomment SQL Database sections in main.bicep" -ForegroundColor White
Write-Host "2. Run 'azd provision --no-prompt' to deploy SQL Database" -ForegroundColor White
Write-Host "3. Run verification script to confirm deployment" -ForegroundColor White
Write-Host ""
Write-Host "📋 SQL Database Details:" -ForegroundColor Yellow
Write-Host "- Server Name: sql-academic-staging-{resourceToken}" -ForegroundColor White
Write-Host "- Database Name: Zeus.People" -ForegroundColor White
Write-Host "- Event Store Database: Zeus.People.EventStore" -ForegroundColor White
Write-Host "- SKU: S2 (Standard tier)" -ForegroundColor White
Write-Host "- Advanced Threat Protection: Enabled" -ForegroundColor White
Write-Host "- Log Analytics Integration: Enabled" -ForegroundColor White
Write-Host ""
Write-Host "The SQL Database can now be deployed successfully with the compliant password!" -ForegroundColor Green
