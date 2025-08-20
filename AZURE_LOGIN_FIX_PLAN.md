# Azure Login Authentication Fix

## Problem

The Azure login steps in workflows are attempting OIDC authentication but need client secret authentication.

## Solution

All `azure/login@v2` steps need the `AZURE_CLIENT_SECRET` environment variable added.

## Files to Fix

- ci-cd-pipeline.yml (3 instances)
- staging-deployment.yml (4 instances)
- infrastructure-validation.yml (1 instance)
- emergency-rollback.yml (1 instance)
- test-staging-deployment.yml (1 instance)
- database-migration.yml (1 instance)
- monitoring.yml (1 instance)

## Target Pattern

```yaml
- name: Azure Login
  uses: azure/login@v2
  with:
    client-id: ${{ secrets.AZURE_CLIENT_ID }}
    tenant-id: ${{ secrets.AZURE_TENANT_ID }}
    subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
  env:
    AZURE_CLIENT_SECRET: ${{ secrets.AZURE_CLIENT_SECRET }}
```
