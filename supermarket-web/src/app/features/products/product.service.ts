import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_URL } from '../../core/config';
import { Product, ProductPayload, UpdateProductPayload } from './product.models';

@Injectable({ providedIn: 'root' })
export class ProductService {
  private readonly http = inject(HttpClient);
  private readonly url = `${inject(API_URL)}/products`;

  list(includeInactive = false): Observable<Product[]> {
    const params = new HttpParams().set('includeInactive', includeInactive);
    return this.http.get<Product[]>(this.url, { params });
  }

  create(payload: ProductPayload): Observable<Product> {
    return this.http.post<Product>(this.url, payload);
  }

  update(id: string, payload: UpdateProductPayload): Observable<Product> {
    return this.http.put<Product>(`${this.url}/${id}`, payload);
  }

  deactivate(id: string): Observable<void> {
    return this.http.delete<void>(`${this.url}/${id}`);
  }
}
