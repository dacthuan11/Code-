// ============================================================
// Trang giỏ hàng — hỗ trợ cả guest (localStorage) và logged-in (backend)
//
// Guest:     hiện items từ CartContext + banner "Đăng nhập để thanh toán"
// Logged-in: hiện items từ backend CommerceService + nút checkout
// ============================================================
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { getCart, removeFromCart } from "@shared/api/commerce";
import { useNavigate, Link } from "react-router-dom";
import { useAuth } from "@app/providers/AuthContext";
import { useCart } from "@app/providers/CartContext";
import LoadingSpinner from "@shared/ui/LoadingSpinner";
import toast from "react-hot-toast";
import { Trash2, ShoppingBag, LogIn } from "lucide-react";

const fmtVND = (n: number) =>
  new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND" }).format(n);

// ─── Guest Cart View ─────────────────────────────────────────────────────────
function GuestCartView() {
  const { guestCart, removeFromGuestCart } = useCart();
  const isEmpty = guestCart.length === 0;

  return (
    <div className="max-w-3xl mx-auto px-4 py-8">
      <h1 className="text-2xl font-bold text-gray-800 mb-6 flex items-center gap-2">
        <ShoppingBag size={26} /> Giỏ hàng của bạn
      </h1>

      {/* Banner nhắc đăng nhập */}
      <div className="bg-nexus-50 border border-nexus-200 rounded-xl p-4 mb-6 flex items-center justify-between gap-4">
        <p className="text-sm text-nexus-700">
          Bạn đang xem giỏ hàng tạm. <strong>Đăng nhập</strong> để lưu và đặt hàng.
        </p>
        <Link
          to="/login"
          state={{ from: "/cart" }}
          className="shrink-0 flex items-center gap-1.5 bg-nexus-600 hover:bg-nexus-700 text-white text-sm font-semibold px-4 py-2 rounded-lg transition-colors"
        >
          <LogIn size={15} /> Đăng nhập
        </Link>
      </div>

      {isEmpty ? (
        <div className="text-center py-16 flex flex-col items-center gap-4 text-gray-400">
          <ShoppingBag size={60} strokeWidth={1} />
          <p>Giỏ hàng đang trống</p>
          <Link to="/" className="text-nexus-600 text-sm hover:underline">
            Khám phá sản phẩm →
          </Link>
        </div>
      ) : (
        <div className="flex flex-col gap-4">
          {guestCart.map((item) => (
            <div
              key={item.productId}
              className="flex items-center gap-4 bg-white rounded-xl p-4 shadow-sm border border-gray-100"
            >
              <div className="w-16 h-16 bg-nexus-50 rounded-lg flex items-center justify-center shrink-0 text-2xl">
                {item.imageUrl
                  ? <img src={item.imageUrl} alt={item.productName} className="w-full h-full object-cover rounded-lg" />
                  : "📦"}
              </div>
              <div className="flex-1 min-w-0">
                <p className="font-semibold text-gray-800 truncate">{item.productName}</p>
                <p className="text-sm text-gray-400">
                  {item.quantity} × {fmtVND(item.price)}
                </p>
              </div>
              <div className="text-right">
                <p className="font-bold text-nexus-700">{fmtVND(item.price * item.quantity)}</p>
                <button
                  onClick={() => removeFromGuestCart(item.productId)}
                  className="text-red-400 hover:text-red-600 mt-1 transition-colors"
                  title="Xóa khỏi giỏ"
                >
                  <Trash2 size={16} />
                </button>
              </div>
            </div>
          ))}

          {/* Tổng tiền + nút đăng nhập để checkout */}
          <div className="bg-nexus-50 rounded-xl p-5 mt-2 flex items-center justify-between">
            <div>
              <p className="text-gray-500 text-sm">Tổng cộng</p>
              <p className="text-2xl font-bold text-nexus-700">
                {fmtVND(guestCart.reduce((s, i) => s + i.price * i.quantity, 0))}
              </p>
            </div>
            {/* Redirect về checkout sau khi login thành công */}
            <Link
              to="/login"
              state={{ from: "/checkout" }}
              className="flex items-center gap-2 bg-nexus-600 hover:bg-nexus-700 text-white font-semibold px-8 py-3 rounded-xl transition-colors"
            >
              <LogIn size={18} /> Đăng nhập để đặt hàng
            </Link>
          </div>
        </div>
      )}
    </div>
  );
}

