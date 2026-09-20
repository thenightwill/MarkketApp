import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { friendlyMessage } from '../../core/api/api-error';
import { LanguageService } from '../../core/i18n/language.service';
import { Alert } from '../../shared/components/alert';
import { MoneyPipe } from '../../shared/pipes/money.pipe';
import { Product } from '../products/product.models';
import { ProductService } from '../products/product.service';
import { Sale } from './sale.models';
import { SaleService } from './sale.service';

interface CartLine {
  product: Product;
  quantity: number;
}

@Component({
  selector: 'app-sales-page',
  imports: [ReactiveFormsModule, DatePipe, DecimalPipe, MoneyPipe, Alert],
  template: `
    <div class="page-header">
      <div>
        <h1>{{ i18n.t().sales.title }}</h1>
        <p>{{ i18n.t().sales.subtitle }}</p>
      </div>
    </div>

    <app-alert [message]="error()" />

    @if (receipt(); as sale) {
      <section class="card" [attr.aria-label]="i18n.t().sales.receiptAriaLabel">
        <h2>{{ i18n.t().sales.receiptTitle }}</h2>
        <p class="muted">{{ sale.saleDate | date: 'dd/MM/yyyy HH:mm' }} · {{ statusLabel(sale.status) }}</p>
        <div class="table-wrap">
          <table>
            <thead>
              <tr><th>{{ i18n.t().common.product }}</th><th class="right">{{ i18n.t().common.quantity }}</th><th class="right">{{ i18n.t().common.price }}</th><th class="right">{{ i18n.t().sales.colSubtotal }}</th></tr>
            </thead>
            <tbody>
              @for (item of sale.items; track item.productId) {
                <tr>
                  <td>{{ item.productName }}</td>
                  <td class="right">{{ item.quantity | number: '1.0-3' }}</td>
                  <td class="right">{{ item.unitPrice | money }}</td>
                  <td class="right">{{ item.subtotal | money }}</td>
                </tr>
              }
            </tbody>
          </table>
        </div>
        <p class="right"><strong>{{ i18n.t().sales.totalLabel }} {{ sale.total | money }}</strong></p>
      </section>
    }

    <section class="card" [attr.aria-label]="i18n.t().sales.newSaleAriaLabel">
      <h2>{{ i18n.t().sales.newSaleTitle }}</h2>
      <form [formGroup]="form" (ngSubmit)="addToCart()" class="toolbar" novalidate>
        <div class="field" style="min-width: 260px; flex: 1">
          <label for="sale-product">{{ i18n.t().common.product }}</label>
          <select id="sale-product" formControlName="productId">
            <option value="">{{ i18n.t().common.select }}</option>
            @for (product of products(); track product.id) {
              <option [value]="product.id">{{ product.name }} — {{ product.brand }}</option>
            }
          </select>
        </div>
        <div class="field" style="width: 140px">
          <label for="sale-quantity">{{ i18n.t().common.quantity }}</label>
          <input id="sale-quantity" type="number" step="any" formControlName="quantity">
        </div>
        <button type="submit" class="btn" style="align-self: flex-end" [disabled]="form.invalid">{{ i18n.t().sales.addToCart }}</button>
      </form>
      @if (cartError()) { <p class="error" style="color: var(--danger)">{{ cartError() }}</p> }

      @if (cart().length === 0) {
        <div class="empty">{{ i18n.t().sales.emptyCart }}</div>
      } @else {
        <div class="table-wrap">
          <table>
            <thead>
              <tr><th>{{ i18n.t().common.product }}</th><th class="right">{{ i18n.t().common.quantity }}</th><th class="right">{{ i18n.t().sales.colReferencePrice }}</th><th></th></tr>
            </thead>
            <tbody>
              @for (line of cart(); track line.product.id) {
                <tr>
                  <td>{{ line.product.name }}<br><span class="muted">{{ unitLabel(line.product) }}</span></td>
                  <td class="right">{{ line.quantity | number: '1.0-3' }}</td>
                  <td class="right">{{ line.product.salePrice | money }}</td>
                  <td class="right"><button type="button" class="btn small danger" (click)="remove(line.product.id)">{{ i18n.t().sales.remove }}</button></td>
                </tr>
              }
            </tbody>
          </table>
        </div>
        <p class="muted right">{{ i18n.t().sales.estimatedTotalLabel }} {{ estimatedTotal() | money }} {{ i18n.t().sales.estimatedTotalNote }}</p>
        <div class="actions">
          <button type="button" class="btn primary" (click)="submit()" [disabled]="submitting()">
            {{ submitting() ? i18n.t().sales.submitting : i18n.t().sales.submit }}
          </button>
          <button type="button" class="btn" (click)="clear()">{{ i18n.t().sales.clearCart }}</button>
        </div>
      }
    </section>

    <section class="card" [attr.aria-label]="i18n.t().sales.historyAriaLabel">
      <h2>{{ i18n.t().sales.historyTitle }}</h2>
      @if (sales().length === 0) {
        <div class="empty">{{ i18n.t().sales.noSales }}</div>
      } @else {
        <div class="table-wrap">
          <table>
            <thead>
              <tr><th>{{ i18n.t().sales.colDate }}</th><th>{{ i18n.t().sales.colProducts }}</th><th>{{ i18n.t().common.status }}</th><th class="right">{{ i18n.t().common.total }}</th></tr>
            </thead>
            <tbody>
              @for (sale of sales(); track sale.id) {
                <tr>
                  <td class="nowrap">{{ sale.saleDate | date: 'dd/MM/yyyy HH:mm' }}</td>
                  <td>
                    @for (item of sale.items; track item.productId) {
                      <div>{{ item.quantity | number: '1.0-3' }} × {{ item.productName }} <span class="muted">@ {{ item.unitPrice | money }}</span></div>
                    }
                  </td>
                  <td><span class="badge" [class.success]="sale.status === 'Completed'" [class.warning]="sale.status !== 'Completed'">{{ statusLabel(sale.status) }}</span></td>
                  <td class="right">{{ sale.total | money }}</td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </section>
  `,
})
export class SalesPage implements OnInit {
  private readonly saleService = inject(SaleService);
  private readonly productService = inject(ProductService);

