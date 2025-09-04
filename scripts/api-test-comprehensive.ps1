# API Testing Script for Zeus.People
# Tests CRUD operations after data seeding

param(
    [string]$BaseUrl = "https://app-academic-dev-dyrtbsyffmtgk.azurewebsites.net",
    [switch]$UseLocalHost = $false,
    [switch]$Verbose = $false
)

if ($UseLocalHost) {
    $BaseUrl = "https://localhost:7001"
}

if ($Verbose) {
    $VerbosePreference = "Continue"
}

Write-Host "🧪 API Testing for Zeus.People" -ForegroundColor Green
Write-Host "Target: $BaseUrl" -ForegroundColor Cyan

# Function to make API calls
function Test-ApiEndpoint {
    param(
        [string]$Name,
        [string]$Url,
        [string]$Method = "GET",
        [object]$Body = $null,
        [string]$ExpectedStatus = "Success"
    )
    
    Write-Host "`n🔍 Testing: $Name" -ForegroundColor Yellow
    Write-Host "   $Method $Url" -ForegroundColor Gray
    
    try {
        $params = @{
            Uri = $Url
            Method = $Method
            ContentType = "application/json"
            TimeoutSec = 30
        }
        
        if ($Body) {
            $params.Body = $Body | ConvertTo-Json -Depth 10
            Write-Verbose "Request Body: $($params.Body)"
        }
        
        $response = Invoke-RestMethod @params
        
        if ($response) {
            if ($response -is [Array]) {
                Write-Host "   ✅ Success - Returned $($response.Count) items" -ForegroundColor Green
                if ($response.Count -gt 0 -and $Verbose) {
                    Write-Host "   📄 Sample item:" -ForegroundColor Cyan
                    $response[0] | ConvertTo-Json -Depth 2 | Write-Host
                }
            }
            elseif ($response -is [PSCustomObject]) {
                Write-Host "   ✅ Success - Returned object" -ForegroundColor Green
                if ($Verbose) {
                    Write-Host "   📄 Response:" -ForegroundColor Cyan
                    $response | ConvertTo-Json -Depth 2 | Write-Host
                }
            }
            else {
                Write-Host "   ✅ Success - Response: $response" -ForegroundColor Green
            }
        }
        else {
            Write-Host "   ✅ Success - No content returned" -ForegroundColor Green
        }
        
        return @{ Success = $true; Data = $response }
    }
    catch {
        $statusCode = "Unknown"
        if ($_.Exception.Response) {
            $statusCode = [int]$_.Exception.Response.StatusCode
        }
        
        Write-Host "   ❌ Failed - Status: $statusCode" -ForegroundColor Red
        Write-Host "   ❌ Error: $($_.Exception.Message)" -ForegroundColor Red
        
        return @{ Success = $false; Error = $_.Exception.Message; StatusCode = $statusCode }
    }
}

# 1. Health Check
Write-Host "`n🏥 === HEALTH CHECK ===" -ForegroundColor Magenta
$healthResult = Test-ApiEndpoint -Name "Health Check" -Url "$BaseUrl/health"

if ($healthResult.Success -and $healthResult.Data.status -eq "Healthy") {
    Write-Host "🎉 API is healthy and ready for testing!" -ForegroundColor Green
} else {
    Write-Host "⚠️  API health issues detected. Proceeding with tests anyway..." -ForegroundColor Yellow
    if ($healthResult.Data.results) {
        Write-Host "`nComponent Status:" -ForegroundColor Yellow
        foreach ($component in $healthResult.Data.results.PSObject.Properties) {
            $status = $component.Value.status
            $color = if ($status -eq "Healthy") { "Green" } else { "Red" }
            Write-Host "  - $($component.Name): $status" -ForegroundColor $color
        }
    }
}

# 2. Academic Operations
Write-Host "`n👨‍🏫 === ACADEMIC OPERATIONS ===" -ForegroundColor Magenta

# Get all academics
$academicsResult = Test-ApiEndpoint -Name "Get All Academics" -Url "$BaseUrl/api/academics"

# Get academics with pagination
Test-ApiEndpoint -Name "Get Academics (Paginated)" -Url "$BaseUrl/api/academics?pageNumber=1&pageSize=5"

# Get academics with filters
Test-ApiEndpoint -Name "Get Academics (Filtered by Rank)" -Url "$BaseUrl/api/academics?rankFilter=P"

# Test specific academic by EmpNr if we have data
if ($academicsResult.Success -and $academicsResult.Data.items -and $academicsResult.Data.items.Count -gt 0) {
    $firstAcademic = $academicsResult.Data.items[0]
    
    # Get specific academic by ID
    Test-ApiEndpoint -Name "Get Academic by ID" -Url "$BaseUrl/api/academics/$($firstAcademic.id)"
    
    # Get academic by EmpNr
    Test-ApiEndpoint -Name "Get Academic by EmpNr" -Url "$BaseUrl/api/academics/by-emp-nr/$($firstAcademic.empNr)"
    
    Write-Host "`n📝 Sample Academic Data:" -ForegroundColor Cyan
    Write-Host "  ID: $($firstAcademic.id)" -ForegroundColor White
    Write-Host "  EmpNr: $($firstAcademic.empNr)" -ForegroundColor White
    Write-Host "  Name: $($firstAcademic.empName)" -ForegroundColor White
    Write-Host "  Rank: $($firstAcademic.rank)" -ForegroundColor White
    if ($firstAcademic.departmentName) {
        Write-Host "  Department: $($firstAcademic.departmentName)" -ForegroundColor White
    }
}

