import { useState, useEffect } from "react";
import { useQuery } from "@tanstack/react-query";
import { getCart, checkout, type DeliveryType } from "@shared/api/commerce";
import { useNavigate } from "react-router-dom";
import LoadingSpinner from "@shared/ui/LoadingSpinner";
import toast from "react-hot-toast";
import { useToast } from "@app/providers/ToastContext";
import {
  MapPin, CreditCard, Package, Truck, ChevronDown,
  Zap, Globe, User, Phone, ShoppingBag,
} from "lucide-react";

const fmtVND = (n: number) =>
  new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND" }).format(n);

// ── Vietnam address API ───────────────────────────────────────────────────
interface VnProvince { code: number; name: string; }
interface VnDistrict { code: number; name: string; }
interface VnWard     { code: string; name: string; }

const VN_API = "https://provinces.open-api.vn/api";
const fetchProvinces = (): Promise<VnProvince[]> =>
  fetch(`${VN_API}/p/?depth=1`).then(r => r.json());
const fetchDistricts = (code: number): Promise<VnDistrict[]> =>
  fetch(`${VN_API}/p/${code}?depth=2`).then(r => r.json()).then(d => d.districts ?? []);
const fetchWards = (code: number): Promise<VnWard[]> =>
  fetch(`${VN_API}/d/${code}?depth=2`).then(r => r.json())
    .then(d => (d.wards ?? []).map((w: any) => ({ code: String(w.code), name: w.name })));

// ── Flat shipping fee ─────────────────────────────────────────────────────
const FLAT_FEE: Record<DeliveryType, number> = {
  Standard: 30_000,
  Express:  60_000,
  PreOrder: 0,
};

// ── Custom Select ─────────────────────────────────────────────────────────
const Select = ({
  value, onChange, disabled, placeholder, children,
}: {
  value: string; onChange: (v: string) => void;
  disabled?: boolean; placeholder: string; children: React.ReactNode;
}) => (
  <div className="relative">
    <select
      value={value} onChange={e => onChange(e.target.value)} disabled={disabled}
      className="w-full appearance-none border border-gray-200 rounded-xl px-4 py-2.5 text-sm
                 outline-none focus:ring-2 focus:ring-nexus-500 focus:border-transparent
                 disabled:bg-gray-50 disabled:text-gray-400 disabled:cursor-not-allowed bg-white pr-10 transition"
    >
      <option value="">{placeholder}</option>
      {children}
    </select>
    <ChevronDown size={15} className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 pointer-events-none" />
  </div>
);

// ── Delivery type options ─────────────────────────────────────────────────
const DELIVERY_TYPES: {
  id: DeliveryType; label: string; sub: string;
  icon: React.ReactNode; badge?: string; serviceTypeId: number;
  maxKm?: number;
}[] = [
  {
    id: "Standard", label: "Giao hàng tiêu chuẩn", serviceTypeId: 2,
    sub: "3–5 ngày nội tỉnh | 5–7 ngày liên tỉnh",
    icon: <Truck size={18} className="text-nexus-500" />,
  },
  {
    id: "Express", label: "Hỏa tốc (trong ngày)", serviceTypeId: 1, maxKm: 20,
    sub: "Giao trong 4 giờ — chỉ áp dụng trong bán kính 20km từ kho",
    icon: <Zap size={18} className="text-amber-500" />,
    badge: "Nhanh nhất",
  },
  {
    id: "PreOrder", label: "Hàng đặt trước / nhập khẩu", serviceTypeId: 2,
    sub: "7–21 ngày — hàng nước ngoài / cần đặt trước",
    icon: <Globe size={18} className="text-blue-500" />,
  },
];

