# Comprehensive Data Seeding Script for Zeus.People API
# Creates realistic test data with relationships and various scenarios

param(
    [string]$BaseUrl = "https://app-academic-dev-dyrtbsyffmtgk.azurewebsites.net",
    [switch]$UseLocalHost = $false,
    [switch]$SkipHealthCheck = $false
)

if ($UseLocalHost) {
    $BaseUrl = "https://localhost:7001"
}

Write-Host "🌱 Comprehensive Data Seeding for Zeus.People API" -ForegroundColor Green
Write-Host "Target: $BaseUrl" -ForegroundColor Cyan

# Function to make API calls with proper error handling
function Invoke-ApiCall {
    param(
        [string]$Url,
        [string]$Method = "GET",
        [object]$Body = $null,
        [hashtable]$Headers = @{},
        [int]$TimeoutSec = 30
    )
    
    try {
        $params = @{
            Uri = $Url
            Method = $Method
            Headers = $Headers
            ContentType = "application/json"
            TimeoutSec = $TimeoutSec
        }
        
        if ($Body) {
            $params.Body = $Body | ConvertTo-Json -Depth 10
        }
        
        Write-Verbose "$Method $Url"
        if ($Body) { Write-Verbose "Body: $($params.Body)" }
        
        $response = Invoke-RestMethod @params
        return @{ Success = $true; Data = $response; StatusCode = 200 }
    }
    catch {
        $statusCode = $null
        if ($_.Exception.Response) {
            $statusCode = [int]$_.Exception.Response.StatusCode
        }
        
        Write-Warning "API call failed: $($_.Exception.Message)"
        return @{ 
            Success = $false; 
            Error = $_.Exception.Message; 
            StatusCode = $statusCode;
            Response = $_.Exception.Response
        }
    }
}

# Function to wait for API readiness
function Wait-ForApi {
    param([string]$HealthUrl, [int]$MaxAttempts = 30)
    
    if ($SkipHealthCheck) {
        Write-Host "⚠️  Skipping health check as requested" -ForegroundColor Yellow
        return $true
    }
    
    Write-Host "🏥 Checking API health..." -ForegroundColor Yellow
    $attempt = 0
    
    do {
        $attempt++
        try {
            $health = Invoke-RestMethod -Uri $HealthUrl -TimeoutSec 10
            Write-Host "Health Status: $($health.status)" -ForegroundColor $(if ($health.status -eq "Healthy") { "Green" } else { "Red" })
            
            if ($health.status -eq "Healthy") {
                Write-Host "✅ API is healthy and ready!" -ForegroundColor Green
                return $true
            }
            else {
                Write-Host "Components status:" -ForegroundColor Yellow
                if ($health.results) {
                    foreach ($component in $health.results.PSObject.Properties) {
                        $status = $component.Value.status
                        $color = if ($status -eq "Healthy") { "Green" } else { "Red" }
                        Write-Host "  - $($component.Name): $status" -ForegroundColor $color
                        if ($component.Value.description) {
                            Write-Host "    $($component.Value.description)" -ForegroundColor Gray
                        }
                    }
                }
            }
        }
        catch {
            Write-Host "❌ Health check failed: $($_.Exception.Message)" -ForegroundColor Red
        }
        
        if ($attempt -lt $MaxAttempts) {
            Write-Host "⏳ Waiting 5 seconds before retry... (Attempt $attempt/$MaxAttempts)" -ForegroundColor Yellow
            Start-Sleep -Seconds 5
        }
    } while ($attempt -lt $MaxAttempts)
    
    Write-Host "💥 API is not ready after $MaxAttempts attempts." -ForegroundColor Red
    return $false
}

# Data arrays for realistic seeding
$buildingData = @(
    @{ Name = "Computer Science Building" },
    @{ Name = "Mathematics and Statistics Building" },
    @{ Name = "Engineering Complex" },
    @{ Name = "Business School" },
    @{ Name = "Science Library" }
)

$departmentData = @(
    @{ Name = "Computer Science" },
    @{ Name = "Mathematics" },
    @{ Name = "Engineering" },
    @{ Name = "Business Administration" },
    @{ Name = "Physics" },
    @{ Name = "Chemistry" }
)

