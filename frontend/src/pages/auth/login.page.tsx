// ============================================================
// Trang đăng nhập — sau khi login thành công:
//   1. Merge guest cart (localStorage) → backend
//   2. Redirect về trang trước hoặc trang chủ
// ============================================================
import { useState } from "react";
import type { FormEvent } from "react";
import { Link, useNavigate, useLocation } from "react-router-dom";
import { useAuth } from "@app/providers/AuthContext";
import { useCart } from "@app/providers/CartContext";
import toast from "react-hot-toast";
import { Package } from "lucide-react";

export default function LoginPage() {
  const { login } = useAuth();
  const { mergeGuestCartToBackend } = useCart();
  const navigate = useNavigate();
  const location = useLocation();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [loading, setLoading] = useState(false);

  // Trang sẽ redirect sau khi đăng nhập (truyền qua Link state hoặc mặc định về /)
  const redirectTo = (location.state as any)?.from ?? "/";

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!email || !password) {
      toast.error("Vui lòng điền đầy đủ thông tin");
      return;
    }
    setLoading(true);
    try {
      await login(email, password);

      // Merge guest cart vào backend sau khi login (silent fail nếu lỗi)
      await mergeGuestCartToBackend().catch(() => {});

      toast.success("Đăng nhập thành công!");
      navigate(redirectTo, { replace: true });
    } catch (err: any) {
      const status = err?.response?.status;
      const msg = err?.response?.data?.message ?? "Email hoặc mật khẩu không đúng";
      if (status === 403) {
        toast.error(msg, { duration: 6000 });
      } else {
        toast.error(msg);
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-nexus-50 to-white flex items-center justify-center px-4">
      <div className="w-full max-w-md">
        <div className="text-center mb-8">
          <div className="inline-flex items-center justify-center w-16 h-16 bg-nexus-600 rounded-2xl shadow-lg mb-4">
            <Package size={32} className="text-white" />
          </div>
          <h1 className="text-2xl font-bold text-gray-800">Đăng nhập Nexus</h1>
          <p className="text-gray-500 text-sm mt-1">Chào mừng trở lại!</p>
        </div>

        <form
          onSubmit={handleSubmit}
          className="bg-white rounded-2xl shadow-md p-8 flex flex-col gap-5"
        >
          <div className="flex flex-col gap-1.5">
            <label className="text-sm font-medium text-gray-700">Email</label>
            <input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="example@email.com"
              className="border border-gray-200 rounded-lg px-4 py-2.5 text-sm outline-none focus:ring-2 focus:ring-nexus-500 focus:border-transparent transition"
              autoFocus
            />
          </div>

          <div className="flex flex-col gap-1.5">
            <label className="text-sm font-medium text-gray-700">Mật khẩu</label>
            <input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="••••••••"
              className="border border-gray-200 rounded-lg px-4 py-2.5 text-sm outline-none focus:ring-2 focus:ring-nexus-500 focus:border-transparent transition"
            />
          </div>

          <button
            type="submit"
            disabled={loading}
            className="bg-nexus-600 hover:bg-nexus-700 disabled:bg-nexus-300 text-white font-semibold py-3 rounded-xl transition-colors"
          >
            {loading ? "Đang đăng nhập..." : "Đăng nhập"}
          </button>

          <p className="text-center text-sm text-gray-500">
            Chưa có tài khoản?{" "}
            <Link to="/register" className="text-nexus-600 font-medium hover:underline">
              Đăng ký ngay
            </Link>
          </p>
        </form>
      </div>
    </div>
  );
}
