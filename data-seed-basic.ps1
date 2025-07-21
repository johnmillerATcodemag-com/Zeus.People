# Basic Data Seeding Script for Zeus.People API
# Creates minimal test data for basic functionality testing

param(
    [string]$BaseUrl = "https://app-academic-dev-dyrtbsyffmtgk.azurewebsites.net",
    [switch]$UseLocalHost = $false
)

if ($UseLocalHost) {
    $BaseUrl = "https://localhost:7001"
}

Write-Host "Seeding basic data to: $BaseUrl" -ForegroundColor Green

# Function to make API calls with proper headers
function Invoke-ApiCall {
    param(
        [string]$Url,
        [string]$Method = "GET",
        [object]$Body = $null,
        [hashtable]$Headers = @{}
    )
    
    try {
        $params = @{
            Uri = $Url
            Method = $Method
            Headers = $Headers
            ContentType = "application/json"
        }
        
        if ($Body) {
            $params.Body = $Body | ConvertTo-Json -Depth 10
        }
        
        $response = Invoke-RestMethod @params
        return @{ Success = $true; Data = $response }
    }
    catch {
        Write-Warning "API call failed: $($_.Exception.Message)"
        return @{ Success = $false; Error = $_.Exception.Message }
    }
}

# Function to wait for API to be ready
function Wait-ForApi {
    param([string]$HealthUrl)
    
    Write-Host "Checking API health..." -ForegroundColor Yellow
    $maxAttempts = 30
    $attempt = 0
    
    do {
        $attempt++
        try {
            $health = Invoke-RestMethod -Uri $HealthUrl -TimeoutSec 10
            if ($health.status -eq "Healthy") {
                Write-Host "API is healthy!" -ForegroundColor Green
                return $true
            }
            elseif ($health.status -eq "Unhealthy") {
                Write-Host "API is unhealthy. Status: $($health | ConvertTo-Json -Depth 2)" -ForegroundColor Red
                if ($attempt -lt $maxAttempts) {
                    Write-Host "Waiting 5 seconds before retry... (Attempt $attempt/$maxAttempts)" -ForegroundColor Yellow
                    Start-Sleep -Seconds 5
                }
            }
        }
        catch {
            Write-Host "Health check failed: $($_.Exception.Message)" -ForegroundColor Red
            if ($attempt -lt $maxAttempts) {
                Write-Host "Waiting 5 seconds before retry... (Attempt $attempt/$maxAttempts)" -ForegroundColor Yellow
                Start-Sleep -Seconds 5
            }
        }
    } while ($attempt -lt $maxAttempts)
    
    Write-Host "API is not ready after $maxAttempts attempts." -ForegroundColor Red
    return $false
}

# Wait for API to be ready
$healthUrl = "$BaseUrl/health"
if (-not (Wait-ForApi -HealthUrl $healthUrl)) {
    Write-Error "API is not ready. Cannot proceed with data seeding."
    exit 1
}

Write-Host "Starting data seeding..." -ForegroundColor Green

# 1. Create Buildings
Write-Host "`n1. Creating Buildings..." -ForegroundColor Cyan
$buildings = @(
    @{ Name = "Computer Science Building" },
    @{ Name = "Mathematics Building" },
    @{ Name = "Engineering Building" }
)

$createdBuildings = @()
foreach ($building in $buildings) {
    $result = Invoke-ApiCall -Url "$BaseUrl/api/buildings" -Method POST -Body $building
    if ($result.Success) {
        $createdBuildings += @{ Name = $building.Name; Id = $result.Data }
        Write-Host "  ✓ Created building: $($building.Name)" -ForegroundColor Green
    } else {
        Write-Warning "  ✗ Failed to create building: $($building.Name) - $($result.Error)"
    }
}

# 2. Create Departments
Write-Host "`n2. Creating Departments..." -ForegroundColor Cyan
$departments = @(
    @{ Name = "Computer Science" },
    @{ Name = "Mathematics" },
    @{ Name = "Engineering" }
)

$createdDepartments = @()
foreach ($department in $departments) {
    $result = Invoke-ApiCall -Url "$BaseUrl/api/departments" -Method POST -Body $department
    if ($result.Success) {
        $createdDepartments += @{ Name = $department.Name; Id = $result.Data }
        Write-Host "  ✓ Created department: $($department.Name)" -ForegroundColor Green
    } else {
        Write-Warning "  ✗ Failed to create department: $($department.Name) - $($result.Error)"
    }
}

# 3. Create Academics
Write-Host "`n3. Creating Academics..." -ForegroundColor Cyan
$academics = @(
    @{ EmpNr = "AB1234"; EmpName = "Dr. John Smith"; Rank = "P" },        # Professor
    @{ EmpNr = "CD5678"; EmpName = "Dr. Jane Doe"; Rank = "SL" },         # Senior Lecturer
    @{ EmpNr = "EF9012"; EmpName = "Dr. Bob Johnson"; Rank = "L" },       # Lecturer
    @{ EmpNr = "GH3456"; EmpName = "Dr. Alice Brown"; Rank = "P" },       # Professor
    @{ EmpNr = "IJ7890"; EmpName = "Dr. Charlie Wilson"; Rank = "SL" },   # Senior Lecturer
    @{ EmpNr = "KL2345"; EmpName = "Dr. Diana Clark"; Rank = "L" }        # Lecturer
)