$academicData = @(
    # Computer Science Department
    @{ EmpNr = "CS1001"; EmpName = "Prof. Alan Turing"; Rank = "P" },
    @{ EmpNr = "CS1002"; EmpName = "Dr. Ada Lovelace"; Rank = "SL" },
    @{ EmpNr = "CS1003"; EmpName = "Dr. Tim Berners-Lee"; Rank = "P" },
    @{ EmpNr = "CS1004"; EmpName = "Dr. Grace Hopper"; Rank = "SL" },
    @{ EmpNr = "CS1005"; EmpName = "Dr. John McCarthy"; Rank = "L" },
    
    # Mathematics Department  
    @{ EmpNr = "MA2001"; EmpName = "Prof. Leonhard Euler"; Rank = "P" },
    @{ EmpNr = "MA2002"; EmpName = "Dr. Emmy Noether"; Rank = "SL" },
    @{ EmpNr = "MA2003"; EmpName = "Dr. Carl Gauss"; Rank = "P" },
    @{ EmpNr = "MA2004"; EmpName = "Dr. Sofia Kovalevskaya"; Rank = "L" },
    
    # Engineering Department
    @{ EmpNr = "EN3001"; EmpName = "Prof. Nikola Tesla"; Rank = "P" },
    @{ EmpNr = "EN3002"; EmpName = "Dr. Marie Curie"; Rank = "SL" },
    @{ EmpNr = "EN3003"; EmpName = "Dr. Thomas Edison"; Rank = "L" },
    
    # Business Administration
    @{ EmpNr = "BA4001"; EmpName = "Prof. Peter Drucker"; Rank = "P" },
    @{ EmpNr = "BA4002"; EmpName = "Dr. Mary Parker Follett"; Rank = "SL" },
    
    # Physics Department  
    @{ EmpNr = "PH5001"; EmpName = "Prof. Albert Einstein"; Rank = "P" },
    @{ EmpNr = "PH5002"; EmpName = "Dr. Rosalind Franklin"; Rank = "SL" },
    
    # Chemistry Department
    @{ EmpNr = "CH6001"; EmpName = "Prof. Dmitri Mendeleev"; Rank = "P" },
    @{ EmpNr = "CH6002"; EmpName = "Dr. Dorothy Hodgkin"; Rank = "L" }
)

$extensionData = @(
    @{ ExtNr = "1001" }, @{ ExtNr = "1002" }, @{ ExtNr = "1003" }, @{ ExtNr = "1004" }, @{ ExtNr = "1005" },
    @{ ExtNr = "2001" }, @{ ExtNr = "2002" }, @{ ExtNr = "2003" }, @{ ExtNr = "2004" },
    @{ ExtNr = "3001" }, @{ ExtNr = "3002" }, @{ ExtNr = "3003" },
    @{ ExtNr = "4001" }, @{ ExtNr = "4002" },
    @{ ExtNr = "5001" }, @{ ExtNr = "5002" },
    @{ ExtNr = "6001" }, @{ ExtNr = "6002" }
)

$roomData = @(
    # Computer Science Building rooms
    @{ RoomNr = "CS101"; BuildingIndex = 0 },
    @{ RoomNr = "CS102"; BuildingIndex = 0 },
    @{ RoomNr = "CS201"; BuildingIndex = 0 },
    @{ RoomNr = "CS202"; BuildingIndex = 0 },
    
    # Mathematics Building rooms
    @{ RoomNr = "MA101"; BuildingIndex = 1 },
    @{ RoomNr = "MA102"; BuildingIndex = 1 },
    @{ RoomNr = "MA201"; BuildingIndex = 1 },
    
    # Engineering Complex rooms
    @{ RoomNr = "EN101"; BuildingIndex = 2 },
    @{ RoomNr = "EN102"; BuildingIndex = 2 },
    @{ RoomNr = "EN201"; BuildingIndex = 2 }
)

# Wait for API to be ready
$healthUrl = "$BaseUrl/health"
if (-not (Wait-ForApi -HealthUrl $healthUrl)) {
    Write-Error "💥 API is not ready. Cannot proceed with data seeding."
    exit 1
}

Write-Host "`n🚀 Starting comprehensive data seeding..." -ForegroundColor Green

# Track created entities
$createdEntities = @{
    Buildings = @()
    Departments = @()
    Academics = @()
    Extensions = @()
    Rooms = @()
}

