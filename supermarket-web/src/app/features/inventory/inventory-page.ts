import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { friendlyMessage } from '../../core/api/api-error';
import { AuthService } from '../../core/auth/auth.service';
import { LanguageService } from '../../core/i18n/language.service';
import { Alert } from '../../shared/components/alert';
import { Product } from '../products/product.models';
import { ProductService } from '../products/product.service';
import { Stock } from './inventory.models';
import { InventoryService } from './inventory.service';

const MIN_ADD_QUANTITY = 0.001;

function toIsoDate(value: string): string {
  return new Date(`${value}T00:00:00Z`).toISOString();
}

function toInputDate(value: string): string {
  return value.slice(0, 10);
}

@Component({
  selector: 'app-inventory-page',
  imports: [ReactiveFormsModule, DatePipe, DecimalPipe, Alert],
  templateUrl: './inventory-page.html',
})
export class InventoryPage implements OnInit {
  private readonly service = inject(InventoryService);
  private readonly productService = inject(ProductService);
  private readonly auth = inject(AuthService);

  protected readonly i18n = inject(LanguageService);
  protected readonly isAdmin = this.auth.isAdministrator;
  protected readonly stocks = signal<Stock[]>([]);
  protected readonly products = signal<Product[]>([]);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly notice = signal<string | null>(null);
  protected readonly formOpen = signal(false);
  protected readonly editing = signal<Stock | null>(null);

  private productFilter = '';
  private lowStock = false;
  private expired = false;

  protected readonly form = inject(NonNullableFormBuilder).group({
    productId: ['', Validators.required],
    batchNumber: ['', [Validators.required, Validators.maxLength(50)]],
    location: ['', [Validators.required, Validators.maxLength(50)]],
    quantity: [1, [Validators.required, Validators.min(0.001)]],
    minimumStock: [0, [Validators.required, Validators.min(0)]],
    receivedDate: ['', Validators.required],
    expirationDate: ['', Validators.required],
    isActive: [true],
  });

  ngOnInit(): void {
    this.productService.list(false).subscribe({
      next: (items) => this.products.set(items),
      error: (err: unknown) => this.error.set(friendlyMessage(err, this.i18n.t().errors)),
    });
    this.load();
  }

  protected invalid(name: keyof typeof this.form.controls): boolean {
    const control = this.form.controls[name];
    return control.touched && control.invalid;
  }

  protected datesInvalid(): boolean {
    const { receivedDate, expirationDate } = this.form.getRawValue();
    return !!receivedDate && !!expirationDate && expirationDate <= receivedDate;
  }

  protected setProductFilter(value: string): void {
    this.productFilter = value;
    this.load();
  }

  protected setLowStock(value: boolean): void {
    this.lowStock = value;
    this.load();
  }

  protected setExpired(value: boolean): void {
    this.expired = value;
    this.load();
  }

  protected openCreate(): void {
    const today = new Date().toISOString().slice(0, 10);
    this.editing.set(null);
    this.form.reset({
      productId: '',
      batchNumber: '',
      location: '',
      quantity: 1,
      minimumStock: 0,
      receivedDate: today,
      expirationDate: '',
      isActive: true,
    });
    this.form.controls.productId.enable();
    this.form.controls.batchNumber.enable();
    this.form.controls.quantity.enable();
    this.form.controls.receivedDate.enable();
    this.formOpen.set(true);
    this.notice.set(null);
  }

  protected openEdit(stock: Stock): void {
    this.editing.set(stock);
    this.form.reset({
      productId: stock.productId,
      batchNumber: stock.batchNumber,
      location: stock.location,
      quantity: stock.quantity,
      minimumStock: stock.minimumStock,
      receivedDate: toInputDate(stock.receivedDate),
      expirationDate: toInputDate(stock.expirationDate),
      isActive: stock.isActive,
    });
    this.form.controls.productId.disable();
    this.form.controls.batchNumber.disable();
    this.form.controls.quantity.disable();
    this.form.controls.receivedDate.disable();
    this.formOpen.set(true);
    this.notice.set(null);
  }

  protected closeForm(): void {
    this.formOpen.set(false);
    this.editing.set(null);
  }

  protected save(): void {
    if (this.form.invalid || this.datesInvalid()) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const current = this.editing();
    const request = current
      ? this.service.update(current.id, {
          location: value.location,
          minimumStock: value.minimumStock,
          expirationDate: toIsoDate(value.expirationDate),
          isActive: value.isActive,
        })
      : this.service.create({
          productId: value.productId,
          batchNumber: value.batchNumber,
          location: value.location,
          quantity: value.quantity,
          minimumStock: value.minimumStock,
          receivedDate: toIsoDate(value.receivedDate),
          expirationDate: toIsoDate(value.expirationDate),
        });

    this.saving.set(true);
    this.error.set(null);

    request.subscribe({
      next: () => {
        this.saving.set(false);
        this.notice.set(current ? this.i18n.t().inventory.updated : this.i18n.t().inventory.registered);
        this.closeForm();
        this.load();
      },
      error: (err: unknown) => {
        this.saving.set(false);
        this.error.set(friendlyMessage(err, this.i18n.t().errors));
      },
    });
  }

  protected addQuantity(stock: Stock, quantity: number): void {
    if (!Number.isFinite(quantity) || quantity < MIN_ADD_QUANTITY) {
      this.error.set(this.i18n.t().inventory.quantityRequiredError);
      return;
    }

    this.error.set(null);
    this.service.addQuantity(stock.id, { quantity }).subscribe({
      next: () => {
        this.notice.set(this.i18n.t().inventory.quantityAdded(quantity, stock.batchNumber));
        this.load();
      },
      error: (err: unknown) => this.error.set(friendlyMessage(err, this.i18n.t().errors)),
    });
  }

  private load(): void {
    this.loading.set(true);
    this.service
      .list({
        productId: this.productFilter || undefined,
        lowStock: this.lowStock,
        expired: this.expired,
      })
      .subscribe({
        next: (items) => {
          this.stocks.set(items);
          this.loading.set(false);
        },
        error: (err: unknown) => {
          this.error.set(friendlyMessage(err, this.i18n.t().errors));
          this.loading.set(false);
        },
      });
  }
}
