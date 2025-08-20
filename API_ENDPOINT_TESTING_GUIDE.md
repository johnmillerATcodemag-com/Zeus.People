# Zeus.People API Endpoint Testing Guide

## API Base URLs
- HTTPS: https://localhost:7001
- HTTP: http://localhost:5001

## Testing Instructions

### 1. Health Check Endpoint ✅
```bash
# Test health endpoint
curl -k https://localhost:7001/health

# Expected: 200 OK with health status JSON
```

### 2. Swagger UI ✅
```bash
# Access Swagger UI
# Browser: https://localhost:7001/swagger/index.html
# Or try: https://localhost:7001/swagger
```

### 3. Academics Controller Endpoints

#### Get All Academics
```bash
# GET /api/academics (with pagination)
curl -k "https://localhost:7001/api/academics?pageNumber=1&pageSize=10"
```

#### Get Academic by ID
```bash
# GET /api/academics/{id}
curl -k "https://localhost:7001/api/academics/00000000-0000-0000-0000-000000000001"
```

#### Create Academic (Invalid Input Test)
```bash
# POST /api/academics (Test error handling)
curl -k -X POST "https://localhost:7001/api/academics" \
  -H "Content-Type: application/json" \
  -d '{
    "empNr": "",
    "empName": "",
    "rank": "InvalidRank"
  }'
```

#### Create Academic (Valid Input)
```bash
# POST /api/academics (Valid request)
curl -k -X POST "https://localhost:7001/api/academics" \
  -H "Content-Type: application/json" \
  -d '{
    "empNr": "EMP001",
    "empName": "Dr. John Smith",
    "rank": "Professor"
  }'
```

### 4. Departments Controller Endpoints

#### Get All Departments
```bash
# GET /api/departments
curl -k "https://localhost:7001/api/departments?pageNumber=1&pageSize=10"
```

#### Create Department
```bash
# POST /api/departments
curl -k -X POST "https://localhost:7001/api/departments" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Computer Science",
    "budget": 500000.00
  }'
```

### 5. Rooms Controller Endpoints

#### Get All Rooms
```bash
# GET /api/rooms
curl -k "https://localhost:7001/api/rooms"
```

### 6. Extensions Controller Endpoints

#### Get All Extensions (May require authentication)
```bash
# GET /api/extensions
curl -k "https://localhost:7001/api/extensions"
```

### 7. Reports Controller Endpoints

#### Get Academic Stats
```bash
# GET /api/reports/academics/stats
curl -k "https://localhost:7001/api/reports/academics/stats"
```

#### Get Dashboard Data
```bash
# GET /api/reports/dashboard
curl -k "https://localhost:7001/api/reports/dashboard"
```

## Error Testing Scenarios

### 1. Invalid JSON Format
```bash
curl -k -X POST "https://localhost:7001/api/academics" \
  -H "Content-Type: application/json" \
  -d '{"invalid": json}'
```

### 2. Missing Required Fields
```bash
curl -k -X POST "https://localhost:7001/api/academics" \
  -H "Content-Type: application/json" \
  -d '{}'
```

### 3. Invalid Data Types
```bash
curl -k -X POST "https://localhost:7001/api/academics" \
  -H "Content-Type: application/json" \
  -d '{
    "empNr": 123,
    "empName": null,
    "rank": "P"
  }'
```

## Authentication Testing

### Endpoints that may require authentication:
- GET /api/extensions (Based on test logs showing 401)
- GET /api/rooms (Based on test logs showing 401) 
- GET /api/reports/dashboard (Based on test logs showing 401)

### Test without authentication:
```bash
curl -k "https://localhost:7001/api/extensions"
# Expected: 401 Unauthorized
```

### Test with JWT token (if available):
```bash
# First get a token (implementation specific)
# Then test with Authorization header:
curl -k -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  "https://localhost:7001/api/extensions"
```

## PowerShell Testing Alternative

```powershell
# Test health endpoint
try {
    $response = Invoke-RestMethod -Uri "https://localhost:7001/health" -Method GET -SkipCertificateCheck
    Write-Host "Health Check: SUCCESS" -ForegroundColor Green
    $response | ConvertTo-Json
} catch {
    Write-Host "Health Check: FAILED - $($_.Exception.Message)" -ForegroundColor Red
}

# Test Academics endpoint
try {
    $response = Invoke-RestMethod -Uri "https://localhost:7001/api/academics" -Method GET -SkipCertificateCheck
    Write-Host "Academics Endpoint: SUCCESS" -ForegroundColor Green
    Write-Host "Total Count: $($response.totalCount)" -ForegroundColor Yellow
} catch {
    Write-Host "Academics Endpoint: FAILED - $($_.Exception.Message)" -ForegroundColor Red
}

# Test Swagger endpoint
try {
    $response = Invoke-WebRequest -Uri "https://localhost:7001/swagger/index.html" -Method GET -SkipCertificateCheck
    Write-Host "Swagger UI: SUCCESS - Status $($response.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "Swagger UI: FAILED - $($_.Exception.Message)" -ForegroundColor Red
}
```

## Expected Test Results

### ✅ Working Endpoints (Based on test execution):
- `/health` - Health checks (some may be unhealthy due to missing services)
- `/api/academics` - CRUD operations  
- `/api/departments` - Department management
- `/api/rooms` - Room listings (may require auth)
- `/api/extensions` - Extensions (may require auth)
- `/api/reports/academics/stats` - Academic statistics
- `/api/reports/dashboard` - Dashboard data (may require auth)

### 🔐 Authentication Required:
- Some endpoints return 401 Unauthorized without proper JWT token
- Test environment uses basic JWT authentication configuration

### 📋 Features Validated:
- RESTful endpoints with proper HTTP verbs
- Pagination support (pageNumber, pageSize parameters)
- Input validation and error handling
- JSON request/response format
- CORS configuration
- Health check endpoints
- Comprehensive logging
