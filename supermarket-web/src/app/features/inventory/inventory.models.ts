export interface Stock {
  id: string;
  productId: string;
  productName: string;
  batchNumber: string;
  location: string;
  quantity: number;
  minimumStock: number;
  receivedDate: string;
  expirationDate: string;
  isActive: boolean;
  isLowStock: boolean;
  isExpired: boolean;
  createdAt: string;
}

export interface StockFilter {
  productId?: string;
  lowStock?: boolean;
  expired?: boolean;
}

export interface CreateStockPayload {
  productId: string;
  batchNumber: string;
  location: string;
  quantity: number;
  minimumStock: number;
  receivedDate: string;
  expirationDate: string;
}

export interface UpdateStockPayload {
  location: string;
  minimumStock: number;
  expirationDate: string;
  isActive: boolean;
}

export interface AddStockQuantityPayload {
  quantity: number;
}
