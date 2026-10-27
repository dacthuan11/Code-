// ============================================================
// API calls cho AuctionService - đấu giá, đặt giá
// ============================================================
import { auctionClient } from "./client";

export interface Auction {
  id: string;
  productId: string;
  sellerId: string;
  startingPrice: number;
  bidIncrement: number;
  currentPrice: number;
  currentWinnerId?: string;
  status: "Scheduled" | "Active" | "Ended" | "Failed";
  startTime: string;
  endTime: string;
  totalBids: number;
  createdAt: string;
}

export interface Bid {
  id: string;
  auctionId: string;
  bidderId: string;
  bidderEmail: string;
  amount: number;
  createdAt: string;
  isWinning: boolean;
}

export interface PlaceBidResult {
  bidId: string;
  currentPrice: number;
  newEndTime: string;
}

// Lấy danh sách đấu giá, lọc theo trạng thái
export const getAuctions = (params?: {
  page?: number;
  pageSize?: number;
  status?: string;
}) =>
  auctionClient
    .get<{ items: Auction[]; total: number; page: number; pageSize: number }>(
      "/api/v1/auctions",
      { params }
    )
    .then((r) => r.data);

// Xem chi tiết cuộc đấu giá
export const getAuction = (id: string) =>
  auctionClient.get<Auction>(`/api/v1/auctions/${id}`).then((r) => r.data);

// Tạo cuộc đấu giá mới (SELLER)
export const createAuction = (data: {
  productId: string;
  startingPrice: number;
  bidIncrement: number;
  startTime: string;
  endTime: string;
}) =>
  auctionClient.post<Auction>("/api/v1/auctions", data).then((r) => r.data);

// Đặt giá - core action của buyer
export const placeBid = (auctionId: string, amount: number) =>
  auctionClient
    .post<PlaceBidResult>(`/api/v1/auctions/${auctionId}/bids`, { amount })
    .then((r) => r.data);

// Xem lịch sử các bid của một auction
export const getBids = (auctionId: string) =>
  auctionClient
    .get<Bid[]>(`/api/v1/auctions/${auctionId}/bids`)
    .then((r) => r.data);
