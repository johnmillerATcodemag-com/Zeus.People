# Script to run database migrations on the deployed Azure SQL database
# This uses the Migration API endpoint to run EF Core migrations

param(
    [string]$BaseUrl = "https://app-academic-dev-dyrtbsyffmtgk.azurewebsites.net",
    [switch]$UseLocalHost = $false,
    [switch]$TestConnectionOnly = $false
)

if ($UseLocalHost) {
    $BaseUrl = "https://localhost:7001"
}

Write-Host "🗃️  Database Migration Script for Zeus.People" -ForegroundColor Green
Write-Host "Target: $BaseUrl" -ForegroundColor Cyan

# Function to make API calls
function Invoke-MigrationApi {
    param(
        [string]$Endpoint,
        [string]$Method = "GET",
        [string]$Description
    )
    
    Write-Host "`n🔄 $Description..." -ForegroundColor Yellow
    
    try {
        $url = "$BaseUrl/api/migration/$Endpoint"
        Write-Host "   $Method $url" -ForegroundColor Gray
        
        $response = Invoke-RestMethod -Uri $url -Method $Method -ContentType "application/json" -TimeoutSec 60
        
        if ($response.success) {
            Write-Host "   ✅ Success!" -ForegroundColor Green
            return $response
        }
        else {
            Write-Host "   ❌ Failed: $($response.message)" -ForegroundColor Red
            if ($response.error) {
                Write-Host "   Error: $($response.error)" -ForegroundColor Red
            }
            return $response
        }
    }
    catch {
        Write-Host "   ❌ API call failed: $($_.Exception.Message)" -ForegroundColor Red
        return @{ success = $false; error = $_.Exception.Message }
    }
}

# Test database connection first
$connectionResult = Invoke-MigrationApi -Endpoint "test-connection" -Description "Testing database connectivity"

if ($connectionResult.success) {
    Write-Host "`n📊 Connection Status:" -ForegroundColor Cyan
    Write-Host "   Academic DB: $(if ($connectionResult.connections.academicDatabase.canConnect) { '✅ Connected' } else { '❌ Failed' })" -ForegroundColor $(if ($connectionResult.connections.academicDatabase.canConnect) { 'Green' } else { 'Red' })
    Write-Host "   Event Store DB: $(if ($connectionResult.connections.eventStoreDatabase.canConnect) { '✅ Connected' } else { '❌ Failed' })" -ForegroundColor $(if ($connectionResult.connections.eventStoreDatabase.canConnect) { 'Green' } else { 'Red' })
}
else {
    Write-Host "`n❌ Connection test failed. API may not be available." -ForegroundColor Red
}

if ($TestConnectionOnly) {
    Write-Host "`n✅ Connection test completed." -ForegroundColor Green
    exit 0
}

# Check migration status
$statusResult = Invoke-MigrationApi -Endpoint "status" -Description "Checking migration status"

if ($statusResult.success) {
    Write-Host "`n📈 Migration Status:" -ForegroundColor Cyan
    Write-Host "   Academic Context:" -ForegroundColor White
    Write-Host "     Applied: $($statusResult.academicContext.migrationCount) migrations" -ForegroundColor Gray
    Write-Host "     Pending: $($statusResult.academicContext.pendingCount) migrations" -ForegroundColor Gray
    
    Write-Host "   Event Store Context:" -ForegroundColor White
    Write-Host "     Applied: $($statusResult.eventStoreContext.migrationCount) migrations" -ForegroundColor Gray
    Write-Host "     Pending: $($statusResult.eventStoreContext.pendingCount) migrations" -ForegroundColor Gray
    
    if ($statusResult.academicContext.pendingCount -eq 0 -and $statusResult.eventStoreContext.pendingCount -eq 0) {
        Write-Host "`n✅ All migrations are up to date!" -ForegroundColor Green
        exit 0
    }
}

# Run migrations if needed
if ($statusResult.success -and ($statusResult.academicContext.pendingCount -gt 0 -or $statusResult.eventStoreContext.pendingCount -gt 0)) {
    Write-Host "`n🚀 Running pending migrations..." -ForegroundColor Yellow
    
    $migrationResult = Invoke-MigrationApi -Endpoint "run" -Method "POST" -Description "Running database migrations"
    
    if ($migrationResult.success) {
        Write-Host "`n🎉 Migrations completed successfully!" -ForegroundColor Green
        Write-Host "   Academic migrations: $($migrationResult.academicMigrations.Count)" -ForegroundColor White
        Write-Host "   Event Store migrations: $($migrationResult.eventStoreMigrations.Count)" -ForegroundColor White
    }
    else {
        Write-Host "`n💥 Migration failed!" -ForegroundColor Red
        exit 1
    }
}
else {
    Write-Host "`n⚠️  Could not determine migration status or API unavailable" -ForegroundColor Yellow
    Write-Host "   You may need to run migrations manually using:" -ForegroundColor Gray
    Write-Host "   - Azure Portal SQL Query Editor with migration.sql" -ForegroundColor Gray
    Write-Host "   - Or fix the API connectivity issues first" -ForegroundColor Gray
}

Write-Host "`n✅ Migration process completed!" -ForegroundColor Green
