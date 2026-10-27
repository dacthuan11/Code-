# ================================================================
# seed-demo.ps1  —  Seed du lieu demo de test tung tinh nang
# Cu phap: Set-Location "E:\ProjectNexus"; .\seed-demo.ps1
# An toan: moi lan chay deu OK (ON CONFLICT DO NOTHING)
# ================================================================

Set-Location "C:\ProjectNexus"
$PSQL  = "C:\Program Files\PostgreSQL\18\bin\psql.exe"
$env:PGPASSWORD = "nexus_pass"
$PGUSER = "nexus_admin"; $PGHOST = "localhost"; $PGPORT = 5432

function pg($db, $sql) {
    $result = $sql | & $PSQL -U $PGUSER -h $PGHOST -p $PGPORT -d $db 2>&1
    if ($LASTEXITCODE -ne 0) { Write-Host "   ERROR in $db`: $result" -ForegroundColor Red }
}
function pgq($db, $sql) {
    return (& $PSQL -U $PGUSER -h $PGHOST -p $PGPORT -d $db -tAc $sql 2>&1)
}

Write-Host ""
Write-Host " ╔══════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host " ║     ProjectNexus — Demo Seed         ║" -ForegroundColor Cyan
Write-Host " ╚══════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# ── 1. Roles ─────────────────────────────────────────────────
Write-Host " ▶  Seed Roles..." -ForegroundColor Yellow
pg "nexus_users" @"
INSERT INTO "Roles" ("Id","Name","Code","Description","Privileges","CreatedAt","UpdatedAt","IsDeleted") VALUES
('11111111-1111-1111-1111-111111111111','Buyer','BUYER','Nguoi mua hang','[]',NOW(),NOW(),false),
('22222222-2222-2222-2222-222222222222','Seller','SELLER','Nguoi ban hang','[]',NOW(),NOW(),false),
('33333333-3333-3333-3333-333333333333','Admin','ADMIN','Quan tri vien','[]',NOW(),NOW(),false)
ON CONFLICT ("Id") DO NOTHING;
"@
Write-Host " ✅ Roles OK" -ForegroundColor Green

# ── 2. Users ─────────────────────────────────────────────────
Write-Host " ▶  Seed Users (password: 12345678)..." -ForegroundColor Yellow

function New-PwHash($pwd) {
    $salt = [byte[]]::new(16)
    $rng = New-Object System.Security.Cryptography.RNGCryptoServiceProvider
$rng.GetBytes($salt)
    $pbkdf2 = [System.Security.Cryptography.Rfc2898DeriveBytes]::new(
        $pwd, $salt, 100000, [System.Security.Cryptography.HashAlgorithmName]::SHA256)
    $hash = $pbkdf2.GetBytes(32); $pbkdf2.Dispose()
    return "$([Convert]::ToBase64String($salt)):$([Convert]::ToBase64String($hash))"
}

$pw = "12345678"
foreach ($u in @(
    @{email="admin@nexus.dev"; name="Admin Nexus"; phone="0900000001"; idn="001111111111"; role="33333333-3333-3333-3333-333333333333"},
    @{email="seller@nexus.dev"; name="Seller Demo"; phone="0900000002"; idn="002222222222"; role="22222222-2222-2222-2222-222222222222"},
    @{email="buyer@nexus.dev"; name="Nguyen Van Mua"; phone="0900000003"; idn="003333333333"; role="11111111-1111-1111-1111-111111111111"},
    @{email="buyer2@nexus.dev"; name="Le Thi Hai"; phone="0900000004"; idn="004444444444"; role="11111111-1111-1111-1111-111111111111"}
)) {
    $exists = pgq "nexus_users" "SELECT 1 FROM ""Users"" WHERE ""Email""='$($u.email)'"
    if ($exists -eq "1") { Write-Host "    ~ $($u.email) (da co)" -ForegroundColor DarkGray; continue }
    $pwHash = New-PwHash $pw
    $uid = [Guid]::NewGuid().ToString()
    pg "nexus_users" @"
INSERT INTO "Users" ("Id","Email","PhoneNumber","FullName","IdentifyNumber","Gender","Address","DateOfBirth","PasswordHash","ReputationScore","TrustLevel","RoleId","CreatedAt","UpdatedAt","IsDeleted","IsEmailVerified")
VALUES ('$uid','$($u.email)','$($u.phone)','$($u.name)','$($u.idn)','Male','Ho Chi Minh City','2000-01-01','$pwHash',100,'TRUSTED','$($u.role)',NOW(),NOW(),false,true);
"@
    Write-Host "    + $($u.email)" -ForegroundColor Gray
}
Write-Host " ✅ Users OK" -ForegroundColor Green

