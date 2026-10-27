import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import { getOrders, cancelOrder, getCancellationReasons } from "@shared/api/commerce";
import LoadingSpinner from "@shared/ui/LoadingSpinner";
import toast from "react-hot-toast";
import { Package, Truck, X, ChevronRight, Clock, MapPin, AlertCircle, Zap, Globe } from "lucide-react";

const fmtVND = (n: number) =>
  new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND" }).format(n);

const fmtDate = (s: string) =>
  new Date(s).toLocaleString("vi-VN", {
    day: "2-digit", month: "2-digit", year: "numeric",
    hour: "2-digit", minute: "2-digit",
  });

const STATUS_CONFIG: Record<string, { label: string; color: string }> = {
  PendingPayment:   { label: "Chờ thanh toán",    color: "bg-yellow-100 text-yellow-700" },
  Paid:             { label: "Đã thanh toán",      color: "bg-blue-100 text-blue-600" },
  Confirmed:        { label: "Đã xác nhận",        color: "bg-sky-100 text-sky-700" },
  Processing:       { label: "Đang xử lý",         color: "bg-indigo-100 text-indigo-700" },
  ReadyToShip:      { label: "Sẵn sàng giao",      color: "bg-purple-100 text-purple-700" },
  Shipped:          { label: "Đang vận chuyển",    color: "bg-violet-100 text-violet-700" },
  OutForDelivery:   { label: "Đang giao",          color: "bg-orange-100 text-orange-700" },
  Delivered:        { label: "Đã giao thành công", color: "bg-green-100 text-green-700" },
  Completed:        { label: "Hoàn thành",         color: "bg-emerald-100 text-emerald-700" },
  Cancelled:        { label: "Đã hủy",             color: "bg-red-100 text-red-500" },
  Refunded:         { label: "Đã hoàn tiền",       color: "bg-gray-100 text-gray-500" },
  RefundRequested:  { label: "Yêu cầu hoàn tiền",  color: "bg-amber-100 text-amber-700" },
  PreOrder:         { label: "Đặt trước",           color: "bg-blue-100 text-blue-700" },
};

const CANCELLABLE = new Set(["PendingPayment", "Paid", "Confirmed", "Processing", "ReadyToShip", "PreOrder"]);

// ── Tab config ────────────────────────────────────────────────────────
interface TabDef {
  key: string;
  label: string;
  statuses: string[] | null; // null = tất cả
}

const TABS: TabDef[] = [
  { key: "all",        label: "Tất cả",           statuses: null },
  { key: "pending",    label: "Chờ xác nhận",     statuses: ["PendingPayment", "Paid", "PreOrder"] },
  { key: "shipping",   label: "Đang giao",        statuses: ["Confirmed", "Processing", "ReadyToShip", "Shipped", "OutForDelivery"] },
  { key: "done",       label: "Đã giao",          statuses: ["Delivered", "Completed"] },
  { key: "cancelled",  label: "Đã hủy",           statuses: ["Cancelled"] },
  { key: "refund",     label: "Trả hàng/Hoàn tiền", statuses: ["RefundRequested", "Refunded"] },
];

const DeliveryBadge = ({ type }: { type?: string }) => {
  if (!type || type === "Standard") return null;
  if (type === "Express")
    return <span className="flex items-center gap-0.5 text-xs text-amber-600 font-medium"><Zap size={11} />Hỏa tốc</span>;
  if (type === "PreOrder")
    return <span className="flex items-center gap-0.5 text-xs text-blue-600 font-medium"><Globe size={11} />Đặt trước</span>;
  return null;
};

