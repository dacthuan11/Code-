import { Routes, Route, Navigate } from "react-router-dom";
import Navbar from "@widgets/navbar";
import ProtectedRoute from "@shared/ui/ProtectedRoute";
import HomePage from "@pages/home/page";
import LoginPage from "@pages/auth/login.page";
import RegisterPage from "@pages/auth/register.page";
import ProductDetailPage from "@pages/product/product-detail.page";
import CartPage from "@pages/cart/cart.page";
import CheckoutPage from "@pages/checkout/checkout.page";
import OrdersPage from "@pages/orders/orders.page";
import OrderDetailPage from "@pages/orders/order-detail.page";
import PaymentPage from "@pages/payment/payment.page";
import AuctionListPage from "@pages/auctions/auction-list.page";
import AuctionDetailPage from "@pages/auctions/auction-detail.page";
import NotificationsPage from "@pages/notifications/notifications.page";
import AdminPage from "@pages/admin/admin.page";
import SellerDashboard from "@pages/seller/seller-dashboard.page";
import VerifyEmailPage from "@pages/verify-email/verify-email.page";

export default function AppRouter() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />
      <Route path="/verify-email" element={<VerifyEmailPage />} />
      <Route
        path="/*"
        element={
          <>
            <Navbar />
            <main>
              <Routes>
                <Route path="/" element={<HomePage />} />
                <Route path="/products/:id" element={<ProductDetailPage />} />
                <Route path="/auctions" element={<AuctionListPage />} />
                <Route path="/auctions/:id" element={<AuctionDetailPage />} />
                <Route path="/cart" element={<CartPage />} />
                <Route path="/checkout" element={<ProtectedRoute><CheckoutPage /></ProtectedRoute>} />
                <Route path="/orders" element={<ProtectedRoute><OrdersPage /></ProtectedRoute>} />
                <Route path="/orders/:id" element={<ProtectedRoute><OrderDetailPage /></ProtectedRoute>} />
                <Route path="/payment" element={<ProtectedRoute><PaymentPage /></ProtectedRoute>} />
                <Route path="/notifications" element={<ProtectedRoute><NotificationsPage /></ProtectedRoute>} />
                <Route path="/seller/dashboard" element={<ProtectedRoute><SellerDashboard /></ProtectedRoute>} />
                <Route path="/admin" element={<ProtectedRoute><AdminPage /></ProtectedRoute>} />
                <Route path="*" element={<Navigate to="/" replace />} />
              </Routes>
            </main>
          </>
        }
      />
    </Routes>
  );
}
