import { userClient } from "./client";

export interface Review {
  id: string;
  reviewerId: string;
  targetUserId: string;
  orderId: string | null;
  rating: number;
  comment: string;
  createdAt: string;
}

export interface ReviewsResponse {
  items: Review[];
  total: number;
  page: number;
  pageSize: number;
}

export const getReviews = (targetUserId: string, page = 1, pageSize = 10) =>
  userClient
    .get<ReviewsResponse>(`/api/v1/users/${targetUserId}/reviews`, {
      params: { page, pageSize },
    })
    .then((r) => r.data);

export const createReview = (
  targetUserId: string,
  data: { rating: number; comment: string; orderId?: string }
) =>
  userClient
    .post<{ id: string }>(`/api/v1/users/${targetUserId}/reviews`, data)
    .then((r) => r.data);
