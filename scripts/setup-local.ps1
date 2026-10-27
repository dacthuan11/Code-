# ================================================================
# setup-local.ps1  —  Cài lần đầu, chạy 1 lần duy nhất
# Cú pháp: Set-Location "E:\ProjectNexus"; .\setup-local.ps1
#
# Làm gì: tạo 6 DB → chạy EF migrations → seed data + tài khoản
# Yêu cầu: PostgreSQL đang chạy, .NET 9 SDK, Node 20+
# ================================================================


$ROOT  = "."
$PG    = "C:\Program Files\PostgreSQL\18\bin\psql.exe"
$env:PGPASSWORD = "nexus@2026"           # password của postgres superuser
$ADMIN_USER = "nexus_admin"

Write-Host ""
Write-Host " ╔══════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host " ║   ProjectNexus — First-time Setup    ║" -ForegroundColor Cyan
Write-Host " ╚══════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# ── 1. Kiểm tra PostgreSQL ────────────────────────────────────
$pgOk = Test-NetConnection localhost -Port 5432 -InformationLevel Quiet -WarningAction SilentlyContinue
if (-not $pgOk) { Write-Host " ❌ PostgreSQL chưa chạy trên :5432" -ForegroundColor Red; exit 1 }
Write-Host " ✅ PostgreSQL :5432 OK" -ForegroundColor Green

# ── 2. Tạo role nexus_admin ───────────────────────────────────
Write-Host " ▶  Tạo role nexus_admin..." -ForegroundColor Yellow
& $PG -U $ADMIN_USER -h localhost -p 5432 -c "DO `$`$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname='nexus_admin') THEN CREATE ROLE nexus_admin LOGIN PASSWORD 'nexus_pass'; END IF; END `$`$;" | Out-Null

# ── 3. Tạo 6 databases ───────────────────────────────────────
Write-Host " ▶  Tạo 6 databases..." -ForegroundColor Yellow
foreach ($db in @("nexus_users","nexus_catalog","nexus_commerce","nexus_auction","nexus_fulfillment","nexus_notification")) {
    $exists = & $PG -U $ADMIN_USER -h localhost -p 5432 -tAc "SELECT 1 FROM pg_database WHERE datname='$db'"
    if ($exists -ne "1") {
        & $PG -U $ADMIN_USER -h localhost -p 5432 -c "CREATE DATABASE $db WITH OWNER nexus_admin;" | Out-Null
        & $PG -U $ADMIN_USER -h localhost -p 5432 -c "GRANT ALL PRIVILEGES ON DATABASE $db TO nexus_admin;" | Out-Null
        Write-Host "    + $db" -ForegroundColor Gray
    } else {
        Write-Host "    ~ $db (đã có)" -ForegroundColor DarkGray
    }
}
Write-Host " ✅ Databases OK" -ForegroundColor Green

# ── 4. Restore dotnet-ef local tool ──────────────────────────
Write-Host " ▶  Restore dotnet-ef tool..." -ForegroundColor Yellow
dotnet tool restore 2>&1 | Out-Null
Write-Host " ✅ dotnet-ef OK" -ForegroundColor Green

