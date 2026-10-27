# Project Nexus — Master Specification & Kế Hoạch Triển Khai v1.0

> **Dự án:** Project Nexus (E-Commerce & Online Auction Platform)  
> **Kiến trúc:** Microservices (.NET 9 + React + PostgreSQL + Redis + RabbitMQ)  
> **Project Manager / Technical Lead:** Võ Thành Đạt  
> **Quy mô:** 10 Thành viên (5 Backend, 2 Frontend, 1 DevOps, 1 QA, 1 PM/Tech Lead)  
> **Thời gian:** 4 Sprints (8 Tuần triển khai)

---

## 1. PHÂN CÔNG TRÁCH NHIỆM 10 THÀNH VIÊN (ROLE-BASED)

| Vị trí | Họ và Tên | Vai trò | Phụ trách chính | Mô tả nhiệm vụ chi tiết triển khai |
|---|---|---|---|---|
| **PM** | **Võ Thành Đạt** | Project Manager · Tech Lead | Toàn dự án & Architecture | - Khởi tạo cấu trúc .NET Microservices Clean Architecture.<br>- Thiết kế OpenAPI/Swagger Specs & Event Schemas trên RabbitMQ.<br>- Cấu hình Ocelot/Nginx API Gateway.<br>- Điều phối Standup, Code Review PRs & giải quyết Technical Blockers. |
| **B1** | **Nguyễn Đặng Quốc Bảo** | Backend Developer | User Service (`nexus_users`) | - Auth JWT: Mã hóa BCrypt, Access Token (60m) + Refresh Token.<br>- Quản lý User & Profile, Soft Delete, Audit log.<br>- RBAC Middleware: Quản lý Roles & Privileges.<br>- Rating & Review 1-5 sao sau giao dịch.<br>- Reputation System: Thuật toán tính điểm uy tín & phạt khóa bid. |
| **B2** | **Đỗ Quốc Huy** | Backend Developer | Catalog Service (`nexus_catalog`) | - CRUD Category: Cấu trúc cây (Tree) tối đa 3 cấp.<br>- CRUD Product: Vòng đời DRAFT → ACTIVE → INACTIVE → SOLD.<br>- Upload Ảnh: Tối đa 5 ảnh / sản phẩm (Cloudinary/S3).<br>- Search & Discovery: SQL LIKE / Full-text, đánh Index tối ưu.<br>- API xác minh sản phẩm đủ điều kiện đưa vào đấu giá. |
| **B3** | **Lê Hoàng Long** | Backend Developer | Commerce Service (`nexus_commerce`) | - Cart Lifecycle: 1 Cart / User, tính subtotal, thuế & ước tính ship.<br>- Thanh toán Stripe Sandbox: PaymentIntent, callback Idempotent (chống trùng tiền).<br>- Create Order & Price Snapshot: Lưu đơn giá tại thời điểm mua, State Machine (PENDING → PAID → SHIPPING → COMPLETED).<br>- Tiếp nhận Event `AuctionWon` tự động tạo đơn hàng đấu giá. |
| **B4** | **Trịnh Chí Kiệt** | Backend Developer | Auction Service (`nexus_auction`) | - Auction Engine: State Machine DRAFT → SCHEDULED → ACTIVE → ENDED → SETTLED.<br>- **Redis Distributed Lock**: Khóa luồng concurrent bidding (latency ≤ 300ms).<br>- Anti-sniping Logic: Tự động gia hạn 5 phút nếu có bid ở 5 phút cuối.<br>- Winner & Penalty: Tự động chốt winner, emit `AuctionWon`. Đếm ngược 24h thanh toán (bùng kèo emit Event phạt Reputation). |
| **B5** | **Trương Hoàng Phúc** | Backend Developer | Fulfillment & Notification Service | - Inventory Kho `WH_001`: Reserve stock 30 phút timeout (Background Job nhả kho), Commit khi PAID. Bảng `inventory_ledger` Append-only.<br>- Shipping GHN Adapter: Gọi API GHN quote phí ship & tạo mã vận đơn.<br>- Notification Service: Lắng nghe Event RabbitMQ, gửi SendGrid Email & FCM Push Notification (retry 3 lần). |
| **F1** | **Lương Đắc Thuận** | Frontend Developer | Buyer UI Portal (React / TS) | - Giao diện Auth: Register, Login, Reset Password, Axios Interceptors auto-refresh JWT.<br>- Khám phá & Chi tiết sản phẩm: Banner, danh mục 3 cấp, search & filter.<br>- Giỏ hàng & Checkout: Stripe Elements SDK nhập thẻ an toàn.<br>- Live Bidding UI: Đồng hồ đếm ngược real-time, lịch sử đè giá. |
| **F2** | **Phạm Thị Thu Thảo** | Frontend Developer | Seller & Admin Portal | - Seller Portal: Đăng sản phẩm mới (drag & drop ảnh), quản lý tồn kho, tạo & quản lý auction, xem đơn bán ra.<br>- Admin Portal: Quản lý User/Role, xem Event Logs toàn sàn, System Health Check, quản lý tất cả đơn hàng & auction. |
| **DO** | **Đoàn Trần Việt Tiến** | DevOps Engineer | Infrastructure & CI/CD | - Dockerization: Cấu hình docker-compose chạy 5 Microservices + 2 Portals + Postgres + Redis + RabbitMQ.<br>- API Gateway: Cấu hình Ocelot/Nginx Gateway điều hướng request.<br>- CI/CD Pipeline: GitHub Actions Build/Lint/Test; CD Pipeline deploy lên Staging / Railway.app.<br>- Logging & Health: Tập trung log với Serilog (ELK/Seq), endpoint `/health`. |
| **QA** | **Đinh Ngọc Toàn** | Quality Assurance | Testing Toàn Bộ Hệ Thống | - Test Cases & Postman Collection: Bộ Test Case SRS cho Happy Path & Edge Cases.<br>- Concurrency & Idempotency Testing: Kiểm thử đặt bid đồng thời (Redis Lock) & chống thanh toán trùng.<br>- Regression & UAT: Kiểm thử hồi quy toàn sàn & UAT Checklist trước Demo. |

