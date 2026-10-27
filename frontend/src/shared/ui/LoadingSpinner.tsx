// Spinner loading đơn giản dùng ở nhiều nơi
export default function LoadingSpinner({ text = "Đang tải..." }: { text?: string }) {
  return (
    <div className="flex flex-col items-center justify-center py-16 gap-3 text-gray-400">
      <div className="w-10 h-10 border-4 border-nexus-200 border-t-nexus-600 rounded-full animate-spin" />
      <span className="text-sm">{text}</span>
    </div>
  );
}
