# Incident Response Runbook

## Alert Types and Responses

### High Error Rate Alert
**Trigger**: >5% error rate in 5 minutes
**Response**:
1. Check Application Insights for error details
2. Review recent deployments
3. Check database connectivity
4. Review application logs in Log Analytics

### Slow Response Time Alert  
**Trigger**: >2 seconds 95th percentile response time
**Response**:
1. Check system resource utilization
2. Review database performance
3. Analyze Application Insights performance data
4. Consider scaling if needed

### Database Connection Failures
**Trigger**: Database connection errors detected
**Response**:
1. Check SQL Database status in Azure portal
2. Verify connection strings and credentials
3. Check Key Vault access
4. Review database firewall rules

### Authentication Failures
**Trigger**: >10 authentication failures in 10 minutes
**Response**:
1. Check for potential security threats
2. Review authentication logs
3. Verify Azure AD configuration
4. Consider temporary account lockouts if needed

### Service Bus Message Backlog
**Trigger**: >100 active messages for 5 minutes
**Response**:
1. Check message processing performance
2. Scale out message processors if needed
3. Review message failure patterns
4. Consider increasing processing capacity

## Escalation Procedures

1. **Level 1**: Automated alerts to operations team
2. **Level 2**: If not resolved in 15 minutes, page on-call engineer
3. **Level 3**: If critical system down >30 minutes, notify management

## Contact Information

- Operations Team: john.miller@codemag.com
- On-call Engineer: john.miller@codemag.com
- Management: john.miller@codemag.com

## Monitoring Dashboards

- Application Insights: Azure Portal → Application Insights → 
- Azure Monitor: Azure Portal → Monitor → Metrics
- Log Analytics: Azure Portal → Log Analytics → Query logs

## Common Queries

### Error Analysis
`kusto
exceptions
| where timestamp > ago(1h)
| summarize count() by problemId, outerMessage
| order by count_ desc
`

### Performance Analysis
`kusto
requests
| where timestamp > ago(1h)
| summarize avg(duration), percentile(duration, 95) by name
| order by avg_duration desc
`

### Custom Metrics
`kusto
customEvents
| where timestamp > ago(1h)
| summarize count() by name
| order by count_ desc
`
