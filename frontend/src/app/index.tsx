import { BrowserRouter } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { Toaster } from "react-hot-toast";
import { AuthProvider } from "@app/providers/AuthContext";
import { CartProvider } from "@app/providers/CartContext";
import { ToastProvider } from "@app/providers/ToastContext";
import AppRouter from "@app/router";

const queryClient = new QueryClient({
  defaultOptions: {
    queries: { retry: 1, staleTime: 30_000 },
  },
});

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <CartProvider>
          <ToastProvider>
            <BrowserRouter>
              <Toaster
                position="top-right"
                toastOptions={{ duration: 3000, style: { fontSize: "14px" } }}
              />
              <div className="min-h-screen bg-gray-50">
                <AppRouter />
              </div>
            </BrowserRouter>
          </ToastProvider>
        </CartProvider>
      </AuthProvider>
    </QueryClientProvider>
  );
}
