# Monitoring Validation KQL Queries

Use these queries in Application Insights / Log Analytics to validate telemetry after running `scripts/monitoring-validation.ps1`.

## 1. Request Volume & Performance
requests
| where timestamp > ago(15m)
| summarize count(), avg(duration), percentiles(duration,50,95,99) by bin(timestamp, 1m)
| order by timestamp asc

## 2. Error Rate
requests
| where timestamp > ago(15m)
| summarize Errors = countif(success == false), Total = count() by bin(timestamp, 5m)
| extend ErrorRate = todouble(Errors) / todouble(Total) * 100
| order by timestamp desc

## 3. Custom Business Events
customEvents
| where timestamp > ago(30m)
| where name startswith "Business." or name == "BusinessRuleEvaluation"
| project timestamp, name, tostring(customDimensions.RuleName), tostring(customDimensions.Passed), tostring(customDimensions.Method), tostring(customDimensions.StatusCode)

## 4. Performance Metrics
customEvents
| where timestamp > ago(30m)
| where name == "PerformanceMetric"
| project timestamp, Operation = tostring(customDimensions.OperationName), DurationMs = todouble(customMeasurements.Duration), Success = tostring(customDimensions.Success)
| summarize AvgDurationMs=avg(DurationMs), P95=percentile(DurationMs,95) by Operation

## 5. Custom Metrics (Gauge / Counter)
customMetrics
| where timestamp > ago(30m)
| where name startswith "Performance." or name startswith "Business." or name startswith "HttpRequests." 
| summarize Latest = any(value), Avg = avg(value) by name
| order by name asc

## 6. Dependency Calls
dependencies
| where timestamp > ago(30m)
| project timestamp, target, name, type, duration, success
| order by timestamp desc

## 7. High Latency Requests (Injected)
requests
| where timestamp > ago(30m)
| where duration > 1000ms
| project timestamp, name, duration, success, resultCode
| order by duration desc

## 8. Correlated Trace (Sample)
requests
| where timestamp > ago(15m)
| take 1
| project operation_Id
| join kind=inner (dependencies | project operation_Id, depTarget=target, depName=name, depDuration=duration) on operation_Id

## 9. Structured Logs (Serilog)
traces
| where timestamp > ago(30m)
| project timestamp, message, severityLevel, appName = tostring(customDimensions["Application"]), env = tostring(customDimensions["Environment"])
| order by timestamp desc

## 10. Health Check Filtering (Should Be Absent)
requests
| where timestamp > ago(30m)
| where name contains "/health"
| take 10

-- Expectation: No results due to custom telemetry processor filter.

## 11. Alert Threshold Simulation (Error Rate > 5%)
requests
| where timestamp > ago(30m)
| summarize Errors=countif(success==false), Total=count() 
| extend ErrorRatePct = todouble(Errors)/todouble(Total)*100

## 12. Auth Patterns
customMetrics
| where timestamp > ago(30m)
| where name == "Business.AuthenticatedRequest" or name == "Business.AnonymousRequest"
| summarize SumValue=sum(value) by name

---

Run queries in order to validate coverage across: Requests, Dependencies, Custom Events, Custom Metrics, Traces, Logs, Filtering, Correlation.
