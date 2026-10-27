// ============================================================
// Trang chi tiết sản phẩm - thông tin đầy đủ + thêm vào giỏ
// Guest: thêm vào localStorage cart
// Logged-in: gọi API backend
// ============================================================
import { useParams, useNavigate } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { getProduct } from "@shared/api/catalog";
import { addToCart } from "@shared/api/commerce";
import { useAuth } from "@app/providers/AuthContext";
import { useCart } from "@app/providers/CartContext";
import LoadingSpinner from "@shared/ui/LoadingSpinner";
import toast from "react-hot-toast";
import { ShoppingCart, ArrowLeft, Package } from "lucide-react";
import { useState } from "react";

const fmtVND = (n: number) =>
  new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND" }).format(n);

export default function ProductDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { isLoggedIn } = useAuth();
  const { addToGuestCart } = useCart();
  const queryClient = useQueryClient();
  const [quantity, setQuantity] = useState(1);
  const [adding, setAdding] = useState(false);

  const { data: product, isLoading, isError } = useQuery({
    queryKey: ["product", id],
    queryFn: () => getProduct(id!),
    enabled: !!id,
  });

  const handleAddToCart = async () => {
    if (!product) return;
    setAdding(true);
    try {
      if (isLoggedIn) {
        // Đã đăng nhập: gọi API backend
        await addToCart({ productId: id!, productName: product.name, unitPrice: product.price, quantity, imageUrl: product.imageUrl });
        queryClient.invalidateQueries({ queryKey: ["cart"] });
        toast.success(`Đã thêm ${quantity} sản phẩm vào giỏ!`);
      } else {
        // Chưa đăng nhập: lưu vào localStorage guest cart
        addToGuestCart({
          productId: product.productId,
          productName: product.name,
          price: product.price,
          quantity,
          imageUrl: product.imageUrl,
        });
        toast.success(`Đã thêm vào giỏ! Đăng nhập để đặt hàng.`, {
          duration: 3500,
          icon: "🛒",
        });
      }
    } catch {
      toast.error("Không thể thêm vào giỏ, thử lại nhé");
    } finally {
      setAdding(false);
    }
  };

  if (isLoading) return <LoadingSpinner />;
  if (isError || !product)
    return (
      <div className="max-w-4xl mx-auto px-4 py-16 text-center text-red-500">
        Không tìm thấy sản phẩm này
      </div>
    );

  return (
    <div className="max-w-5xl mx-auto px-4 py-8">
      <button
        onClick={() => navigate(-1)}
        className="flex items-center gap-1 text-sm text-gray-500 hover:text-nexus-600 mb-6 transition-colors"
      >
        <ArrowLeft size={16} /> Quay lại
      </button>

      <div className="grid md:grid-cols-2 gap-8">
        {/* Ảnh sản phẩm */}
        <div className="bg-gradient-to-br from-nexus-50 to-nexus-100 rounded-2xl flex items-center justify-center aspect-square">
          {product.imageUrl ? (
            <img
              src={product.imageUrl}
              alt={product.name}
              className="w-full h-full object-cover rounded-2xl"
            />
          ) : (
            <Package size={80} className="text-nexus-300" />
          )}
        </div>

        {/* Thông tin sản phẩm */}
        <div className="flex flex-col gap-5">
          <div>
            <h1 className="text-2xl font-bold text-gray-800">{product.name}</h1>
            {product.categoryName && (
              <span className="inline-block mt-1 text-xs bg-nexus-100 text-nexus-700 px-2 py-0.5 rounded-full">
                {product.categoryName}
              </span>
            )}
          </div>

          <div className="text-3xl font-bold text-nexus-700">{fmtVND(product.price)}</div>

          <p className="text-gray-600 leading-relaxed">{product.description}</p>

          <div className="text-sm text-gray-500">
            Tồn kho:{" "}
            <span className={product.stockQuantity > 0 ? "text-green-600 font-medium" : "text-red-500 font-medium"}>
              {product.stockQuantity > 0 ? `${product.stockQuantity} cái` : "Hết hàng"}
            </span>
          </div>

          {product.stockQuantity > 0 && (
            <div className="flex items-center gap-3">
              <span className="text-sm text-gray-700">Số lượng:</span>
              <div className="flex items-center border border-gray-200 rounded-lg overflow-hidden">
                <button
                  onClick={() => setQuantity((q) => Math.max(1, q - 1))}
                  className="px-3 py-1.5 hover:bg-gray-100 transition-colors text-lg"
                >−</button>
                <span className="px-4 py-1.5 text-sm font-medium min-w-[40px] text-center">{quantity}</span>
                <button
                  onClick={() => setQuantity((q) => Math.min(product.stockQuantity, q + 1))}
                  className="px-3 py-1.5 hover:bg-gray-100 transition-colors text-lg"
                >+</button>
              </div>
            </div>
          )}

          <button
            onClick={handleAddToCart}
            disabled={product.stockQuantity === 0 || adding}
            className="flex items-center justify-center gap-2 bg-nexus-600 hover:bg-nexus-700 disabled:bg-gray-200 disabled:cursor-not-allowed text-white font-semibold py-3.5 rounded-xl transition-colors text-base"
          >
            <ShoppingCart size={20} />
            {adding
              ? "Đang thêm..."
              : product.stockQuantity > 0
              ? "Thêm vào giỏ hàng"
              : "Hết hàng"}
          </button>

          {/* Gợi ý đăng nhập nếu là guest */}
          {!isLoggedIn && product.stockQuantity > 0 && (
            <p className="text-xs text-gray-400 text-center -mt-2">
              Chưa đăng nhập? Vẫn thêm được — sẽ tự merge khi bạn đăng nhập.
            </p>
          )}
        </div>
      </div>
    </div>
  );
}