// ─── Logged-in Cart View ──────────────────────────────────────────────────────
function LoggedInCartView() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const { data: cart, isLoading, isError } = useQuery({
    queryKey: ["cart"],
    queryFn: getCart,
  });

  const removeMutation = useMutation({
    mutationFn: removeFromCart,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["cart"] });
      toast.success("Đã xóa khỏi giỏ");
    },
    onError: () => toast.error("Không thể xóa, thử lại nhé"),
  });

  if (isLoading) return <LoadingSpinner />;
  if (isError)
    return (
      <div className="max-w-3xl mx-auto px-4 py-16 text-center text-red-500">
        Không thể tải giỏ hàng. Kiểm tra CommerceService (port 5003).
      </div>
    );

  const isEmpty = !cart?.items?.length;

  return (
    <div className="max-w-3xl mx-auto px-4 py-8">
      <h1 className="text-2xl font-bold text-gray-800 mb-6 flex items-center gap-2">
        <ShoppingBag size={26} /> Giỏ hàng của bạn
      </h1>

      {isEmpty ? (
        <div className="text-center py-16 flex flex-col items-center gap-4 text-gray-400">
          <ShoppingBag size={60} strokeWidth={1} />
          <p>Giỏ hàng đang trống</p>
          <button
            onClick={() => navigate("/")}
            className="text-nexus-600 text-sm hover:underline"
          >
            Khám phá sản phẩm →
          </button>
        </div>
      ) : (
        <div className="flex flex-col gap-4">
          {cart.items.map((item) => (
            <div
              key={item.productId}
              className="flex items-center gap-4 bg-white rounded-xl p-4 shadow-sm border border-gray-100"
            >
              <div className="w-16 h-16 bg-nexus-50 rounded-lg flex items-center justify-center shrink-0 text-2xl overflow-hidden">
                {item.imageUrl
                  ? <img src={item.imageUrl} alt={item.productName} className="w-full h-full object-cover" onError={(e) => { (e.target as HTMLImageElement).style.display = "none"; }} />
                  : "📦"}
              </div>
              <div className="flex-1 min-w-0">
                <p className="font-semibold text-gray-800 truncate">{item.productName}</p>
                <p className="text-sm text-gray-400">
                  {item.quantity} × {fmtVND(item.unitPrice)}
                </p>
              </div>
              <div className="text-right">
                <p className="font-bold text-nexus-700">{fmtVND(item.subtotal)}</p>
                <button
                  onClick={() => removeMutation.mutate(item.productId)}
                  className="text-red-400 hover:text-red-600 mt-1 transition-colors"
                  title="Xóa khỏi giỏ"
                >
                  <Trash2 size={16} />
                </button>
              </div>
            </div>
          ))}

          <div className="bg-nexus-50 rounded-xl p-5 mt-2 flex items-center justify-between">
            <div>
              <p className="text-gray-500 text-sm">Tổng cộng</p>
              <p className="text-2xl font-bold text-nexus-700">{fmtVND(cart.totalAmount)}</p>
            </div>
            <button
              onClick={() => navigate("/checkout")}
              className="bg-nexus-600 hover:bg-nexus-700 text-white font-semibold px-8 py-3 rounded-xl transition-colors"
            >
              Đặt hàng
            </button>
          </div>
        </div>
      )}
    </div>
  );
}

// ─── Main component — chọn view theo trạng thái đăng nhập ────────────────────
export default function CartPage() {
  const { isLoggedIn } = useAuth();
  return isLoggedIn ? <LoggedInCartView /> : <GuestCartView />;
}
