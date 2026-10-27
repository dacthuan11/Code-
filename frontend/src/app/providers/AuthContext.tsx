// ============================================================
// AuthContext - quản lý trạng thái đăng nhập toàn ứng dụng
// Bao gồm: user info, JWT token, login/logout helper
// ============================================================
import { createContext, useContext, useState } from "react";
import type { ReactNode } from "react";
import { login as apiLogin } from "@shared/api/auth";
import type { LoginResponse } from "@shared/api/auth";

interface User {
  userId: string;
  email: string;
  displayName: string;
  role: string;
}

interface AuthContextType {
  user: User | null;
  token: string | null;
  isLoggedIn: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
}

// Context mặc định trống - sẽ được fill bởi Provider
const AuthContext = createContext<AuthContextType | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  // Lấy user/token từ localStorage khi load trang (nhớ đăng nhập)
  const [user, setUser] = useState<User | null>(() => {
    try {
      const saved = localStorage.getItem("nexus_user");
      return saved ? JSON.parse(saved) : null;
    } catch {
      return null;
    }
  });

  const [token, setToken] = useState<string | null>(() =>
    localStorage.getItem("nexus_token")
  );

  // Đăng nhập: gọi API rồi lưu vào state + localStorage
  const login = async (email: string, password: string) => {
    const res: LoginResponse = await apiLogin({ email, password });
    const userData: User = {
      userId: res.userId,
      email: res.email,
      displayName: res.displayName,
      role: res.role,
    };
    setUser(userData);
    setToken(res.accessToken);
    localStorage.setItem("nexus_token", res.accessToken);
    localStorage.setItem("nexus_user", JSON.stringify(userData));
  };

  // Đăng xuất: xóa state và localStorage
  const logout = () => {
    setUser(null);
    setToken(null);
    localStorage.removeItem("nexus_token");
    localStorage.removeItem("nexus_user");
  };

  return (
    <AuthContext.Provider
      value={{ user, token, isLoggedIn: !!user, login, logout }}
    >
      {children}
    </AuthContext.Provider>
  );
}

// Custom hook tiện dùng trong component
export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth phải dùng bên trong AuthProvider");
  return ctx;
}
