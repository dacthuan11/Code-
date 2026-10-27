# Architecture Overview

ProjectNexus là nền tảng microservices xây dựng với .NET 9 Clean Architecture + React Feature-Sliced Design.

---

## Backend — Clean Architecture per service

```
Domain          → Entities, Value Objects, Repository interfaces
Application     → Use Cases (Command/Query/Handler), DTOs, Validators (MediatR + FluentValidation)
Infrastructure  → EF Core DbContext, Repository impl, external adapters (JWT, Email, Shipping)
API             → Controllers (thin), Program.cs, DI wiring
```

Mỗi service là 1 **bounded context** độc lập:

| Service | Port (local) | Port (Docker) | Database | Trách nhiệm |
|---------|-------------|---------------|----------|-------------|
| API Gateway | 5000 | 5000:80 | — | Ocelot reverse-proxy, route tất cả request |
| UserService | 5001 | 5001:80 | nexus_users | Auth JWT, register/login, profile, RBAC |
| CatalogService | 5002 | 5002:80 | nexus_catalog | Sản phẩm, danh mục (3 cấp), tìm kiếm |
| CommerceService | 5003 | 5003:80 | nexus_commerce | Cart, checkout, order, payment |
| AuctionService | 5004 | 5004:80 | nexus_auction | Auction engine, bidding, anti-sniping |
| FulfillmentService | 5005 | 5005:80 | nexus_fulfillment | Kho hàng, GHN shipping |
| NotificationService | 5006 | 5006:80 | nexus_notification | Email SMTP, Push (stub) |

### Quy tắc tách mạch DB

> **KHÔNG JOIN hoặc query sang DB của service khác.**  
> Mọi trao đổi dữ liệu liên service **chỉ qua REST API hoặc RabbitMQ Events**.

### Communication patterns

- **Sync:** REST qua Gateway (Ocelot) — FE → Gateway `:5000` → Service
- **Async:** Integration Events qua MassTransit (InMemory dev · RabbitMQ production)

### Shared patterns

- `BaseEntity` — `Id (Guid)`, `CreatedAt`, `UpdatedAt`, `IsDeleted` + global query filter
- Soft Delete — mọi entity dùng `IsDeleted`, không xóa vật lý
- CQRS — tách Command (ghi) và Query (đọc) qua MediatR
- Stub adapters: `StubEmailSender`, `StubPushSender`, `StubPaymentGateway` (dev)

### Integration Events (Nexus.Shared/Events/)

| Event | Publisher | Subscriber |
|-------|-----------|------------|
| `OrderCreatedEvent` | CommerceService | FulfillmentService, NotificationService |
| `OrderPaymentConfirmedEvent` | CommerceService | FulfillmentService |
| `ShipmentCreatedEvent` | FulfillmentService | NotificationService |
| `ShipmentDeliveredEvent` | FulfillmentService | NotificationService |
| `AuctionEndedEvent` | AuctionService | CommerceService, NotificationService |

---

## Frontend — Feature-Sliced Design (FSD)

```
src/
├─ app/       Composition root: providers (Auth/Cart/Toast), router
├─ pages/     Page-level routes (thin, delegate xuống features/widgets)
│  ├─ home/           Homepage: Hero banner, CategoryGrid, FlashSale, Gợi ý hôm nay
│  ├─ product/        Product detail + reviews
│  ├─ cart/           Shopping cart (guest + logged-in merge)
│  ├─ checkout/       Checkout flow
│  ├─ orders/         Orders list (6 tabs: Tất cả / Chờ thanh toán / ... / Trả hàng)
│  ├─ auctions/       Auction list + detail (real-time bidding, anti-sniping)
│  ├─ auth/           Login / Register / Verify email
│  ├─ seller/         Seller dashboard
│  ├─ admin/          Admin panel
│  ├─ notifications/  Notification center
│  └─ payment/        Payment result
├─ widgets/
│  ├─ navbar/         2-row navbar: branding+search (row 1) · categories sub-nav (row 2)
│  └─ notification-bell/  Real-time notification bell
└─ shared/
   ├─ api/    HTTP clients + API functions (axios, per-service)
   │  ├─ client.ts       — axios instances (gateway proxy)
   │  ├─ auth.ts         — register, login, getProfile
   │  ├─ catalog.ts      — products, categories, reviews
   │  ├─ commerce.ts     — cart, orders, payment
   │  ├─ auction.ts      — auctions, bids
   │  ├─ shipping.ts     — shipments (qua fulfillmentClient)
   │  ├─ notification.ts — notifications list + mark read
   │  └─ reviews.ts      — product reviews
   ├─ ui/     Shared components: LoadingSpinner, ProductCard, ProtectedRoute
   └─ styles/ Tailwind globals
```

**Dependency rule:** `app → pages → widgets → shared`  
**Path aliases:** `@app/` · `@pages/` · `@widgets/` · `@shared/`

### FE Port mapping

| Môi trường | URL |
|------------|-----|
| Local dev (`npm run dev`) | http://localhost:5173 |
| Docker (`docker compose up`) | http://localhost:3000 |

Vite dev proxy: `/api → http://localhost:5000` (Gateway)  
Docker nginx: serve static SPA, API calls đến Gateway qua browser (CORS hoặc nginx proxy)

---

## Infrastructure (Docker)

```yaml
# docker-compose.yml services
postgres:16-alpine     — 1 instance, 6 databases riêng (init.sql)
rabbitmq:3.13          — message broker (management UI :15672)
redis:7-alpine         — AuctionService distributed cache
gateway                — Ocelot :5000
{6 services}           — :5001–5006
frontend               — nginx SPA :3000
```

---

## Key design decisions

Xem [`../adr/`](../adr/) để biết lý do chọn từng công nghệ.