# Lay seller UUID de dung lam SellerId trong products
$sellerId = pgq "nexus_users" "SELECT ""Id"" FROM ""Users"" WHERE ""Email""='seller@nexus.dev'"
Write-Host "    SellerId = $sellerId" -ForegroundColor DarkGray

# ── 3. Categories — dung UUID hop le (chi co ky tu 0-9 va a-f) ──
Write-Host " ▶  Seed Categories..." -ForegroundColor Yellow
pg "nexus_catalog" @"
SET client_encoding = 'UTF8';
-- Root categories (Level 1)
INSERT INTO "Categories" ("Id","Name","Slug","ParentId","Level","CreatedAt","IsDeleted") VALUES
('c1000000-0000-0000-0000-000000000000','Dien tu','dien-tu',NULL,1,NOW(),false),
('c2000000-0000-0000-0000-000000000000','Thoi trang','thoi-trang',NULL,1,NOW(),false),
('c3000000-0000-0000-0000-000000000000','Nha cua','nha-cua',NULL,1,NOW(),false),
('c4000000-0000-0000-0000-000000000000','The thao','the-thao',NULL,1,NOW(),false),
('c5000000-0000-0000-0000-000000000000','Sach','sach',NULL,1,NOW(),false)
ON CONFLICT ("Id") DO NOTHING;

-- Sub categories (Level 2)
INSERT INTO "Categories" ("Id","Name","Slug","ParentId","Level","CreatedAt","IsDeleted") VALUES
('c1100000-0000-0000-0000-000000000000','Dien thoai','dien-thoai','c1000000-0000-0000-0000-000000000000',2,NOW(),false),
('c1200000-0000-0000-0000-000000000000','Laptop','laptop','c1000000-0000-0000-0000-000000000000',2,NOW(),false),
('c1300000-0000-0000-0000-000000000000','Tai nghe','tai-nghe','c1000000-0000-0000-0000-000000000000',2,NOW(),false),
('c1400000-0000-0000-0000-000000000000','Dong ho','dong-ho','c1000000-0000-0000-0000-000000000000',2,NOW(),false),
('c1500000-0000-0000-0000-000000000000','May tinh bang','may-tinh-bang','c1000000-0000-0000-0000-000000000000',2,NOW(),false),
('c1600000-0000-0000-0000-000000000000','Phu kien may tinh','phu-kien-may-tinh','c1000000-0000-0000-0000-000000000000',2,NOW(),false),
('c2100000-0000-0000-0000-000000000000','Ao','ao','c2000000-0000-0000-0000-000000000000',2,NOW(),false),
('c2200000-0000-0000-0000-000000000000','Quan','quan','c2000000-0000-0000-0000-000000000000',2,NOW(),false),
('c2300000-0000-0000-0000-000000000000','Giay dep','giay-dep','c2000000-0000-0000-0000-000000000000',2,NOW(),false),
('c2400000-0000-0000-0000-000000000000','Tui xach & Phu kien','tui-xach','c2000000-0000-0000-0000-000000000000',2,NOW(),false),
('c3100000-0000-0000-0000-000000000000','Noi that','noi-that','c3000000-0000-0000-0000-000000000000',2,NOW(),false),
('c3200000-0000-0000-0000-000000000000','Do bep','do-bep','c3000000-0000-0000-0000-000000000000',2,NOW(),false),
('c3300000-0000-0000-0000-000000000000','Thiet bi gia dung','thiet-bi-gia-dung','c3000000-0000-0000-0000-000000000000',2,NOW(),false),
('c4100000-0000-0000-0000-000000000000','Dung cu the thao','dung-cu-the-thao','c4000000-0000-0000-0000-000000000000',2,NOW(),false),
('c4200000-0000-0000-0000-000000000000','Quan ao the thao','quan-ao-the-thao','c4000000-0000-0000-0000-000000000000',2,NOW(),false),
('c5100000-0000-0000-0000-000000000000','Sach ky thuat','sach-ky-thuat','c5000000-0000-0000-0000-000000000000',2,NOW(),false),
('c5200000-0000-0000-0000-000000000000','Sach van hoc & Phat trien ban than','sach-van-hoc','c5000000-0000-0000-0000-000000000000',2,NOW(),false)
ON CONFLICT ("Id") DO NOTHING;
"@
Write-Host " ✅ Categories OK" -ForegroundColor Green