---

## 2. QUY TẮC KIẾN TRÚC DATABASE & NGUYÊN TẮC TÁCH MẠCH

> ⚠️ **MANDATORY ARCHITECTURAL RULE:**  
> **TUYỆT ĐỐI KHÔNG JOIN HOẶC TRUY VẤN SANG DB SCHEMA KHÁC!**  
> Mỗi Microservice sở hữu độc quyền Database Schema của mình. Mọi thao tác trao đổi dữ liệu liên Service **CHỈ ĐƯỢC THỰC HIỆN QUA REST API HOẶC RABBITMQ EVENTS**.

| Microservice | PostgreSQL Schema / DB | Bảng Chính (Primary Tables) | Ghi Chú Kỹ Thuật Triển Khai |
|---|---|---|---|
| **User Service** | `nexus_users` | `users`, `roles`, `privileges`, `role_privileges`, `reputation_scores`, `ratings`, `penalties` | Hỗ trợ xóa mềm (Soft Delete), Audit log thay đổi mật khẩu & quyền RBAC. |
| **Catalog Service** | `nexus_catalog` | `products`, `categories`, `product_images`, `product_status_history` | Danh mục Category cấu trúc cây tối đa 3 cấp, quản lý trạng thái sản phẩm độc lập. |
| **Commerce Service** | `nexus_commerce` | `carts`, `cart_items`, `orders`, `order_items`, `payments`, `refunds`, `idempotency_keys` | Snapshot giá sản phẩm tại thời điểm mua, lưu khóa Idempotency Key chống trùng tiền. |
| **Auction Service** | `nexus_auction` | `auctions`, `bids`, `auction_state_history`, `settlements` | Sử dụng **Redis Cache & Distributed Lock** để giữ và xử lý lượt bid cao nhất hiện tại. |
| **Fulfillment Service**| `nexus_fulfillment` | `warehouses`, `inventory`, `inventory_reservations`, `inventory_ledger`, `shipments` | Bảng `inventory_ledger` chuẩn Append-only (không sửa/xóa), cố định 1 kho hàng (`WH_001`). |
| **Notification Service**| `nexus_notification` | `notifications`, `notification_preferences`, `event_logs` | Lưu log event từ RabbitMQ, gửi Email SendGrid & Push FCM với retry 3 lần. |

---

## 3. CÁC KỸ THUẬT CHUYÊN SÂU BỔ SUNG

1. **Redis Distributed Lock (Auction Concurrency):** Đảm bảo khi hàng ngàn lượt bid đặt cùng một lúc, hệ thống sử dụng Redis Lock để khóa luồng, chỉ ghi nhận 1 lượt bid cao nhất hợp lệ với độ trễ latency ≤ 300ms.
2. **Inventory Reserve Stock 30 phút:** Khi khởi tạo Checkout hoặc phiên đấu giá, hệ thống giữ hàng 30 phút trong kho `WH_001`. Nếu quá 30 phút chưa thanh toán, Background Job sẽ tự động nhả kho. Bảng `inventory_ledger` là Append-only.
3. **Stripe Idempotency Key:** Mọi request thanh toán Stripe đều đi kèm `idempotency_key` để chống việc người dùng bấm 2 lần hoặc lag mạng làm trừ tiền nhiều lần.
4. **Price Snapshot:** Lưu lại đơn giá thực tế của sản phẩm ngay tại thời điểm tạo đơn hàng, đảm bảo biến động giá về sau của Seller không ảnh hưởng đến đơn hàng đã đặt.
5. **Đếm ngược 24h Thanh toán Đấu giá (24h Payment Deadline):** Người thắng cuộc có 24 giờ để thanh toán đơn hàng đấu giá. Nếu quá 24h không thanh toán ➔ Tự động phát Event `AuctionPaymentTimeout`, trừ điểm Uy tín (Reputation Score) và khóa quyền tham gia đấu giá.

