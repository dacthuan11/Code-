import { fulfillmentClient } from "./client";

export interface Province { provinceId: number; provinceName: string; }
export interface District { districtId: number; districtName: string; provinceId: number; }
export interface Ward     { wardCode: string;   wardName: string;    districtId: number; }

export interface ShippingFeeResult {
  totalFee: number; serviceFee: number; insuranceFee: number; serviceType: string;
}

export const getProvinces = () =>
  fulfillmentClient.get<Province[]>("/api/v1/shipping/provinces").then(r => r.data);

export const getDistricts = (provinceId: number) =>
  fulfillmentClient.get<District[]>(`/api/v1/shipping/districts?provinceId=${provinceId}`).then(r => r.data);

export const getWards = (districtId: number) =>
  fulfillmentClient.get<Ward[]>(`/api/v1/shipping/wards?districtId=${districtId}`).then(r => r.data);

export const calculateShippingFee = (data: {
  toDistrictId: number; toWardCode: string;
  insuranceValue: number; weight?: number;
}) => fulfillmentClient.post<ShippingFeeResult>("/api/v1/shipping/fee", {
  weight: 500, length: 20, width: 15, height: 10, ...data
}).then(r => r.data);
