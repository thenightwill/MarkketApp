import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_URL } from '../../core/config';
import { AddStockQuantityPayload, CreateStockPayload, Stock, StockFilter, UpdateStockPayload } from './inventory.models';

@Injectable({ providedIn: 'root' })
export class InventoryService {
  private readonly http = inject(HttpClient);
  private readonly url = `${inject(API_URL)}/inventory`;

  list(filter: StockFilter = {}): Observable<Stock[]> {
    let params = new HttpParams();

    if (filter.productId) {
      params = params.set('productId', filter.productId);
    }
    if (filter.lowStock) {
      params = params.set('lowStock', true);
    }
    if (filter.expired) {
      params = params.set('expired', true);
    }

    return this.http.get<Stock[]>(this.url, { params });
  }

  create(payload: CreateStockPayload): Observable<Stock> {
    return this.http.post<Stock>(this.url, payload);
  }

  update(id: string, payload: UpdateStockPayload): Observable<Stock> {
    return this.http.put<Stock>(`${this.url}/${id}`, payload);
  }

  addQuantity(id: string, payload: AddStockQuantityPayload): Observable<Stock> {
    return this.http.post<Stock>(`${this.url}/${id}/add-quantity`, payload);
  }
}