# ── 4. Products ───────────────────────────────────────────────
Write-Host " ▶  Seed Products (dung SellerId dong)..." -ForegroundColor Yellow
pg "nexus_catalog" @"
SET client_encoding = 'UTF8';
-- Dien thoai
INSERT INTO "Products" ("Id","Name","Description","Price","Status","CategoryId","SellerId","ImageUrls","CreatedAt","UpdatedAt","IsDeleted") VALUES
('bb010001-0000-0000-0000-000000000000','iPhone 15 Pro Max 256GB','Apple iPhone 15 Pro Max chip A17 Pro, 48MP 3 ong kinh, khung titan, bao hanh Apple 12 thang',28500000,'Active','c1100000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1696446702183-cbd13c8e10c0?w=400'],NOW(),NOW(),false),
('bb010002-0000-0000-0000-000000000000','Samsung Galaxy S24 Ultra 512GB','Samsung S24 Ultra but S Pen tich hop, camera 200MP, Snapdragon 8 Gen 3',27000000,'Active','c1100000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1676757038248-2fec2c0c0f19?w=400'],NOW(),NOW(),false),
('bb010003-0000-0000-0000-000000000000','Xiaomi 14 Ultra','Xiaomi hop tac Leica, camera 1 inch, Snapdragon 8 Gen 3, sac nhanh 90W',22000000,'Active','c1100000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1523206489230-c012c64b2b48?w=400'],NOW(),NOW(),false),
('bb010004-0000-0000-0000-000000000000','OPPO Find X7 Pro 256GB','Hasselblad camera, sac nhanh 100W, Dimensity 9300',20000000,'Active','c1100000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?w=400'],NOW(),NOW(),false)
ON CONFLICT ("Id") DO NOTHING;

-- Laptop
INSERT INTO "Products" ("Id","Name","Description","Price","Status","CategoryId","SellerId","ImageUrls","CreatedAt","UpdatedAt","IsDeleted") VALUES
('bb020001-0000-0000-0000-000000000000','MacBook Air M3 13 inch','Apple M3 8GB 256GB SSD, man hinh Liquid Retina, pin 18h, sieu mong',27000000,'Active','c1200000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1611532736597-de2d4265fba3?w=400'],NOW(),NOW(),false),
('bb020002-0000-0000-0000-000000000000','Dell XPS 15 Core i9 2024','Core i9-13900H, 32GB DDR5, 1TB SSD, RTX 4070, 3.5K OLED',42000000,'Active','c1200000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1593642632559-0c6d3fc62b89?w=400'],NOW(),NOW(),false),
('bb020003-0000-0000-0000-000000000000','ASUS ROG Zephyrus G14 2024','Ryzen 9 7940HS, 16GB, 1TB, RTX 4060, 2K 165Hz, gaming chuen nghiep',35000000,'Active','c1200000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1603302576837-37561b2e2302?w=400'],NOW(),NOW(),false),
('bb020004-0000-0000-0000-000000000000','ThinkPad X1 Carbon Gen 11','Core i7-1365U, 16GB, 512GB SSD, sieu nhe 1.12kg, bao mat van tay',32000000,'Active','c1200000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1496181133206-80ce9b88a853?w=400'],NOW(),NOW(),false)
ON CONFLICT ("Id") DO NOTHING;

-- Tai nghe
INSERT INTO "Products" ("Id","Name","Description","Price","Status","CategoryId","SellerId","ImageUrls","CreatedAt","UpdatedAt","IsDeleted") VALUES
('bb030001-0000-0000-0000-000000000000','Sony WH-1000XM5','Chong on ANC hang dau, pin 30h, Hi-Res Audio, Bluetooth 5.2',7500000,'Active','c1300000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=400'],NOW(),NOW(),false),
('bb030002-0000-0000-0000-000000000000','Apple AirPods Pro 2nd Gen','ANC, Adaptive Audio, chip H2, hop sac MagSafe, giam on -29dB',6500000,'Active','c1300000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1606220945770-b5b6c2c55bf1?w=400'],NOW(),NOW(),false),
('bb030003-0000-0000-0000-000000000000','Bose QuietComfort 45','Chong on chu dong, am thanh Bose signature, pin 24h',6200000,'Active','c1300000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1590658268037-6bf12165a8df?w=400'],NOW(),NOW(),false)
ON CONFLICT ("Id") DO NOTHING;

-- Dong ho
INSERT INTO "Products" ("Id","Name","Description","Price","Status","CategoryId","SellerId","ImageUrls","CreatedAt","UpdatedAt","IsDeleted") VALUES
('bb040001-0000-0000-0000-000000000000','Apple Watch Series 9 45mm','Chip S9, man hinh Always-On, GPS, do nhip tim, cellular',11000000,'Active','c1400000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1546868871-7041f2a55e12?w=400'],NOW(),NOW(),false),
('bb040002-0000-0000-0000-000000000000','Samsung Galaxy Watch 6 Classic 47mm','Bezel xoay co dien, theo doi suc khoe toan dien',9800000,'Active','c1400000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=400'],NOW(),NOW(),false),
('bb040003-0000-0000-0000-000000000000','Garmin Fenix 7 Sapphire Solar','GPS multi-band, pin 28 ngay solar, do ben chuyen nghiep cho ngoai troi',18000000,'Active','c1400000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1434056886845-dac89ffe9b56?w=400'],NOW(),NOW(),false)
ON CONFLICT ("Id") DO NOTHING;

