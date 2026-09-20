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
  templateUrl: './sales-page.html',
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
