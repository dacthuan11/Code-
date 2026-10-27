// ============================================================
// Trang danh sách đấu giá - lọc theo trạng thái, phân trang
// ============================================================
import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { getAuctions } from "@shared/api/auction";
import { Link } from "react-router-dom";
import LoadingSpinner from "@shared/ui/LoadingSpinner";
import { Gavel, Clock, TrendingUp } from "lucide-react";

const fmtVND = (n: number) =>
  new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND" }).format(n);

const fmtDate = (s: string) =>
  new Date(s).toLocaleString("vi-VN", {
    day: "2-digit", month: "2-digit",
    hour: "2-digit", minute: "2-digit",
  });

const statusLabels: Record<string, { label: string; color: string }> = {
  Scheduled: { label: "Sắp diễn ra", color: "bg-yellow-100 text-yellow-700" },
  Active:    { label: "Đang diễn ra", color: "bg-green-100 text-green-700" },
  Ended:     { label: "Đã kết thúc", color: "bg-gray-100 text-gray-600" },
  Failed:    { label: "Không thành công", color: "bg-red-100 text-red-500" },
};

export default function AuctionListPage() {
  const [status, setStatus] = useState("Active");
  const [page, setPage] = useState(1);

  const { data, isLoading } = useQuery({
    queryKey: ["auctions", status, page],
    queryFn: () => getAuctions({ status, page, pageSize: 12 }),
    placeholderData: (prev) => prev,
  });

  const totalPages = Math.ceil((data?.total ?? 0) / 12);

  return (
    <div className="max-w-7xl mx-auto px-4 py-8">
      <div className="flex items-center gap-3 mb-6">
        <Gavel size={28} className="text-nexus-600" />
        <h1 className="text-2xl font-bold text-gray-800">Đấu giá trực tuyến</h1>
      </div>

      {/* Filter trạng thái */}
      <div className="flex gap-2 mb-6 overflow-x-auto pb-1">
        {(["Active", "Scheduled", "Ended", "Failed"] as const).map((s) => (
          <button
            key={s}
            onClick={() => { setStatus(s); setPage(1); }}
            className={`px-4 py-2 rounded-full text-sm font-medium whitespace-nowrap transition-colors ${
              status === s
                ? "bg-nexus-600 text-white"
                : "bg-gray-100 text-gray-600 hover:bg-gray-200"
            }`}
          >
            {statusLabels[s].label}
          </button>
        ))}
      </div>

      {isLoading ? (
        <LoadingSpinner />
      ) : data?.items.length === 0 ? (
        <div className="text-center py-16 text-gray-400">
          Không có cuộc đấu giá nào trong trạng thái này
        </div>
      ) : (
        <>
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
            {data?.items.map((auction) => {
              const st = statusLabels[auction.status];
              return (
                <Link
                  key={auction.id}
                  to={`/auctions/${auction.id}`}
                  className="bg-white rounded-xl border border-gray-100 shadow-sm p-5 hover:shadow-md transition-shadow block"
                >
                  {/* Header: id sản phẩm + badge trạng thái */}
                  <div className="flex items-start justify-between mb-3 gap-2">
                    <p className="font-semibold text-gray-800 leading-snug line-clamp-2 flex-1">
                      Sản phẩm #{auction.productId.slice(0, 8)}
                    </p>
                    <span className={`shrink-0 text-xs font-medium px-2 py-0.5 rounded-full ${st?.color ?? "bg-gray-100 text-gray-600"}`}>
                      {st?.label ?? auction.status}
                    </span>
                  </div>

                  {/* Giá */}
                  <div className="flex gap-4 mb-3">
                    <div>
                      <p className="text-xs text-gray-400">Giá khởi điểm</p>
                      <p className="font-semibold text-gray-700">{fmtVND(auction.startingPrice)}</p>
                    </div>
                    {auction.currentPrice > auction.startingPrice && (
                      <div>
                        <p className="text-xs text-gray-400 flex items-center gap-1">
                          <TrendingUp size={11} /> Giá cao nhất
                        </p>
                        <p className="font-bold text-nexus-600">{fmtVND(auction.currentPrice)}</p>
                      </div>
                    )}
                  </div>

                  {/* Thời gian */}
                  <div className="text-xs text-gray-400 flex items-center gap-1">
                    <Clock size={11} />
                    {auction.status === "Active"
                      ? `Kết thúc: ${fmtDate(auction.endTime)}`
                      : `Bắt đầu: ${fmtDate(auction.startTime)}`}
                  </div>
                </Link>
              );
            })}
          </div>

          {totalPages > 1 && (
            <div className="flex justify-center gap-2 mt-6">
              <button onClick={() => setPage((p) => Math.max(1, p - 1))} disabled={page === 1}
                className="px-4 py-2 rounded-lg border text-sm disabled:opacity-40 hover:bg-nexus-50">
                ‹ Trước
              </button>
              <span className="px-4 py-2 text-sm">{page} / {totalPages}</span>
              <button onClick={() => setPage((p) => Math.min(totalPages, p + 1))} disabled={page === totalPages}
                className="px-4 py-2 rounded-lg border text-sm disabled:opacity-40 hover:bg-nexus-50">
                Tiếp ›
              </button>
            </div>
          )}
        </>
      )}
    </div>
  );
}
