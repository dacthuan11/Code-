// ============================================================
// API calls cho CatalogService - sản phẩm, danh mục
// ============================================================
import { catalogClient } from "./client";

export interface Product {
  productId: string;
  name: string;
  description: string;
  price: number;
  stockQuantity: number;
  categoryId: string;
  categoryName?: string;
  sellerId: string;
  imageUrl?: string;
  createdAt: string;
}

export interface Category {
  categoryId: string;
  name: string;
  parentCategoryId?: string;
}

export interface ProductListResponse {
  items: Product[];
  totalCount: number;
  page: number;
  pageSize: number;
}

// Lấy danh sách sản phẩm, có thể lọc theo category hoặc tìm kiếm
export const getProducts = (params?: {
  page?: number;
  pageSize?: number;
  categoryId?: string;
  search?: string;
}) =>
  catalogClient
    .get<ProductListResponse>("/api/v1/products", { params: {
      page: params?.page,
      pageSize: params?.pageSize,
      categoryId: params?.categoryId,
      keyword: params?.search,  // backend dùng "keyword" thay vì "search"
    }})
    .then((r) => r.data);

// Xem chi tiết một sản phẩm
export const getProduct = (id: string) =>
  catalogClient.get<Product>(`/api/v1/products/${id}`).then((r) => r.data);

// Tạo sản phẩm mới (chỉ SELLER/ADMIN)
export const createProduct = (data: {
  name: string;
  description: string;
  price: number;
  stockQuantity: number;
  categoryId: string;
}) => catalogClient.post<Product>("/api/v1/products", data).then((r) => r.data);

// Sửa sản phẩm
export const updateProduct = (id: string, data: Partial<Product>) =>
  catalogClient.put<Product>(`/api/v1/products/${id}`, data).then((r) => r.data);

// Xóa sản phẩm
export const deleteProduct = (id: string) =>
  catalogClient.delete(`/api/v1/products/${id}`);

// Lấy tất cả danh mục
export const getCategories = () =>
  catalogClient.get<Category[]>("/api/v1/categories").then((r) => r.data);

// Tạo danh mục mới (ADMIN)
export const createCategory = (data: { name: string; parentCategoryId?: string }) =>
  catalogClient.post<Category>("/api/v1/categories", data).then((r) => r.data);
