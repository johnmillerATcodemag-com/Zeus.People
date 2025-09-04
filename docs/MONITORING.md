# Zeus.People Application Monitoring Setup

## Overview
This document outlines the monitoring and alerting setup for the Zeus.People application deployed to Azure.

## Key Metrics Monitored

### Application Performance
- **Response Time**: Average response time for HTTP requests
- **Request Count**: Total number of requests per minute/hour
- **Error Rate**: Percentage of failed requests (5xx status codes)
- **Availability**: Application uptime percentage

### Infrastructure Health
- **CPU Usage**: App Service CPU utilization
- **Memory Usage**: App Service memory consumption
- **Storage**: Disk usage and I/O operations
- **Network**: Incoming/outgoing network traffic

### Dependencies
- **Key Vault**: Secret access success/failure rates
- **Service Bus**: Message processing rates and errors
- **SQL Database**: Connection counts, query performance, DTU usage
- **Application Insights**: Custom telemetry and traces

## Alerts Configuration

### Critical Alerts (Immediate Action Required)
1. **High Error Rate**: When error rate > 5% for 5 minutes
2. **Application Down**: When availability < 99% for 2 minutes
3. **High Response Time**: When average response time > 5 seconds for 5 minutes
4. **Database Issues**: When database DTU > 80% for 10 minutes

### Warning Alerts (Monitor Closely)
1. **Memory Usage**: When memory > 85% for 15 minutes
2. **CPU Usage**: When CPU > 80% for 15 minutes
3. **Key Vault Access**: When key vault failures > 5 in 10 minutes

## Dashboard Components

### Real-time Monitoring
- Live metrics stream from Application Insights
- Request/response times
- Active user sessions
- Real-time error tracking

### Historical Analysis
- 24-hour performance trends
- Weekly/monthly usage patterns
- Error rate analysis over time
- Performance degradation tracking

## Health Check Endpoints

### Application Health
- **URL**: `/health`
- **Expected Response**: HTTP 200 with health status JSON
- **Check Frequency**: Every 60 seconds

### Detailed Health Checks
- **Database Connectivity**: `/health/database`
- **Key Vault Access**: `/health/keyvault`
- **Service Bus Connection**: `/health/servicebus`
- **External Dependencies**: `/health/dependencies`

## Incident Response

### Severity Levels
1. **Critical (P0)**: Application completely down, data loss risk
2. **High (P1)**: Major functionality impaired, user impact
3. **Medium (P2)**: Minor functionality issues, limited impact
4. **Low (P3)**: Cosmetic issues, no user impact

### Escalation Process
1. **L1 Support**: Initial response within 15 minutes
2. **Development Team**: Escalated after 30 minutes for P0/P1
3. **Management**: Notified for P0 issues or if P1 persists > 2 hours

## Monitoring Tools Integration

### Azure Monitor
- Centralized logging and metrics collection
- Custom queries using Kusto Query Language (KQL)
- Integration with Azure Logic Apps for automated responses

### Application Insights
- Detailed application performance monitoring
- User behavior analytics
- Custom event tracking
- Distributed tracing for microservices

### Log Analytics Workspace
- Centralized log storage and analysis
- Security monitoring and threat detection
- Performance baseline establishment
- Automated log retention policies

## Performance Baselines

### Normal Operation Ranges
- **Response Time**: < 1 second (95th percentile)
- **Error Rate**: < 1%
- **CPU Usage**: 10-40%
- **Memory Usage**: 30-70%
- **Availability**: > 99.9%

### Performance SLAs
- **Availability**: 99.9% uptime during business hours
- **Response Time**: 95% of requests < 2 seconds
- **Error Budget**: 0.1% monthly error budget
- **Recovery Time**: < 30 minutes for critical issues

## Automated Recovery Actions

### Auto-scaling Rules
- Scale out when CPU > 70% for 10 minutes
- Scale out when memory > 80% for 10 minutes
- Scale in when utilization < 30% for 30 minutes

### Failover Procedures
- Automatic failover to secondary region if primary fails
- Database connection retry policies with exponential backoff
- Circuit breaker pattern for external service calls

## Compliance and Auditing

### Security Monitoring
- Failed authentication attempts
- Unusual access patterns
- Configuration changes
- Security policy violations

### Audit Trails
- All administrative actions logged
- Configuration changes tracked
- Access to sensitive data monitored
- Compliance report generation

## Cost Optimization

### Resource Utilization Monitoring
- Track underutilized resources
- Identify optimization opportunities
- Monitor spending against budget alerts
- Regular cost analysis reports

### Recommendations
- Right-size compute resources based on usage patterns
- Implement cost-effective storage tiers
- Optimize database performance and costs
- Review and remove unused resources regularly
