# Incident Response Runbook (Monitoring & Observability)

Applies to production & staging environments.

## 1. Classification
| Severity | Definition | Initial Target MTTA | MTTR |
|----------|------------|---------------------|------|
| Sev1 | Outage / critical API failure | < 5 min | < 60 min |
| Sev2 | Degradation / elevated errors | < 10 min | < 4 hrs |
| Sev3 | Minor impact / partial feature issue | < 30 min | < 1 day |

## 2. Detection
- Alerts fire from Azure Monitor (email/webhook/Teams)
- On-call engineer reviews alert payload (error rate, latency, dependency failure)
- Confirm in Application Insights Live Metrics + Logs

## 3. Triage Checklist
1. Check `Live Metrics` for CPU, memory, request rate, failure rate
2. Run KQL: recent failed requests (`requests | where success==false | take 20`)
3. Identify common operation_Id for correlation
4. Inspect dependencies for failure clusters
5. Review deployment history (was a new deployment just made?)
6. Check Key Vault / configuration access failures

## 4. Mitigation Paths
| Scenario | Action |
|----------|--------|
| High error rate (5xx) | Roll back deployment or restart App Service |
| Auth failures | Rotate secrets / validate AAD outage status |
| Service Bus backlog | Scale processing / inspect dead-letter queues |
| DB latency | Scale up tier / review slow queries |
| Memory leak suspicion | Capture memory dump & recycle instance |

## 5. Communication
- Open incident ticket (include correlation IDs, timeframe, suspected root cause)
- Update stakeholders every 15–30 min (Sev1/2)
- Document decisions & commands executed

## 6. Containment Commands (Examples)
```powershell
# Restart App Service
a z webapp restart --name <app> --resource-group <rg>

# Scale out plan
az appservice plan update --name <plan> --resource-group <rg> --number-of-workers 3

# List recent deployments
az webapp deployment list --name <app> --resource-group <rg>
```

## 7. Verification After Mitigation
1. Error rate returns < baseline threshold
2. Latency P95 within SLO band (<2s)
3. No sustained dependency failures
4. Alerts auto-resolve or manually close with justification
5. Business metrics recovering (e.g., request counters increment normally)

## 8. Post-Incident Review
| Artifact | Description |
|----------|-------------|
| Timeline | Chronological list of events/actions |
| Root Cause | Technical + contributing factors |
| Impact | Duration, affected users, functions |
| Resolution | Steps that ended impact |
| Follow-ups | Preventive actions, backlog tickets |

## 9. Preventive Improvements
- Add missing metrics (coverage gaps)
- Tighten alert thresholds if noisy
- Add synthetic canaries for critical pathways
- Automate rollback detection

## 10. Runbook Validation
Simulate once per quarter:
1. Induce non-destructive error spike (bad endpoint calls)
2. Induce artificial latency (client delay + heavy query test if available)
3. Validate alert fire + Slack/Teams channel notification
4. Measure MTTA + MTTR simulation
5. Record in operational log

---
Maintain this runbook with each platform or architecture change.
