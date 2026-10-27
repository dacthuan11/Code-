-- Tạo tất cả databases cho từng service
CREATE DATABASE nexus_users;
CREATE DATABASE nexus_catalog;
CREATE DATABASE nexus_commerce;
CREATE DATABASE nexus_auction;
CREATE DATABASE nexus_fulfillment;
CREATE DATABASE nexus_notification;

-- Grant permissions
GRANT ALL PRIVILEGES ON DATABASE nexus_users        TO nexus_admin;
GRANT ALL PRIVILEGES ON DATABASE nexus_catalog      TO nexus_admin;
GRANT ALL PRIVILEGES ON DATABASE nexus_commerce     TO nexus_admin;
GRANT ALL PRIVILEGES ON DATABASE nexus_auction      TO nexus_admin;
GRANT ALL PRIVILEGES ON DATABASE nexus_fulfillment  TO nexus_admin;
GRANT ALL PRIVILEGES ON DATABASE nexus_notification TO nexus_admin;
