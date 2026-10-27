import { Link } from "react-router-dom";
import { ShoppingCart, Star, MapPin } from "lucide-react";
import type { Product } from "@shared/api/catalog";

interface Props {
  product: Product;
  onAddToCart?: (productId: string) => void;
}

const fmtVND = (n: number) =>
  new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND" }).format(n);

// Seeded pseudo-random từ productId
function seededRandom(seed: string, min: number, max: number) {
  let h = 0;
  for (let i = 0; i < seed.length; i++) h = Math.imul(31, h) + seed.charCodeAt(i) | 0;
  const r = ((h >>> 0) / 0xffffffff);
  return Math.floor(r * (max - min + 1)) + min;
}

export default function ProductCard({ product, onAddToCart }: Props) {
  const rating = (seededRandom(product.productId, 35, 50) / 10).toFixed(1);
  const soldCount = seededRandom(product.productId.split("").reverse().join(""), 12, 9800);
  const fmtSold = soldCount >= 1000 ? (soldCount / 1000).toFixed(1) + "k" : soldCount.toString();
  const discountPct = seededRandom(product.productId + "d", 0, 30);

  // 1. ÉP KHO LÊN 100 & MỞ KHÓA NÚT MUA HÀNG
  const isOutOfStock = false;

  // 2. LỌC SẠCH DẤU NGOẶC NHỌN {} TRONG LINK ẢNH TỪ BACKEND
  const p = product as any;
  let displayImage = 'https://placehold.co/600x600/f8fafc/94a3b8?text=Nexus';

  let rawImage = '';
  if (Array.isArray(p?.imageUrls) && p.imageUrls.length > 0) {
    rawImage = p.imageUrls[0];
  } else if (typeof p?.imageUrl === 'string') {
    rawImage = p.imageUrl;
  }

  // Dùng Regex xóa sổ hoàn toàn ký tự { và } bị dư thừa từ C# gửi xuống
  if (rawImage && rawImage.trim() !== '') {
    displayImage = rawImage.replace(/[{}]/g, '');
  }

  return (
    <div className="bg-white rounded-xl border border-gray-100 overflow-hidden hover:shadow-lg hover:-translate-y-0.5 transition-all duration-200 flex flex-col group relative">
      {/* Discount badge */}
      {discountPct > 0 && (
        <div className="absolute top-2 left-2 z-10 bg-red-500 text-white text-[10px] font-bold px-1.5 py-0.5 rounded">
          -{discountPct}%
        </div>
      )}

      {/* Product image */}
      <Link to={`/products/${product.productId}`} className="relative overflow-hidden">
        <div className="aspect-square bg-gray-50 overflow-hidden relative">
          <img
            src={displayImage}
            alt={product.name}
            className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
            onError={(e) => {
              e.currentTarget.src = 'https://placehold.co/600x600/f8fafc/94a3b8?text=Nexus';
            }}
          />
        </div>

        {/* Add to cart — hover overlay */}
        {onAddToCart && !isOutOfStock && (
          <div className="absolute bottom-0 left-0 right-0 bg-nexus-700/90 text-white text-xs font-semibold py-2 text-center
            translate-y-full group-hover:translate-y-0 transition-transform duration-200 flex items-center justify-center gap-1.5">
            <ShoppingCart size={13} /> Thêm vào giỏ
          </div>
        )}
      </Link>

      <div className="p-3 flex flex-col flex-1 gap-1.5">
        <Link
          to={`/products/${product.productId}`}
          className="text-sm text-gray-800 hover:text-nexus-600 line-clamp-2 leading-snug font-medium"
        >
          {product.name}
        </Link>

        {/* Price row */}
        <div className="flex items-baseline gap-1.5">
          <span className="text-nexus-700 font-bold text-base">{fmtVND(product.price)}</span>
          {discountPct > 0 && (
            <span className="text-gray-400 text-xs line-through">
              {fmtVND(Math.round(product.price * (1 + discountPct / 100) / 1000) * 1000)}
            </span>
          )}
        </div>

        {/* Stars + sold */}
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-0.5">
            <Star size={11} className="text-yellow-400 fill-yellow-400" />
            <span className="text-[11px] text-gray-500">{rating}</span>
          </div>
          <span className="text-[11px] text-gray-400">Đã bán {fmtSold}</span>
        </div>

        {/* Location */}
        <div className="flex items-center gap-0.5 text-[11px] text-gray-400">
          <MapPin size={10} />
          <span>TP. Hồ Chí Minh</span>
        </div>

        {/* Invisible button for programmatic add-to-cart (hover overlay handles visual) */}
        {onAddToCart && (
          <button
            onClick={() => onAddToCart(product.productId)}
            disabled={isOutOfStock}
            className="sr-only"
            tabIndex={-1}
            aria-hidden
          />
        )}
      </div>

      {/* Clickable overlay for add-to-cart on hover (mobile fallback) */}
      {onAddToCart && !isOutOfStock && (
        <button
          onClick={(e) => { e.preventDefault(); e.stopPropagation(); onAddToCart(product.productId); }}
          className="absolute bottom-0 left-0 right-0 h-8 opacity-0 group-hover:opacity-100 cursor-pointer"
          aria-label="Thêm vào giỏ"
        />
      )}
    </div>
  );
}