-- Thoi trang
INSERT INTO "Products" ("Id","Name","Description","Price","Status","CategoryId","SellerId","ImageUrls","CreatedAt","UpdatedAt","IsDeleted") VALUES
('bb050001-0000-0000-0000-000000000000','Ao thun Uniqlo DRY-EX','Thoat mo hoi nhanh, chong tia UV, form Regular Fit, nhieu mau',299000,'Active','c2100000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?w=400'],NOW(),NOW(),false),
('bb050002-0000-0000-0000-000000000000','Ao khoac Bomber local brand','Chat lieu du pont nhe, in hoa tiet ca chep, form rong',550000,'Active','c2100000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1591047139829-d91aecb6caea?w=400'],NOW(),NOW(),false),
('bb050003-0000-0000-0000-000000000000','Quan Jeans Levis 501 Original','Denim 12oz, fit thang, wash co dien, size 28-36',1200000,'Active','c2200000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1542272604-787c3835535d?w=400'],NOW(),NOW(),false),
('bb050004-0000-0000-0000-000000000000','Nike Air Force 1 07 White','Trang thuan tuy, de cao su, size 38-45, best seller moi nam',2200000,'Active','c2300000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1542291026-7eec264c27ff?w=400'],NOW(),NOW(),false),
('bb050005-0000-0000-0000-000000000000','Adidas Stan Smith Green','Co dien tu 1971, da vat nhan, vach 3 soc xanh la',1900000,'Active','c2300000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1539185441755-769473a23570?w=400'],NOW(),NOW(),false)
ON CONFLICT ("Id") DO NOTHING;

-- Nha cua & Thiet bi
INSERT INTO "Products" ("Id","Name","Description","Price","Status","CategoryId","SellerId","ImageUrls","CreatedAt","UpdatedAt","IsDeleted") VALUES
('bb060001-0000-0000-0000-000000000000','Ghe gaming DXRacer Racing Pro','Khung thep, nem PU, co dinh 3D, max 120kg, bao hanh 2 nam',3500000,'Active','c3100000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1598300042247-d088f8ab3a91?w=400'],NOW(),NOW(),false),
('bb060002-0000-0000-0000-000000000000','Noi com dien Cuckoo CR-0655F','Ap suat cao 1.8at, 6 cup, giu nhiet 24h, bao hanh 3 nam',3200000,'Active','c3200000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1556909114-f6e7ad7d3136?w=400'],NOW(),NOW(),false),
('bb060003-0000-0000-0000-000000000000','Den ban LED Xiaomi Mi 1S','Cam bien anh sang tu dong, do sang 0-100%, ket noi Mi Home App',790000,'Active','c3100000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1518455027359-f3f8164ba6bd?w=400'],NOW(),NOW(),false),
('bb060004-0000-0000-0000-000000000000','May pha ca phe DeLonghi Dedica EC685','Pump 15 bar, hoi suong nong, lam espresso & cappuccino, thiet ke mong 15cm',4500000,'Active','c3300000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1495474472287-4d71bcdd2085?w=400'],NOW(),NOW(),false),
('bb060005-0000-0000-0000-000000000000','Quat dieu hoa Xiaomi Smart Tower Fan','Ket noi Mi Home, 38 muc toc do, che do ban dem, loc bui 3 lop',2800000,'Active','c3300000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=400'],NOW(),NOW(),false),
('bb060006-0000-0000-0000-000000000000','Bo noi inox 5 mon Fissler Original-Profi','Inox 304 cao cap, day ba lop, dung nuoc hai tam tay, EU standard',2200000,'Active','c3200000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1556909114-f6e7ad7d3136?w=400'],NOW(),NOW(),false),
('bb060007-0000-0000-0000-000000000000','May loc khong khi Xiaomi Air Purifier 4 Pro','HEPA H13, loc PM2.5, dien tich 60m2, CADR 500m3/h, ket noi Mi Home',3600000,'Active','c3300000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1585771724684-38269d6639fd?w=400'],NOW(),NOW(),false),
('bb060008-0000-0000-0000-000000000000','Robot hut bui Roborock S7 MaxV','Camera RGB nhan dien vat can, luc rua san 5100Pa, ban do 3D lase',8900000,'Active','c3300000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1558618047-f4e90e8beb30?w=400'],NOW(),NOW(),false)
ON CONFLICT ("Id") DO NOTHING;