# 1. Create Buildings
Write-Host "`n🏢 Creating Buildings..." -ForegroundColor Cyan
foreach ($building in $buildingData) {
    $result = Invoke-ApiCall -Url "$BaseUrl/api/buildings" -Method POST -Body $building
    if ($result.Success) {
        $createdEntities.Buildings += @{ Name = $building.Name; Id = $result.Data }
        Write-Host "  ✅ Created building: $($building.Name)" -ForegroundColor Green
    } else {
        Write-Warning "  ❌ Failed to create building: $($building.Name) - $($result.Error)"
    }
}

# 2. Create Departments
Write-Host "`n🏛️ Creating Departments..." -ForegroundColor Cyan
foreach ($department in $departmentData) {
    $result = Invoke-ApiCall -Url "$BaseUrl/api/departments" -Method POST -Body $department
    if ($result.Success) {
        $createdEntities.Departments += @{ Name = $department.Name; Id = $result.Data }
        Write-Host "  ✅ Created department: $($department.Name)" -ForegroundColor Green
    } else {
        Write-Warning "  ❌ Failed to create department: $($department.Name) - $($result.Error)"
    }
}

# 3. Create Extensions
Write-Host "`n📞 Creating Extensions..." -ForegroundColor Cyan
foreach ($extension in $extensionData) {
    $result = Invoke-ApiCall -Url "$BaseUrl/api/extensions" -Method POST -Body $extension
    if ($result.Success) {
        $createdEntities.Extensions += @{ ExtNr = $extension.ExtNr; Id = $result.Data }
        Write-Host "  ✅ Created extension: $($extension.ExtNr)" -ForegroundColor Green
    } else {
        Write-Warning "  ❌ Failed to create extension: $($extension.ExtNr) - $($result.Error)"
    }
}

# 4. Create Rooms (depends on buildings)
if ($createdEntities.Buildings.Count -gt 0) {
    Write-Host "`n🚪 Creating Rooms..." -ForegroundColor Cyan
    foreach ($room in $roomData) {
        if ($room.BuildingIndex -lt $createdEntities.Buildings.Count) {
            $buildingId = $createdEntities.Buildings[$room.BuildingIndex].Id
            $roomBody = @{ 
                RoomNr = $room.RoomNr; 
                BuildingId = $buildingId 
            }
            
            $result = Invoke-ApiCall -Url "$BaseUrl/api/rooms" -Method POST -Body $roomBody
            if ($result.Success) {
                $createdEntities.Rooms += @{ 
                    RoomNr = $room.RoomNr; 
                    Id = $result.Data; 
                    BuildingId = $buildingId 
                }
                Write-Host "  ✅ Created room: $($room.RoomNr)" -ForegroundColor Green
            } else {
                Write-Warning "  ❌ Failed to create room: $($room.RoomNr) - $($result.Error)"
            }
        }
    }
}

# 5. Create Academics
Write-Host "`n👨‍🏫 Creating Academics..." -ForegroundColor Cyan
foreach ($academic in $academicData) {
    $result = Invoke-ApiCall -Url "$BaseUrl/api/academics" -Method POST -Body $academic
    if ($result.Success) {
        $createdEntities.Academics += @{ 
            EmpNr = $academic.EmpNr; 
            EmpName = $academic.EmpName; 
            Rank = $academic.Rank; 
            Id = $result.Data 
        }
        Write-Host "  ✅ Created academic: $($academic.EmpName) ($($academic.EmpNr)) - $($academic.Rank)" -ForegroundColor Green
    } else {
        Write-Warning "  ❌ Failed to create academic: $($academic.EmpName) - $($result.Error)"
    }
}

# 6. Assign Academics to Departments
if ($createdEntities.Academics.Count -gt 0 -and $createdEntities.Departments.Count -gt 0) {
    Write-Host "`n🔗 Assigning Academics to Departments..." -ForegroundColor Cyan
    
    # Define department assignments based on EmpNr prefixes
    $departmentAssignments = @{
        "CS" = "Computer Science"
        "MA" = "Mathematics" 
        "EN" = "Engineering"
        "BA" = "Business Administration"
        "PH" = "Physics"
        "CH" = "Chemistry"
    }
    
    foreach ($academic in $createdEntities.Academics) {
        $prefix = $academic.EmpNr.Substring(0, 2)
        $targetDeptName = $departmentAssignments[$prefix]
        
        if ($targetDeptName) {
            $targetDept = $createdEntities.Departments | Where-Object { $_.Name -eq $targetDeptName }
            if ($targetDept) {
                $result = Invoke-ApiCall -Url "$BaseUrl/api/academics/$($academic.Id)/assign-department" -Method PUT -Body @{ DepartmentId = $targetDept.Id }
                if ($result.Success) {
                    Write-Host "  ✅ Assigned $($academic.EmpName) to $targetDeptName" -ForegroundColor Green
                } else {
                    Write-Warning "  ❌ Failed to assign $($academic.EmpName) to $targetDeptName - $($result.Error)"
                }
            }
        }
    }
}

