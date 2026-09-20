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
  template: `
    <div class="page-header">
      <div>
        <h1>{{ i18n.t().inventory.title }}</h1>
        <p>{{ i18n.t().inventory.subtitle }}</p>
      </div>
      @if (isAdmin()) {
        <button type="button" class="btn primary" (click)="openCreate()">{{ i18n.t().inventory.newBatch }}</button>
      }
    </div>

    <app-alert [message]="notice()" kind="success" />
    <app-alert [message]="error()" />

    @if (formOpen()) {
      <section class="card" [attr.aria-label]="i18n.t().inventory.formAriaLabel">
        <h2>{{ editing() ? i18n.t().inventory.editBatch(editing()!.batchNumber) : i18n.t().inventory.newBatchTitle }}</h2>
        <form [formGroup]="form" (ngSubmit)="save()" novalidate>
          <div class="grid">
            <div class="field">
              <label for="s-product">{{ i18n.t().common.product }}</label>
              <select id="s-product" formControlName="productId" [class.invalid]="invalid('productId')">
                <option value="">{{ i18n.t().common.select }}</option>
                @for (product of products(); track product.id) {
                  <option [value]="product.id">{{ product.name }} — {{ product.brand }}</option>
                }
              </select>
              @if (invalid('productId')) { <span class="error">{{ i18n.t().inventory.productRequired }}</span> }
            </div>
            <div class="field">
              <label for="s-batch">{{ i18n.t().inventory.batchNumber }}</label>
              <input id="s-batch" formControlName="batchNumber" [class.invalid]="invalid('batchNumber')">
              @if (invalid('batchNumber')) { <span class="error">{{ i18n.t().inventory.batchNumberRequired }}</span> }
            </div>
            <div class="field">
              <label for="s-location">{{ i18n.t().inventory.location }}</label>
              <input id="s-location" formControlName="location" [class.invalid]="invalid('location')">
              @if (invalid('location')) { <span class="error">{{ i18n.t().inventory.locationRequired }}</span> }
            </div>
            <div class="field">
              <label for="s-quantity">{{ i18n.t().common.quantity }}</label>
              <input id="s-quantity" type="number" step="any" formControlName="quantity" [class.invalid]="invalid('quantity')">
              @if (editing()) {
                <span class="hint">{{ i18n.t().inventory.quantityHint }}</span>
              } @else if (invalid('quantity')) {
                <span class="error">{{ i18n.t().inventory.quantityError }}</span>
              }
            </div>
            <div class="field">
              <label for="s-min">{{ i18n.t().inventory.minimumStock }}</label>
              <input id="s-min" type="number" step="any" formControlName="minimumStock" [class.invalid]="invalid('minimumStock')">
              @if (invalid('minimumStock')) { <span class="error">{{ i18n.t().inventory.minimumStockError }}</span> }
            </div>
            <div class="field">
              <label for="s-received">{{ i18n.t().inventory.receivedDate }}</label>
              <input id="s-received" type="date" formControlName="receivedDate" [class.invalid]="invalid('receivedDate')">
            </div>
            <div class="field">
              <label for="s-expiration">{{ i18n.t().inventory.expirationDate }}</label>
              <input id="s-expiration" type="date" formControlName="expirationDate" [class.invalid]="invalid('expirationDate') || datesInvalid()">
              @if (datesInvalid()) { <span class="error">{{ i18n.t().inventory.expirationError }}</span> }
            </div>
            @if (editing()) {
              <div class="field">
                <label for="s-active">{{ i18n.t().common.status }}</label>
                <label class="check"><input id="s-active" type="checkbox" formControlName="isActive"> {{ i18n.t().common.active }}</label>
              </div>
            }
          </div>
          <div class="actions" style="margin-top: 1rem">
            <button type="submit" class="btn primary" [disabled]="saving()">{{ saving() ? i18n.t().common.saving : i18n.t().common.save }}</button>
            <button type="button" class="btn" (click)="closeForm()">{{ i18n.t().common.cancel }}</button>
          </div>
        </form>
      </section>
    }

    <div class="toolbar">
      <div class="field">
        <label for="f-product">{{ i18n.t().common.product }}</label>
        <select id="f-product" (change)="setProductFilter($any($event.target).value)">
          <option value="">{{ i18n.t().common.all }}</option>
          @for (product of products(); track product.id) {
            <option [value]="product.id">{{ product.name }}</option>
          }
        </select>
      </div>
      <label class="check"><input type="checkbox" (change)="setLowStock($any($event.target).checked)"> {{ i18n.t().inventory.lowStockOnly }}</label>
      <label class="check"><input type="checkbox" (change)="setExpired($any($event.target).checked)"> {{ i18n.t().inventory.expiredOnly }}</label>
    </div>

    <section class="card">
      @if (loading()) {
        <div class="empty">{{ i18n.t().common.loading }}</div>
      } @else if (stocks().length === 0) {
        <div class="empty">{{ i18n.t().inventory.empty }}</div>
      } @else {
        <div class="table-wrap">
          <table>
            <thead>
              <tr>
                <th>{{ i18n.t().common.product }}</th>
                <th>{{ i18n.t().inventory.colBatch }}</th>
                <th>{{ i18n.t().inventory.location }}</th>
                <th class="right">{{ i18n.t().common.quantity }}</th>
                <th class="right">{{ i18n.t().inventory.colMinimum }}</th>
                <th>{{ i18n.t().inventory.colExpires }}</th>
                <th>{{ i18n.t().common.status }}</th>
                @if (isAdmin()) { <th></th> }
              </tr>
            </thead>
            <tbody>
              @for (stock of stocks(); track stock.id) {
                <tr>
                  <td>{{ stock.productName }}</td>
                  <td>{{ stock.batchNumber }}</td>
                  <td>{{ stock.location }}</td>
                  <td class="right">{{ stock.quantity | number: '1.0-3' }}</td>
                  <td class="right">{{ stock.minimumStock | number: '1.0-3' }}</td>
                  <td class="nowrap">{{ stock.expirationDate | date: 'dd/MM/yyyy' : 'UTC' }}</td>
                  <td>
                    @if (stock.isExpired) { <span class="badge danger">{{ i18n.t().inventory.expired }}</span> }
                    @if (stock.isLowStock) { <span class="badge warning">{{ i18n.t().inventory.lowStock }}</span> }
                    @if (!stock.isActive) { <span class="badge">{{ i18n.t().common.inactive }}</span> }
                    @if (!stock.isExpired && !stock.isLowStock && stock.isActive) { <span class="badge success">{{ i18n.t().inventory.normal }}</span> }
                  </td>
                  @if (isAdmin()) {
                    <td class="right nowrap">
                      <div class="actions" style="justify-content: flex-end">
                        <input #qty type="number" step="any" min="0.001" [placeholder]="i18n.t().inventory.addQuantityPlaceholder" style="width: 5.5rem"
                          [attr.aria-label]="i18n.t().inventory.addQuantityAriaLabel(stock.batchNumber)">
                        <button type="button" class="btn small" (click)="addQuantity(stock, qty.valueAsNumber); qty.value = ''">
                          {{ i18n.t().inventory.add }}
                        </button>
                        <button type="button" class="btn small" (click)="openEdit(stock)">{{ i18n.t().common.edit }}</button>
                      </div>
                    </td>
                  }
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </section>
  `,
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
