import { useEffect, useRef, useState } from "react";
import { getNotifications, markAllAsRead, markAsRead } from "@shared/api/notification";
import type { Notification } from "@shared/api/notification";
import { useAuth } from "@app/providers/AuthContext";

export default function NotificationBell() {
  const { user } = useAuth();
  const [open, setOpen] = useState(false);
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [unread, setUnread] = useState(0);
  const ref = useRef<HTMLDivElement>(null);

  const load = async () => {
    if (!user) return;
    try {
      const data = await getNotifications({ pageSize: 15 });
      setNotifications(data.items ?? []);
      setUnread(data.unreadCount ?? 0);
    } catch {
      // ignore – notification service may not be running in all envs
    }
  };

  useEffect(() => {
    load();
    const interval = setInterval(load, 30_000);
    return () => clearInterval(interval);
  }, [user]);

  // close dropdown on outside click
  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false);
    };
    document.addEventListener("mousedown", handler);
    return () => document.removeEventListener("mousedown", handler);
  }, []);

  const handleMarkAll = async () => {
    await markAllAsRead();
    setUnread(0);
    setNotifications(prev => prev.map(n => ({ ...n, isRead: true })));
  };

  const handleClick = async (n: Notification) => {
    if (!n.isRead) {
      await markAsRead(n.notificationId ?? (n as any).id);
      setNotifications(prev => prev.map(x => x === n ? { ...x, isRead: true } : x));
      setUnread(c => Math.max(0, c - 1));
    }
  };

  if (!user) return null;

  return (
    <div ref={ref} style={{ position: "relative" }}>
      <button
        onClick={() => { setOpen(o => !o); if (!open) load(); }}
        style={{
          background: "none", border: "none", cursor: "pointer",
          position: "relative", padding: "6px 8px", borderRadius: 8,
          color: "inherit", fontSize: 20, lineHeight: 1,
        }}
        title="Thông báo"
      >
        🔔
        {unread > 0 && (
          <span style={{
            position: "absolute", top: 0, right: 0,
            background: "#ef4444", color: "#fff",
            borderRadius: 99, fontSize: 10, fontWeight: 700,
            minWidth: 16, height: 16, lineHeight: "16px", textAlign: "center",
            padding: "0 4px",
          }}>
            {unread > 99 ? "99+" : unread}
          </span>
        )}
      </button>

      {open && (
        <div style={{
          position: "absolute", right: 0, top: "calc(100% + 8px)",
          width: 340, maxHeight: 440, overflowY: "auto",
          background: "#fff", color: "#111",
          border: "1px solid #e5e7eb", borderRadius: 10,
          boxShadow: "0 8px 24px rgba(0,0,0,0.12)",
          zIndex: 1000,
        }}>
          {/* Header */}
          <div style={{
            display: "flex", alignItems: "center", justifyContent: "space-between",
            padding: "12px 16px", borderBottom: "1px solid #e5e7eb",
          }}>
            <span style={{ fontWeight: 700, fontSize: 15 }}>Thông báo</span>
            {unread > 0 && (
              <button
                onClick={handleMarkAll}
                style={{ background: "none", border: "none", cursor: "pointer", color: "#2563eb", fontSize: 12 }}
              >
                Đọc hết
              </button>
            )}
          </div>

          {/* List */}
          {notifications.length === 0 ? (
            <div style={{ padding: 24, textAlign: "center", color: "#9ca3af", fontSize: 14 }}>
              Chưa có thông báo nào
            </div>
          ) : (
            notifications.map(n => (
              <div
                key={n.notificationId ?? (n as any).id}
                onClick={() => handleClick(n)}
                style={{
                  padding: "12px 16px",
                  borderBottom: "1px solid #f3f4f6",
                  background: n.isRead ? "#fff" : "#eff6ff",
                  cursor: "pointer",
                  transition: "background 0.2s",
                }}
              >
                <div style={{ fontWeight: n.isRead ? 400 : 700, fontSize: 14, marginBottom: 3 }}>
                  {n.title}
                </div>
                <div style={{ fontSize: 13, color: "#6b7280", lineHeight: 1.4 }}>
                  {n.body.replace(/<[^>]*>/g, "").substring(0, 100)}
                  {n.body.length > 100 ? "…" : ""}
                </div>
                <div style={{ fontSize: 11, color: "#9ca3af", marginTop: 4 }}>
                  {new Date(n.createdAt).toLocaleString("vi-VN")}
                </div>
              </div>
            ))
          )}
        </div>
      )}
    </div>
  );
}
