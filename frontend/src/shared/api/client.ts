// ============================================================
// HTTP client dùng chung - tự động gắn JWT token vào header
// và xử lý 401 (hết hạn token -> redirect về login)
// ============================================================
import axios from "axios";

// Dev: VITE_API_BASE_URL="" → relative URL → Vite proxy → Gateway :5000
// Production: VITE_API_BASE_URL="https://api.example.com" → direct đến Gateway
const API_BASE = import.meta.env.VITE_API_BASE_URL ?? ''

export const SERVICE_URLS = {
  user:         API_BASE,
  catalog:      API_BASE,
  commerce:     API_BASE,
  auction:      API_BASE,
  fulfillment:  API_BASE,
  notification: API_BASE,
};

// Tạo một axios instance mới cho mỗi service
function makeClient(baseURL: string) {
  const instance = axios.create({ baseURL });

  // Request interceptor: gắn Bearer token nếu đã đăng nhập
  instance.interceptors.request.use((config) => {
    const token = localStorage.getItem("nexus_token");
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  });

  // Response interceptor: nếu 401 thì xóa token và về trang login
  instance.interceptors.response.use(
    (res) => res,
    (err) => {
      if (err.response?.status === 401) {
        localStorage.removeItem("nexus_token");
        localStorage.removeItem("nexus_user");
        window.location.href = "/login";
      }
      return Promise.reject(err);
    }
  );

  return instance;
}

// Export các client riêng biệt cho từng service
export const userClient         = makeClient(SERVICE_URLS.user);
export const catalogClient      = makeClient(SERVICE_URLS.catalog);
export const commerceClient     = makeClient(SERVICE_URLS.commerce);
export const auctionClient      = makeClient(SERVICE_URLS.auction);
export const fulfillmentClient  = makeClient(SERVICE_URLS.fulfillment);
export const notificationClient = makeClient(SERVICE_URLS.notification);
