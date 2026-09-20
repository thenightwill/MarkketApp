export type UnitType = 'Unit' | 'Weight' | 'Volume';

export const UNIT_TYPES: UnitType[] = ['Unit', 'Weight', 'Volume'];

export interface Product {
  id: string;
  name: string;
  brand: string;
  category: string;
  unitType: UnitType;
  unitValue: number;
  cost: number | null;
  salePrice: number;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export interface ProductPayload {
  name: string;
  brand: string;
  category: string;
  unitType: UnitType;
  unitValue: number;
  cost: number;
  salePrice: number;
}

export interface UpdateProductPayload extends ProductPayload {
  isActive: boolean;
}
