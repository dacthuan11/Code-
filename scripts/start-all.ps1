# ============================================================
# start-all.ps1 — Khởi động toàn bộ ProjectNexus local
# Chạy: Set-Location "E:\ProjectNexus"; .\start-all.ps1
# ============================================================

param(
    [switch]$Stop,   # dùng: .\start-all.ps1 -Stop để tắt tất cả
    [switch]$Wait    # giữ tiến trình chờ để duy trì background process
)

$ROOT = "E:\ProjectNexus"
$LOGS = "$ROOT\logs"

# ── Tắt tất cả ───────────────────────────────────────────────
if ($Stop) {
    Write-Host "Stopping all services..." -ForegroundColor Yellow
    Get-Process dotnet -ErrorAction SilentlyContinue | ForEach-Object {
        $proc = Get-CimInstance Win32_Process -Filter "ProcessId=$($_.Id)" -ErrorAction SilentlyContinue
        if ($proc -and $proc.CommandLine -like "*Nexus*") { $_.Kill(); Write-Host "  Killed PID $($_.Id)" }
    }
    Get-Process node -ErrorAction SilentlyContinue | ForEach-Object {
        $_.Kill(); Write-Host "  Killed node PID $($_.Id)"
    }
    Write-Host "All stopped." -ForegroundColor Green
    exit
}

# ── Khởi động ────────────────────────────────────────────────
New-Item -ItemType Directory -Force $LOGS | Out-Null

Write-Host ""
Write-Host "  ╔══════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "  ║      ProjectNexus — Starting...      ║" -ForegroundColor Cyan
Write-Host "  ╚══════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Kiểm tra PostgreSQL
$pgUp = Test-NetConnection localhost -Port 5432 -InformationLevel Quiet -WarningAction SilentlyContinue
if (-not $pgUp) {
    Write-Host "  ❌ PostgreSQL :5432 chưa chạy! Hãy start PostgreSQL trước." -ForegroundColor Red
    exit 1
}
Write-Host "  ✅ PostgreSQL :5432 đang chạy" -ForegroundColor Green

# Build toàn bộ solution 1 lần để tránh xung đột lock DLL khi start song song
Write-Host ""
Write-Host "  Building .NET solution..." -ForegroundColor Yellow
dotnet build "$ROOT\ProjectNexus.slnx" -c Debug -v q
if ($LASTEXITCODE -ne 0) {
    Write-Host "  ❌ Build thất bại!" -ForegroundColor Red
    exit 1
}
Write-Host "  ✅ Build thành công" -ForegroundColor Green

# Danh sách Gateway + 6 BE services
$services = @(
    @{ name="Gateway";              proj="backend\Gateway\Nexus.Gateway";                                      port=5000 },
    @{ name="UserService";          proj="backend\Services\UserService\Nexus.UserService.API";                 port=5001 },
    @{ name="CatalogService";       proj="backend\Services\CatalogService\Nexus.CatalogService.API";           port=5002 },
    @{ name="CommerceService";      proj="backend\Services\CommerceService\Nexus.CommerceService.API";         port=5003 },
    @{ name="AuctionService";       proj="backend\Services\AuctionService\Nexus.AuctionService.API";           port=5004 },
    @{ name="FulfillmentService";   proj="backend\Services\FulfillmentService\Nexus.FulfillmentService.API";   port=5005 },
    @{ name="NotificationService";  proj="backend\Services\NotificationService\Nexus.NotificationService.API"; port=5006 }
)

# Start từng service
Write-Host ""
Write-Host "  Starting Backend Services..." -ForegroundColor Yellow

foreach ($s in $services) {
    $log = "$LOGS\svc$($s.port).log"
    "" | Out-File $log
    $cmd = "`$env:ASPNETCORE_ENVIRONMENT='Development'; Set-Location '$ROOT'; dotnet run --project '$($s.proj)' --no-build --urls 'http://localhost:$($s.port)' *> '$log'"
    Start-Process pwsh -ArgumentList "-NoProfile","-Command",$cmd -WindowStyle Minimized
    Write-Host "  ▶  $($s.name) :$($s.port)" -ForegroundColor Gray
}

# Start Frontend
Write-Host ""
Write-Host "  Starting Frontend..." -ForegroundColor Yellow
$felog = "$LOGS\frontend.log"
"" | Out-File $felog
$fecmd = "Set-Location '$ROOT\frontend'; npm run dev -- --host 127.0.0.1 *> '$felog'"
Start-Process pwsh -ArgumentList "-NoProfile","-Command",$fecmd -WindowStyle Minimized
Write-Host "  ▶  React Frontend :5173" -ForegroundColor Gray

# Chờ services khởi động
Write-Host ""
Write-Host "  Waiting for services to start (25s)..." -ForegroundColor Yellow
$dots = ""
for ($i = 1; $i -le 25; $i++) {
    Start-Sleep -Seconds 1
    $dots += "."
    if ($i % 5 -eq 0) { Write-Host "  $dots" -NoNewline }
}
Write-Host ""

# Kiểm tra health
Write-Host ""
Write-Host "  ─────────────── Health Check ───────────────" -ForegroundColor Cyan

$allUp = $true
foreach ($s in $services) {
    $up = Test-NetConnection localhost -Port $s.port -InformationLevel Quiet -WarningAction SilentlyContinue
    if ($up) {
        Write-Host "  ✅ $($s.name.PadRight(22)) :$($s.port)" -ForegroundColor Green
    } else {
        Write-Host "  ❌ $($s.name.PadRight(22)) :$($s.port)  (xem log: logs\svc$($s.port).log)" -ForegroundColor Red
        $allUp = $false
    }
}

$feUp = Test-NetConnection localhost -Port 5173 -InformationLevel Quiet -WarningAction SilentlyContinue
if ($feUp) {
    Write-Host "  ✅ React Frontend          :5173" -ForegroundColor Green
} else {
    Write-Host "  ❌ React Frontend          :5173  (xem log: logs\frontend.log)" -ForegroundColor Red
    $allUp = $false
}

Write-Host "  ─────────────────────────────────────────────" -ForegroundColor Cyan
Write-Host ""

if ($allUp) {
    Write-Host "  🚀 Tất cả services đang chạy!" -ForegroundColor Green
    Write-Host ""
    Write-Host "  Frontend:  http://localhost:5173" -ForegroundColor White
    Write-Host "  Swagger:   http://localhost:5001/swagger  (UserService)" -ForegroundColor White
    Write-Host ""
    Write-Host "  Tài khoản test:" -ForegroundColor White
    Write-Host "    ADMIN   admin@nexus.dev   / 12345678" -ForegroundColor Gray
    Write-Host "    SELLER  seller@nexus.dev  / 12345678" -ForegroundColor Gray
    Write-Host "    BUYER   buyer@nexus.dev   / 12345678" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  Để tắt tất cả: .\start-all.ps1 -Stop" -ForegroundColor DarkGray
} else {
    Write-Host "  ⚠️  Một số service chưa lên, kiểm tra logs/ để debug." -ForegroundColor Yellow
}

Write-Host ""

if ($Wait) {
    Write-Host "  [Running in daemon mode. Press Ctrl+C to stop]" -ForegroundColor DarkCyan
    while ($true) { Start-Sleep -Seconds 3600 }
}
