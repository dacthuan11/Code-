import { useParams, Link } from "react-router-dom";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { getOrder, confirmPayment, cancelOrder, getOrderHistory } from "@shared/api/commerce";
import LoadingSpinner from "@shared/ui/LoadingSpinner";
import toast from "react-hot-toast";
import {
  Package, ArrowLeft, MapPin, CreditCard, CheckCircle,
  Truck, Box, Home, Clock, XCircle, RefreshCw,
} from "lucide-react";
import { useState } from "react";

const fmtVND = (n: number) =>
  new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND" }).format(n);

const fmtDate = (s: string) =>
  new Date(s).toLocaleString("vi-VN", {
    day: "2-digit", month: "2-digit", year: "numeric",
    hour: "2-digit", minute: "2-digit",
  });

// ── Status → step index (1‒5) ─────────────────────────────────────────────
const STATUS_STEP: Record<string, number> = {
  PendingPayment:  1,
  PreOrder:        1,
  Confirmed:       2,
  Paid:            2,
  Processing:      3,
  ReadyToShip:     3,
  Shipped:         4,
  OutForDelivery:  4,
  Delivered:       5,
  Completed:       5,
  Cancelled:       -1,
  Refunded:        -2,
  RefundRequested: -3,
};

const STATUS_VI: Record<string, string> = {
  PendingPayment:  "Chờ xác nhận",
  PreOrder:        "Đặt hàng trước",
  Confirmed:       "Đã xác nhận",
  Paid:            "Đã thanh toán",
  Processing:      "Đang chuẩn bị hàng",
  ReadyToShip:     "Sẵn sàng giao",
  Shipped:         "Đang vận chuyển",
  OutForDelivery:  "Đang giao đến bạn",
  Delivered:       "Đã giao thành công",
  Completed:       "Hoàn thành",
  Cancelled:       "Đã hủy",
  Refunded:        "Đã hoàn tiền",
  RefundRequested: "Đang xử lý hoàn tiền",
};

const STATUS_COLOR: Record<string, string> = {
  PendingPayment:  "bg-yellow-100 text-yellow-700 border-yellow-200",
  PreOrder:        "bg-purple-100 text-purple-700 border-purple-200",
  Confirmed:       "bg-blue-100 text-blue-700 border-blue-200",
  Paid:            "bg-blue-100 text-blue-700 border-blue-200",
  Processing:      "bg-indigo-100 text-indigo-700 border-indigo-200",
  ReadyToShip:     "bg-cyan-100 text-cyan-700 border-cyan-200",
  Shipped:         "bg-sky-100 text-sky-700 border-sky-200",
  OutForDelivery:  "bg-orange-100 text-orange-700 border-orange-200",
  Delivered:       "bg-green-100 text-green-700 border-green-200",
  Completed:       "bg-green-100 text-green-700 border-green-200",
  Cancelled:       "bg-red-100 text-red-600 border-red-200",
  Refunded:        "bg-gray-100 text-gray-600 border-gray-200",
  RefundRequested: "bg-amber-100 text-amber-700 border-amber-200",
};

// ── Timeline steps ─────────────────────────────────────────────────────────
const STEPS = [
  { step: 1, label: "Đặt hàng",      icon: <Package size={16} /> },
  { step: 2, label: "Xác nhận",      icon: <CheckCircle size={16} /> },
  { step: 3, label: "Chuẩn bị hàng", icon: <Box size={16} /> },
  { step: 4, label: "Đang giao",     icon: <Truck size={16} /> },
  { step: 5, label: "Đã giao",       icon: <Home size={16} /> },
];

const DELIVERY_TYPE_VI: Record<string, string> = {
  Standard: "Giao hàng tiêu chuẩn",
  Express:  "Hỏa tốc (trong ngày)",
  PreOrder: "Hàng đặt trước / nhập khẩu",
};

