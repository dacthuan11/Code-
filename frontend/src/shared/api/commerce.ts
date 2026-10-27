import { commerceClient } from "./client";

export interface CartItem {
  productId: string; productName: string;
  unitPrice: number; quantity: number; subtotal: number;
  imageUrl?: string;
}
export interface Cart {
  cartId: string; userId: string; items: CartItem[]; totalAmount: number;
}
export interface Order {
  orderId: string; userId?: string;
  status: string;
  deliveryType?: string;
  totalAmount: number;
  shippingFee?: number;
  items: CartItem[];
  createdAt: string;
  shippingAddress?: string;
  paymentRef?: string;
  auctionId?: string;
  cancellationReason?: string;
  estimatedDelivery?: string; // ISO string
}

export type DeliveryType = "Standard" | "Express" | "PreOrder";

export interface CheckoutPayload {
  recipientName: string;
  phone: string;
  address: string;
  province: string;
  district: string;
  ward: string;
  toDistrictId: number;
  toWardCode: string;
  shippingFee: number;
  paymentMethod: "COD" | "VNPAY" | "MOMO";
  deliveryType?: DeliveryType;
  serviceTypeId?: number;   // GHN: 1=express, 2=standard
  isInternational?: boolean;
}

export interface CheckoutResult {
  orderId: string;
  paymentUrl?: string;
  totalAmount: number;
  estimatedDelivery?: string;
  deliveryTypeLabel?: string;
}

// ── Giỏ hàng ─────────────────────────────────────────────────────────────────

export const getCart = () =>
  commerceClient.get<Cart>("/api/v1/cart").then(r => r.data);

export const addToCart = (data: {
  productId: string; productName: string; unitPrice: number; quantity: number; imageUrl?: string;
}) => commerceClient.post<{ cartId: string; itemCount: number; subtotal: number }>(
  "/api/v1/cart/items", data
).then(r => r.data);

export const removeFromCart = (productId: string) =>
  commerceClient.delete(`/api/v1/cart/items/${productId}`);

// ── Đặt hàng ─────────────────────────────────────────────────────────────────

export const checkout = (data: CheckoutPayload) =>
  commerceClient.post<CheckoutResult>("/api/v1/cart/checkout", data).then(r => r.data);

// ── Đơn hàng ─────────────────────────────────────────────────────────────────

export const getOrders = (params?: { page?: number; pageSize?: number }) =>
  commerceClient.get<{ items: Order[]; totalCount: number }>("/api/v1/orders", { params }).then(r => r.data);

export const getOrder = (orderId: string) =>
  commerceClient.get<Order>(`/api/v1/orders/${orderId}`).then(r => r.data);

export const cancelOrder = (orderId: string, reason: string) =>
  commerceClient.delete(`/api/v1/orders/${orderId}`, { data: { reason } });

export const confirmPayment = (orderId: string, paymentRef?: string) =>
  commerceClient.post<{ success: boolean }>("/api/v1/payment/callback", {
    orderId, paymentRef: paymentRef ?? `PAY-${orderId}`,
  }).then(r => r.data);

export const getCancellationReasons = () =>
  commerceClient.get<string[]>("/api/v1/orders/cancellation-reasons").then(r => r.data);

export interface OrderHistoryEntry {
  status: string;
  note?: string;
  actor?: string;
  createdAt: string;
}

export const getOrderHistory = (orderId: string) =>
  commerceClient.get<OrderHistoryEntry[]>(`/api/v1/orders/${orderId}/history`).then(r => r.data);