# ── 5. EF Migrations ─────────────────────────────────────────
Write-Host " ▶  Chạy EF Migrations..." -ForegroundColor Yellow
$svcs = @(
    @{ name="UserService";         infra="backend\Services\UserService\Nexus.UserService.Infrastructure";                 api="backend\Services\UserService\Nexus.UserService.API" },
    @{ name="CatalogService";      infra="backend\Services\CatalogService\Nexus.CatalogService.Infrastructure";           api="backend\Services\CatalogService\Nexus.CatalogService.API" },
    @{ name="CommerceService";     infra="backend\Services\CommerceService\Nexus.CommerceService.Infrastructure";         api="backend\Services\CommerceService\Nexus.CommerceService.API" },
    @{ name="AuctionService";      infra="backend\Services\AuctionService\Nexus.AuctionService.Infrastructure";           api="backend\Services\AuctionService\Nexus.AuctionService.API" },
    @{ name="FulfillmentService";  infra="backend\Services\FulfillmentService\Nexus.FulfillmentService.Infrastructure";   api="backend\Services\FulfillmentService\Nexus.FulfillmentService.API" },
    @{ name="NotificationService"; infra="backend\Services\NotificationService\Nexus.NotificationService.Infrastructure"; api="backend\Services\NotificationService\Nexus.NotificationService.API" }
)
foreach ($s in $svcs) {
    $migDir = "$ROOT\$($s.infra)\Migrations"
    $hasMig = (Test-Path $migDir) -and ((Get-ChildItem $migDir -Filter "*.cs" -EA SilentlyContinue).Count -gt 0)
    if (-not $hasMig) {
        dotnet ef migrations add InitialCreate --project "$ROOT\$($s.infra)" --startup-project "$ROOT\$($s.api)" 2>&1 | Out-Null
    }
    $out = dotnet ef database update --project "$ROOT\$($s.infra)" --startup-project "$ROOT\$($s.api)" 2>&1
    $ok = $LASTEXITCODE -eq 0
    Write-Host "    $(if ($ok) {'✅'} else {'❌'}) $($s.name)" -ForegroundColor $(if ($ok) {'Green'} else {'Red'})
}

# ── 6. Seed catalog (categories + products) ───────────────────
Write-Host " ▶  Seed dữ liệu mẫu..." -ForegroundColor Yellow
$env:PGPASSWORD = "nexus@2026"
$seedSQL = @"
SET client_encoding = 'UTF8';
INSERT INTO "Categories" ("Id","Name","Slug","ParentId","Level","CreatedAt","IsDeleted") VALUES
('aaaaaaaa-0001-0001-0001-000000000001','Dien tu','dien-tu',NULL,1,NOW(),false),
('aaaaaaaa-0001-0001-0001-000000000002','Thoi trang','thoi-trang',NULL,1,NOW(),false),
('aaaaaaaa-0001-0001-0001-000000000003','Nha cua','nha-cua',NULL,1,NOW(),false),
('aaaaaaaa-0001-0001-0001-000000000004','Dien thoai','dien-thoai','aaaaaaaa-0001-0001-0001-000000000001',2,NOW(),false),
('aaaaaaaa-0001-0001-0001-000000000005','Laptop','laptop','aaaaaaaa-0001-0001-0001-000000000001',2,NOW(),false),
('aaaaaaaa-0001-0001-0001-000000000006','Ao','ao','aaaaaaaa-0001-0001-0001-000000000002',2,NOW(),false)
ON CONFLICT DO NOTHING;

INSERT INTO "Products" ("Id","Name","Description","Price","Status","CategoryId","SellerId","ImageUrls","CreatedAt","IsDeleted") VALUES
(gen_random_uuid(),'iPhone 15 Pro','Apple iPhone 15 Pro 256GB chip A17 Pro',25000000,'Active','aaaaaaaa-0001-0001-0001-000000000004','00000000-0000-0000-0000-000000000001',ARRAY['https://via.placeholder.com/400x400?text=iPhone15'],NOW(),false),
(gen_random_uuid(),'Samsung Galaxy S24','Samsung Galaxy S24 Ultra 512GB',22000000,'Active','aaaaaaaa-0001-0001-0001-000000000004','00000000-0000-0000-0000-000000000001',ARRAY['https://via.placeholder.com/400x400?text=Samsung'],NOW(),false),
(gen_random_uuid(),'MacBook Air M2','Apple MacBook Air 13 inch chip M2',28000000,'Active','aaaaaaaa-0001-0001-0001-000000000005','00000000-0000-0000-0000-000000000001',ARRAY['https://via.placeholder.com/400x400?text=MacBook'],NOW(),false),
(gen_random_uuid(),'Dell XPS 15','Dell XPS 15 Intel Core i9 32GB RAM',35000000,'Active','aaaaaaaa-0001-0001-0001-000000000005','00000000-0000-0000-0000-000000000001',ARRAY['https://via.placeholder.com/400x400?text=Dell'],NOW(),false),
(gen_random_uuid(),'Ao thun basic','Ao thun cotton 100% form rong unisex',150000,'Active','aaaaaaaa-0001-0001-0001-000000000006','00000000-0000-0000-0000-000000000001',ARRAY['https://via.placeholder.com/400x400?text=AoThun'],NOW(),false),
(gen_random_uuid(),'Ao khoac bomber','Ao khoac bomber style Han Quoc',450000,'Active','aaaaaaaa-0001-0001-0001-000000000006','00000000-0000-0000-0000-000000000001',ARRAY['https://via.placeholder.com/400x400?text=Bomber'],NOW(),false)
ON CONFLICT DO NOTHING;
"@
$seedSQL | & $PG -U nexus_admin -h localhost -p 5432 -d nexus_catalog 2>&1 | Out-Null
Write-Host " ✅ Catalog seeded (6 categories, 6 products)" -ForegroundColor Green