$createdAcademics = @()
foreach ($academic in $academics) {
    $result = Invoke-ApiCall -Url "$BaseUrl/api/academics" -Method POST -Body $academic
    if ($result.Success) {
        $createdAcademics += @{ 
            EmpNr = $academic.EmpNr; 
            EmpName = $academic.EmpName; 
            Rank = $academic.Rank; 
            Id = $result.Data 
        }
        Write-Host "  ✓ Created academic: $($academic.EmpName) ($($academic.EmpNr))" -ForegroundColor Green
    } else {
        Write-Warning "  ✗ Failed to create academic: $($academic.EmpName) - $($result.Error)"
    }
}

# 4. Create Extensions
Write-Host "`n4. Creating Extensions..." -ForegroundColor Cyan
$extensions = @(
    @{ ExtNr = "1001" },
    @{ ExtNr = "1002" },
    @{ ExtNr = "1003" },
    @{ ExtNr = "2001" },
    @{ ExtNr = "2002" },
    @{ ExtNr = "3001" }
)

$createdExtensions = @()
foreach ($extension in $extensions) {
    $result = Invoke-ApiCall -Url "$BaseUrl/api/extensions" -Method POST -Body $extension
    if ($result.Success) {
        $createdExtensions += @{ ExtNr = $extension.ExtNr; Id = $result.Data }
        Write-Host "  ✓ Created extension: $($extension.ExtNr)" -ForegroundColor Green
    } else {
        Write-Warning "  ✗ Failed to create extension: $($extension.ExtNr) - $($result.Error)"
    }
}

# 5. Assign Academics to Departments (if both were created successfully)
if ($createdAcademics.Count -gt 0 -and $createdDepartments.Count -gt 0) {
    Write-Host "`n5. Assigning Academics to Departments..." -ForegroundColor Cyan
    
    # Assign first 2 academics to Computer Science
    if ($createdDepartments.Count -ge 1 -and $createdAcademics.Count -ge 2) {
        $csDeptId = ($createdDepartments | Where-Object { $_.Name -eq "Computer Science" }).Id
        foreach ($academic in $createdAcademics[0..1]) {
            $result = Invoke-ApiCall -Url "$BaseUrl/api/academics/$($academic.Id)/assign-department" -Method PUT -Body @{ DepartmentId = $csDeptId }
            if ($result.Success) {
                Write-Host "  ✓ Assigned $($academic.EmpName) to Computer Science" -ForegroundColor Green
            } else {
                Write-Warning "  ✗ Failed to assign $($academic.EmpName) to Computer Science - $($result.Error)"
            }
        }
    }
    
    # Assign next 2 academics to Mathematics
    if ($createdDepartments.Count -ge 2 -and $createdAcademics.Count -ge 4) {
        $mathDeptId = ($createdDepartments | Where-Object { $_.Name -eq "Mathematics" }).Id
        foreach ($academic in $createdAcademics[2..3]) {
            $result = Invoke-ApiCall -Url "$BaseUrl/api/academics/$($academic.Id)/assign-department" -Method PUT -Body @{ DepartmentId = $mathDeptId }
            if ($result.Success) {
                Write-Host "  ✓ Assigned $($academic.EmpName) to Mathematics" -ForegroundColor Green
            } else {
                Write-Warning "  ✗ Failed to assign $($academic.EmpName) to Mathematics - $($result.Error)"
            }
        }
    }
    
    # Assign remaining academics to Engineering
    if ($createdDepartments.Count -ge 3 -and $createdAcademics.Count -ge 6) {
        $engDeptId = ($createdDepartments | Where-Object { $_.Name -eq "Engineering" }).Id
        foreach ($academic in $createdAcademics[4..5]) {
            $result = Invoke-ApiCall -Url "$BaseUrl/api/academics/$($academic.Id)/assign-department" -Method PUT -Body @{ DepartmentId = $engDeptId }
            if ($result.Success) {
                Write-Host "  ✓ Assigned $($academic.EmpName) to Engineering" -ForegroundColor Green
            } else {
                Write-Warning "  ✗ Failed to assign $($academic.EmpName) to Engineering - $($result.Error)"
            }
        }
    }
}

Write-Host "`n✅ Basic data seeding completed!" -ForegroundColor Green
Write-Host "`nCreated:" -ForegroundColor Yellow
Write-Host "  - $($createdBuildings.Count) Buildings" -ForegroundColor White
Write-Host "  - $($createdDepartments.Count) Departments" -ForegroundColor White
Write-Host "  - $($createdAcademics.Count) Academics" -ForegroundColor White
Write-Host "  - $($createdExtensions.Count) Extensions" -ForegroundColor White

Write-Host "`nTo test the API, try these endpoints:" -ForegroundColor Yellow
Write-Host "  GET $BaseUrl/api/academics" -ForegroundColor White
Write-Host "  GET $BaseUrl/api/departments" -ForegroundColor White
Write-Host "  GET $BaseUrl/health" -ForegroundColor White
