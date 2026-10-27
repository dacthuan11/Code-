// ============================================================
// Trang chi tiết đấu giá - xem thông tin, lịch sử bid, đặt giá
// ============================================================
import { useState, useEffect, useRef } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { getAuction, getBids, placeBid } from "@shared/api/auction";
import { useAuth } from "@app/providers/AuthContext";
import LoadingSpinner from "@shared/ui/LoadingSpinner";
import toast from "react-hot-toast";
import { useToast } from "@app/providers/ToastContext";
import { ArrowLeft, Gavel, TrendingUp, Clock } from "lucide-react";

const fmtVND = (n: number) =>
  new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND" }).format(n);

const fmtDate = (s: string) =>
  new Date(s).toLocaleString("vi-VN", {
    day: "2-digit", month: "2-digit", year: "numeric",
    hour: "2-digit", minute: "2-digit",
  });

export default function AuctionDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { user, isLoggedIn } = useAuth();
  const queryClient = useQueryClient();
  const [bidAmount, setBidAmount] = useState("");
  const { show } = useToast();
  const prevStatusRef = useRef<string | undefined>(undefined);

  // Lấy thông tin auction
  const { data: auction, isLoading } = useQuery({
    queryKey: ["auction", id],
    queryFn: () => getAuction(id!),
    // Tự động refetch mỗi 10 giây khi đang Active để cập nhật giá
    refetchInterval: (query) => query.state.data?.status === "Active" ? 10000 : false,
    enabled: !!id,
  });

  // Lịch sử các bid
  const { data: bids } = useQuery({
    queryKey: ["bids", id],
    queryFn: () => getBids(id!),
    refetchInterval: auction?.status === "Active" ? 10000 : false,
    enabled: !!id,
  });

  // Phát hiện khi đấu giá kết thúc và người dùng thắng
  useEffect(() => {
    if (!auction || !user) return;
    const prevStatus = prevStatusRef.current;
    if (prevStatus === "Active" && auction.status !== "Active") {
      if (auction.currentWinnerId && auction.currentWinnerId === user.userId) {
        show(`Chúc mừng! Bạn đã thắng với giá ${fmtVND(auction.currentPrice)}`, "success", "Bạn đã thắng đấu giá 🎉");
      }
    }
    prevStatusRef.current = auction.status;
  }, [auction?.status, auction?.currentWinnerId]);

  // Đặt giá
  const bidMutation = useMutation({
    mutationFn: (amount: number) => placeBid(id!, amount),
    onSuccess: () => {
      toast.success("Đặt giá thành công!");
      setBidAmount("");
      // Cập nhật lại auction và bids
      queryClient.invalidateQueries({ queryKey: ["auction", id] });
      queryClient.invalidateQueries({ queryKey: ["bids", id] });
    },
    onError: (err: any) => {
      const msg = err?.response?.data?.message ?? "Đặt giá thất bại";
      toast.error(msg);
    },
  });

  const handleBid = () => {
    if (!isLoggedIn) { toast.error("Đăng nhập để đặt giá"); return; }
    const amount = parseFloat(bidAmount);
    if (isNaN(amount) || amount <= 0) { toast.error("Nhập số tiền hợp lệ"); return; }
    const minBid = (auction?.currentPrice || auction?.startingPrice || 0) + (auction?.bidIncrement || 1000);
    if (amount < minBid) {
      toast.error(`Giá phải cao hơn giá hiện tại ít nhất ${fmtVND(auction?.bidIncrement ?? 1000)}`);
      return;
    }
    bidMutation.mutate(amount);
  };

  if (isLoading) return <LoadingSpinner />;
  if (!auction)
    return <div className="text-center py-16 text-red-500">Không tìm thấy cuộc đấu giá</div>;

  const isActive = auction.status === "Active";
  const minBid = (auction.currentPrice || auction.startingPrice) + auction.bidIncrement;

  return (
    <div className="max-w-4xl mx-auto px-4 py-8">
      <button onClick={() => navigate(-1)} className="flex items-center gap-1 text-sm text-gray-500 hover:text-nexus-600 mb-6">
        <ArrowLeft size={16} /> Quay lại danh sách đấu giá
      </button>

      <div className="grid md:grid-cols-5 gap-6">
        {/* Thông tin đấu giá - chiếm 3/5 */}
        <div className="md:col-span-3 space-y-5">
          <div className="bg-white rounded-2xl border border-gray-100 shadow-sm p-6">
            <div className="flex items-start justify-between mb-4">
              <h1 className="text-xl font-bold text-gray-800">
                Sản phẩm #{auction.productId.slice(0, 8)}
              </h1>
              <span className={`text-xs font-semibold px-3 py-1 rounded-full ${
                isActive ? "bg-green-100 text-green-700" : "bg-gray-100 text-gray-600"
              }`}>
                {isActive ? "🟢 Đang diễn ra" : auction.status}
              </span>
            </div>

            <div className="grid grid-cols-2 gap-4 mb-4">
              <div className="bg-gray-50 rounded-xl p-3">
                <p className="text-xs text-gray-400">Giá khởi điểm</p>
                <p className="text-lg font-bold text-gray-700">{fmtVND(auction.startingPrice)}</p>
              </div>
              <div className="bg-nexus-50 rounded-xl p-3">
                <p className="text-xs text-gray-400 flex items-center gap-1"><TrendingUp size={11} /> Giá hiện tại</p>
                <p className="text-lg font-bold text-nexus-700">
                  {auction.currentPrice > auction.startingPrice
                    ? fmtVND(auction.currentPrice)
                    : "Chưa có bid"}
                </p>
              </div>
            </div>

            <div className="text-sm text-gray-500 space-y-1">
              <p className="flex items-center gap-1"><Clock size={14} /> Bắt đầu: {fmtDate(auction.startTime)}</p>
              <p className="flex items-center gap-1"><Clock size={14} /> Kết thúc: {fmtDate(auction.endTime)}</p>
              {auction.totalBids > 0 && (
                <p className="text-xs text-gray-400">Tổng số bid: {auction.totalBids}</p>
              )}
            </div>
          </div>

          {/* Form đặt giá */}
          {isActive && (
            <div className="bg-white rounded-2xl border border-nexus-200 shadow-sm p-6">
              <h2 className="font-semibold text-gray-700 mb-3 flex items-center gap-2">
                <Gavel size={18} className="text-nexus-600" /> Đặt giá của bạn
              </h2>
              {isLoggedIn ? (
                <div className="flex gap-3">
                  <div className="relative flex-1">
                    <input
                      type="number"
                      value={bidAmount}
                      onChange={(e) => setBidAmount(e.target.value)}
                      placeholder={`Tối thiểu ${fmtVND(minBid)}`}
                      min={minBid}
                      step={auction.bidIncrement}
                      className="w-full border border-gray-200 rounded-xl px-4 py-3 text-sm outline-none focus:ring-2 focus:ring-nexus-500 focus:border-transparent"
                    />
                  </div>
                  <button
                    onClick={handleBid}
                    disabled={bidMutation.isPending}
                    className="bg-nexus-600 hover:bg-nexus-700 disabled:bg-nexus-300 text-white font-semibold px-6 rounded-xl transition-colors"
                  >
                    {bidMutation.isPending ? "Đang đặt..." : "Đặt giá"}
                  </button>
                </div>
              ) : (
                <p className="text-sm text-gray-500">
                  <a href="/login" className="text-nexus-600 hover:underline font-medium">Đăng nhập</a> để tham gia đấu giá
                </p>
              )}
            </div>
          )}
        </div>

        {/* Lịch sử bid - chiếm 2/5 */}
        <div className="md:col-span-2">
          <div className="bg-white rounded-2xl border border-gray-100 shadow-sm p-5 sticky top-24">
            <h2 className="font-semibold text-gray-700 mb-4 flex items-center gap-2">
              <TrendingUp size={16} className="text-nexus-500" />
              Lịch sử đặt giá
              {bids && bids.length > 0 && (
                <span className="ml-auto text-xs font-normal text-gray-400">{bids.length} lượt bid</span>
              )}
            </h2>
            {!bids?.length ? (
              <div className="text-center py-8">
                <p className="text-2xl mb-2">🔨</p>
                <p className="text-sm text-gray-400">Chưa có ai đặt giá</p>
                <p className="text-xs text-gray-300 mt-1">Hãy là người đầu tiên!</p>
              </div>
            ) : (
              <div className="relative pl-5 space-y-1 max-h-[420px] overflow-y-auto pr-1">
                {/* Timeline line */}
                <div className="absolute left-2 top-0 bottom-0 w-0.5 bg-gray-100" />

                {bids.map((bid, idx) => {
                  const maskEmail = (email: string) => {
                    const [local, domain] = email.split("@");
                    if (!domain) return email.slice(0, 6) + "***";
                    return local.slice(0, 3) + "***@" + domain;
                  };
                  return (
                    <div key={bid.id} className="relative flex gap-3 py-2.5">
                      {/* Dot */}
                      <div className={`absolute -left-5 top-4 w-2.5 h-2.5 rounded-full border-2 shrink-0 ${
                        bid.isWinning
                          ? "bg-nexus-600 border-nexus-600"
                          : idx === 0 ? "bg-orange-400 border-orange-400" : "bg-white border-gray-300"
                      }`} />

                      <div className={`flex-1 rounded-xl px-3 py-2.5 ${
                        bid.isWinning ? "bg-nexus-50 border border-nexus-100" :
                        idx === 0 ? "bg-orange-50 border border-orange-100" : "bg-gray-50"
                      }`}>
                        <div className="flex items-center justify-between">
                          <span className={`text-sm font-bold ${bid.isWinning ? "text-nexus-700" : idx === 0 ? "text-orange-600" : "text-gray-700"}`}>
                            {fmtVND(bid.amount)}
                          </span>
                          {bid.isWinning && (
                            <span className="text-[10px] bg-nexus-600 text-white px-1.5 py-0.5 rounded-full font-bold">Cao nhất</span>
                          )}
                        </div>
                        <p className="text-xs text-gray-500 mt-0.5">{maskEmail(bid.bidderEmail || bid.bidderId.slice(0, 8))}</p>
                        <p className="text-[10px] text-gray-400 flex items-center gap-1 mt-0.5">
                          <Clock size={9} />
                          {fmtDate(bid.createdAt)}
                        </p>
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
