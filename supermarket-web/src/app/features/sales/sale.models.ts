export interface SaleItem {
  productId: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  subtotal: number;
}

export interface Sale {
  id: string;
  userId: string;
  saleDate: string;
  total: number;
  status: string;
  items: SaleItem[];
}

export interface CreateSalePayload {
  items: { productId: string; quantity: number }[];
}
