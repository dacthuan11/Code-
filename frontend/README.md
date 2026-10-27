# ProjectNexus — React Frontend

React SPA cho nền tảng mua bán & đấu giá Nexus.  
Stack: **React 19** · **TypeScript** · **Vite 8** · **Tailwind CSS** · **React Query** · **Axios**

---

## Kiến trúc: Feature-Sliced Design (FSD)

```
src/
├─ app/
│  ├─ providers/        AuthContext · CartContext · ToastContext
│  ├─ router/           Route config (React Router v6)
│  └─ index.tsx         Composition root (providers + router)
├─ pages/               Page-level routes (thin components)
│  ├─ home/
│  ├─ auth/             login · register
│  ├─ product/
│  ├─ cart/  checkout/  payment/
│  ├─ orders/
│  ├─ auctions/
│  ├─ notifications/
│  ├─ admin/
│  └─ seller/
├─ widgets/             Block UI tái sử dụng cross-page
│  ├─ navbar/
│  └─ notification-bell/
└─ shared/
   ├─ api/              HTTP clients + API functions cho từng service
   ├─ ui/               LoadingSpinner · ProductCard · ProtectedRoute
   └─ styles/           Tailwind globals + token CSS
```

**Dependency rule:** `app → pages → widgets → shared` (layer trên chỉ import layer dưới)

**Path aliases** (không cần relative `../..`):
```ts
@app/providers/AuthContext
@pages/cart/cart.page
@widgets/navbar
@shared/api/catalog
@shared/ui/LoadingSpinner
```

---

## Chạy local

```bash
npm install
npm run dev       # dev server → http://localhost:5173
npm run build     # production build → dist/
```

---

## Biến môi trường

| Biến | Mô tả | Default |
|------|-------|---------|
| `VITE_API_BASE_URL` | URL Gateway (để trống = Vite proxy) | `""` |
| `VITE_GATEWAY_URL` | Proxy target (dùng cho vite.config.ts) | `http://localhost:5000` |

Copy `.env.example` → `.env.development` để override local.

---

## Tài khoản test

| Role | Email | Password |
|------|-------|----------|
| ADMIN | admin@nexus.dev | 12345678 |
| SELLER | seller@nexus.dev | 12345678 |
| BUYER | buyer@nexus.dev | 12345678 |

---

## Tính năng

- **Guest Cart** — thêm giỏ hàng không cần đăng nhập, tự merge khi login
- **JWT auto-attach** — Axios interceptor gắn Bearer token mọi request
- **401 auto-redirect** — hết hạn token → về `/login` tự động
- **Role-based UI** — SELLER không thấy cart, ADMIN thấy trang quản lý
- **Live Auction** — đồng hồ đếm ngược, lịch sử bid real-time (polling 30s)
- **Notification Bell** — polling 30s, mark-as-read, mark-all-read
