// ============================================================
// Trang Admin - quản lý sản phẩm, danh mục, tài khoản (chỉ ADMIN)
// ============================================================
import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { getProducts, getCategories, createProduct, createCategory, deleteProduct } from "@shared/api/catalog";
import { getUsers, getRoles, changeUserRole, deleteUser } from "@shared/api/auth";
import { useAuth } from "@app/providers/AuthContext";
import { Navigate } from "react-router-dom";
import LoadingSpinner from "@shared/ui/LoadingSpinner";
import toast from "react-hot-toast";
import { Trash2, Plus, Settings, Users, CheckCircle, XCircle } from "lucide-react";

const fmtVND = (n: number) =>
  new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND" }).format(n);

export default function AdminPage() {
  const { user } = useAuth();
  const queryClient = useQueryClient();

  // Chỉ ADMIN hoặc SELLER mới được vào
  if (user?.role !== "ADMIN" && user?.role !== "SELLER") {
    return <Navigate to="/" replace />;
  }

  const [tab, setTab] = useState<"products" | "categories" | "users">("products");

  // ── Products ──────────────────────────────────────────────────────────────
  const [newProduct, setNewProduct] = useState({ name: "", description: "", price: "", stockQuantity: "", categoryId: "" });
  const { data: products, isLoading: loadingProducts } = useQuery({
    queryKey: ["admin-products"],
    queryFn: () => getProducts({ pageSize: 50 }),
  });
  const createProductMutation = useMutation({
    mutationFn: () => createProduct({
      name: newProduct.name,
      description: newProduct.description,
      price: parseFloat(newProduct.price),
      stockQuantity: parseInt(newProduct.stockQuantity),
      categoryId: newProduct.categoryId,
    }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["admin-products"] });
      setNewProduct({ name: "", description: "", price: "", stockQuantity: "", categoryId: "" });
      toast.success("Đã tạo sản phẩm!");
    },
    onError: () => toast.error("Tạo sản phẩm thất bại"),
  });
  const deleteProductMutation = useMutation({
    mutationFn: deleteProduct,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["admin-products"] });
      toast.success("Đã xóa sản phẩm");
    },
    onError: () => toast.error("Không thể xóa sản phẩm này"),
  });

  // ── Users ─────────────────────────────────────────────────────────────────
  const [userSearch, setUserSearch] = useState("");
  const { data: usersData, isLoading: loadingUsers } = useQuery({
    queryKey: ["admin-users", userSearch],
    queryFn: () => getUsers({ search: userSearch || undefined, pageSize: 50 }),
    enabled: tab === "users",
  });
  const { data: roles } = useQuery({
    queryKey: ["roles"],
    queryFn: getRoles,
    enabled: tab === "users",
  });
  const changeRoleMutation = useMutation({
    mutationFn: ({ userId, roleId }: { userId: string; roleId: string }) =>
      changeUserRole(userId, roleId),
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ["admin-users"] }); toast.success("Đã đổi quyền!"); },
    onError: () => toast.error("Đổi quyền thất bại"),
  });
  const deleteUserMutation = useMutation({
    mutationFn: (userId: string) => deleteUser(userId),
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ["admin-users"] }); toast.success("Đã xóa tài khoản"); },
    onError: () => toast.error("Không thể xóa tài khoản này"),
  });

  // ── Categories ────────────────────────────────────────────────────────────
  const [newCatName, setNewCatName] = useState("");
  const { data: categories } = useQuery({ queryKey: ["categories"], queryFn: getCategories });
  const createCatMutation = useMutation({
    mutationFn: () => createCategory({ name: newCatName }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["categories"] });
      setNewCatName("");
      toast.success("Đã tạo danh mục!");
    },
    onError: () => toast.error("Tạo danh mục thất bại"),
  });

  return (
    <div className="max-w-6xl mx-auto px-4 py-8">
      <h1 className="text-2xl font-bold text-gray-800 mb-6 flex items-center gap-2">
        <Settings size={26} /> Quản lý ({user?.role})
      </h1>

      {/* Tab chuyển section */}
      <div className="flex gap-2 mb-6 border-b border-gray-200">
        {(["products", "categories", "users"] as const).map((t) => (
          <button
            key={t}
            onClick={() => setTab(t)}
            className={`pb-3 px-4 text-sm font-medium border-b-2 transition-colors ${
              tab === t ? "border-nexus-600 text-nexus-600" : "border-transparent text-gray-500 hover:text-gray-700"
            }`}
          >
            {t === "products" ? "Sản phẩm" : t === "categories" ? "Danh mục" : (
              <span className="flex items-center gap-1"><Users size={14} />Người dùng</span>
            )}
          </button>
        ))}
      </div>

      {/* ── Tab Sản phẩm ── */}
      {tab === "products" && (
        <div className="space-y-6">
          {/* Form tạo sản phẩm mới */}
          <div className="bg-white rounded-xl border border-gray-100 shadow-sm p-5">
            <h2 className="font-semibold text-gray-700 mb-4 flex items-center gap-1">
              <Plus size={16} /> Thêm sản phẩm mới
            </h2>
            <div className="grid sm:grid-cols-2 gap-3">
              <input placeholder="Tên sản phẩm *" value={newProduct.name}
                onChange={(e) => setNewProduct({ ...newProduct, name: e.target.value })}
                className="border border-gray-200 rounded-lg px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-nexus-500" />
              <select value={newProduct.categoryId}
                onChange={(e) => setNewProduct({ ...newProduct, categoryId: e.target.value })}
                className="border border-gray-200 rounded-lg px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-nexus-500 bg-white">
                <option value="">Chọn danh mục</option>
                {categories?.map((c) => <option key={c.categoryId} value={c.categoryId}>{c.name}</option>)}
              </select>
              <input placeholder="Giá (VND) *" type="number" value={newProduct.price}
                onChange={(e) => setNewProduct({ ...newProduct, price: e.target.value })}
                className="border border-gray-200 rounded-lg px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-nexus-500" />
              <input placeholder="Số lượng tồn kho *" type="number" value={newProduct.stockQuantity}
                onChange={(e) => setNewProduct({ ...newProduct, stockQuantity: e.target.value })}
                className="border border-gray-200 rounded-lg px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-nexus-500" />
              <textarea placeholder="Mô tả sản phẩm" value={newProduct.description}
                onChange={(e) => setNewProduct({ ...newProduct, description: e.target.value })}
                rows={2}
                className="sm:col-span-2 border border-gray-200 rounded-lg px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-nexus-500 resize-none" />
            </div>
            <button
              onClick={() => createProductMutation.mutate()}
              disabled={createProductMutation.isPending || !newProduct.name || !newProduct.price}
              className="mt-3 bg-nexus-600 hover:bg-nexus-700 disabled:bg-gray-200 text-white text-sm font-medium px-5 py-2 rounded-lg transition-colors"
            >
              {createProductMutation.isPending ? "Đang tạo..." : "Tạo sản phẩm"}
            </button>
          </div>

          {/* Danh sách sản phẩm */}
          {loadingProducts ? <LoadingSpinner /> : (
            <div className="bg-white rounded-xl border border-gray-100 shadow-sm overflow-hidden">
              <table className="w-full text-sm">
                <thead className="bg-gray-50 text-gray-500 text-xs uppercase">
                  <tr>
                    <th className="text-left px-4 py-3">Tên</th>
                    <th className="text-right px-4 py-3">Giá</th>
                    <th className="text-right px-4 py-3">Tồn kho</th>
                    <th className="px-4 py-3"></th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-50">
                  {products?.items.map((p) => (
                    <tr key={p.productId} className="hover:bg-gray-50 transition-colors">
                      <td className="px-4 py-3 text-gray-800 font-medium">{p.name}</td>
                      <td className="px-4 py-3 text-right text-nexus-700">{fmtVND(p.price)}</td>
                      <td className="px-4 py-3 text-right text-gray-500">{p.stockQuantity}</td>
                      <td className="px-4 py-3 text-right">
                        <button
                          onClick={() => {
                            if (confirm("Xóa sản phẩm này?")) deleteProductMutation.mutate(p.productId);
                          }}
                          className="text-red-400 hover:text-red-600 transition-colors"
                        >
                          <Trash2 size={15} />
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}

      {/* ── Tab Danh mục ── */}
      {tab === "categories" && (
        <div className="space-y-4">
          <div className="bg-white rounded-xl border border-gray-100 shadow-sm p-5 flex gap-3">
            <input
              placeholder="Tên danh mục mới"
              value={newCatName}
              onChange={(e) => setNewCatName(e.target.value)}
              className="flex-1 border border-gray-200 rounded-lg px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-nexus-500"
            />
            <button
              onClick={() => createCatMutation.mutate()}
              disabled={createCatMutation.isPending || !newCatName}
              className="bg-nexus-600 hover:bg-nexus-700 disabled:bg-gray-200 text-white text-sm font-medium px-5 py-2 rounded-lg transition-colors"
            >
              Thêm
            </button>
          </div>
          <div className="bg-white rounded-xl border border-gray-100 shadow-sm p-5">
            <div className="grid sm:grid-cols-2 gap-2">
              {categories?.map((c) => (
                <div key={c.categoryId} className="flex items-center justify-between bg-gray-50 rounded-lg px-3 py-2">
                  <span className="text-sm text-gray-700">{c.name}</span>
                  <span className="text-xs text-gray-400 font-mono">{c.categoryId.slice(0, 6)}</span>
                </div>
              ))}
            </div>
          </div>
        </div>
      )}

      {/* ── Tab Người dùng ── */}
      {tab === "users" && (
        <div className="space-y-4">
          {/* Tìm kiếm */}
          <div className="bg-white rounded-xl border border-gray-100 shadow-sm p-4">
            <input
              placeholder="Tìm theo tên hoặc email..."
              value={userSearch}
              onChange={(e) => setUserSearch(e.target.value)}
              className="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-nexus-500"
            />
          </div>

          {loadingUsers ? <LoadingSpinner /> : (
            <div className="bg-white rounded-xl border border-gray-100 shadow-sm overflow-hidden">
              <table className="w-full text-sm">
                <thead className="bg-gray-50 text-gray-500 text-xs uppercase">
                  <tr>
                    <th className="text-left px-4 py-3">Tên / Email</th>
                    <th className="text-center px-4 py-3">Xác thực</th>
                    <th className="text-center px-4 py-3">Quyền</th>
                    <th className="px-4 py-3"></th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-50">
                  {usersData?.items.map((u) => (
                    <tr key={u.id} className="hover:bg-gray-50 transition-colors">
                      <td className="px-4 py-3">
                        <div className="font-medium text-gray-800">{u.fullName}</div>
                        <div className="text-xs text-gray-400">{u.email}</div>
                      </td>
                      <td className="px-4 py-3 text-center">
                        {u.isEmailVerified
                          ? <CheckCircle size={16} className="inline text-green-500" />
                          : <XCircle size={16} className="inline text-red-400" />}
                      </td>
                      <td className="px-4 py-3 text-center">
                        <select
                          value={u.roleId}
                          onChange={(e) => {
                            if (e.target.value !== u.roleId)
                              changeRoleMutation.mutate({ userId: u.id, roleId: e.target.value });
                          }}
                          className="border border-gray-200 rounded-lg px-2 py-1 text-xs bg-white outline-none focus:ring-2 focus:ring-nexus-500"
                          disabled={changeRoleMutation.isPending}
                        >
                          {roles?.map((r) => (
                            <option key={r.id} value={r.id}>{r.name}</option>
                          )) ?? (
                            <option value={u.roleId}>{u.roleCode}</option>
                          )}
                        </select>
                      </td>
                      <td className="px-4 py-3 text-right">
                        {u.id !== user?.userId && (
                          <button
                            onClick={() => {
                              if (confirm(`Xóa tài khoản "${u.fullName}"?`))
                                deleteUserMutation.mutate(u.id);
                            }}
                            className="text-red-400 hover:text-red-600 transition-colors"
                          >
                            <Trash2 size={15} />
                          </button>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
              {usersData && (
                <div className="px-4 py-3 text-xs text-gray-400 border-t border-gray-50">
                  Tổng cộng {usersData.total} tài khoản
                </div>
              )}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
