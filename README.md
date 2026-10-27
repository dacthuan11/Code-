# Project Nexus — E-Commerce & Auction Platform

Nền tảng mua bán và đấu giá trực tuyến, xây dựng theo kiến trúc **Microservices** + **Clean Architecture**.  
Stack: **.NET 9** | **React + Vite + TypeScript** | **PostgreSQL** | **MassTransit** | **Docker**

---

## Kiến trúc tổng quan

```
┌─────────────────────────────────────────────────────────────┐
│         React Frontend :3000 (Docker) / :5173 (dev)         │
│       (Vite · TypeScript · Tailwind · React Query · FSD)    │
└───────────────────────────┬─────────────────────────────────┘
                            │ HTTP / axios → /api/*
                            ▼
               ┌────────────────────────┐
               │  API Gateway :5000     │
               │  (Ocelot reverse-proxy)│
               └────────────┬───────────┘
          ┌─────────────────┼──────────────────┐
          ▼                 ▼                  ▼
  ┌──────────────┐  ┌──────────────┐  ┌───────────────┐
  │ UserService  │  │CatalogService│  │CommerceService│
  │   :5001      │  │   :5002      │  │   :5003       │
  └──────────────┘  └──────────────┘  └───────────────┘
  ┌──────────────┐  ┌──────────────┐  ┌───────────────┐
  │AuctionService│  │Fulfillment   │  │Notification   │
  │   :5004      │  │   :5005      │  │   :5006       │
  └──────────────┘  └──────────────┘  └───────────────┘
          │                 │                  │
          └─────────────────┼──────────────────┘
                   ┌────────┴────────┐
                   │   PostgreSQL    │   RabbitMQ    Redis
                   │ (6 databases)   │   :5672       :6379
                   └─────────────────┘
```

Services giao tiếp qua **Integration Events** (MassTransit InMemory dev · RabbitMQ production).

---

## Services

| Service | Port (local) | Port (Docker) | Database | Trạng thái |
|---------|-------------|---------------|----------|------------|
| API Gateway | 5000 | 5000:80 | — | ✅ |
| UserService | 5001 | 5001:80 | nexus_users | ✅ |
| CatalogService | 5002 | 5002:80 | nexus_catalog | ✅ |
| CommerceService | 5003 | 5003:80 | nexus_commerce | ✅ |
| AuctionService | 5004 | 5004:80 | nexus_auction | ✅ |
| FulfillmentService | 5005 | 5005:80 | nexus_fulfillment | ✅ |
| NotificationService | 5006 | 5006:80 | nexus_notification | ✅ |
| React Frontend | 5173 (dev) | 3000:80 | — | ✅ |

---

## Chạy với Docker (khuyến nghị)

```bash
# Copy env và điền JWT_KEY (≥32 ký tự)
cp .env.example .env

docker compose up -d
```

Truy cập: **http://localhost:3000**

Seed dữ liệu mẫu:

```powershell
.\scripts\seed-demo.ps1
```

Kiểm tra health tất cả services:

```powershell
.\scripts\check-health.ps1
```

---

## Chạy local (không Docker)

**Yêu cầu:** .NET 9 SDK · Node.js 20+ · PostgreSQL đang chạy

```powershell
# 1. Tạo databases + chạy migrations
.\scripts\setup-local.ps1

# 2. Start tất cả BE services
.\scripts\start-services.ps1

# 3. Start Frontend (tab khác)
cd frontend
npm install
npm run dev    # → http://localhost:5173
```

---

## Tài khoản test

| Role | Email | Password |
|------|-------|----------|
| ADMIN | admin@nexus.dev | 12345678 |
| SELLER | seller@nexus.dev | 12345678 |
| BUYER | buyer@nexus.dev | 12345678 |

---

## Swagger UI (local)

| Service | URL |
|---------|-----|
| UserService | http://localhost:5001/swagger |
| CatalogService | http://localhost:5002/swagger |
| CommerceService | http://localhost:5003/swagger |
| AuctionService | http://localhost:5004/swagger |
| FulfillmentService | http://localhost:5005/swagger |
| NotificationService | http://localhost:5006/swagger |

---

## Cấu trúc thư mục

```
ProjectNexus/
├─ backend/
│  ├─ Gateway/                    # Ocelot API Gateway (:5000)
│  ├─ Services/
│  │  ├─ UserService/             # Clean Architecture: Domain·Application·Infrastructure·API
│  │  ├─ CatalogService/
│  │  ├─ CommerceService/
│  │  ├─ AuctionService/
│  │  ├─ FulfillmentService/
│  │  └─ NotificationService/
│  └─ Shared/                     # Nexus.Shared: BaseEntity, Events, Messaging
├─ frontend/
│  └─ src/
│     ├─ app/                     # Composition root: providers (Auth/Cart/Toast) + router
│     ├─ pages/                   # home · product · cart · checkout · orders · auctions · auth · seller · admin
│     ├─ widgets/                 # navbar (2-row) · notification-bell
│     └─ shared/                  # api (7 clients) · ui · styles
├─ docs/
│  ├─ adr/                        # Architecture Decision Records
│  ├─ api/                        # API reference
│  └─ architecture/               # Sơ đồ, design docs
├─ scripts/                       # PowerShell: setup · seed · health · start
├─ infra/postgres/                # init.sql — tạo 6 databases
├─ tests/                         # Unit + Integration tests (WIP)
├─ docker-compose.yml
├─ .env.example                   # copy → .env, điền JWT_KEY ≥32 chars
└─ ProjectNexus.sln
```

---

## Clean Architecture (mỗi service)

```
Domain          → Entities, Repository interfaces (không phụ thuộc gì)
Application     → Use Cases: Command/Handler/Validator (MediatR + FluentValidation)
Infrastructure  → EF Core DbContext, Repository impl, external adapters
API             → Controllers (thin), Program.cs, DI wiring
```

---

## Tính năng nổi bật

### Backend
- ✅ **JWT Auth** — BUYER / SELLER / ADMIN, policy-based authorization
- ✅ **Soft Delete** — `IsDeleted` + global query filter trên tất cả entity
- ✅ **CQRS + MediatR** — tách Command (ghi) và Query (đọc)
- ✅ **Integration Events** — MassTransit InMemory (dev) / RabbitMQ (production)
- ✅ **Anti-sniping** — auto-extend +5 phút khi bid vào 5 phút cuối
- ✅ **GHN Shipping** — stub adapter, dễ thay bằng real GHN API
- ✅ **Email SMTP** — configurable (stub dev, real SMTP production)

### Frontend
- ✅ **Shopee-style UI** — 2-row navbar, Hero banner, FlashSale countdown, CategoryGrid
- ✅ **Guest Cart** — localStorage, tự merge khi đăng nhập
- ✅ **ProductCard** — rating ⭐, "Đã bán Xk", location badge, hover "Thêm vào giỏ"
- ✅ **Orders** — 6 tab: Tất cả / Chờ thanh toán / Chờ lấy hàng / Đang giao / Đã giao / Trả hàng
- ✅ **Feature-Sliced Design** — path aliases `@app · @pages · @widgets · @shared`
- ✅ **React Query** — server state caching + invalidation
- ✅ **Notification Bell** — real-time notification từ NotificationService
