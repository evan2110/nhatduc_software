# Chay web + Cloudflare Tunnel (mien phi, URL cong khai tam thoi)
# Yeu cau: da publish vao publish/web (dotnet publish -c Release -o publish/web)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$appDir = Join-Path $root "publish\web"
$port = 5050

if (-not (Test-Path (Join-Path $appDir "NhatDucSoftware.Web.dll"))) {
    Write-Host "Dang publish..."
    dotnet publish (Join-Path $root "NhatDucSoftware.Web\NhatDucSoftware.Web.csproj") -c Release -o $appDir
}

$password = $env:SUPABASE_DB_PASSWORD
if (-not $password) {
    $password = Read-Host "Nhap SUPABASE_DB_PASSWORD"
}

Write-Host "Khoi dong web tai port $port..."
$webJob = Start-Job {
    param($dir, $port, $pwd)
    $env:ASPNETCORE_ENVIRONMENT = "Production"
    $env:ASPNETCORE_URLS = "http://127.0.0.1:$port"
    $env:SUPABASE_DB_PASSWORD = $pwd
    Set-Location $dir
    dotnet NhatDucSoftware.Web.dll
} -ArgumentList $appDir, $port, $password

Start-Sleep -Seconds 4

$cf = "C:\Program Files (x86)\cloudflared\cloudflared.exe"
if (-not (Test-Path $cf)) { $cf = "cloudflared" }

Write-Host "Tao Cloudflare Tunnel (mien phi)..."
Write-Host "URL cong khai se hien ben duoi (dang https://....trycloudflare.com)"
Write-Host "Nhan Ctrl+C de dung."
& $cf tunnel --url "http://127.0.0.1:$port"

Stop-Job $webJob -ErrorAction SilentlyContinue
Remove-Job $webJob -ErrorAction SilentlyContinue
