import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { getProducts, createProduct, deleteProduct, getCategories } from "@shared/api/catalog";
import { useAuth } from "@app/providers/AuthContext";
import LoadingSpinner from "@shared/ui/LoadingSpinner";
import toast from "react-hot-toast";
import { Plus, Trash2, Package, Tag } from "lucide-react";

const fmtVND = (n: number) =>
  new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND" }).format(n);

export default function SellerDashboard() {
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const [showAdd, setShowAdd] = useState(false);
  const [form, setForm] = useState({
    name: "", description: "", price: "", stockQuantity: "1", categoryId: "",
  });

  const { data: products, isLoading } = useQuery({
    queryKey: ["products", "seller", user?.userId],
    queryFn: () => getProducts({ pageSize: 100 }),
  });

  const { data: categories } = useQuery({
    queryKey: ["categories"],
    queryFn: getCategories,
  });

  const createMutation = useMutation({
    mutationFn: () => createProduct({
      name: form.name,
      description: form.description,
      price: Number(form.price),
      stockQuantity: Number(form.stockQuantity),
      categoryId: form.categoryId,
    }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["products"] });
      toast.success("Thêm sản phẩm thành công!");
      setShowAdd(false);
      setForm({ name: "", description: "", price: "", stockQuantity: "1", categoryId: "" });
    },
    onError: () => toast.error("Thêm sản phẩm thất bại"),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteProduct(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["products"] });
      toast.success("Đã xóa sản phẩm");
    },
    onError: () => toast.error("Không thể xóa sản phẩm"),
  });

  if (isLoading) return <LoadingSpinner />;

  const myProducts = products?.items ?? [];

  return (
    <div className="max-w-5xl mx-auto px-4 py-8">
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-gray-800 flex items-center gap-2">
          <Package size={26} /> Quản lý sản phẩm
        </h1>
        <button
          onClick={() => setShowAdd(!showAdd)}
          className="flex items-center gap-2 px-4 py-2 bg-nexus-600 text-white rounded-xl hover:bg-nexus-700 transition text-sm font-medium"
        >
          <Plus size={16} /> Thêm sản phẩm
        </button>
      </div>

      {/* Add product form */}
      {showAdd && (
        <div className="bg-white rounded-2xl border border-gray-100 shadow-sm p-6 mb-6">
          <h2 className="text-base font-semibold text-gray-700 mb-4">Sản phẩm mới</h2>
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <input
              placeholder="Tên sản phẩm *"
              value={form.name}
              onChange={e => setForm(f => ({ ...f, name: e.target.value }))}
              className="border border-gray-200 rounded-xl px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-nexus-400"
            />
            <input
              type="number"
              placeholder="Giá (VNĐ) *"
              value={form.price}
              onChange={e => setForm(f => ({ ...f, price: e.target.value }))}
              className="border border-gray-200 rounded-xl px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-nexus-400"
            />
            <textarea
              placeholder="Mô tả sản phẩm"
              value={form.description}
              onChange={e => setForm(f => ({ ...f, description: e.target.value }))}
              rows={2}
              className="sm:col-span-2 border border-gray-200 rounded-xl px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-nexus-400 resize-none"
            />
            <input
              type="number"
              placeholder="Số lượng"
              value={form.stockQuantity}
              onChange={e => setForm(f => ({ ...f, stockQuantity: e.target.value }))}
              className="border border-gray-200 rounded-xl px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-nexus-400"
            />
            <select
              value={form.categoryId}
              onChange={e => setForm(f => ({ ...f, categoryId: e.target.value }))}
              className="border border-gray-200 rounded-xl px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-nexus-400 bg-white"
            >
              <option value="">-- Chọn danh mục --</option>
              {categories?.map(c => (
                <option key={c.categoryId} value={c.categoryId}>{c.name}</option>
              ))}
            </select>
          </div>
          <div className="flex gap-2 mt-4">
            <button
              onClick={() => createMutation.mutate()}
              disabled={createMutation.isPending || !form.name || !form.price}
              className="px-5 py-2 bg-nexus-600 text-white rounded-xl text-sm font-medium hover:bg-nexus-700 disabled:opacity-50 transition"
            >
              {createMutation.isPending ? "Đang lưu..." : "Lưu sản phẩm"}
            </button>
            <button
              onClick={() => setShowAdd(false)}
              className="px-5 py-2 border border-gray-200 rounded-xl text-sm text-gray-500 hover:bg-gray-50 transition"
            >
              Hủy
            </button>
          </div>
        </div>
      )}

      {/* Product list */}
      {myProducts.length === 0 ? (
        <div className="text-center py-16 text-gray-400">
          Bạn chưa có sản phẩm nào. Hãy thêm sản phẩm đầu tiên!
        </div>
      ) : (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {myProducts.map((p) => (
            <div key={p.productId} className="bg-white rounded-2xl border border-gray-100 shadow-sm p-4 flex flex-col gap-2">
              {p.imageUrl && (
                <img src={p.imageUrl} alt={p.name} className="w-full h-36 object-cover rounded-xl" />
              )}
              <h3 className="font-semibold text-gray-800 text-sm line-clamp-2">{p.name}</h3>
              <div className="flex items-center gap-2 text-xs text-gray-400">
                <Tag size={12} /> {p.categoryName ?? "Chưa phân loại"}
              </div>
              <p className="font-bold text-nexus-600">{fmtVND(p.price)}</p>
              <button
                onClick={() => {
                  if (confirm("Xóa sản phẩm này?")) deleteMutation.mutate(p.productId);
                }}
                className="flex items-center gap-1 text-xs text-red-500 hover:text-red-700 transition mt-auto"
              >
                <Trash2 size={12} /> Xóa
              </button>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
