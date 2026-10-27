import { createContext, useCallback, useContext, useState } from "react";

export type ToastType = "success" | "error" | "info" | "warning";

interface Toast {
  id: number;
  message: string;
  type: ToastType;
  title?: string;
}

interface ToastCtx {
  show: (message: string, type?: ToastType, title?: string) => void;
}

const ToastContext = createContext<ToastCtx>({ show: () => {} });

let _id = 0;

export function ToastProvider({ children }: { children: React.ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([]);

  const show = useCallback((message: string, type: ToastType = "info", title?: string) => {
    const id = ++_id;
    setToasts(prev => [...prev, { id, message, type, title }]);
    setTimeout(() => setToasts(prev => prev.filter(t => t.id !== id)), 4000);
  }, []);

  const remove = (id: number) => setToasts(prev => prev.filter(t => t.id !== id));

  const colors: Record<ToastType, { bg: string; icon: string }> = {
    success: { bg: "#16a34a", icon: "✓" },
    error:   { bg: "#dc2626", icon: "✕" },
    warning: { bg: "#d97706", icon: "⚠" },
    info:    { bg: "#2563eb", icon: "ℹ" },
  };

  return (
    <ToastContext.Provider value={{ show }}>
      {children}
      <div style={{ position: "fixed", bottom: 24, right: 24, zIndex: 9999, display: "flex", flexDirection: "column", gap: 10 }}>
        {toasts.map(t => (
          <div
            key={t.id}
            onClick={() => remove(t.id)}
            style={{
              background: colors[t.type].bg,
              color: "#fff",
              padding: "14px 18px",
              borderRadius: 10,
              boxShadow: "0 4px 16px rgba(0,0,0,0.25)",
              minWidth: 280,
              maxWidth: 380,
              cursor: "pointer",
              animation: "toast-in 0.3s ease",
              display: "flex",
              gap: 12,
              alignItems: "flex-start",
            }}
          >
            <span style={{ fontSize: 18, lineHeight: 1.2 }}>{colors[t.type].icon}</span>
            <div>
              {t.title && <div style={{ fontWeight: 700, marginBottom: 2 }}>{t.title}</div>}
              <div style={{ fontSize: 14, opacity: 0.95 }}>{t.message}</div>
            </div>
          </div>
        ))}
      </div>
      <style>{`@keyframes toast-in { from { opacity:0; transform:translateX(50px) } to { opacity:1; transform:translateX(0) } }`}</style>
    </ToastContext.Provider>
  );
}

export const useToast = () => useContext(ToastContext);
