// ============================================================
// Trang thông báo - danh sách, đánh dấu đã đọc
// ============================================================
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { getNotifications, markAsRead, markAllAsRead } from "@shared/api/notification";
import LoadingSpinner from "@shared/ui/LoadingSpinner";
import toast from "react-hot-toast";
import { Bell, CheckCheck } from "lucide-react";

const fmtDate = (s: string) =>
  new Date(s).toLocaleString("vi-VN", {
    day: "2-digit", month: "2-digit", year: "numeric",
    hour: "2-digit", minute: "2-digit",
  });

export default function NotificationsPage() {
  const queryClient = useQueryClient();

  const { data, isLoading } = useQuery({
    queryKey: ["notifications"],
    queryFn: () => getNotifications({ page: 1, pageSize: 50 }),
  });

  const readOneMutation = useMutation({
    mutationFn: markAsRead,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["notifications"] }),
  });

  const readAllMutation = useMutation({
    mutationFn: markAllAsRead,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["notifications"] });
      toast.success("Đã đánh dấu tất cả đã đọc");
    },
  });

  if (isLoading) return <LoadingSpinner />;

  const notifications = data?.items ?? [];
  const unreadCount = notifications.filter((n) => !n.isRead).length;

  return (
    <div className="max-w-2xl mx-auto px-4 py-8">
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-gray-800 flex items-center gap-2">
          <Bell size={26} /> Thông báo
          {unreadCount > 0 && (
            <span className="ml-1 bg-red-500 text-white text-xs rounded-full px-2 py-0.5">{unreadCount}</span>
          )}
        </h1>
        {unreadCount > 0 && (
          <button
            onClick={() => readAllMutation.mutate()}
            disabled={readAllMutation.isPending}
            className="flex items-center gap-1 text-sm text-nexus-600 hover:text-nexus-700 transition-colors"
          >
            <CheckCheck size={16} /> Đọc tất cả
          </button>
        )}
      </div>

      {notifications.length === 0 ? (
        <div className="text-center py-16 text-gray-400 flex flex-col items-center gap-3">
          <Bell size={50} strokeWidth={1} />
          <p>Không có thông báo nào</p>
        </div>
      ) : (
        <div className="flex flex-col gap-2">
          {notifications.map((n) => (
            <div
              key={n.notificationId}
              onClick={() => !n.isRead && readOneMutation.mutate(n.notificationId)}
              className={`bg-white rounded-xl border p-4 transition-all cursor-pointer hover:shadow-sm ${
                n.isRead
                  ? "border-gray-100 opacity-70"
                  : "border-nexus-200 shadow-sm ring-1 ring-nexus-100"
              }`}
            >
              <div className="flex items-start justify-between gap-2">
                <div className="flex-1">
                  <div className="flex items-center gap-2 mb-1">
                    {!n.isRead && <span className="w-2 h-2 bg-nexus-600 rounded-full shrink-0" />}
                    <p className="font-semibold text-gray-800 text-sm">{n.title}</p>
                  </div>
                  <p className="text-sm text-gray-600 leading-relaxed">{n.body}</p>
                </div>
                <span className="text-xs text-gray-400 whitespace-nowrap">{fmtDate(n.createdAt)}</span>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