-- May tinh bang
INSERT INTO "Products" ("Id","Name","Description","Price","Status","CategoryId","SellerId","ImageUrls","CreatedAt","UpdatedAt","IsDeleted") VALUES
('bb070001-0000-0000-0000-000000000000','iPad Pro M4 11 inch Wi-Fi 256GB','Chip M4, man hinh OLED Ultra Retina XDR, mong 5.1mm, bao but Apple Pencil Pro',22000000,'Active','c1500000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1544244015-0df4b3ffc6b0?w=400'],NOW(),NOW(),false),
('bb070002-0000-0000-0000-000000000000','Samsung Galaxy Tab S9 FE 10.9 inch','Exynos 1380, 6GB 128GB, IP68, DeX mode, but S Pen kem theo',9900000,'Active','c1500000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1561154464-82e9adf32764?w=400'],NOW(),NOW(),false),
('bb070003-0000-0000-0000-000000000000','Xiaomi Pad 6 Pro 12.4 inch','Snapdragon 8 Gen 2, 8GB 256GB, 144Hz, sac nhanh 67W HyperCharge',10500000,'Active','c1500000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1623126908029-58cb08a2b272?w=400'],NOW(),NOW(),false)
ON CONFLICT ("Id") DO NOTHING;

-- Phu kien may tinh
INSERT INTO "Products" ("Id","Name","Description","Price","Status","CategoryId","SellerId","ImageUrls","CreatedAt","UpdatedAt","IsDeleted") VALUES
('bb080001-0000-0000-0000-000000000000','Man hinh LG UltraWide 34WQ75C 34 inch','IPS 3440x1440 QHD, 160Hz, USB-C 96W, HDR10, curl 1900R',12500000,'Active','c1600000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1527443224154-c4a573d5f5f8?w=400'],NOW(),NOW(),false),
('bb080002-0000-0000-0000-000000000000','Ban phim co Keychron K2 Pro','Wireless BT5.1 + USB-C, switch Gateron Red, backlit RGB, compact 75%',1850000,'Active','c1600000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1541140532154-b024d705b90a?w=400'],NOW(),NOW(),false),
('bb080003-0000-0000-0000-000000000000','Chuot Logitech MX Master 3S','8K DPI, ket noi 3 thiet bi, sac USB-C, 70 ngay pin, click im lang 90%',2200000,'Active','c1600000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1527864550417-7fd91fc51a46?w=400'],NOW(),NOW(),false),
('bb080004-0000-0000-0000-000000000000','USB Hub Anker 7-in-1 USB-C','HDMI 4K, USB-A 3.0 x3, SD/microSD, PD 85W pass-through, aluminum',790000,'Active','c1600000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=400'],NOW(),NOW(),false),
('bb080005-0000-0000-0000-000000000000','Webcam Logitech C920 Pro HD','1080p 30fps, ong kinh Carl Zeiss, micro khu o, clip gan man hinh, bao hanh 2 nam',1450000,'Active','c1600000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1587825140708-dfaf72ae4b04?w=400'],NOW(),NOW(),false),
('bb080006-0000-0000-0000-000000000000','SSD Samsung 990 Pro 2TB M.2 NVMe','PCIe 4.0 doc 7450MB/s ghi 6900MB/s, tao nhiet thap hon 50% so M.2 truoc',2800000,'Active','c1600000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1597872200969-2b65d56bd16b?w=400'],NOW(),NOW(),false)
ON CONFLICT ("Id") DO NOTHING;

