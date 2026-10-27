// ============================================================
// API calls cho UserService - đăng ký, đăng nhập, profile
// ============================================================
import { userClient } from "./client";

// Kiểu dữ liệu trả về khi login
export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  userId: string;
  email: string;
  displayName: string;
  role: string;
}

// Đăng ký tài khoản mới — backend nhận đủ trường (fullName, phoneNumber, ...)
export const register = (data: {
  email: string;
  password: string;
  fullName: string;
  phoneNumber?: string;
  identifyNumber?: string;
  gender?: string;
  address?: string;
  dateOfBirth?: string;
}) => userClient.post<{ userId: string }>("/api/v1/users", data).then((r) => r.data);

// Đăng nhập - trả về JWT token
export const login = (data: { email: string; password: string }) =>
  userClient.post<LoginResponse>("/api/v1/auth/login", data).then((r) => r.data);

// Xem profile người dùng theo ID
export const getProfile = (userId: string) =>
  userClient.get<{ userId: string; email: string; displayName: string; role: string }>(
    `/api/v1/users/${userId}`
  ).then((r) => r.data);

// Refresh JWT access token using a stored refresh token
export const refreshToken = (token: string) =>
  userClient.post<{ accessToken: string; refreshToken: string }>(
    "/api/v1/auth/refresh", { token }
  ).then((r) => r.data);

// Xác thực email sau đăng ký
export const verifyEmail = (token: string) =>
  userClient.post<{ message: string }>("/api/v1/auth/verify-email", { token }).then((r) => r.data);

// ─── Admin user management ─────────────────────────────────────────────────

export interface UserSummary {
  id: string;
  fullName: string;
  email: string;
  phoneNumber: string;
  gender: string;
  address: string;
  dateOfBirth: string;
  trustLevel: string;
  reputationScore: number;
  roleId: string;
  roleCode: string;
  isEmailVerified: boolean;
}

export interface UsersResult {
  items: UserSummary[];
  total: number;
  page: number;
  pageSize: number;
}

export const getUsers = (params?: { search?: string; page?: number; pageSize?: number }) =>
  userClient.get<UsersResult>("/api/v1/users", { params }).then((r) => r.data);

export const changeUserRole = (userId: string, roleId: string) =>
  userClient.patch(`/api/v1/users/${userId}/role`, { roleId }).then((r) => r.data);

export const deleteUser = (userId: string) =>
  userClient.delete(`/api/v1/users/${userId}`).then((r) => r.data);

export interface RoleOption { id: string; name: string; code: string; }
export const getRoles = () =>
  userClient.get<RoleOption[]>("/api/v1/roles").then((r) => r.data);