// ── Cancel modal ──────────────────────────────────────────────────────
function CancelModal({ orderId, onClose, onConfirm, loading }: {
  orderId: string; onClose: () => void; onConfirm: (reason: string) => void; loading: boolean;
}) {
  const [selected, setSelected] = useState("");
  const [custom, setCustom] = useState("");
  const { data: reasons = [] } = useQuery({
    queryKey: ["cancellation-reasons"],
    queryFn: getCancellationReasons,
    staleTime: Infinity,
  });
  const finalReason = selected === "__custom__" ? custom.trim() : selected;

  return (
    <div className="fixed inset-0 bg-black/40 z-50 flex items-center justify-center px-4" onClick={onClose}>
      <div className="bg-white rounded-2xl shadow-xl max-w-md w-full p-6" onClick={(e) => e.stopPropagation()}>
        <div className="flex items-start justify-between mb-4">
          <div>
            <h3 className="font-semibold text-gray-800 flex items-center gap-2">
              <AlertCircle size={16} className="text-red-500" /> Hủy đơn hàng
            </h3>
            <p className="text-xs text-gray-400 mt-0.5">#{orderId.slice(0, 8)}</p>
          </div>
          <button onClick={onClose} className="text-gray-400 hover:text-gray-600"><X size={18} /></button>
        </div>

        <label className="text-sm text-gray-600 font-medium mb-2 block">Lý do hủy *</label>
        <div className="flex flex-col gap-1.5 mb-3 max-h-52 overflow-y-auto pr-1">
          {reasons.map((r) => (
            <label key={r} className={`flex items-start gap-2.5 p-2.5 rounded-lg cursor-pointer border transition
              ${selected === r ? "border-red-300 bg-red-50" : "border-gray-100 hover:border-gray-200"}`}>
              <input type="radio" name="reason" value={r} checked={selected === r}
                onChange={() => setSelected(r)} className="mt-0.5 accent-red-500 shrink-0" />
              <span className="text-sm text-gray-700">{r}</span>
            </label>
          ))}
          <label className={`flex items-start gap-2.5 p-2.5 rounded-lg cursor-pointer border transition
            ${selected === "__custom__" ? "border-red-300 bg-red-50" : "border-gray-100 hover:border-gray-200"}`}>
            <input type="radio" name="reason" value="__custom__" checked={selected === "__custom__"}
              onChange={() => setSelected("__custom__")} className="mt-0.5 accent-red-500 shrink-0" />
            <span className="text-sm text-gray-700">Lý do khác...</span>
          </label>
        </div>
        {selected === "__custom__" && (
          <textarea value={custom} onChange={(e) => setCustom(e.target.value)}
            placeholder="Nhập lý do hủy..." rows={2}
            className="w-full text-sm border border-gray-200 rounded-xl px-3 py-2 outline-none focus:ring-2 focus:ring-red-300 resize-none mb-3" />
        )}
        <div className="flex gap-2 mt-2">
          <button onClick={onClose}
            className="flex-1 py-2.5 rounded-xl border border-gray-200 text-sm text-gray-600 hover:bg-gray-50 transition">
            Không hủy
          </button>
          <button onClick={() => finalReason && onConfirm(finalReason)} disabled={!finalReason || loading}
            className="flex-1 py-2.5 rounded-xl bg-red-500 hover:bg-red-600 disabled:bg-red-200 text-white text-sm font-semibold transition">
            {loading ? "Đang hủy..." : "Xác nhận hủy"}
          </button>
        </div>
      </div>
    </div>
  );
}