-- The thao
INSERT INTO "Products" ("Id","Name","Description","Price","Status","CategoryId","SellerId","ImageUrls","CreatedAt","UpdatedAt","IsDeleted") VALUES
('bb090001-0000-0000-0000-000000000000','Ta tay 5kg doi Bowflex SelectTech','Thay the 15 cap ta lien, dieu chinh tu 2kg den 24kg, tiet kiem khong gian',4500000,'Active','c4100000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1583454110551-21f2fa2afe61?w=400'],NOW(),NOW(),false),
('bb090002-0000-0000-0000-000000000000','Tham yoga TPE cao su chong tron 6mm','Kich thuoc 183x61cm, 2 lop TPE eco-friendly, tui dung va day deo','350000','Active','c4100000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1599447421416-3414500d18a5?w=400'],NOW(),NOW(),false),
('bb090003-0000-0000-0000-000000000000','Vot cau long Yonex Astrox 88D Pro','Carbon nang cao, phu hop nguoi tan cong, 4U (83g), dau shot chinh xac',4200000,'Active','c4100000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1626224583764-f87db24ac4ea?w=400'],NOW(),NOW(),false),
('bb090004-0000-0000-0000-000000000000','Bong da FIFA Quality Pro Adidas Teamgeist','Thuong hieu FIFA Quality Pro, be mat 12 manh kep nhiet, kich co 5',850000,'Active','c4100000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1575361204480-aadea25e6e68?w=400'],NOW(),NOW(),false),
('bb090005-0000-0000-0000-000000000000','Day nhay toc do Tangram Smart Jump Rope','Cam bien nhip tim, dem nhay qua app Bluetooth, can tang hop kim loai',890000,'Active','c4100000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1434682881908-b43d0467b798?w=400'],NOW(),NOW(),false),
('bb090006-0000-0000-0000-000000000000','Giay chay bo Nike React Infinity Run FK 3','ReactX foam giam chan dong vuot troi, Flyknit thoang khi, trong luong 255g',3100000,'Active','c4200000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1542291026-7eec264c27ff?w=400'],NOW(),NOW(),false),
('bb090007-0000-0000-0000-000000000000','Ao the thao Nike Dri-FIT ADV TechKnit','Vai dua am, phan luong mo hoi ra be mat, form slim fit',850000,'Active','c4200000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?w=400'],NOW(),NOW(),false),
('bb090008-0000-0000-0000-000000000000','Binh nuoc the thao Hydro Flask 32oz','Giu lanh 24h giu nong 12h, thep khong gi 18/8, nap loop, khong BPA',750000,'Active','c4100000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1602143407151-7111542de6e8?w=400'],NOW(),NOW(),false)
ON CONFLICT ("Id") DO NOTHING;

-- Tui xach & Phu kien thoi trang
INSERT INTO "Products" ("Id","Name","Description","Price","Status","CategoryId","SellerId","ImageUrls","CreatedAt","UpdatedAt","IsDeleted") VALUES
('bb100001-0000-0000-0000-000000000000','Tui da that handmade Viet Nam','Da bo thiet, khau tay 100%, trong luong nhe, dung laptop 15 inch, bao hanh 2 nam',1800000,'Active','c2400000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1548036328-c9fa89d128fa?w=400'],NOW(),NOW(),false),
('bb100002-0000-0000-0000-000000000000','Ba lo laptop Samsonite Openroad 15.6 inch','Chong nuoc IPX4, ngan laptop rieng, day khoa TSA, 20L',1650000,'Active','c2400000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1553062407-98eeb64c6a62?w=400'],NOW(),NOW(),false),
('bb100003-0000-0000-0000-000000000000','Ao polo nam Ralph Lauren Custom Fit','Cotton pique 100%, co be bo, form slim, nhieu mau, size S-XXL',1200000,'Active','c2100000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1586790170083-2f9ceadc732d?w=400'],NOW(),NOW(),false),
('bb100004-0000-0000-0000-000000000000','Mu bucket canvas Uniqlo Round Mini Hat','Vanh rong chong nang, co the gap gon, nhieu mau pastel & solid',290000,'Active','c2400000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1534215754734-18e55d13e346?w=400'],NOW(),NOW(),false),
('bb100005-0000-0000-0000-000000000000','Quan short the thao Nike Dri-FIT 7 inch','Dri-FIT thoat mo hoi, tui keo, lot trong, size S-XXL',650000,'Active','c4200000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1591195853828-11db59a44f43?w=400'],NOW(),NOW(),false),
('bb100006-0000-0000-0000-000000000000','Giay Converse Chuck Taylor All Star Hi','Da canvas, de cao su luu hoa, co dien tu 1923, size 36-45',1250000,'Active','c2300000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1542291026-7eec264c27ff?w=400'],NOW(),NOW(),false)
ON CONFLICT ("Id") DO NOTHING;