  protected readonly i18n = inject(LanguageService);
  protected readonly products = signal<Product[]>([]);
  protected readonly sales = signal<Sale[]>([]);
  protected readonly cart = signal<CartLine[]>([]);
  protected readonly receipt = signal<Sale | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly cartError = signal<string | null>(null);
  protected readonly submitting = signal(false);

  protected readonly estimatedTotal = computed(() =>
    this.cart().reduce((sum, line) => sum + line.quantity * line.product.salePrice, 0),
  );

  protected readonly form = inject(NonNullableFormBuilder).group({
    productId: ['', Validators.required],
    quantity: [1, [Validators.required, Validators.min(0.001)]],
  });

  ngOnInit(): void {
    this.productService.list(false).subscribe({
      next: (items) => this.products.set(items),
      error: (err: unknown) => this.error.set(friendlyMessage(err, this.i18n.t().errors)),
    });
    this.loadSales();
  }

  protected statusLabel(status: string): string {
    return status === 'Completed' ? this.i18n.t().sales.statusCompleted : this.i18n.t().sales.statusIncomplete;
  }

  protected unitLabel(product: Product): string {
    return `${product.unitValue} · ${this.i18n.t().products.unitTypes[product.unitType]}`;
  }

  protected addToCart(): void {
    const { productId, quantity } = this.form.getRawValue();
    const product = this.products().find((p) => p.id === productId);

    if (!product || quantity <= 0) {
      return;
    }

    if (product.unitType === 'Unit' && !Number.isInteger(quantity)) {
      this.cartError.set(this.i18n.t().sales.fractionalError);
      return;
    }

    this.cartError.set(null);
    this.cart.update((lines) => {
      const existing = lines.find((l) => l.product.id === productId);
      return existing
        ? lines.map((l) => (l === existing ? { ...l, quantity: l.quantity + quantity } : l))
        : [...lines, { product, quantity }];
    });
    this.form.reset({ productId: '', quantity: 1 });
  }

  protected remove(productId: string): void {
    this.cart.update((lines) => lines.filter((l) => l.product.id !== productId));
  }

  protected clear(): void {
    this.cart.set([]);
    this.cartError.set(null);
  }

  protected submit(): void {
    const items = this.cart().map((l) => ({ productId: l.product.id, quantity: l.quantity }));

    if (items.length === 0) {
      return;
    }

    this.submitting.set(true);
    this.error.set(null);
    this.receipt.set(null);

    this.saleService.create({ items }).subscribe({
      next: (sale) => {
        this.submitting.set(false);
        this.receipt.set(sale);
        this.cart.set([]);
        this.loadSales();
      },
      error: (err: unknown) => {
        this.submitting.set(false);
        this.error.set(friendlyMessage(err, this.i18n.t().errors));
      },
    });
  }

  private loadSales(): void {
    this.saleService.list().subscribe({
      next: (items) => this.sales.set(items),
      error: (err: unknown) => this.error.set(friendlyMessage(err, this.i18n.t().errors)),
    });
  }
}