// ── Component ──────────────────────────────────────────────────────────────
export default function OrderDetailPage() {
  const { id } = useParams<{ id: string }>();
  const queryClient = useQueryClient();
  const [showCancelModal, setShowCancelModal] = useState(false);
  const [cancelReason, setCancelReason] = useState("");
  const [switchingPayment, setSwitchingPayment] = useState(false);

  const { data: order, isLoading } = useQuery({
    queryKey: ["order", id],
    queryFn: () => getOrder(id!),
    enabled: !!id,
    refetchInterval: 15_000,
  });

  const { data: history } = useQuery({
    queryKey: ["order-history", id],
    queryFn: () => getOrderHistory(id!),
    enabled: !!id,
    refetchInterval: 15_000,
  });

  const cancelMutation = useMutation({
    mutationFn: () => cancelOrder(id!, cancelReason || "Người mua muốn thay đổi sản phẩm"),
    onSuccess: () => {
      toast.success("Đã hủy đơn hàng");
      queryClient.invalidateQueries({ queryKey: ["order", id] });
      queryClient.invalidateQueries({ queryKey: ["orders"] });
      setShowCancelModal(false);
    },
    onError: () => toast.error("Không thể hủy đơn, thử lại nhé"),
  });

  const handleSwitchToOnline = async () => {
    setSwitchingPayment(true);
    try {
      // Simulate online payment confirmation
      await confirmPayment(id!);
      queryClient.invalidateQueries({ queryKey: ["order", id] });
      toast.success("Đã chuyển sang thanh toán online — đơn sẽ được xác nhận ngay!");
    } catch {
      toast.error("Không thể chuyển phương thức, thử lại nhé");
    } finally {
      setSwitchingPayment(false);
    }
  };

  if (isLoading) return <LoadingSpinner />;
  if (!order) return (
    <div className="max-w-2xl mx-auto px-4 py-16 text-center text-gray-500">
      Không tìm thấy đơn hàng
      <div className="mt-4"><Link to="/orders" className="text-nexus-600 hover:underline">← Quay lại</Link></div>
    </div>
  );

  const currentStep = STATUS_STEP[order.status] ?? 1;
  const isCancelled = currentStep < 0;
  const isCOD       = !order.paymentRef;  // không có paymentRef = COD / chưa thanh toán online
  const canCancel   = ["PendingPayment", "Confirmed", "PreOrder"].includes(order.status);
  const canSwitchToOnline = isCOD && order.status === "PendingPayment";

  return (
    <div className="max-w-2xl mx-auto px-4 py-8">
      <Link to="/orders" className="flex items-center gap-1.5 text-sm text-gray-500 hover:text-nexus-600 mb-6 transition-colors">
        <ArrowLeft size={16} /> Quay lại đơn hàng
      </Link>

      {/* Header */}
      <div className="bg-white rounded-2xl border border-gray-100 shadow-sm p-6 mb-4">
        <div className="flex items-start justify-between flex-wrap gap-3">
          <div>
            <h1 className="text-lg font-bold text-gray-800 flex items-center gap-2">
              <Package size={20} className="text-nexus-600" /> Chi tiết đơn hàng
            </h1>
            <p className="text-xs text-gray-400 font-mono mt-1">
              #{order.orderId?.slice(0, 8) ?? id?.slice(0, 8)}
            </p>
            <p className="text-xs text-gray-400 flex items-center gap-1 mt-0.5">
              <Clock size={11} /> {order.createdAt ? fmtDate(order.createdAt) : ""}
            </p>
          </div>
          <span className={`text-xs font-bold px-3 py-1.5 rounded-full border ${STATUS_COLOR[order.status] ?? "bg-gray-100 text-gray-600 border-gray-200"}`}>
            {STATUS_VI[order.status] ?? order.status}
          </span>
        </div>

        {order.cancellationReason && (
          <div className="mt-3 bg-red-50 text-red-600 text-xs rounded-xl p-3 flex items-start gap-2">
            <XCircle size={14} className="shrink-0 mt-0.5" />
            Lý do hủy: {order.cancellationReason}
          </div>
        )}
      </div>

      {/* ── Timeline ─────────────────────────────────────────────────────── */}
      {!isCancelled ? (
        <div className="bg-white rounded-2xl border border-gray-100 shadow-sm p-6 mb-4">
          <h2 className="text-sm font-semibold text-gray-700 mb-5">Trạng thái đơn hàng</h2>
          <div className="relative flex items-start justify-between">
            {/* Progress line */}
            <div className="absolute top-5 left-0 right-0 h-0.5 bg-gray-100 z-0" />
            <div
              className="absolute top-5 left-0 h-0.5 bg-nexus-500 z-0 transition-all duration-500"
              style={{ width: `${Math.max(0, Math.min(100, ((currentStep - 1) / (STEPS.length - 1)) * 100))}%` }}
            />

            {STEPS.map(({ step, label, icon }) => {
              const done    = currentStep >= step;
              const current = currentStep === step;
              return (
                <div key={step} className="relative z-10 flex flex-col items-center gap-2" style={{ width: `${100 / STEPS.length}%` }}>
                  <div className={`w-10 h-10 rounded-full flex items-center justify-center border-2 transition-all
                    ${done
                      ? "bg-nexus-600 border-nexus-600 text-white shadow-md shadow-nexus-200"
                      : "bg-white border-gray-200 text-gray-300"}`}
                  >
                    {done && !current
                      ? <CheckCircle size={18} />
                      : <span className={done ? "text-white" : "text-gray-300"}>{icon}</span>}
                  </div>
                  <span className={`text-xs text-center leading-tight font-medium ${done ? "text-nexus-700" : "text-gray-400"}`}>
                    {label}
                  </span>
                  {current && (
                    <span className="text-[10px] text-nexus-500 font-semibold animate-pulse">Hiện tại</span>
                  )}
                </div>
              );
            })}
          </div>

          {order.estimatedDelivery && currentStep < 5 && (
            <div className="mt-5 bg-nexus-50 rounded-xl px-4 py-2.5 flex items-center gap-2 text-sm">
              <Truck size={14} className="text-nexus-500" />
              <span className="text-gray-600">Dự kiến giao:</span>
              <span className="font-semibold text-nexus-700">
                {fmtDate(order.estimatedDelivery)}
              </span>
            </div>
          )}
        </div>
      ) : (
        <div className="bg-red-50 rounded-2xl border border-red-100 p-5 mb-4 flex items-center gap-3">
          <XCircle size={22} className="text-red-500 shrink-0" />
          <div>
            <p className="font-semibold text-red-700">
              {order.status === "Refunded" ? "Đã hoàn tiền" : order.status === "RefundRequested" ? "Đang xử lý hoàn tiền" : "Đơn hàng đã bị hủy"}
            </p>
            <p className="text-xs text-red-500 mt-0.5">
              {order.status === "Refunded" ? "Tiền đã được hoàn về tài khoản của bạn" : "Liên hệ hỗ trợ nếu cần giải quyết"}
            </p>
          </div>
        </div>
      )}

      {/* ── History log ──────────────────────────────────────────────────── */}
      {history && history.length > 0 && (
        <div className="bg-white rounded-2xl border border-gray-100 shadow-sm p-6 mb-4">
          <h2 className="text-sm font-semibold text-gray-700 mb-4 flex items-center gap-2">
            <Clock size={15} /> Lịch sử đơn hàng
          </h2>
          <div className="relative pl-6">
            {/* Vertical line */}
            <div className="absolute left-2.5 top-2 bottom-2 w-0.5 bg-gray-100" />

            {history.map((entry, idx) => {
              const isFirst = idx === 0;
              const isLast = idx === history.length - 1;
              const dot = isLast
                ? "w-3 h-3 bg-nexus-600 border-2 border-nexus-600 rounded-full"
                : "w-2.5 h-2.5 bg-white border-2 border-nexus-400 rounded-full";
              return (
                <div key={idx} className={`relative flex gap-3 ${isFirst ? "" : "mt-4"}`}>
                  {/* Dot */}
                  <div className={`absolute -left-6 top-1 ${dot} shrink-0`} />
                  <div className="flex-1 min-w-0">
                    <div className="flex items-start justify-between gap-2 flex-wrap">
                      <div>
                        <span className={`text-sm font-semibold ${isLast ? "text-nexus-700" : "text-gray-700"}`}>
                          {STATUS_VI[entry.status] ?? entry.status}
                        </span>
                        {entry.note && (
                          <p className="text-xs text-gray-500 mt-0.5">{entry.note}</p>
                        )}
                        {entry.actor && entry.actor !== "system" && (
                          <p className="text-[10px] text-gray-400 mt-0.5">bởi {entry.actor}</p>
                        )}
                      </div>
                      <span className="text-xs text-gray-400 shrink-0 whitespace-nowrap">
                        {fmtDate(entry.createdAt)}
                      </span>
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      )}

      {/* ── Products ─────────────────────────────────────────────────────── */}
      <div className="bg-white rounded-2xl border border-gray-100 shadow-sm p-6 mb-4">
        <h2 className="text-sm font-semibold text-gray-700 mb-4 flex items-center gap-2">
          <Package size={15} /> Sản phẩm
        </h2>
        <div className="flex flex-col gap-3">
          {order.items?.map(item => (
            <div key={item.productId} className="flex items-center gap-3 py-2 border-b border-gray-50 last:border-0">
              <div className="w-12 h-12 rounded-lg bg-gray-50 border border-gray-100 overflow-hidden shrink-0">
                {(item as any).imageUrl
                  ? <img src={(item as any).imageUrl} alt={item.productName} className="w-full h-full object-cover" />
                  : <div className="w-full h-full flex items-center justify-center text-lg">📦</div>}
              </div>
              <div className="flex-1 min-w-0">
                <p className="text-sm font-medium text-gray-800 truncate">{item.productName}</p>
                <p className="text-xs text-gray-400">× {item.quantity}</p>
              </div>
              <span className="text-sm font-semibold text-gray-700 shrink-0">
                {fmtVND(item.subtotal ?? item.unitPrice * item.quantity)}
              </span>
            </div>
          ))}
        </div>

        <div className="mt-4 pt-3 border-t border-gray-100 space-y-2">
          <div className="flex justify-between text-sm text-gray-500">
            <span>Phí vận chuyển</span>
            <span>{fmtVND(order.shippingFee ?? 0)}</span>
          </div>
          <div className="flex justify-between font-bold text-base">
            <span>Tổng cộng</span>
            <span className="text-nexus-700">{fmtVND(order.totalAmount)}</span>
          </div>
        </div>
      </div>

      {/* ── Delivery info ─────────────────────────────────────────────────── */}
      <div className="bg-white rounded-2xl border border-gray-100 shadow-sm p-6 mb-4">
        <h2 className="text-sm font-semibold text-gray-700 mb-3 flex items-center gap-2">
          <MapPin size={15} /> Thông tin giao hàng
        </h2>
        {order.shippingAddress && (
          <p className="text-sm text-gray-600 flex items-start gap-2">
            <MapPin size={14} className="text-nexus-500 mt-0.5 shrink-0" />
            {order.shippingAddress}
          </p>
        )}
        {order.deliveryType && (
          <div className="mt-2 flex items-center gap-2 text-sm text-gray-500">
            <Truck size={14} className="text-nexus-400" />
            {DELIVERY_TYPE_VI[order.deliveryType] ?? order.deliveryType}
          </div>
        )}
      </div>

      {/* ── Payment section ───────────────────────────────────────────────── */}
      <div className="bg-white rounded-2xl border border-gray-100 shadow-sm p-6 mb-4">
        <h2 className="text-sm font-semibold text-gray-700 mb-4 flex items-center gap-2">
          <CreditCard size={15} /> Phương thức thanh toán
        </h2>

        <div className="flex items-center gap-3 p-3 rounded-xl bg-gray-50 border border-gray-100">
          <span className="text-2xl">{isCOD ? "💵" : "🏦"}</span>
          <div className="flex-1">
            <p className="text-sm font-semibold text-gray-800">
              {isCOD ? "Thanh toán khi nhận hàng (COD)" : "Thanh toán online"}
            </p>
            <p className="text-xs text-gray-400 mt-0.5">
              {isCOD
                ? "Trả tiền mặt hoặc thẻ cho shipper khi nhận hàng"
                : `Mã thanh toán: ${order.paymentRef}`}
            </p>
          </div>
          {!isCOD && (
            <span className="text-xs bg-green-100 text-green-700 px-2 py-0.5 rounded-full font-medium">Đã thanh toán</span>
          )}
        </div>

        {/* COD → Online: cho phép đổi */}
        {canSwitchToOnline && (
          <div className="mt-3 p-4 rounded-xl bg-blue-50 border border-blue-100">
            <p className="text-sm font-medium text-blue-800 mb-1">Chuyển sang thanh toán online</p>
            <p className="text-xs text-blue-600 mb-3">
              Đổi sang ví điện tử / thẻ ngân hàng để đơn được xác nhận nhanh hơn.
              Sau khi chuyển, không thể đổi lại COD.
            </p>
            <button
              onClick={handleSwitchToOnline}
              disabled={switchingPayment}
              className="flex items-center gap-2 bg-blue-600 hover:bg-blue-700 disabled:bg-blue-300
                         text-white text-sm font-semibold px-4 py-2 rounded-lg transition-colors"
            >
              {switchingPayment
                ? <><div className="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin" />Đang xử lý...</>
                : <><RefreshCw size={14} />Chuyển sang thanh toán online</>}
            </button>
          </div>
        )}

        {/* Online → COD: không cho đổi, giải thích lý do */}
        {!isCOD && (
          <div className="mt-3 p-3 rounded-xl bg-gray-50 border border-gray-100 flex items-start gap-2">
            <XCircle size={15} className="text-gray-400 shrink-0 mt-0.5" />
            <p className="text-xs text-gray-500">
              Đơn đã thanh toán online — không thể chuyển về COD.
              Liên hệ hỗ trợ nếu cần hoàn tiền.
            </p>
          </div>
        )}
      </div>

      {/* ── Actions ───────────────────────────────────────────────────────── */}
      {canCancel && (
        <div className="bg-white rounded-2xl border border-gray-100 shadow-sm p-6">
          <button
            onClick={() => setShowCancelModal(true)}
            className="text-sm text-red-500 hover:text-red-700 flex items-center gap-1.5 transition-colors"
          >
            <XCircle size={16} /> Hủy đơn hàng
          </button>
        </div>
      )}

      {/* Cancel modal */}
      {showCancelModal && (
        <div className="fixed inset-0 bg-black/40 z-50 flex items-center justify-center p-4">
          <div className="bg-white rounded-2xl p-6 w-full max-w-sm shadow-xl">
            <h3 className="font-bold text-gray-800 mb-1">Hủy đơn hàng?</h3>
            <p className="text-sm text-gray-500 mb-4">Chọn lý do hủy đơn:</p>
            <select
              value={cancelReason}
              onChange={e => setCancelReason(e.target.value)}
              className="w-full border border-gray-200 rounded-xl px-3 py-2.5 text-sm mb-4 outline-none focus:ring-2 focus:ring-red-400"
            >
              <option value="">-- Chọn lý do --</option>
              {[
                "Người mua muốn thay đổi sản phẩm",
                "Người mua muốn thay đổi địa chỉ giao",
                "Người mua tìm được giá rẻ hơn",
                "Người mua không còn nhu cầu",
                "Thời gian giao hàng quá lâu",
              ].map(r => <option key={r} value={r}>{r}</option>)}
            </select>
            <div className="flex gap-3">
              <button
                onClick={() => setShowCancelModal(false)}
                className="flex-1 border border-gray-200 rounded-xl py-2.5 text-sm font-medium text-gray-600 hover:bg-gray-50 transition-colors"
              >
                Giữ đơn
              </button>
              <button
                onClick={() => cancelMutation.mutate()}
                disabled={cancelMutation.isPending}
                className="flex-1 bg-red-500 hover:bg-red-600 disabled:bg-red-300 text-white rounded-xl py-2.5 text-sm font-bold transition-colors"
              >
                {cancelMutation.isPending ? "Đang hủy..." : "Xác nhận hủy"}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