---

## 4. TIMELINE 4 SPRINTS (8 TUẦN TRIỂN KHAI)

### 🗓️ Sprint 1 — Nền Móng (Tuần 1 – 2)
- Setup .NET Microservices Architecture & Solution Structure.
- Docker-compose khởi tạo PostgreSQL, Redis, RabbitMQ.
- Auth APIs (Register, Login, JWT Tokens, Refresh Session).
- CRUD Category (3 cấp) & Product Management (DRAFT/ACTIVE).
- CI Pipeline (Build + Lint + Unit Test) qua GitHub Actions.
- React Frontend Project Setup, Layout & Auth UI Forms.

### 🗓️ Sprint 2 — Core Mua Hàng (Tuần 3 – 4)
- Shopping Cart Lifecycle (1 Cart / User) & Checkout Flow.
- Tích hợp Stripe Sandbox payment với Idempotent callback.
- Inventory Reserve Stock / Timeout 30p / Commit (Kho `WH_001`).
- Search, Filter & Sort Sản Phẩm (SQL LIKE / Full-text).
- API Gateway (Ocelot/Nginx) Routing & RabbitMQ Queues.
- Buyer UI: Cart & Checkout Page; Seller UI: Đăng sản phẩm (drag-drop ảnh).

### 🗓️ Sprint 3 — End-to-End Auction (Tuần 5 – 6)
- Auction Engine: State Machine, Auto Scheduler start/end.
- Concurrent Bidding qua Redis Lock, Anti-sniping logic (gia hạn 5p sát giờ).
- Xác định Winner & Emit Event `AuctionWon` sang CommerceService.
- Tích hợp GHN Shipping Adapter (Quote & Create Shipment).
- SendGrid Email & FCM Push Notification (Retry 3 lần).
- Rating & Reputation Score System, Live Bidding UI (Countdown & History).

### 🗓️ Sprint 4 — Production Ready (Tuần 7 – 8)
- Auction Winner 24h Payment Deadline & Penalty Event (bùng kèo trừ điểm).
- Tạo Order tự động từ kết quả Auction Settlement.
- Admin Portal: User & Role Management, Event Logs, Health Check.
- CD Pipeline deploy hoàn chỉnh lên Staging / Railway.app.
- Regression Test, Edge-case testing, UAT Checklist & Demo.

---

## 5. CÁC TÍNH NĂNG BỎ QUA (SCOPE PHASE 2 - OUT OF SCOPE)

Để tập trung hoàn thành xuất sắc phiên bản MVP trong 2 tháng, các tính năng sau được **tạm hoãn**:
1. 🚫 **Discount / Promotion Module:** Mã giảm giá đòi hỏi rule tính toán phức tạp.
2. 🚫 **Multi-Warehouse Support:** Chỉ làm 1 kho duy nhất (`WH_001`).
3. 🚫 **SMS Notification:** Tắt SMS (`SMS_ENABLED = false`), chỉ dùng Email SendGrid + Push FCM.
4. 🚫 **Dispute System:** Quy trình xử lý tranh chấp khiếu nại phát triển ở Phase 2.
5. 🚫 **Elasticsearch Engine:** Sử dụng SQL LIKE & Full-text index của PostgreSQL.
6. 🚫 **GHTK Carrier Adapter:** Chỉ tích hợp duy nhất đơn vị vận chuyển GHN (Giao Hàng Nhanh).
7. 🚫 **Guest Cart (Giỏ hàng vãng lai):** Bắt buộc người dùng Đăng nhập trước khi tạo giỏ và mua hàng.
8. 🚫 **PDF Invoice Generation:** Chỉ gửi thông báo hóa đơn qua Email HTML/Text đơn giản.
9. 🚫 **Distributed Tracing (Jaeger):** Lưu vết qua Correlation Trace ID trong Log tập trung là đủ.

---

## 6. LINK THAM KHẢO & CẤU HÌNH LOCAL

- API Gateway: http://localhost:5000
- UserService: http://localhost:5001/swagger
- CatalogService: http://localhost:5002/swagger
- CommerceService: http://localhost:5003/swagger
- AuctionService: http://localhost:5004/swagger
- Frontend: http://localhost:5173
- PostgreSQL port: 5432 | User: `nexus_admin` | Pass: `nexus_pass`
