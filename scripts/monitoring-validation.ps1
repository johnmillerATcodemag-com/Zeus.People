<#!
.SYNOPSIS
Generates synthetic load, error scenarios, and latency spikes to validate monitoring, logging, metrics, tracing, and alerts.

.PARAMETER BaseUrl
Base URL of the Zeus.People API (e.g. https://zeus-people-api-staging.azurewebsites.net)

.PARAMETER DurationSeconds
Total duration to run the validation scenarios.

.PARAMETER Concurrency
Number of parallel request workers.

.PARAMETER IncludeErrorScenarios
If specified, injects controlled error responses.

.PARAMETER IncludeLatencySpikes
If specified, injects intentional slow requests.

.PARAMETER Verbose
Prints detailed progress and scenario narration.

.EXAMPLE
pwsh ./scripts/monitoring-validation.ps1 -BaseUrl https://api -DurationSeconds 120 -IncludeErrorScenarios -IncludeLatencySpikes -Verbose
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$BaseUrl,
    [int]$DurationSeconds = 120,
    [int]$Concurrency = 5,
    [switch]$IncludeErrorScenarios,
    [switch]$IncludeLatencySpikes,
    [switch]$Verbose
)

$ErrorActionPreference = 'Stop'
Write-Host "[Monitoring Validation] Starting at $(Get-Date -AsUTC)" -ForegroundColor Cyan
Write-Host "BaseUrl=$BaseUrl Duration=$DurationSeconds Concurrency=$Concurrency Errors=$IncludeErrorScenarios Latency=$IncludeLatencySpikes" -ForegroundColor DarkCyan

$stopWatch = [System.Diagnostics.Stopwatch]::StartNew()
$endTime = (Get-Date).AddSeconds($DurationSeconds)

$endpoints = @(
    '/api/academics',
    '/api/departments',
    '/api/rooms'
)

$client = New-Object System.Net.Http.HttpClient
$client.Timeout = [TimeSpan]::FromSeconds(30)

$summary = [ordered]@{
    TotalRequests = 0
    Success = 0
    ClientErrors = 0
    ServerErrors = 0
    LatencyInjected = 0
    ErrorsInjected = 0
}

function Invoke-RandomRequest {
    param([string]$Endpoint)
    $url = "$BaseUrl$Endpoint"
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        $resp = $client.GetAsync($url).GetAwaiter().GetResult()
        $sw.Stop()
        $summary.TotalRequests++
        $code = [int]$resp.StatusCode
        if ($code -ge 200 -and $code -lt 300) { $summary.Success++ }
        elseif ($code -ge 400 -and $code -lt 500) { $summary.ClientErrors++ }
        elseif ($code -ge 500) { $summary.ServerErrors++ }
        if ($Verbose) { Write-Host ("{0} {1}ms -> {2}" -f $Endpoint,$sw.ElapsedMilliseconds,$code) }
    }
    catch {
        $sw.Stop()
        $summary.TotalRequests++
        $summary.ServerErrors++
        if ($Verbose) { Write-Warning "Request failed $Endpoint $_" }
    }
}

function Invoke-ErrorScenario {
    if (-not $IncludeErrorScenarios) { return }
    # Call a likely invalid endpoint to force 404
    $invalid = "$BaseUrl/api/invalid-endpoint-$(Get-Random)"
    try { $null = $client.GetAsync($invalid).GetAwaiter().GetResult() } catch {}
    $summary.ErrorsInjected++
}

function Invoke-LatencyScenario {
    if (-not $IncludeLatencySpikes) { return }
    # Simulate client-side wait (server side latency spikes would need a dedicated endpoint)
    Start-Sleep -Milliseconds (Get-Random -Minimum 1200 -Maximum 2500)
    $summary.LatencyInjected++
}

$tasks = @()
while ((Get-Date) -lt $endTime) {
    for ($i=0;$i -lt $Concurrency;$i++) {
        $endpoint = Get-Random -InputObject $endpoints
        Invoke-RandomRequest -Endpoint $endpoint
        if ($IncludeErrorScenarios -and (Get-Random -Minimum 1 -Maximum 10) -le 2) { Invoke-ErrorScenario }
        if ($IncludeLatencySpikes -and (Get-Random -Minimum 1 -Maximum 15) -le 2) { Invoke-LatencyScenario }
    }
}

$stopWatch.Stop()

Write-Host "\n[Monitoring Validation Summary]" -ForegroundColor Cyan
$summary.GetEnumerator() | ForEach-Object { Write-Host ("{0}: {1}" -f $_.Key,$_.Value) }
Write-Host ("ElapsedSeconds: {0}" -f [math]::Round($stopWatch.Elapsed.TotalSeconds,2))

Write-Host "\nNext Steps:" -ForegroundColor Yellow
Write-Host "1. Open Application Insights > Logs and run KQL from MONITORING_VALIDATION_QUERIES.md"
Write-Host "2. Verify traces show dependency & request correlation"
Write-Host "3. Check custom metrics: perf = Performance.*, business = Business.*, http = HttpRequests.*"
Write-Host "4. Confirm alert rules fired if thresholds crossed"
Write-Host "5. Execute incident runbook if simulating outage"