# ── 7. Tạo tài khoản test (hash PBKDF2 bằng .NET inline) ─────
Write-Host " ▶  Tạo tài khoản test (password: 12345678)..." -ForegroundColor Yellow

function New-PasswordHash($pwd) {
    $salt = [byte[]]::new(16)
   $rng = New-Object System.Security.Cryptography.RNGCryptoServiceProvider
$rng.GetBytes($salt)
    $pbkdf2 = [System.Security.Cryptography.Rfc2898DeriveBytes]::new(
        $pwd, $salt, 100000, [System.Security.Cryptography.HashAlgorithmName]::SHA256)
    $hash = $pbkdf2.GetBytes(32)
    $pbkdf2.Dispose()
    return "$([Convert]::ToBase64String($salt)):$([Convert]::ToBase64String($hash))"
}

$pw = "12345678"
$buyerRoleId  = "11111111-1111-1111-1111-111111111111"
$sellerRoleId = "22222222-2222-2222-2222-222222222222"
$adminRoleId  = "33333333-3333-3333-3333-333333333333"

$users = @(
    @{ email="admin@nexus.dev";  name="Admin Nexus"; phone="0900000001"; id="001001001001"; role=$adminRoleId  },
    @{ email="seller@nexus.dev"; name="Seller Demo";  phone="0900000002"; id="002002002002"; role=$sellerRoleId },
    @{ email="buyer@nexus.dev";  name="Buyer Demo";   phone="0900000003"; id="003003003003"; role=$buyerRoleId  },
    @{ email="buyer2@nexus.dev"; name="Buyer Hai";    phone="0900000004"; id="004004004004"; role=$buyerRoleId  }
)

foreach ($u in $users) {
    $exists = & $PG -U nexus_admin -h localhost -p 5432 -d nexus_users -tAc "SELECT 1 FROM ""Users"" WHERE ""Email""='$($u.email)'"
    if ($exists -eq "1") {
        Write-Host "    ~ $($u.email) (đã có)" -ForegroundColor DarkGray
        continue
    }
    $hash = New-PasswordHash $pw
    $uid  = [Guid]::NewGuid().ToString()
    $sql  = @"
INSERT INTO "Users" ("Id","Email","PhoneNumber","FullName","IdentifyNumber","Gender","Address","DateOfBirth","PasswordHash","ReputationScore","RoleId","CreatedAt","IsDeleted")
VALUES ('$uid','$($u.email)','$($u.phone)','$($u.name)','$($u.id)','Male','Ho Chi Minh City','2000-01-01','$hash',100,'$($u.role)',NOW(),false);
"@
    & $PG -U nexus_admin -h localhost -p 5432 -d nexus_users -c $sql 2>&1 | Out-Null
    Write-Host "    + $($u.email)" -ForegroundColor Gray
}
Write-Host " ✅ Tài khoản test OK" -ForegroundColor Green

# ── 8. npm install ────────────────────────────────────────────
Write-Host " ▶  npm install frontend..." -ForegroundColor Yellow
Set-Location "$ROOT\frontend"
npm install --silent 2>&1 | Out-Null
Set-Location $ROOT
Write-Host " ✅ npm OK" -ForegroundColor Green