// ── Main page ─────────────────────────────────────────────────────────
export default function OrdersPage() {
  const queryClient = useQueryClient();
  const [page, setPage] = useState(1);
  const [activeTab, setActiveTab] = useState("all");
  const [cancelingId, setCancelingId] = useState<string | null>(null);

  const { data, isLoading } = useQuery({
    queryKey: ["orders", page],
    queryFn: () => getOrders({ page, pageSize: 20 }),
    placeholderData: (prev) => prev,
  });

  const cancelMutation = useMutation({
    mutationFn: ({ orderId, reason }: { orderId: string; reason: string }) => cancelOrder(orderId, reason),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["orders"] });
      toast.success("Đã hủy đơn hàng");
      setCancelingId(null);
    },
    onError: (err: any) => toast.error(err?.response?.data?.message ?? "Không thể hủy đơn này"),
  });

  const allOrders = data?.items ?? [];

  // Client-side filter theo tab
  const activeTabDef = TABS.find(t => t.key === activeTab)!;
  const orders = activeTabDef.statuses
    ? allOrders.filter(o => activeTabDef.statuses!.includes(o.status))
    : allOrders;

  // Badge counts per tab
  const counts = Object.fromEntries(TABS.map(t => [
    t.key,
    t.statuses ? allOrders.filter(o => t.statuses!.includes(o.status)).length : allOrders.length,
  ]));

  const totalPages = Math.ceil((data?.totalCount ?? 0) / 20);

  return (
    <div className="bg-gray-50 min-h-screen">
      <div className="max-w-3xl mx-auto px-4 py-6">
        <h1 className="text-xl font-bold text-gray-800 mb-4 flex items-center gap-2">
          <Package size={20} /> Đơn hàng của tôi
        </h1>

        {/* ── Status tabs ─────────────────────────────────────────── */}
        <div className="bg-white rounded-2xl border border-gray-100 shadow-sm mb-4 overflow-hidden">
          <div className="flex overflow-x-auto scrollbar-none">
            {TABS.map((tab) => {
              const isActive = activeTab === tab.key;
              const count = counts[tab.key];
              return (
                <button
                  key={tab.key}
                  onClick={() => { setActiveTab(tab.key); setPage(1); }}
                  className={`relative flex items-center gap-1.5 px-4 py-3.5 text-xs font-medium whitespace-nowrap border-b-2 transition-colors shrink-0
                    ${isActive
                      ? "border-nexus-600 text-nexus-700 bg-nexus-50/50"
                      : "border-transparent text-gray-500 hover:text-gray-700 hover:bg-gray-50"}`}
                >
                  {tab.label}
                  {count > 0 && (
                    <span className={`text-[10px] rounded-full px-1.5 py-0.5 font-bold leading-none
                      ${isActive ? "bg-nexus-600 text-white" : "bg-gray-200 text-gray-500"}`}>
                      {count}
                    </span>
                  )}
                </button>
              );
            })}
          </div>
        </div>

        {/* ── Order list ──────────────────────────────────────────── */}
        {isLoading ? (
          <LoadingSpinner />
        ) : orders.length === 0 ? (
          <div className="bg-white rounded-2xl border border-gray-100 shadow-sm py-20 text-center">
            <Package size={40} className="mx-auto mb-3 text-gray-200" />
            <p className="text-gray-400 text-sm">
              {activeTab === "all" ? "Bạn chưa có đơn hàng nào" : "Không có đơn nào trong mục này"}
            </p>
            {activeTab !== "all" && (
              <button onClick={() => setActiveTab("all")}
                className="mt-2 text-xs text-nexus-600 hover:underline">
                Xem tất cả đơn hàng
              </button>
            )}
          </div>
        ) : (
          <div className="flex flex-col gap-3">
            {orders.map((order) => {
              const sc = STATUS_CONFIG[order.status] ?? { label: order.status, color: "bg-gray-100 text-gray-600" };
              const canCancel = CANCELLABLE.has(order.status);

              return (
                <div key={order.orderId} className="bg-white rounded-2xl border border-gray-100 shadow-sm overflow-hidden">
                  {/* Shop header bar */}
                  <div className="flex items-center justify-between px-5 py-3 border-b border-gray-50">
                    <div className="flex items-center gap-2">
                      <span className="text-xs font-mono text-gray-400">#{order.orderId.slice(0, 8)}</span>
                      <DeliveryBadge type={order.deliveryType} />
                    </div>
                    <span className={`text-xs font-semibold px-2.5 py-1 rounded-full ${sc.color}`}>
                      {sc.label}
                    </span>
                  </div>

                  <div className="p-5">
                    {/* Estimated delivery */}
                    {order.estimatedDelivery && !["Delivered", "Completed", "Cancelled", "Refunded"].includes(order.status) && (
                      <div className="flex items-center gap-1.5 text-xs text-nexus-600 mb-3 bg-nexus-50 rounded-lg px-3 py-1.5">
                        <Clock size={12} />
                        Dự kiến giao: {fmtDate(order.estimatedDelivery)}
                      </div>
                    )}

                    {/* Shipping address */}
                    {order.shippingAddress && (
                      <div className="flex items-start gap-1.5 text-xs text-gray-400 mb-3">
                        <MapPin size={11} className="mt-0.5 shrink-0" />
                        <span className="truncate">{order.shippingAddress}</span>
                      </div>
                    )}

                    {/* Order date */}
                    <p className="text-xs text-gray-400 mb-3">{fmtDate(order.createdAt)}</p>

                    {/* Items */}
                    <div className="border-t border-gray-50 pt-3 mb-3 space-y-2">
                      {order.items?.map((item) => (
                        <div key={item.productId} className="flex items-center gap-3">
                          <div className="w-12 h-12 bg-gray-50 rounded-lg flex items-center justify-center shrink-0 text-lg">
                            📦
                          </div>
                          <div className="flex-1 min-w-0">
                            <p className="text-sm text-gray-700 truncate">{item.productName}</p>
                            <p className="text-xs text-gray-400">x{item.quantity}</p>
                          </div>
                          <span className="text-sm text-gray-600 whitespace-nowrap">{fmtVND(item.subtotal)}</span>
                        </div>
                      ))}
                    </div>

                    {/* Cancellation reason */}
                    {order.cancellationReason && (
                      <div className="text-xs text-red-500 bg-red-50 rounded-lg px-3 py-2 mb-3 flex gap-1.5">
                        <AlertCircle size={12} className="mt-0.5 shrink-0" />
                        Lý do hủy: {order.cancellationReason}
                      </div>
                    )}

                    {/* Footer */}
                    <div className="flex items-center justify-between gap-2 pt-3 border-t border-gray-50">
                      <div className="text-sm text-gray-500">
                        Tổng tiền:{" "}
                        <span className="font-bold text-nexus-700 text-base">{fmtVND(order.totalAmount)}</span>
                        {order.shippingFee != null && order.shippingFee > 0 && (
                          <span className="text-xs text-gray-400 ml-2 flex items-center gap-1 inline-flex">
                            <Truck size={11} />+{fmtVND(order.shippingFee)}
                          </span>
                        )}
                      </div>

                      <div className="flex items-center gap-2">
                        {canCancel && (
                          <button
                            onClick={() => setCancelingId(order.orderId)}
                            className="text-xs text-red-400 hover:text-red-600 border border-red-200 hover:border-red-400 px-3 py-1.5 rounded-lg transition-colors">
                            Hủy đơn
                          </button>
                        )}
                        <Link to={`/orders/${order.orderId}`}
                          className="text-xs bg-nexus-600 hover:bg-nexus-700 text-white px-3 py-1.5 rounded-lg flex items-center gap-0.5 transition-colors">
                          Xem chi tiết <ChevronRight size={12} />
                        </Link>
                      </div>
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        )}

        {/* Phân trang */}
        {totalPages > 1 && activeTab === "all" && (
          <div className="flex justify-center gap-2 mt-6">
            <button onClick={() => setPage((p) => Math.max(1, p - 1))} disabled={page === 1}
              className="px-4 py-2 rounded-xl border text-sm disabled:opacity-40 hover:bg-nexus-50 transition">
              ‹ Trước
            </button>
            <span className="px-4 py-2 text-sm text-gray-500">{page} / {totalPages}</span>
            <button onClick={() => setPage((p) => Math.min(totalPages, p + 1))} disabled={page === totalPages}
              className="px-4 py-2 rounded-xl border text-sm disabled:opacity-40 hover:bg-nexus-50 transition">
              Tiếp ›
            </button>
          </div>
        )}
      </div>

      {/* Cancel modal */}
      {cancelingId && (
        <CancelModal
          orderId={cancelingId}
          onClose={() => setCancelingId(null)}
          loading={cancelMutation.isPending}
          onConfirm={(reason) => cancelMutation.mutate({ orderId: cancelingId, reason })}
        />
      )}
    </div>
  );
}