# 7. Assign Academics to Extensions and Rooms
if ($createdEntities.Academics.Count -gt 0) {
    Write-Host "`n📱 Assigning Extensions and Rooms..." -ForegroundColor Cyan
    
    $extensionIndex = 0
    $roomIndex = 0
    
    foreach ($academic in $createdEntities.Academics) {
        # Assign extension if available
        if ($extensionIndex -lt $createdEntities.Extensions.Count) {
            $extension = $createdEntities.Extensions[$extensionIndex]
            $result = Invoke-ApiCall -Url "$BaseUrl/api/academics/$($academic.Id)/assign-extension" -Method PUT -Body @{ ExtensionId = $extension.Id }
            if ($result.Success) {
                Write-Host "  ✅ Assigned extension $($extension.ExtNr) to $($academic.EmpName)" -ForegroundColor Green
            } else {
                Write-Warning "  ❌ Failed to assign extension to $($academic.EmpName) - $($result.Error)"
            }
            $extensionIndex++
        }
        
        # Assign room if available (only to professors and senior lecturers)
        if ($academic.Rank -in @("P", "SL") -and $roomIndex -lt $createdEntities.Rooms.Count) {
            $room = $createdEntities.Rooms[$roomIndex]
            $result = Invoke-ApiCall -Url "$BaseUrl/api/academics/$($academic.Id)/assign-room" -Method PUT -Body @{ RoomId = $room.Id }
            if ($result.Success) {
                Write-Host "  ✅ Assigned room $($room.RoomNr) to $($academic.EmpName)" -ForegroundColor Green
            } else {
                Write-Warning "  ❌ Failed to assign room to $($academic.EmpName) - $($result.Error)"
            }
            $roomIndex++
        }
    }
}

# 8. Set some academics as tenured (professors only)
Write-Host "`n🎓 Setting Tenure Status..." -ForegroundColor Cyan
$professors = $createdEntities.Academics | Where-Object { $_.Rank -eq "P" }
foreach ($professor in $professors[0..([math]::Min(2, $professors.Count - 1))]) {
    $result = Invoke-ApiCall -Url "$BaseUrl/api/academics/$($professor.Id)/tenure" -Method PUT -Body @{ IsTenured = $true }
    if ($result.Success) {
        Write-Host "  ✅ Granted tenure to $($professor.EmpName)" -ForegroundColor Green
    } else {
        Write-Warning "  ❌ Failed to grant tenure to $($professor.EmpName) - $($result.Error)"
    }
}

# Final Summary
Write-Host "`n🎉 Comprehensive data seeding completed!" -ForegroundColor Green
Write-Host "`n📊 Summary of created entities:" -ForegroundColor Yellow
Write-Host "  🏢 Buildings: $($createdEntities.Buildings.Count)" -ForegroundColor White
Write-Host "  🏛️  Departments: $($createdEntities.Departments.Count)" -ForegroundColor White
Write-Host "  👨‍🏫 Academics: $($createdEntities.Academics.Count)" -ForegroundColor White
Write-Host "  📞 Extensions: $($createdEntities.Extensions.Count)" -ForegroundColor White
Write-Host "  🚪 Rooms: $($createdEntities.Rooms.Count)" -ForegroundColor White

Write-Host "`n🧪 Test these endpoints:" -ForegroundColor Yellow
Write-Host "  GET $BaseUrl/api/academics" -ForegroundColor White
Write-Host "  GET $BaseUrl/api/departments" -ForegroundColor White
Write-Host "  GET $BaseUrl/api/academics/by-emp-nr/CS1001" -ForegroundColor White
Write-Host "  GET $BaseUrl/health" -ForegroundColor White

Write-Host "`n✨ Ready for comprehensive testing!" -ForegroundColor Green