// ═══════════════════════════════════════════════════════════════════════════
export default function CheckoutPage() {
  const navigate = useNavigate();
  const { show } = useToast();

  const [recipientName, setRecipientName] = useState("");
  const [phone, setPhone]                 = useState("");
  const [streetAddress, setStreetAddress] = useState("");

  const [provinces, setProvinces] = useState<VnProvince[]>([]);
  const [districts, setDistricts] = useState<VnDistrict[]>([]);
  const [wards, setWards]         = useState<VnWard[]>([]);

  const [selProvince, setSelProvince] = useState("");
  const [selDistrict, setSelDistrict] = useState("");
  const [selWard, setSelWard]         = useState("");

  const [deliveryType, setDeliveryType]   = useState<DeliveryType>("Standard");
  const [paymentMethod, setPaymentMethod] = useState<"COD" | "VNPAY">("COD");
  const [placing, setPlacing]             = useState(false);

  const provinceName = provinces.find(p => String(p.code) === selProvince)?.name ?? "";
  const districtName = districts.find(d => String(d.code) === selDistrict)?.name ?? "";
  const wardName     = wards.find(w => w.code === selWard)?.name ?? "";

  const shippingFee = FLAT_FEE[deliveryType];
  const { data: cart, isLoading } = useQuery({ queryKey: ["cart"], queryFn: getCart });

  useEffect(() => { fetchProvinces().then(setProvinces).catch(() => {}); }, []);

  const handleProvinceChange = async (val: string) => {
    setSelProvince(val); setSelDistrict(""); setSelWard("");
    setDistricts([]); setWards([]);
    if (!val) return;
    setDistricts(await fetchDistricts(Number(val)).catch(() => []));
  };

  const handleDistrictChange = async (val: string) => {
    setSelDistrict(val); setSelWard(""); setWards([]);
    if (!val) return;
    setWards(await fetchWards(Number(val)).catch(() => []));
  };

  if (isLoading) return <LoadingSpinner />;
  if (!cart?.items?.length) { navigate("/cart"); return null; }

  const totalWithShipping = cart.totalAmount + shippingFee;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!recipientName.trim() || !phone.trim() || !streetAddress.trim()) {
      toast.error("Vui lòng nhập đầy đủ họ tên, SĐT và địa chỉ"); return;
    }
    if (!selProvince || !selDistrict || !selWard) {
      toast.error("Vui lòng chọn đầy đủ Tỉnh / Quận / Phường"); return;
    }
    const opt = DELIVERY_TYPES.find(d => d.id === deliveryType)!;
    setPlacing(true);
    try {
      const result = await checkout({
        recipientName, phone, address: streetAddress,
        province: provinceName, district: districtName, ward: wardName,
        toDistrictId: 0, toWardCode: selWard,
        shippingFee, paymentMethod,
        deliveryType, serviceTypeId: opt.serviceTypeId,
        isInternational: deliveryType === "PreOrder",
      });
      if (result.paymentUrl && paymentMethod !== "COD") {
        navigate(`/payment?orderId=${result.orderId}`);
      } else {
        show(`Đơn hàng đặt thành công! ${result.deliveryTypeLabel ?? opt.label}.`, "success", "Đặt hàng thành công 🎉");
        navigate("/orders");
      }
    } catch (err: any) {
      toast.error(err?.response?.data?.message ?? "Đặt hàng thất bại, thử lại nhé");
    } finally { setPlacing(false); }
  };

  return (
    <div className="max-w-6xl mx-auto px-4 py-8">
      <h1 className="text-2xl font-bold text-gray-800 mb-6 flex items-center gap-2">
        <ShoppingBag size={24} className="text-nexus-600" /> Xác nhận đơn hàng
      </h1>

      <form onSubmit={handleSubmit}>
        <div className="grid lg:grid-cols-[1fr_380px] gap-6">

          {/* ─── LEFT ─────────────────────────────────────────────────── */}
          <div className="flex flex-col gap-5">

            {/* Địa chỉ */}
            <div className="bg-white rounded-2xl border border-gray-100 p-6 shadow-sm">
              <h2 className="font-semibold text-gray-800 mb-5 flex items-center gap-2">
                <MapPin size={18} className="text-nexus-600" /> Địa chỉ giao hàng
              </h2>

              <div className="grid sm:grid-cols-2 gap-4 mb-4">
                <div>
                  <label className="text-xs font-medium text-gray-500 mb-1.5 flex items-center gap-1">
                    <User size={12} /> Họ và tên *
                  </label>
                  <input value={recipientName} onChange={e => setRecipientName(e.target.value)}
                    placeholder="Nguyễn Văn A"
                    className="w-full border border-gray-200 rounded-xl px-4 py-2.5 text-sm outline-none focus:ring-2 focus:ring-nexus-500 transition" />
                </div>
                <div>
                  <label className="text-xs font-medium text-gray-500 mb-1.5 flex items-center gap-1">
                    <Phone size={12} /> Số điện thoại *
                  </label>
                  <input value={phone} onChange={e => setPhone(e.target.value)}
                    placeholder="0901 234 567" type="tel"
                    className="w-full border border-gray-200 rounded-xl px-4 py-2.5 text-sm outline-none focus:ring-2 focus:ring-nexus-500 transition" />
                </div>
              </div>

              <div className="grid grid-cols-3 gap-3 mb-4">
                <div>
                  <label className="text-xs font-medium text-gray-500 mb-1.5 block">Tỉnh / Thành phố *</label>
                  <Select value={selProvince} onChange={handleProvinceChange} placeholder="Chọn tỉnh">
                    {provinces.map(p => <option key={p.code} value={String(p.code)}>{p.name}</option>)}
                  </Select>
                </div>
                <div>
                  <label className="text-xs font-medium text-gray-500 mb-1.5 block">Quận / Huyện *</label>
                  <Select value={selDistrict} onChange={handleDistrictChange} disabled={!selProvince} placeholder="Chọn quận">
                    {districts.map(d => <option key={d.code} value={String(d.code)}>{d.name}</option>)}
                  </Select>
                </div>
                <div>
                  <label className="text-xs font-medium text-gray-500 mb-1.5 block">Phường / Xã *</label>
                  <Select value={selWard} onChange={setSelWard} disabled={!selDistrict} placeholder="Chọn phường">
                    {wards.map(w => <option key={w.code} value={w.code}>{w.name}</option>)}
                  </Select>
                </div>
              </div>

              <div className="mb-5">
                <label className="text-xs font-medium text-gray-500 mb-1.5 block">Số nhà, tên đường *</label>
                <input value={streetAddress} onChange={e => setStreetAddress(e.target.value)}
                  placeholder="Ví dụ: 50 Lê Văn Việt, Phường Hiệp Phú"
                  className="w-full border border-gray-200 rounded-xl px-4 py-2.5 text-sm outline-none focus:ring-2 focus:ring-nexus-500 transition" />
              </div>

            </div>

            {/* Vận chuyển */}
            <div className="bg-white rounded-2xl border border-gray-100 p-6 shadow-sm">
              <h2 className="font-semibold text-gray-800 mb-4 flex items-center gap-2">
                <Truck size={18} className="text-nexus-600" /> Phương thức vận chuyển
              </h2>
              <div className="flex flex-col gap-2">
                {DELIVERY_TYPES.map(opt => {
                  const selected = deliveryType === opt.id;
                  return (
                    <label
                      key={opt.id}
                      className={`flex items-center gap-3 p-4 rounded-xl border-2 cursor-pointer transition
                        ${selected ? "border-nexus-500 bg-nexus-50" : "border-gray-100 hover:border-gray-200 bg-white"}`}
                    >
                      <input
                        type="radio" name="delivery" value={opt.id}
                        checked={selected}
                        onChange={() => setDeliveryType(opt.id)}
                        className="accent-nexus-600 w-4 h-4"
                      />
                      <div className="flex-1">
                        <div className="flex items-center gap-2 flex-wrap">
                          {opt.icon}
                          <span className="text-sm font-semibold text-gray-800">{opt.label}</span>
                          {opt.badge && (
                            <span className="text-xs bg-amber-100 text-amber-700 px-2 py-0.5 rounded-full font-medium">{opt.badge}</span>
                          )}
                        </div>
                        <div className="text-xs text-gray-500 mt-0.5 ml-6">{opt.sub}</div>
                      </div>
                      <span className="text-sm font-bold shrink-0 text-nexus-700">
                        {FLAT_FEE[opt.id] === 0 ? "Thoả thuận" : fmtVND(FLAT_FEE[opt.id])}
                      </span>
                    </label>
                  );
                })}
              </div>
            </div>

            {/* Thanh toán */}
            <div className="bg-white rounded-2xl border border-gray-100 p-6 shadow-sm">
              <h2 className="font-semibold text-gray-800 mb-4 flex items-center gap-2">
                <CreditCard size={18} className="text-nexus-600" /> Phương thức thanh toán
              </h2>
              <div className="flex flex-col gap-2">
                {([
                  { id: "COD",   label: "Thanh toán khi nhận hàng (COD)", icon: "💵", sub: "Trả tiền mặt hoặc thẻ cho shipper" },
                  { id: "VNPAY", label: "VNPay / QR Code", icon: "🏦", sub: "Sắp ra mắt", disabled: true },
                ] as const).map(opt => (
                  <label
                    key={opt.id}
                    className={`flex items-center gap-3 p-4 rounded-xl border-2 cursor-pointer transition
                      ${paymentMethod === opt.id ? "border-nexus-500 bg-nexus-50" : "border-gray-100 hover:border-gray-200"}
                      ${"disabled" in opt && opt.disabled ? "opacity-40 cursor-not-allowed" : ""}`}
                  >
                    <input type="radio" name="payment" value={opt.id}
                      checked={paymentMethod === opt.id}
                      disabled={"disabled" in opt && opt.disabled}
                      onChange={() => !("disabled" in opt && opt.disabled) && setPaymentMethod(opt.id)}
                      className="accent-nexus-600 w-4 h-4" />
                    <span className="text-xl">{opt.icon}</span>
                    <div>
                      <div className="text-sm font-semibold text-gray-800">{opt.label}</div>
                      <div className="text-xs text-gray-500">{opt.sub}</div>
                    </div>
                  </label>
                ))}
              </div>
            </div>
          </div>

          {/* ─── RIGHT ────────────────────────────────────────────────── */}
          <div className="flex flex-col gap-5">
            <div className="bg-white rounded-2xl border border-gray-100 p-6 shadow-sm sticky top-20">
              <h2 className="font-semibold text-gray-800 mb-4 flex items-center gap-2">
                <Package size={18} className="text-nexus-600" /> Sản phẩm ({cart.items.length})
              </h2>

              <div className="flex flex-col gap-3 max-h-64 overflow-y-auto pr-1">
                {cart.items.map(item => (
                  <div key={item.productId} className="flex items-center gap-3">
                    <div className="w-14 h-14 rounded-xl bg-gray-50 border border-gray-100 overflow-hidden shrink-0">
                      {item.imageUrl
                        ? <img src={item.imageUrl} alt={item.productName} className="w-full h-full object-cover"
                               onError={e => { (e.target as HTMLImageElement).style.display = "none"; }} />
                        : <div className="w-full h-full flex items-center justify-center text-xl">📦</div>}
                    </div>
                    <div className="flex-1 min-w-0">
                      <p className="text-sm text-gray-800 font-medium truncate">{item.productName}</p>
                      <p className="text-xs text-gray-400">× {item.quantity}</p>
                    </div>
                    <span className="text-sm font-semibold text-gray-800 shrink-0">{fmtVND(item.subtotal)}</span>
                  </div>
                ))}
              </div>

              {/* Kho info */}
              <div className="mt-4 bg-blue-50 rounded-xl px-3 py-2 flex items-start gap-2">
                <MapPin size={13} className="text-blue-500 mt-0.5 shrink-0" />
                <p className="text-xs text-blue-700">Nexus Marketplace</p>
              </div>

              <div className="border-t border-gray-100 mt-4 pt-4 flex flex-col gap-2.5">
                <div className="flex justify-between text-sm text-gray-600">
                  <span>Tạm tính</span>
                  <span>{fmtVND(cart.totalAmount)}</span>
                </div>
                <div className="flex justify-between text-sm text-gray-600">
                  <span className="flex items-center gap-1"><Truck size={13} />
                    {DELIVERY_TYPES.find(d => d.id === deliveryType)?.label}</span>
                  <span>{shippingFee === 0 ? "Thoả thuận" : fmtVND(shippingFee)}</span>
                </div>
                {selWard && (
                  <div className="text-xs text-gray-400 bg-gray-50 rounded-lg p-2">
                    📦 Dự kiến:{" "}
                    <span className="font-medium text-nexus-600">
                      {deliveryType === "Express"  ? "Trong ngày" :
                       deliveryType === "PreOrder" ? "7–21 ngày" :
                       provinceName.includes("Hồ Chí Minh") || provinceName.includes("Hà Nội")
                         ? "3–4 ngày làm việc" : "5–7 ngày làm việc"}
                    </span>
                  </div>
                )}
                <div className="flex justify-between font-bold text-nexus-700 text-lg pt-2 border-t border-gray-100">
                  <span>Tổng thanh toán</span>
                  <span>{fmtVND(totalWithShipping)}</span>
                </div>
              </div>

              <button type="submit" disabled={placing}
                className="mt-5 w-full bg-nexus-600 hover:bg-nexus-700 disabled:bg-nexus-300
                           text-white font-bold py-4 rounded-2xl transition-colors text-base
                           shadow-lg shadow-nexus-200 flex items-center justify-center gap-2">
                {placing ? (
                  <><div className="w-5 h-5 border-2 border-white border-t-transparent rounded-full animate-spin" />Đang đặt hàng...</>
                ) : (
                  <><ShoppingBag size={18} />Đặt hàng — {fmtVND(totalWithShipping)}</>
                )}
              </button>
              <p className="text-xs text-gray-400 text-center mt-3">
                Bằng cách đặt hàng, bạn đồng ý với{" "}
                <span className="text-nexus-600 cursor-pointer hover:underline">Điều khoản dịch vụ</span> của Nexus
              </p>
            </div>
          </div>
        </div>
      </form>
    </div>
  );
}
