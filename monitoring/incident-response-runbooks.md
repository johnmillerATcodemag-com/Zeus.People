# Zeus.People System - Incident Response Runbooks

## Table of Contents
1. [High Error Rate Response](#high-error-rate-response)
2. [Slow Response Time Response](#slow-response-time-response)
3. [Database Connection Failure Response](#database-connection-failure-response)
4. [Authentication Failure Response](#authentication-failure-response)
5. [Resource Exhaustion Response](#resource-exhaustion-response)
6. [Business Rule Violation Response](#business-rule-violation-response)
7. [Service Bus Issues Response](#service-bus-issues-response)

---

## High Error Rate Response

### Alert Condition
- Error rate exceeds 5% for 5 minutes
- Severity: 1 (High)

### Immediate Actions (0-15 minutes)
1. **Acknowledge the Alert**
   - Check Application Insights for error details
   - Identify the most common error types and affected endpoints

2. **Quick Assessment**
   ```kql
   requests
   | where timestamp > ago(15m)
   | where success == false
   | summarize count() by resultCode, name
   | order by count_ desc
   ```

3. **Determine Impact**
   - Check if errors are isolated to specific endpoints
   - Verify if database connections are healthy
   - Check authentication services status

### Investigation Steps (15-30 minutes)
1. **Deep Dive Analysis**
   ```kql
   requests
   | where timestamp > ago(30m) and success == false
   | join (exceptions | where timestamp > ago(30m)) on operation_Id
   | project timestamp, name, resultCode, type, outerMessage
   | order by timestamp desc
   ```

2. **Check Dependencies**
   ```kql
   dependencies
   | where timestamp > ago(30m)
   | where success == false
   | summarize count() by type, target
   ```

3. **Review Application Logs**
   - Check Serilog entries in Application Insights
   - Look for correlation patterns in exception details

### Resolution Actions
1. **Application Issues**: Restart the App Service if needed
2. **Database Issues**: Check connection strings and database availability
3. **External Dependencies**: Verify Service Bus, Key Vault, and other services
4. **Code Issues**: Consider rolling back recent deployments if applicable

### Follow-up (30+ minutes)
- Document root cause analysis
- Update monitoring thresholds if false positive
- Plan preventive measures

---

## Slow Response Time Response

### Alert Condition
- 95th percentile response time exceeds 2 seconds for 5 minutes
- Severity: 2 (Medium)

### Immediate Actions (0-15 minutes)
1. **Check Performance Dashboard**
   - Review response time trends
   - Identify slowest endpoints

2. **Quick Performance Query**
   ```kql
   requests
   | where timestamp > ago(15m)
   | summarize avg(duration), percentile(duration, 95) by name
   | order by percentile_duration_95 desc
   ```

### Investigation Steps (15-30 minutes)
1. **Database Performance Check**
   ```kql
   dependencies
   | where type == "SQL" and timestamp > ago(30m)
   | summarize avg(duration), count() by target, name
   | order by avg_duration desc
   ```

2. **Memory and Resource Analysis**
   ```kql
   customMetrics
   | where name startswith "Performance.Memory" or name startswith "Performance.ThreadPool"
   | where timestamp > ago(30m)
   | summarize avg(value) by name
   ```

3. **Check for Resource Contention**
   - Review CPU and Memory usage in App Service metrics
   - Check for thread pool starvation indicators

### Resolution Actions
1. **Scale Up**: Increase App Service plan if resource constrained
2. **Scale Out**: Add more instances if high load
3. **Database Optimization**: Check for long-running queries or locks
4. **Cache Issues**: Verify caching is working correctly

---

## Database Connection Failure Response

### Alert Condition
- Database connection failures detected
- Severity: 0 (Critical)

### Immediate Actions (0-5 minutes)
1. **Check Application Health**
   - Verify /health endpoint status
   - Check if application is responding

2. **Database Status Check**
   ```kql
   dependencies
   | where type == "SQL" and timestamp > ago(10m)
   | summarize SuccessRate = countif(success) * 100.0 / count() by target
   ```

### Investigation Steps (5-15 minutes)
1. **Connection String Validation**
   - Verify Key Vault secrets are accessible
   - Check if connection strings are properly configured

2. **Database Server Status**
   - Check Azure SQL Database metrics
   - Verify firewall rules
   - Check for maintenance windows

3. **Authentication Issues**
   - Verify managed identity permissions
   - Check for expired certificates or credentials

### Resolution Actions
1. **Connection Issues**: Update firewall rules or connection strings
2. **Authentication**: Refresh managed identity assignments
3. **Database Issues**: Contact Azure support if database server issue
4. **Fallback**: Switch to read-only mode if write database is down

---

## Authentication Failure Response

### Alert Condition
- Authentication failures exceed 10 per minute
- Severity: 1 (High)

### Immediate Actions (0-10 minutes)
1. **Check Authentication Pattern**
   ```kql
   requests
   | where timestamp > ago(15m) and resultCode in (401, 403)
   | summarize count() by url, client_IP, client_Type
   | order by count_ desc
   ```

2. **Identify Attack Pattern**
   - Check if failures come from single IP (potential attack)
   - Verify if legitimate users are affected

### Investigation Steps (10-20 minutes)
1. **JWT Token Analysis**
   ```kql
   traces
   | where timestamp > ago(30m)
   | where message contains "JWT" or message contains "Token"
   | order by timestamp desc
   ```

2. **Key Vault Access**
   - Verify JWT signing keys are accessible
   - Check Key Vault audit logs

3. **Azure AD Integration**
   - Check Azure AD logs for authentication issues
   - Verify application registration settings

### Resolution Actions
1. **Token Issues**: Refresh JWT signing keys
2. **Azure AD Issues**: Check service health and app registration
3. **Attack Response**: Implement IP blocking if malicious traffic detected
4. **Configuration**: Verify authentication middleware configuration

---

## Resource Exhaustion Response

### Alert Condition
- CPU usage > 80% for 10 minutes OR Memory usage > 80% for 10 minutes
- Severity: 2 (Medium)

### Immediate Actions (0-10 minutes)
1. **Check Current Resource Usage**
   - Review App Service metrics dashboard
   - Identify resource bottlenecks

2. **Performance Impact Assessment**
   ```kql
   requests
   | where timestamp > ago(15m)
   | summarize avg(duration), count() by bin(timestamp, 1m)
   | render timechart
   ```

### Investigation Steps (10-20 minutes)
1. **Memory Analysis**
   ```kql
   customMetrics
   | where name in ("Performance.Memory.WorkingSet", "Performance.Memory.GCTotalMemory")
   | where timestamp > ago(30m)
   | render timechart
   ```

2. **Thread Pool Analysis**
   ```kql
   customMetrics
   | where name startswith "Performance.ThreadPool"
   | where timestamp > ago(30m)
   | render timechart
   ```

### Resolution Actions
1. **Immediate Relief**: Scale up App Service plan
2. **Load Distribution**: Scale out to additional instances
3. **Memory Leaks**: Restart application if memory continuously growing
4. **Long-term**: Investigate memory usage patterns and optimize code

---

## Business Rule Violation Response

### Alert Condition
- Business rule violations exceed 5 per 15-minute window
- Severity: 2 (Medium)

### Immediate Actions (0-10 minutes)
1. **Identify Violation Types**
   ```kql
   customEvents
   | where name == "BusinessRuleEvaluation" and customDimensions.Passed == "False"
   | where timestamp > ago(30m)
   | summarize count() by tostring(customDimensions.RuleName)
   | order by count_ desc
   ```

2. **Check Data Integrity**
   - Review recent data changes
   - Verify no data corruption occurred

### Investigation Steps (10-20 minutes)
1. **Rule Analysis**
   ```kql
   customEvents
   | where name == "BusinessRuleEvaluation"
   | where timestamp > ago(1h)
   | extend RuleName = tostring(customDimensions.RuleName), Passed = tostring(customDimensions.Passed)
   | summarize Total = count(), Violations = countif(Passed == "False") by RuleName
   | extend ViolationRate = Violations * 100.0 / Total
   | order by ViolationRate desc
   ```

2. **Data Quality Check**
   - Query database directly for data validation
   - Check for recent bulk imports or changes

### Resolution Actions
1. **Rule Configuration**: Verify business rules are correctly configured
2. **Data Issues**: Investigate data quality problems
3. **Business Process**: Contact business stakeholders if legitimate violations
4. **System Issues**: Check for race conditions or concurrency problems

---

## Service Bus Issues Response

### Alert Condition
- Service Bus message backlog exceeds 100 messages
- Severity: 2 (Medium)

### Immediate Actions (0-10 minutes)
1. **Check Message Backlog**
   ```kql
   dependencies
   | where type == "Azure Service Bus" and timestamp > ago(15m)
   | summarize MessageCount = count(), AvgDuration = avg(duration)
   ```

2. **Verify Processing Status**
   - Check if message handlers are running
   - Review Service Bus namespace metrics

### Investigation Steps (10-20 minutes)
1. **Message Processing Analysis**
   ```kql
   traces
   | where timestamp > ago(30m) and message contains "ServiceBus"
   | order by timestamp desc
   ```

2. **Dead Letter Queue Check**
   - Review dead letter queue for failed messages
   - Check message processing errors

### Resolution Actions
1. **Processing Issues**: Restart message handlers
2. **Configuration**: Verify Service Bus connection strings
3. **Scaling**: Increase concurrent message processing
4. **Dead Letter**: Process dead letter messages manually if needed

---

## General Escalation Process

### Level 1 (0-30 minutes)
- On-call developer responds
- Follow runbook procedures
- Attempt immediate resolution

### Level 2 (30-60 minutes)
- Escalate to senior developer/architect
- Involve database administrator if needed
- Consider emergency maintenance window

### Level 3 (60+ minutes)
- Escalate to management
- Consider external vendor support
- Implement disaster recovery procedures

### Communication
1. **Incident Declaration**: Create incident ticket
2. **Status Updates**: Every 15 minutes during active incident
3. **Stakeholder Notification**: Business impact assessment
4. **Post-Incident**: Root cause analysis and lessons learned

---

## Contact Information

### Primary Contacts
- **On-Call Developer**: [Phone/Email]
- **Senior Developer**: [Phone/Email]  
- **Database Administrator**: [Phone/Email]
- **DevOps Engineer**: [Phone/Email]

### Escalation Contacts
- **Development Manager**: [Phone/Email]
- **IT Director**: [Phone/Email]
- **Business Owner**: [Phone/Email]

### External Support
- **Azure Support**: [Support Plan Details]
- **Vendor Support**: [Contact Information]

---

## Tools and Resources

### Monitoring Tools
- **Application Insights**: [URL]
- **Azure Portal**: [URL]
- **Log Analytics**: [URL]

### Documentation
- **System Architecture**: [Link]
- **Deployment Guide**: [Link] 
- **API Documentation**: [Link]

### Emergency Procedures
- **Rollback Process**: [Link]
- **Disaster Recovery**: [Link]
- **Emergency Contacts**: [Link]
