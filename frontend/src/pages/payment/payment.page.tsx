import { useEffect, useState } from "react";
import { useSearchParams, useNavigate } from "react-router-dom";
import { confirmPayment } from "@shared/api/commerce";
import toast from "react-hot-toast";
import LoadingSpinner from "@shared/ui/LoadingSpinner";
import { CheckCircle, XCircle } from "lucide-react";

export default function PaymentPage() {
  const [params] = useSearchParams();
  const navigate = useNavigate();
  const [status, setStatus] = useState<"loading" | "success" | "error">("loading");

  useEffect(() => {
    const orderId = params.get("orderId");
    if (!orderId) { setStatus("error"); return; }

    confirmPayment(orderId, `STUB-${orderId}`)
      .then((r) => {
        if (r.success) {
          setStatus("success");
          toast.success("Thanh toán thành công!");
          setTimeout(() => navigate("/orders"), 2000);
        } else {
          setStatus("error");
        }
      })
      .catch(() => setStatus("error"));
  }, []);

  if (status === "loading") return <LoadingSpinner />;

  return (
    <div className="max-w-md mx-auto px-4 py-20 text-center">
      {status === "success" ? (
        <>
          <CheckCircle size={64} className="mx-auto text-green-500 mb-4" />
          <h1 className="text-2xl font-bold text-gray-800">Thanh toán thành công!</h1>
          <p className="text-gray-500 mt-2">Đơn hàng của bạn đang được xử lý. Đang chuyển trang...</p>
        </>
      ) : (
        <>
          <XCircle size={64} className="mx-auto text-red-500 mb-4" />
          <h1 className="text-2xl font-bold text-gray-800">Thanh toán thất bại</h1>
          <p className="text-gray-500 mt-2">Có lỗi xảy ra. Vui lòng thử lại.</p>
          <button
            onClick={() => navigate("/orders")}
            className="mt-6 px-6 py-2 bg-nexus-600 text-white rounded-xl hover:bg-nexus-700 transition"
          >
            Xem đơn hàng
          </button>
        </>
      )}
    </div>
  );
}