-- Sach
INSERT INTO "Products" ("Id","Name","Description","Price","Status","CategoryId","SellerId","ImageUrls","CreatedAt","UpdatedAt","IsDeleted") VALUES
('bb110001-0000-0000-0000-000000000000','Clean Code - Robert C. Martin','Sach kinh dien ve lap trinh sach, nguyen tac SOLID, refactoring, naming convention. Ban dich tieng Viet',280000,'Active','c5100000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?w=400'],NOW(),NOW(),false),
('bb110002-0000-0000-0000-000000000000','Design Patterns - Gang of Four','23 mau thiet ke phan mem co dien: Creational, Structural, Behavioral. Sach goc tieng Anh',450000,'Active','c5100000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?w=400'],NOW(),NOW(),false),
('bb110003-0000-0000-0000-000000000000','Atomic Habits - James Clear','Thay doi nho, ket qua lon — he thong xay dung thoi quen tot va loai bo thoi quen xau. Ban dich',185000,'Active','c5200000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?w=400'],NOW(),NOW(),false),
('bb110004-0000-0000-0000-000000000000','Dac Nhan Tam - Dale Carnegie','Bi quyet thanh cong trong giao tiep va dan dat nguoi khac. Quyen sach ban chay 30 trieu ban the gioi',145000,'Active','c5200000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?w=400'],NOW(),NOW(),false),
('bb110005-0000-0000-0000-000000000000','Nha Gia Kim - Paulo Coelho','Hanh trinh theo duoi uoc mo cua Santiago — mot trong nhung quyen sach ban chay nhat moi thoi dai',120000,'Active','c5200000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?w=400'],NOW(),NOW(),false),
('bb110006-0000-0000-0000-000000000000','Thinking Fast and Slow - Daniel Kahneman','Tam ly hoc nhan thuc: he thong tu duy nhanh (cam tinh) va cham (ly tri). Nobel Kinh te 2002',320000,'Active','c5200000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?w=400'],NOW(),NOW(),false),
('bb110007-0000-0000-0000-000000000000','The Pragmatic Programmer - Hunt & Thomas','Tu programmer moi den master: DRY, YAGNI, refactoring, testing, automation. Ban moi 20th Anniversary',490000,'Active','c5100000-0000-0000-0000-000000000000','$sellerId',ARRAY['https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?w=400'],NOW(),NOW(),false)
ON CONFLICT ("Id") DO NOTHING;
"@
$cnt = pgq "nexus_catalog" 'SELECT COUNT(*) FROM "Products"'
Write-Host " ✅ Products OK (tong: $cnt san pham trong DB)" -ForegroundColor Green

