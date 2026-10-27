// Route bảo vệ - chỉ cho phép truy cập khi đã đăng nhập
import { Navigate } from "react-router-dom";
import { useAuth } from "@app/providers/AuthContext";

interface Props {
  children: React.ReactNode;
  // Có thể giới hạn thêm theo role (ví dụ: chỉ ADMIN)
  requiredRole?: string;
}

export default function ProtectedRoute({ children, requiredRole }: Props) {
  const { isLoggedIn, user } = useAuth();

  if (!isLoggedIn) return <Navigate to="/login" replace />;
  if (requiredRole && user?.role !== requiredRole) return <Navigate to="/" replace />;

  return <>{children}</>;
}
