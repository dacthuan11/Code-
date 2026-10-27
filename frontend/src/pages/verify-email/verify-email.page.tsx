// ============================================================
// Trang xác thực email — nhận token từ URL, gọi BE verify
// ============================================================
import { useEffect, useState } from "react";
import { useSearchParams, Link } from "react-router-dom";
import { verifyEmail } from "@shared/api/auth";
import { CheckCircle, XCircle, Loader2 } from "lucide-react";

export default function VerifyEmailPage() {
  const [params] = useSearchParams();
  const token = params.get("token") ?? "";
  const [status, setStatus] = useState<"loading" | "success" | "error">("loading");
  const [message, setMessage] = useState("");

  useEffect(() => {
    if (!token) {
      setStatus("error");
      setMessage("Token xác thực không hợp lệ.");
      return;
    }
    verifyEmail(token)
      .then((res) => { setStatus("success"); setMessage(res.message); })
      .catch(() => { setStatus("error"); setMessage("Token không hợp lệ hoặc đã hết hạn."); });
  }, [token]);

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 px-4">
      <div className="bg-white rounded-2xl shadow-md border border-gray-100 p-10 max-w-md w-full text-center">
        {status === "loading" && (
          <>
            <Loader2 className="animate-spin mx-auto mb-4 text-nexus-600" size={48} />
            <p className="text-gray-600">Đang xác thực email...</p>
          </>
        )}
        {status === "success" && (
          <>
            <CheckCircle className="mx-auto mb-4 text-green-500" size={48} />
            <h1 className="text-xl font-bold text-gray-800 mb-2">Xác thực thành công!</h1>
            <p className="text-gray-600 mb-6">{message}</p>
            <Link
              to="/login"
              className="inline-block bg-nexus-600 hover:bg-nexus-700 text-white font-semibold px-6 py-3 rounded-xl transition-colors"
            >
              Đăng nhập ngay
            </Link>
          </>
        )}
        {status === "error" && (
          <>
            <XCircle className="mx-auto mb-4 text-red-500" size={48} />
            <h1 className="text-xl font-bold text-gray-800 mb-2">Xác thực thất bại</h1>
            <p className="text-gray-600 mb-6">{message}</p>
            <Link
              to="/register"
              className="inline-block bg-gray-100 hover:bg-gray-200 text-gray-700 font-semibold px-6 py-3 rounded-xl transition-colors"
            >
              Đăng ký lại
            </Link>
          </>
        )}
      </div>
    </div>
  );
}
