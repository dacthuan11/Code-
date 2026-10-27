import { useState, useEffect } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useSearchParams, Link } from "react-router-dom";
import { getProducts, getCategories } from "@shared/api/catalog";
import { addToCart } from "@shared/api/commerce";
import { useAuth } from "@app/providers/AuthContext";
import { useCart } from "@app/providers/CartContext";
import ProductCard from "@shared/ui/ProductCard";
import LoadingSpinner from "@shared/ui/LoadingSpinner";
import toast from "react-hot-toast";
import { Flame, Tag, ChevronRight, Gavel, Shield, Truck, RotateCcw } from "lucide-react";

// Category emoji icons
const CAT_ICONS: Record<string, string> = {
  "Laptop": "💻", "Điện thoại": "📱", "Gia dụng": "🏠", "Thời trang": "👗",
  "Đồ chơi": "🧸", "Sách": "📚", "Thể thao": "⚽", "Mỹ phẩm": "💄",
  "Đồng hồ": "⌚", "Túi xách": "👜", "Giày dép": "👟", "Thực phẩm": "🥗",
  "Điện tử": "🔌", "Nội thất": "🛋️", "Xe cộ": "🚗",
};
const defaultCatIcon = "🛍️";

export default function HomePage() {
  const { isLoggedIn } = useAuth();
  const { addToGuestCart } = useCart();
  const queryClient = useQueryClient();
  const [searchParams, setSearchParams] = useSearchParams();

  const urlSearch = searchParams.get("search") ?? "";
  const urlCat    = searchParams.get("cat") ?? "";
  const [page, setPage] = useState(1);
  const PAGE_SIZE = 20;

  // Reset về trang 1 khi filter đổi
  useEffect(() => { setPage(1); }, [urlSearch, urlCat]);

  const { data, isLoading, isError } = useQuery({
    queryKey: ["products", page, urlSearch, urlCat],
    queryFn: () => getProducts({ page, pageSize: PAGE_SIZE, search: urlSearch || undefined, categoryId: urlCat || undefined }),
    placeholderData: (prev) => prev,
  });

  const { data: categories } = useQuery({
    queryKey: ["categories"],
    queryFn: getCategories,
    staleTime: 5 * 60 * 1000,
  });

  const handleAddToCart = async (productId: string) => {
    const product = data?.items?.find((p) => p.productId === productId);
    if (!isLoggedIn) {
      if (product) {
        addToGuestCart({ productId: product.productId, productName: product.name, price: product.price, quantity: 1, imageUrl: product.imageUrl });
        toast.success("Đã thêm vào giỏ! Đăng nhập để đặt hàng.", { icon: "🛒" });
      }
      return;
    }
    try {
      await addToCart({ productId, productName: product?.name ?? "", unitPrice: product?.price ?? 0, quantity: 1, imageUrl: product?.imageUrl });
      queryClient.invalidateQueries({ queryKey: ["cart"] });
      toast.success("Đã thêm vào giỏ hàng!");
    } catch {
      toast.error("Không thể thêm vào giỏ, hãy thử lại");
    }
  };

  const totalPages = Math.ceil((data?.totalCount ?? 0) / PAGE_SIZE);
  const isFiltered = !!urlSearch || !!urlCat;
  const activeCatName = categories?.find(c => c.categoryId === urlCat)?.name ?? "";

  return (
    <div className="bg-gray-50 min-h-screen">
      {/* ── Hero / Promo Banner ────────────────────────────────────────── */}
      {!isFiltered && (
        <div className="bg-gradient-to-r from-nexus-800 via-nexus-700 to-nexus-500 py-10">
          <div className="max-w-7xl mx-auto px-4 flex flex-col md:flex-row items-center gap-8">
            <div className="text-white flex-1">
              <p className="text-nexus-200 text-sm font-medium mb-1 uppercase tracking-wider">Chào mừng đến với</p>
              <h1 className="text-3xl md:text-4xl font-extrabold mb-3">Nexus Marketplace</h1>
              <p className="text-nexus-100 text-base mb-6 max-w-lg">Mua sắm, đấu giá và nhận ưu đãi tốt nhất — Giao nhanh toàn quốc, đổi trả dễ dàng.</p>
              <div className="flex flex-wrap gap-3">
                <Link to="/auctions" className="bg-white text-nexus-700 px-5 py-2.5 rounded-xl font-bold text-sm hover:bg-nexus-50 transition-colors flex items-center gap-2">
                  <Gavel size={16} /> Tham gia đấu giá
                </Link>
                <button
                  onClick={() => setSearchParams({ search: "laptop" })}
                  className="border border-white/40 text-white px-5 py-2.5 rounded-xl font-medium text-sm hover:bg-white/10 transition-colors"
                >
                  Khám phá ngay →
                </button>
              </div>
            </div>
            {/* Trust badges */}
            <div className="grid grid-cols-2 gap-3 shrink-0">
              {[
                { icon: <Truck size={18} />, label: "Giao hàng toàn quốc", sub: "Từ 30.000đ" },
                { icon: <Shield size={18} />, label: "Bảo đảm chính hãng", sub: "100% authentic" },
                { icon: <RotateCcw size={18} />, label: "Đổi trả 7 ngày", sub: "Không cần lý do" },
                { icon: <Tag size={18} />, label: "Giá tốt nhất", sub: "So sánh 1000+ shop" },
              ].map(b => (
                <div key={b.label} className="bg-white/10 backdrop-blur rounded-xl p-3 flex items-center gap-2 text-white">
                  <span className="text-nexus-200">{b.icon}</span>
                  <div>
                    <p className="text-xs font-semibold">{b.label}</p>
                    <p className="text-[10px] text-nexus-200">{b.sub}</p>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>
      )}

      <div className="max-w-7xl mx-auto px-4 py-6">
        {/* ── Category Icons Grid ────────────────────────────────────────── */}
        {!isFiltered && categories && categories.length > 0 && (
          <div className="bg-white rounded-2xl shadow-sm border border-gray-100 p-5 mb-6">
            <div className="flex items-center justify-between mb-4">
              <h2 className="font-bold text-gray-800 text-base">Danh mục nổi bật</h2>
              <span className="text-xs text-nexus-600 cursor-pointer hover:underline" onClick={() => {}}>Xem tất cả</span>
            </div>
            <div className="grid grid-cols-5 sm:grid-cols-8 md:grid-cols-10 gap-2">
              {categories.map(c => (
                <button
                  key={c.categoryId}
                  onClick={() => setSearchParams({ cat: c.categoryId })}
                  className="flex flex-col items-center gap-1.5 p-2 rounded-xl hover:bg-nexus-50 transition-colors group"
                >
                  <span className="text-2xl group-hover:scale-110 transition-transform">
                    {CAT_ICONS[c.name] ?? defaultCatIcon}
                  </span>
                  <span className="text-[10px] text-gray-600 text-center leading-tight line-clamp-2 font-medium">
                    {c.name}
                  </span>
                </button>
              ))}
            </div>
          </div>
        )}

        {/* ── Flash sale / Ưu đãi hôm nay ───────────────────────────────── */}
        {!isFiltered && (
          <div className="bg-white rounded-2xl shadow-sm border border-gray-100 p-5 mb-6">
            <div className="flex items-center justify-between mb-1">
              <h2 className="font-bold text-gray-800 text-base flex items-center gap-2">
                <Flame size={18} className="text-red-500" />
                <span className="text-red-500">Flash Sale</span>
              </h2>
              <button
                onClick={() => setSearchParams({})}
                className="text-xs text-nexus-600 hover:underline flex items-center gap-0.5"
              >
                Xem tất cả <ChevronRight size={12} />
              </button>
            </div>
            <p className="text-xs text-gray-400 mb-4">Ưu đãi chỉ hôm nay — số lượng có hạn!</p>
            <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 gap-3">
              {data?.items.slice(0, 5).map(p => (
                <Link key={p.productId} to={`/products/${p.productId}`}
                  className="group relative bg-gray-50 rounded-xl overflow-hidden border border-gray-100 hover:border-red-200 hover:shadow-md transition-all">
                  <div className="absolute top-2 left-2 z-10 bg-red-500 text-white text-[10px] font-bold px-1.5 py-0.5 rounded">
                    -{Math.floor(10 + (p.price % 30))}%
                  </div>
                  <div className="h-28 overflow-hidden">
                    {p.imageUrl
                      ? <img src={p.imageUrl} alt={p.name} className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300" />
                      : <div className="w-full h-full flex items-center justify-center text-3xl bg-nexus-50">📦</div>}
                  </div>
                  <div className="p-2">
                    <p className="text-xs text-gray-700 line-clamp-2 leading-tight mb-1">{p.name}</p>
                    <p className="text-sm font-bold text-red-500">
                      {new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND" }).format(p.price)}
                    </p>
                  </div>
                </Link>
              ))}
            </div>
          </div>
        )}

        {/* ── Product grid section ────────────────────────────────────────── */}
        <div className="bg-white rounded-2xl shadow-sm border border-gray-100 p-5">
          <div className="flex items-center justify-between mb-4">
            <h2 className="font-bold text-gray-800 text-base flex items-center gap-2">
              {isFiltered
                ? (urlSearch
                    ? <>Kết quả tìm kiếm "<span className="text-nexus-600">{urlSearch}</span>"</>
                    : <>Danh mục: <span className="text-nexus-600">{activeCatName}</span></>)
                : <><Tag size={16} className="text-nexus-500" /> Gợi ý hôm nay</>}
            </h2>
            {isFiltered && (
              <button
                onClick={() => setSearchParams({})}
                className="text-xs text-gray-400 hover:text-nexus-600 transition-colors"
              >
                ✕ Xóa bộ lọc
              </button>
            )}
            {!isFiltered && data?.totalCount && (
              <span className="text-xs text-gray-400">{data.totalCount} sản phẩm</span>
            )}
          </div>

          {isLoading ? (
            <LoadingSpinner />
          ) : isError ? (
            <div className="text-center py-16 text-red-500 text-sm">
              Không thể tải sản phẩm. Kiểm tra kết nối đến CatalogService.
            </div>
          ) : data?.items.length === 0 ? (
            <div className="text-center py-16">
              <p className="text-4xl mb-3">🔍</p>
              <p className="text-gray-500 font-medium">Không tìm thấy sản phẩm nào</p>
              <button onClick={() => setSearchParams({})} className="mt-3 text-sm text-nexus-600 hover:underline">
                Xem tất cả sản phẩm
              </button>
            </div>
          ) : (
            <>
              <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 gap-4">
                {data?.items.map((p) => (
                  <ProductCard key={p.productId} product={p} onAddToCart={handleAddToCart} />
                ))}
              </div>

              {totalPages > 1 && (
                <div className="flex justify-center gap-2 mt-8">
                  <button onClick={() => setPage((p) => Math.max(1, p - 1))} disabled={page === 1}
                    className="px-4 py-2 rounded-lg border text-sm disabled:opacity-40 hover:bg-nexus-50 transition-colors">
                    ‹ Trước
                  </button>
                  {Array.from({ length: Math.min(totalPages, 5) }, (_, i) => {
                    const p = Math.max(1, Math.min(totalPages - 4, page - 2)) + i;
                    return (
                      <button key={p} onClick={() => setPage(p)}
                        className={`px-3 py-2 rounded-lg text-sm transition-colors ${p === page ? "bg-nexus-600 text-white" : "border hover:bg-nexus-50"}`}>
                        {p}
                      </button>
                    );
                  })}
                  <button onClick={() => setPage((p) => Math.min(totalPages, p + 1))} disabled={page === totalPages}
                    className="px-4 py-2 rounded-lg border text-sm disabled:opacity-40 hover:bg-nexus-50 transition-colors">
                    Tiếp ›
                  </button>
                </div>
              )}
            </>
          )}
        </div>
      </div>
    </div>
  );
}
