# Parse Coverage Results and Generate Coverage Report

$coverageFiles = @(
    "tests\Zeus.People.Domain.Tests\TestResults\ffca8b7a-61b2-4300-b0b9-3511e8c72b4f\coverage.cobertura.xml",
    "tests\Zeus.People.Application.Tests\TestResults\4a638ed7-121c-4451-a9fe-68243d8b4b2a\coverage.cobertura.xml", 
    "tests\Zeus.People.Infrastructure.Tests\TestResults\669b22d4-8889-4dcd-b4b5-221c58747525\coverage.cobertura.xml",
    "tests\Zeus.People.API.Tests\TestResults\1ec57f0d-c23e-4169-bf9d-897f9340d088\coverage.cobertura.xml"
)

Write-Host "=== Zeus.People Test Coverage Analysis ===" -ForegroundColor Cyan
Write-Host ""

$totalLinesCovered = 0
$totalLinesValid = 0
$totalBranchesCovered = 0
$totalBranchesValid = 0

foreach ($file in $coverageFiles) {
    $fullPath = Join-Path $PSScriptRoot $file
    if (Test-Path $fullPath) {
        Write-Host "Processing: $file" -ForegroundColor Yellow
        
        [xml]$xml = Get-Content $fullPath
        $coverage = $xml.coverage
        
        $lineRate = [double]$coverage.'line-rate'
        $branchRate = [double]$coverage.'branch-rate'
        $linesCovered = [int]$coverage.'lines-covered'
        $linesValid = [int]$coverage.'lines-valid'
        $branchesCovered = [int]$coverage.'branches-covered'
        $branchesValid = [int]$coverage.'branches-valid'
        
        $totalLinesCovered += $linesCovered
        $totalLinesValid += $linesValid
        $totalBranchesCovered += $branchesCovered
        $totalBranchesValid += $branchesValid
        
        Write-Host "  Line Coverage: $($lineRate * 100)% ($linesCovered/$linesValid)" -ForegroundColor White
        Write-Host "  Branch Coverage: $($branchRate * 100)% ($branchesCovered/$branchesValid)" -ForegroundColor White
        Write-Host ""
    }
}

# Calculate overall coverage
$overallLineRate = if ($totalLinesValid -gt 0) { $totalLinesCovered / $totalLinesValid } else { 0 }
$overallBranchRate = if ($totalBranchesValid -gt 0) { $totalBranchesCovered / $totalBranchesValid } else { 0 }

Write-Host "=== OVERALL COVERAGE SUMMARY ===" -ForegroundColor Cyan
Write-Host "Total Line Coverage: $($overallLineRate * 100)% ($totalLinesCovered/$totalLinesValid)" -ForegroundColor $(if ($overallLineRate -ge 0.8) { "Green" } else { "Red" })
Write-Host "Total Branch Coverage: $($overallBranchRate * 100)% ($totalBranchesCovered/$totalBranchesValid)" -ForegroundColor $(if ($overallBranchRate -ge 0.8) { "Green" } else { "Red" })
Write-Host ""

$meetsRequirement = $overallLineRate -ge 0.8
Write-Host "Coverage Requirement (>80%): $(if ($meetsRequirement) { '✅ PASSED' } else { '❌ FAILED' })" -ForegroundColor $(if ($meetsRequirement) { "Green" } else { "Red" })

if (-not $meetsRequirement) {
    $needed = 0.8 - $overallLineRate
    $additionalLines = [math]::Ceiling($needed * $totalLinesValid)
    Write-Host "Additional lines needed for 80% coverage: $additionalLines" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== COVERAGE BY PROJECT ===" -ForegroundColor Cyan

# Project-specific analysis
$projects = @{
    "Domain" = "tests\Zeus.People.Domain.Tests\TestResults\ffca8b7a-61b2-4300-b0b9-3511e8c72b4f\coverage.cobertura.xml"
    "Application" = "tests\Zeus.People.Application.Tests\TestResults\4a638ed7-121c-4451-a9fe-68243d8b4b2a\coverage.cobertura.xml"
    "Infrastructure" = "tests\Zeus.People.Infrastructure.Tests\TestResults\669b22d4-8889-4dcd-b4b5-221c58747525\coverage.cobertura.xml"
    "API" = "tests\Zeus.People.API.Tests\TestResults\1ec57f0d-c23e-4169-bf9d-897f9340d088\coverage.cobertura.xml"
}

foreach ($project in $projects.GetEnumerator()) {
    $fullPath = Join-Path $PSScriptRoot $project.Value
    if (Test-Path $fullPath) {
        [xml]$xml = Get-Content $fullPath
        $coverage = $xml.coverage
        $lineRate = [double]$coverage.'line-rate'
        
        $status = if ($lineRate -ge 0.8) { "✅" } else { "⚠️" }
        Write-Host "$($project.Key): $($lineRate * 100)% $status" -ForegroundColor $(if ($lineRate -ge 0.8) { "Green" } else { "Yellow" })
    }
}