# 3. Department Operations
Write-Host "`n🏛️ === DEPARTMENT OPERATIONS ===" -ForegroundColor Magenta

# Get all departments
$departmentsResult = Test-ApiEndpoint -Name "Get All Departments" -Url "$BaseUrl/api/departments"

# Test specific department if we have data
if ($departmentsResult.Success -and $departmentsResult.Data -and $departmentsResult.Data.Count -gt 0) {
    $firstDepartment = $departmentsResult.Data[0]
    
    # Get specific department by ID
    Test-ApiEndpoint -Name "Get Department by ID" -Url "$BaseUrl/api/departments/$($firstDepartment.id)"
    
    # Get department by name
    $encodedName = [System.Web.HttpUtility]::UrlEncode($firstDepartment.name)
    Test-ApiEndpoint -Name "Get Department by Name" -Url "$BaseUrl/api/departments/by-name/$encodedName"
    
    # Get academics in department
    Test-ApiEndpoint -Name "Get Academics in Department" -Url "$BaseUrl/api/academics/by-department/$($firstDepartment.id)"
    
    Write-Host "`n📝 Sample Department Data:" -ForegroundColor Cyan
    Write-Host "  ID: $($firstDepartment.id)" -ForegroundColor White
    Write-Host "  Name: $($firstDepartment.name)" -ForegroundColor White
    if ($firstDepartment.totalAcademics) {
        Write-Host "  Total Academics: $($firstDepartment.totalAcademics)" -ForegroundColor White
    }
}

# 4. Extension Operations
Write-Host "`n📞 === EXTENSION OPERATIONS ===" -ForegroundColor Magenta

# Get all extensions
$extensionsResult = Test-ApiEndpoint -Name "Get All Extensions" -Url "$BaseUrl/api/extensions"

# 5. Room Operations
Write-Host "`n🚪 === ROOM OPERATIONS ===" -ForegroundColor Magenta

# Get all rooms
$roomsResult = Test-ApiEndpoint -Name "Get All Rooms" -Url "$BaseUrl/api/rooms"

# 6. Test CRUD Operations (Create, Read, Update, Delete)
Write-Host "`n📝 === CRUD OPERATIONS TEST ===" -ForegroundColor Magenta

# Create a test academic
$testAcademicData = @{
    EmpNr = "TS9999"
    EmpName = "Dr. Test Subject"
    Rank = "L"
}

$createResult = Test-ApiEndpoint -Name "Create Test Academic" -Url "$BaseUrl/api/academics" -Method "POST" -Body $testAcademicData

if ($createResult.Success) {
    $testAcademicId = $createResult.Data
    Write-Host "   📋 Created academic with ID: $testAcademicId" -ForegroundColor Green
    
    # Read the created academic
    $readResult = Test-ApiEndpoint -Name "Read Created Academic" -Url "$BaseUrl/api/academics/$testAcademicId"
    
    # Update the academic
    $updateData = @{
        Id = $testAcademicId
        EmpName = "Dr. Test Subject Updated"
        Rank = "SL"
        HomePhone = "555-TEST"
    }
    
    $updateResult = Test-ApiEndpoint -Name "Update Test Academic" -Url "$BaseUrl/api/academics/$testAcademicId" -Method "PUT" -Body $updateData
    
    if ($updateResult.Success) {
        # Verify the update
        Test-ApiEndpoint -Name "Verify Update" -Url "$BaseUrl/api/academics/$testAcademicId"
    }
    
    # Clean up - delete the test academic
    $deleteResult = Test-ApiEndpoint -Name "Delete Test Academic" -Url "$BaseUrl/api/academics/$testAcademicId" -Method "DELETE"
    
    if ($deleteResult.Success) {
        Write-Host "   🗑️  Test academic cleaned up successfully" -ForegroundColor Green
        
        # Verify deletion
        Test-ApiEndpoint -Name "Verify Deletion" -Url "$BaseUrl/api/academics/$testAcademicId"
    }
}

# 7. Error Handling Tests
Write-Host "`n🚨 === ERROR HANDLING TESTS ===" -ForegroundColor Magenta

# Test invalid ID
Test-ApiEndpoint -Name "Invalid Academic ID" -Url "$BaseUrl/api/academics/00000000-0000-0000-0000-000000000000"

# Test invalid EmpNr format
Test-ApiEndpoint -Name "Invalid EmpNr Format" -Url "$BaseUrl/api/academics/by-emp-nr/INVALID"

# Test creating academic with invalid data
$invalidAcademicData = @{
    EmpNr = ""  # Invalid: empty
    EmpName = "Test"
    Rank = "INVALID"  # Invalid rank
}
Test-ApiEndpoint -Name "Create Invalid Academic" -Url "$BaseUrl/api/academics" -Method "POST" -Body $invalidAcademicData

# Final Summary
Write-Host "`n🎉 === TESTING SUMMARY ===" -ForegroundColor Green
Write-Host "✅ Health check completed" -ForegroundColor Green
Write-Host "✅ Academic operations tested" -ForegroundColor Green
Write-Host "✅ Department operations tested" -ForegroundColor Green
Write-Host "✅ Extension operations tested" -ForegroundColor Green
Write-Host "✅ Room operations tested" -ForegroundColor Green
Write-Host "✅ CRUD operations tested" -ForegroundColor Green
Write-Host "✅ Error handling tested" -ForegroundColor Green

Write-Host "`n💡 Testing completed! Check the results above for any issues." -ForegroundColor Cyan
