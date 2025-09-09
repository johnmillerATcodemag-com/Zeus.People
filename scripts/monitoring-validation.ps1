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

.NOTES
Use built-in -Verbose common parameter for detailed progress output.

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
    [string]$BearerToken,
    [switch]$GenerateDevToken,
    [string]$JwtSecret,
    [string]$JwtIssuer = 'dev-zeus-auth',
    [string]$JwtAudience = 'zeus-people-api',
    [switch]$AllowLocalExecution
)

$ErrorActionPreference = 'Stop'

# --- Execution Safety Guard -------------------------------------------------
# Enforce that performance / monitoring validation runs ONLY against a deployed
# environment (e.g. Azure App Service) unless explicitly overridden.
$isLocalUrl = ($BaseUrl -match 'localhost' -or $BaseUrl -match '127\.0\.0\.1' -or $BaseUrl -match '://0\.0\.0\.0' -or $BaseUrl -match '^http://')
if ($isLocalUrl -and -not $AllowLocalExecution) {
    Write-Error @"
[Safety Guard] Refusing to run performance/monitoring validation against local URL: $BaseUrl
Reason: Local execution can produce misleading results (no real network, disabled platform services, missing distributed telemetry) and may trigger heavy load on your dev machine.
Action: Re-run with a deployed environment base URL (e.g. https://zeus-people-api-staging.azurewebsites.net)
Override (if you really need to): add -AllowLocalExecution
"@
    exit 2
}

if ($isLocalUrl -and $AllowLocalExecution) {
    Write-Warning "[Safety Guard] Local execution override in effect (-AllowLocalExecution). Results are NOT production-representative." 
}

Write-Host "[Monitoring Validation] Starting at $(Get-Date -AsUTC)" -ForegroundColor Cyan
Write-Host "BaseUrl=$BaseUrl Duration=$DurationSeconds Concurrency=$Concurrency Errors=$IncludeErrorScenarios Latency=$IncludeLatencySpikes AllowLocal=$AllowLocalExecution" -ForegroundColor DarkCyan

$stopWatch = [System.Diagnostics.Stopwatch]::StartNew()
$endTime = (Get-Date).AddSeconds($DurationSeconds)

$endpoints = @(
    '/api/academics',
    '/api/departments',
    '/api/rooms'
)

$client = New-Object System.Net.Http.HttpClient
$client.Timeout = [TimeSpan]::FromSeconds(30)

function Convert-ToBase64Url {
    param([byte[]]$Bytes)
    return ([Convert]::ToBase64String($Bytes).TrimEnd('=')).Replace('+','-').Replace('/','_')
}

function New-DevJwt {
    param(
        [Parameter(Mandatory)] [string]$Secret,
        [Parameter(Mandatory)] [string]$Issuer,
        [Parameter(Mandatory)] [string]$Audience,
        [int]$ExpiresMinutes = 60,
        [string]$Subject = 'dev-user'
    )
    $headerJson = '{"alg":"HS256","typ":"JWT"}'
    $now = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
    $exp = ([DateTimeOffset]::UtcNow.AddMinutes($ExpiresMinutes)).ToUnixTimeSeconds()
    $jti = [guid]::NewGuid().ToString()
    $payload = [ordered]@{
        iss = $Issuer
        aud = $Audience
        sub = $Subject
        name = 'Dev User'
        iat = $now
        nbf = $now
        exp = $exp
        jti = $jti
        role = 'Tester'
    } | ConvertTo-Json -Compress

    $headerB64 = Convert-ToBase64Url ([Text.Encoding]::UTF8.GetBytes($headerJson))
    $payloadB64 = Convert-ToBase64Url ([Text.Encoding]::UTF8.GetBytes($payload))
    $unsigned = "$headerB64.$payloadB64"
    $keyBytes = [Text.Encoding]::UTF8.GetBytes($Secret)
    $hmac = [System.Security.Cryptography.HMACSHA256]::new($keyBytes)
    $signature = Convert-ToBase64Url ($hmac.ComputeHash([Text.Encoding]::UTF8.GetBytes($unsigned)))
    return "$unsigned.$signature"
}

if ($GenerateDevToken -and -not $BearerToken) {
    if (-not $JwtSecret) { throw "JwtSecret is required when using -GenerateDevToken" }
    Write-Host "[Auth] Generating dev JWT (Issuer=$JwtIssuer Audience=$JwtAudience)" -ForegroundColor DarkYellow
    $BearerToken = New-DevJwt -Secret $JwtSecret -Issuer $JwtIssuer -Audience $JwtAudience
}

if ($BearerToken) {
    try {
        $client.DefaultRequestHeaders.Authorization = New-Object System.Net.Http.Headers.AuthenticationHeaderValue('Bearer',$BearerToken)
        Write-Host "[Auth] Using bearer token (length=$($BearerToken.Length))" -ForegroundColor DarkYellow
    } catch { Write-Warning "Failed to set Authorization header: $_" }
}

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
    if ($VerbosePreference -eq 'Continue') { Write-Host ("{0} {1}ms -> {2}" -f $Endpoint,$sw.ElapsedMilliseconds,$code) }
    }
    catch {
        $sw.Stop()
        $summary.TotalRequests++
        $summary.ServerErrors++
    if ($VerbosePreference -eq 'Continue') { Write-Warning "Request failed $Endpoint $_" }
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
Write-Host "1. If Success=0 and all 401, provide a bearer token (-BearerToken) or generate one (-GenerateDevToken -JwtSecret <secret>)"
Write-Host "2. Start API with matching env vars: Jwt__Secret / Jwt__Issuer / Jwt__Audience before running script"
Write-Host "3. Open Application Insights > Logs and run KQL from MONITORING_VALIDATION_QUERIES.md"
Write-Host "4. Verify traces show dependency & request correlation"
Write-Host "5. Check custom metrics: perf = Performance.*, business = Business.*, http = HttpRequests.*"
Write-Host "6. Confirm alert rules fired if thresholds crossed"
Write-Host "7. Execute incident runbook if simulating outage"