# ── 5. Inventory ─────────────────────────────────────────────
Write-Host " ▶  Seed Inventory..." -ForegroundColor Yellow
pg "nexus_fulfillment" @"
INSERT INTO "inventory" ("Id","SkuId","WarehouseId","TotalQuantity","ReservedQuantity","UnavailableQuantity","CreatedAt","UpdatedAt","IsDeleted") VALUES
(gen_random_uuid(),'bb010001-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',50,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb010002-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',40,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb010003-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',35,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb010004-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',30,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb020001-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',25,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb020002-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',20,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb020003-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',15,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb020004-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',18,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb030001-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',80,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb030002-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',60,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb030003-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',55,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb040001-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',45,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb040002-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',30,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb040003-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',20,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb050001-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',200,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb050002-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',100,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb050003-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',150,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb050004-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',80,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb050005-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',70,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb060001-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',30,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb060002-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',40,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb060003-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',60,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb060004-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',25,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb060005-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',35,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb060006-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',45,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb060007-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',50,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb060008-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',20,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb070001-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',30,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb070002-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',40,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb070003-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',25,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb080001-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',15,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb080002-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',60,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb080003-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',70,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb080004-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',100,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb080005-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',40,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb080006-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',55,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb090001-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',80,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb090002-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',120,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb090003-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',100,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb090004-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',200,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb090005-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',150,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb090006-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',60,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb090007-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',180,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb090008-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',90,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb100001-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',35,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb100002-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',50,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb100003-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',120,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb100004-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',200,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb100005-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',160,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb100006-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',75,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb110001-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',500,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb110002-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',300,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb110003-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',400,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb110004-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',350,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb110005-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',280,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb110006-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',250,0,0,NOW(),NOW(),false),
(gen_random_uuid(),'bb110007-0000-0000-0000-000000000000','fe000001-0000-0000-0000-000000000000',200,0,0,NOW(),NOW(),false)
ON CONFLICT ("SkuId","WarehouseId") DO NOTHING;
"@
Write-Host " ✅ Inventory OK (59 SKUs)" -ForegroundColor Green

# ── 6. Auctions ──────────────────────────────────────────────
Write-Host " ▶  Seed Auctions..." -ForegroundColor Yellow
$now    = [DateTime]::UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
$m2h    = [DateTime]::UtcNow.AddHours(-2).ToString("yyyy-MM-dd HH:mm:ss")
$m1h    = [DateTime]::UtcNow.AddHours(-1).ToString("yyyy-MM-dd HH:mm:ss")
$p1h    = [DateTime]::UtcNow.AddHours(1).ToString("yyyy-MM-dd HH:mm:ss")
$p3h    = [DateTime]::UtcNow.AddHours(3).ToString("yyyy-MM-dd HH:mm:ss")
$p6h    = [DateTime]::UtcNow.AddHours(6).ToString("yyyy-MM-dd HH:mm:ss")
$p24h   = [DateTime]::UtcNow.AddHours(24).ToString("yyyy-MM-dd HH:mm:ss")

pg "nexus_auction" @"
-- ACTIVE: iPhone 15 Pro Max — bat dau 2h truoc, con 6h
INSERT INTO "Auctions" ("Id","ProductId","SellerId","StartingPrice","BidIncrement","CurrentPrice","CurrentWinnerId","StartTime","EndTime","Status","AntiSnipingExtensions","CreatedAt","UpdatedAt","IsDeleted")
VALUES ('a0000001-0000-0000-0000-000000000000','bb010001-0000-0000-0000-000000000000','$sellerId',15000000,500000,15000000,NULL,'$m2h','$p6h','Active',0,NOW(),NOW(),false)
ON CONFLICT ("Id") DO NOTHING;

-- ACTIVE: Sony WH-1000XM5 — bat dau 1h truoc, con 3h
INSERT INTO "Auctions" ("Id","ProductId","SellerId","StartingPrice","BidIncrement","CurrentPrice","CurrentWinnerId","StartTime","EndTime","Status","AntiSnipingExtensions","CreatedAt","UpdatedAt","IsDeleted")
VALUES ('a0000002-0000-0000-0000-000000000000','bb030001-0000-0000-0000-000000000000','$sellerId',3000000,100000,3000000,NULL,'$m1h','$p3h','Active',0,NOW(),NOW(),false)
ON CONFLICT ("Id") DO NOTHING;

-- ACTIVE: Garmin Fenix 7 — bat dau 1h truoc, con 6h
INSERT INTO "Auctions" ("Id","ProductId","SellerId","StartingPrice","BidIncrement","CurrentPrice","CurrentWinnerId","StartTime","EndTime","Status","AntiSnipingExtensions","CreatedAt","UpdatedAt","IsDeleted")
VALUES ('a0000003-0000-0000-0000-000000000000','bb040003-0000-0000-0000-000000000000','$sellerId',8000000,250000,8000000,NULL,'$m1h','$p6h','Active',0,NOW(),NOW(),false)
ON CONFLICT ("Id") DO NOTHING;

-- SCHEDULED: MacBook Air M3 — bat dau sau 1h
INSERT INTO "Auctions" ("Id","ProductId","SellerId","StartingPrice","BidIncrement","CurrentPrice","CurrentWinnerId","StartTime","EndTime","Status","AntiSnipingExtensions","CreatedAt","UpdatedAt","IsDeleted")
VALUES ('a0000004-0000-0000-0000-000000000000','bb020001-0000-0000-0000-000000000000','$sellerId',18000000,500000,18000000,NULL,'$p1h','$p24h','Scheduled',0,NOW(),NOW(),false)
ON CONFLICT ("Id") DO NOTHING;

-- SCHEDULED: Apple Watch S9 — bat dau sau 3h
INSERT INTO "Auctions" ("Id","ProductId","SellerId","StartingPrice","BidIncrement","CurrentPrice","CurrentWinnerId","StartTime","EndTime","Status","AntiSnipingExtensions","CreatedAt","UpdatedAt","IsDeleted")
VALUES ('a0000005-0000-0000-0000-000000000000','bb040001-0000-0000-0000-000000000000','$sellerId',5000000,200000,5000000,NULL,'$p3h','$p24h','Scheduled',0,NOW(),NOW(),false)
ON CONFLICT ("Id") DO NOTHING;
"@
$aucCnt = pgq "nexus_auction" 'SELECT COUNT(*) FROM "Auctions"'
Write-Host " ✅ Auctions OK (tong: $aucCnt auctions)" -ForegroundColor Green

# ── Done ─────────────────────────────────────────────────────
$pCnt = pgq "nexus_catalog" 'SELECT COUNT(*) FROM "Products"'
$cCnt = pgq "nexus_catalog" 'SELECT COUNT(*) FROM "Categories"'
Write-Host ""
Write-Host "=======================================================" -ForegroundColor Green
Write-Host "               SEED DEMO HOAN TAT!                     " -ForegroundColor Green
Write-Host "=======================================================" -ForegroundColor Green
Write-Host " Products: $pCnt | Categories: $cCnt | Auctions: $aucCnt" -ForegroundColor Green
Write-Host "-------------------------------------------------------" -ForegroundColor Green
Write-Host " Tai khoan test (Mat khau: 12345678):" -ForegroundColor Green
Write-Host " - admin@nexus.dev   (ADMIN)" -ForegroundColor Green
Write-Host " - seller@nexus.dev  (SELLER)" -ForegroundColor Green
Write-Host " - buyer@nexus.dev   (BUYER 1)" -ForegroundColor Green
Write-Host " - buyer2@nexus.dev  (BUYER 2)" -ForegroundColor Green
Write-Host "=======================================================" -ForegroundColor Green
Write-Host ""