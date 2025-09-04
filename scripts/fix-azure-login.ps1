# Fix all Azure login actions to use v1 with creds format for client secret authentication

# Read the file
$filePath = "C:\git\blogcode\Zeus.People\.github\workflows\ci-cd-pipeline.yml"
$content = Get-Content $filePath -Raw

# Replace all azure/login@v2 instances with v1 and proper creds format
$oldPattern = @"
      - name: Azure Login
        uses: azure/login@v2
        with:
          client-id: \$\{\{ secrets\.AZURE_CLIENT_ID \}\}
          tenant-id: \$\{\{ secrets\.AZURE_TENANT_ID \}\}
          subscription-id: \$\{\{ secrets\.AZURE_SUBSCRIPTION_ID \}\}
        env:
          AZURE_CLIENT_SECRET: \$\{\{ secrets\.AZURE_CLIENT_SECRET \}\}
"@

$newPattern = @"
      - name: Azure Login
        uses: azure/login@v1
        with:
          creds: |
            {
              "clientId": "`${{ secrets.AZURE_CLIENT_ID }}",
              "clientSecret": "`${{ secrets.AZURE_CLIENT_SECRET }}",
              "subscriptionId": "`${{ secrets.AZURE_SUBSCRIPTION_ID }}",
              "tenantId": "`${{ secrets.AZURE_TENANT_ID }}"
            }
"@

# Apply the replacement
$updatedContent = $content -replace $oldPattern, $newPattern, "RegexOptions.MultiLine"

# Write back to file
$updatedContent | Set-Content $filePath -NoNewline

Write-Host "Updated all Azure login actions in ci-cd-pipeline.yml"
