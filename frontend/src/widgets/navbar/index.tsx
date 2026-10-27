import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "@app/providers/AuthContext";
import { useCart } from "@app/providers/CartContext";
import { ShoppingCart, LogOut, Package, Gavel, Settings, Search, Home } from "lucide-react";
import { useState, useRef } from "react";
import { useQuery } from "@tanstack/react-query";
import { getCart } from "@shared/api/commerce";
import { getCategories } from "@shared/api/catalog";
import NotificationBell from "@widgets/notification-bell";

export default function Navbar() {
  const { user, isLoggedIn, logout } = useAuth();
  const { guestCartCount } = useCart();
  const navigate = useNavigate();
  const [searchText, setSearchText] = useState("");
  const searchInputRef = useRef<HTMLInputElement>(null);

  const { data: backendCart } = useQuery({
    queryKey: ["cart"],
    queryFn: getCart,
    enabled: isLoggedIn,
    staleTime: 30_000,
  });

  const { data: categories } = useQuery({
    queryKey: ["categories"],
    queryFn: getCategories,
    staleTime: 5 * 60 * 1000,
  });

  const backendCartCount = backendCart?.items.reduce((s, i) => s + i.quantity, 0) ?? 0;
  const totalCartCount = isLoggedIn ? backendCartCount : guestCartCount;

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    if (searchText.trim()) {
      navigate(`/?search=${encodeURIComponent(searchText.trim())}`);
    }
  };

  const handleLogout = () => { logout(); navigate("/login"); };

  return (
    <nav className="bg-nexus-700 sticky top-0 z-50 shadow-md">
      {/* ── Row 1: Logo + Search + Actions ─────────────────────────── */}
      <div className="max-w-7xl mx-auto px-4 py-3 flex items-center gap-4">
        {/* Logo */}
        <Link to="/" className="flex items-center gap-2 font-bold text-xl text-white shrink-0">
          <Package size={24} />
          <span className="hidden sm:block">Nexus</span>
        </Link>

        {/* Search bar — trung tâm, nổi bật */}
        <form onSubmit={handleSearch} className="flex-1 max-w-2xl mx-auto">
          <div className="flex items-stretch rounded-lg overflow-hidden bg-white shadow-sm">
            <input
              ref={searchInputRef}
              type="text"
              value={searchText}
              onChange={e => setSearchText(e.target.value)}
              placeholder="Tìm kiếm sản phẩm, thương hiệu..."
              className="flex-1 px-4 py-2.5 text-sm text-gray-800 outline-none placeholder:text-gray-400 min-w-0"
            />
            <button
              type="submit"
              className="bg-nexus-500 hover:bg-nexus-400 text-white px-5 flex items-center gap-1.5 font-medium text-sm shrink-0 transition-colors"
            >
              <Search size={15} />
              <span className="hidden sm:block">Tìm</span>
            </button>
          </div>
        </form>

        {/* Actions */}
        <div className="flex items-center gap-4 text-white shrink-0">
          {/* Giỏ hàng */}
          {(!isLoggedIn || user?.role !== "SELLER") && (
            <Link to="/cart" className="flex flex-col items-center gap-0.5 hover:text-nexus-200 transition-colors relative">
              <span className="relative">
                <ShoppingCart size={22} />
                {totalCartCount > 0 && (
                  <span className="absolute -top-2 -right-2 bg-red-500 text-white text-[10px] font-bold rounded-full min-w-[16px] h-4 flex items-center justify-center px-0.5">
                    {totalCartCount > 99 ? "99+" : totalCartCount}
                  </span>
                )}
              </span>
              <span className="text-[10px] hidden sm:block">Giỏ hàng</span>
            </Link>
          )}

          {isLoggedIn ? (
            <>
              <NotificationBell />
              {(user?.role === "ADMIN" || user?.role === "SELLER") && (
                <Link to="/admin" className="flex flex-col items-center gap-0.5 hover:text-nexus-200 transition-colors">
                  <Settings size={22} />
                  <span className="text-[10px] hidden sm:block">Quản lý</span>
                </Link>
              )}
              {/* User info + logout */}
              <div className="flex items-center gap-2 pl-3 border-l border-nexus-500">
                <div className="hidden md:block text-right">
                  <p className="text-xs font-semibold leading-none">{user?.displayName}</p>
                  <p className="text-[10px] text-nexus-200 mt-0.5">{user?.role}</p>
                </div>
                <button onClick={handleLogout} className="hover:text-red-300 transition-colors" title="Đăng xuất">
                  <LogOut size={18} />
                </button>
              </div>
            </>
          ) : (
            <div className="flex items-center gap-2 pl-3 border-l border-nexus-500">
              <Link to="/login" className="text-sm hover:text-nexus-200 transition-colors">Đăng nhập</Link>
              <span className="text-nexus-400">|</span>
              <Link to="/register" className="text-sm hover:text-nexus-200 transition-colors">Đăng ký</Link>
            </div>
          )}
        </div>
      </div>

      {/* ── Row 2: Category nav bar ───────────────────────────────── */}
      <div className="border-t border-nexus-600 bg-nexus-800">
        <div className="max-w-7xl mx-auto px-4">
          <div className="flex items-center gap-0 overflow-x-auto scrollbar-none">
            <Link
              to="/"
              className="flex items-center gap-1.5 px-3 py-2 text-xs text-nexus-200 hover:text-white hover:bg-nexus-700 transition-colors whitespace-nowrap shrink-0"
            >
              <Home size={13} /> Trang chủ
            </Link>
            <Link
              to="/auctions"
              className="flex items-center gap-1.5 px-3 py-2 text-xs text-nexus-200 hover:text-white hover:bg-nexus-700 transition-colors whitespace-nowrap shrink-0"
            >
              <Gavel size={13} /> Đấu giá trực tuyến
            </Link>
            {isLoggedIn && (
              <Link
                to="/orders"
                className="flex items-center gap-1.5 px-3 py-2 text-xs text-nexus-200 hover:text-white hover:bg-nexus-700 transition-colors whitespace-nowrap shrink-0"
              >
                <Package size={13} /> Đơn hàng của tôi
              </Link>
            )}
            {/* Separator */}
            <span className="mx-1 text-nexus-600 shrink-0">|</span>
            {/* Top categories */}
            {categories?.slice(0, 8).map(c => (
              <button
                key={c.categoryId}
                onClick={() => navigate(`/?cat=${c.categoryId}`)}
                className="px-3 py-2 text-xs text-nexus-200 hover:text-white hover:bg-nexus-700 transition-colors whitespace-nowrap shrink-0"
              >
                {c.name}
              </button>
            ))}
          </div>
        </div>
      </div>

      {/* Mobile search (below nav on small screens) */}
      <div className="sm:hidden bg-nexus-800 px-4 pb-2">
        <form onSubmit={handleSearch} className="flex rounded-lg overflow-hidden bg-white">
          <input
            type="text"
            value={searchText}
            onChange={e => setSearchText(e.target.value)}
            placeholder="Tìm kiếm..."
            className="flex-1 px-3 py-2 text-sm outline-none text-gray-800 placeholder:text-gray-400"
          />
          <button type="submit" className="bg-nexus-500 text-white px-4 flex items-center">
            <Search size={15} />
          </button>
        </form>
      </div>
    </nav>
  );
}
