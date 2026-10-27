import { notificationClient } from "./client";

export interface Notification {
  notificationId: string;
  userId: string;
  type: string;
  title: string;
  body: string;
  isRead: boolean;
  createdAt: string;
}

interface NotificationsResponse {
  items: Notification[];
  unreadCount: number;
}

export const getNotifications = (params?: { page?: number; pageSize?: number }) =>
  notificationClient
    .get<NotificationsResponse>("/api/v1/notifications", { params })
    .then((r) => r.data);

export const getUnreadCount = () =>
  notificationClient
    .get<{ count: number }>("/api/v1/notifications/unread-count")
    .then((r) => r.data.count);

export const markAsRead = (notificationId: string) =>
  notificationClient.patch(`/api/v1/notifications/${notificationId}/read`);

export const markAllAsRead = () =>
  notificationClient.patch("/api/v1/notifications/read-all");
