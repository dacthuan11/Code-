$env:PGPASSWORD = "nexus_pass"
$PG = "C:\Program Files\PostgreSQL\18\bin\psql.exe"
& $PG -U nexus_admin -h localhost -p 5432 -d nexus_catalog -c 'SELECT "Id","Name","ImageUrls" FROM "Products" WHERE "IsDeleted"=false ORDER BY "CreatedAt";'
