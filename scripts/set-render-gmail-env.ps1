# Copy NHATDUC_GMAIL_REFRESH_TOKEN vao clipboard va mo trang Environment tren Render.
$secretsPath = Join-Path $PSScriptRoot "..\NhatDucSoftware.Web\appsettings.Secrets.json"
$tokenPath = Join-Path $PSScriptRoot "google-gmail-token.json"

$token = $null
if (Test-Path $secretsPath) {
    $secrets = Get-Content $secretsPath -Raw | ConvertFrom-Json
    $token = $secrets.GoogleDrive.GmailRefreshToken
}
if (-not $token -and (Test-Path $tokenPath)) {
    $token = (Get-Content $tokenPath -Raw | ConvertFrom-Json).refresh_token
}

if (-not $token) {
    Write-Error "Chua co Gmail refresh token. Chay: python scripts/generate-gmail-token.py"
    exit 1
}

Set-Clipboard -Value $token
Write-Host "Da copy NHATDUC_GMAIL_REFRESH_TOKEN vao clipboard."
Write-Host ""
Write-Host "Tren Render:"
Write-Host "  1. Add Environment Variable"
Write-Host "  2. Key: NHATDUC_GMAIL_REFRESH_TOKEN"
Write-Host "  3. Value: Ctrl+V (da copy san)"
Write-Host "  4. Save Changes -> doi redeploy"
Write-Host ""

$renderUrl = "https://dashboard.render.com/web/srv-d8k2b9eq1p3s7fchag/env"
Start-Process $renderUrl
Write-Host "Da mo: $renderUrl"
