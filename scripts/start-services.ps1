$root = "E:\ProjectNexus\backend\Services"
$logs = "E:\ProjectNexus\logs"
New-Item -ItemType Directory -Force -Path $logs | Out-Null

$services = @(
  @{ name="CatalogService";      port=5002; path="CatalogService\Nexus.CatalogService.API" },
  @{ name="CommerceService";     port=5003; path="CommerceService\Nexus.CommerceService.API" },
  @{ name="AuctionService";      port=5004; path="AuctionService\Nexus.AuctionService.API" },
  @{ name="FulfillmentService";  port=5005; path="FulfillmentService\Nexus.FulfillmentService.API" },
  @{ name="NotificationService"; port=5006; path="NotificationService\Nexus.NotificationService.API" }
)

foreach ($svc in $services) {
  $proj = Join-Path $root $svc.path
  $log  = Join-Path $logs "$($svc.name).log"
  Start-Process -FilePath "dotnet" `
    -ArgumentList "run", "--project", $proj, "--urls", "http://localhost:$($svc.port)" `
    -RedirectStandardOutput $log `
    -RedirectStandardError "$log.err" `
    -WindowStyle Hidden
  Write-Host "Started $($svc.name) on :$($svc.port)"
}

Write-Host ""
Write-Host "All services started. Logs in E:\ProjectNexus\logs\"
