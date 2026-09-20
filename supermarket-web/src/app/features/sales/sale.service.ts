import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_URL } from '../../core/config';
import { CreateSalePayload, Sale } from './sale.models';

@Injectable({ providedIn: 'root' })
export class SaleService {
  private readonly http = inject(HttpClient);
  private readonly url = `${inject(API_URL)}/sales`;

  list(): Observable<Sale[]> {
    return this.http.get<Sale[]>(this.url);
  }

  create(payload: CreateSalePayload): Observable<Sale> {
    return this.http.post<Sale>(this.url, payload);
  }
}
