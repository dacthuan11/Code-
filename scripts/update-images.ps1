$env:PGPASSWORD = "nexus_pass"
$PG = "C:\Program Files\PostgreSQL\18\bin\psql.exe"

# Bước 1: Strip curly braces {url} → url (từ ARRAY insert trực tiếp SQL)
$stripSQL = "UPDATE `"Products`" SET `"ImageUrls`" = REGEXP_REPLACE(`"ImageUrls`", '^\{(.+)\}$', '\1') WHERE `"ImageUrls`" ~ '^\{.+\}$';"
& $PG -U nexus_admin -h localhost -p 5432 -d nexus_catalog -c $stripSQL
Write-Host "Strip curly braces done"

# Bước 2: Update 6 sản phẩm via.placeholder → Unsplash thật
$updates = @(
  @{ id="b5291ebc-7314-4228-9579-ba19a279beaf"; url="https://images.unsplash.com/photo-1510557880182-3d4d3cba35a5?w=400&fit=crop"; name="iPhone 15 Pro" },
  @{ id="39d2c12e-4870-4c3b-b39d-3257a2a08512"; url="https://images.unsplash.com/photo-1567581935884-3349723552ca?w=400&fit=crop"; name="Samsung Galaxy S24" },
  @{ id="551982cf-0dc6-4049-b24d-0ade0c233497"; url="https://images.unsplash.com/photo-1611532736597-de2d4265fba3?w=400&fit=crop"; name="MacBook Air M2" },
  @{ id="726d6a54-8166-42e2-b9bd-3ae435be77a6"; url="https://images.unsplash.com/photo-1593642632559-0c6d3fc62b89?w=400&fit=crop"; name="Dell XPS 15" },
  @{ id="67872fe3-7b46-4edf-9027-5d2b948723fe"; url="https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?w=400&fit=crop"; name="Ao thun basic" },
  @{ id="dee9c1ad-ddcf-4e0d-a0fa-38676ff2c417"; url="https://images.unsplash.com/photo-1551028719-00167b16eac5?w=400&fit=crop"; name="Ao khoac bomber" }
)

foreach ($p in $updates) {
  $sql = "UPDATE `"Products`" SET `"ImageUrls`" = '$($p.url)' WHERE `"Id`" = '$($p.id)';"
  & $PG -U nexus_admin -h localhost -p 5432 -d nexus_catalog -c $sql | Out-Null
  Write-Host "Updated: $($p.name)"
}

Write-Host ""
Write-Host "Done! Verify:"
& $PG -U nexus_admin -h localhost -p 5432 -d nexus_catalog --pset=pager=off -c 'SELECT "Name","ImageUrls" FROM "Products" WHERE "IsDeleted"=false ORDER BY "CreatedAt" LIMIT 10;'
