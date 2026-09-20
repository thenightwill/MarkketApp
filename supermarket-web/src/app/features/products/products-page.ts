import { Component, OnInit, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { friendlyMessage } from '../../core/api/api-error';
import { AuthService } from '../../core/auth/auth.service';
import { LanguageService } from '../../core/i18n/language.service';
import { Alert } from '../../shared/components/alert';
import { MoneyPipe } from '../../shared/pipes/money.pipe';
import { Product, UNIT_TYPES, UnitType } from './product.models';
import { ProductService } from './product.service';

@Component({
  selector: 'app-products-page',
  imports: [ReactiveFormsModule, MoneyPipe, Alert],
  template: `
    <div class="page-header">
      <div>
        <h1>{{ i18n.t().products.title }}</h1>
        <p>{{ i18n.t().products.subtitle }}</p>
      </div>
      @if (isAdmin()) {
        <button type="button" class="btn primary" (click)="openCreate()">{{ i18n.t().products.newProduct }}</button>
      }
    </div>

    <app-alert [message]="notice()" kind="success" />
    <app-alert [message]="error()" />

    @if (formOpen()) {
      <section class="card" [attr.aria-label]="i18n.t().products.formAriaLabel">
        <h2>{{ editing() ? i18n.t().products.editProduct : i18n.t().products.newProduct }}</h2>
        <form [formGroup]="form" (ngSubmit)="save()" novalidate>
          <div class="grid">
            <div class="field">
              <label for="p-name">{{ i18n.t().products.name }}</label>
              <input id="p-name" formControlName="name" [class.invalid]="invalid('name')">
              @if (invalid('name')) { <span class="error">{{ i18n.t().products.nameRequired }}</span> }
            </div>
            <div class="field">
              <label for="p-brand">{{ i18n.t().products.brand }}</label>
              <input id="p-brand" formControlName="brand" [class.invalid]="invalid('brand')">
              @if (invalid('brand')) { <span class="error">{{ i18n.t().products.brandRequired }}</span> }
            </div>
            <div class="field">
              <label for="p-category">{{ i18n.t().common.category }}</label>
              <input id="p-category" formControlName="category" [class.invalid]="invalid('category')">
              @if (invalid('category')) { <span class="error">{{ i18n.t().products.categoryRequired }}</span> }
            </div>
            <div class="field">
              <label for="p-unit">{{ i18n.t().products.unitType }}</label>
              <select id="p-unit" formControlName="unitType">
                @for (type of unitTypes; track type) {
                  <option [value]="type">{{ i18n.t().products.unitTypes[type] }}</option>
                }
              </select>
            </div>
            <div class="field">
              <label for="p-unit-value">{{ i18n.t().products.unitValue }}</label>
              <input id="p-unit-value" type="number" step="any" formControlName="unitValue" [class.invalid]="invalid('unitValue')">
              @if (invalid('unitValue')) { <span class="error">{{ i18n.t().products.unitValueError }}</span> }
            </div>
            <div class="field">
              <label for="p-cost">{{ i18n.t().products.cost }}</label>
              <input id="p-cost" type="number" step="any" formControlName="cost" [class.invalid]="invalid('cost')">
              @if (invalid('cost')) { <span class="error">{{ i18n.t().products.costError }}</span> }
            </div>
            <div class="field">
              <label for="p-price">{{ i18n.t().products.salePrice }}</label>
              <input id="p-price" type="number" step="any" formControlName="salePrice" [class.invalid]="invalid('salePrice') || priceBelowCost()">
              @if (priceBelowCost()) { <span class="error">{{ i18n.t().products.salePriceError }}</span> }
            </div>
            @if (editing()) {
              <div class="field">
                <label for="p-active">{{ i18n.t().common.status }}</label>
                <label class="check"><input id="p-active" type="checkbox" formControlName="isActive"> {{ i18n.t().common.active }}</label>
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

    @if (isAdmin()) {
      <div class="toolbar">
        <label class="check">
          <input type="checkbox" [checked]="showInactive()" (change)="toggleInactive($any($event.target).checked)">
          {{ i18n.t().products.showInactive }}
        </label>
      </div>
    }

    <section class="card">
      @if (loading()) {
        <div class="empty">{{ i18n.t().common.loading }}</div>
      } @else if (products().length === 0) {
        <div class="empty">{{ i18n.t().products.empty }}</div>
      } @else {
        <div class="table-wrap">
          <table>
            <thead>
              <tr>
                <th>{{ i18n.t().common.product }}</th>
                <th>{{ i18n.t().common.category }}</th>
                <th>{{ i18n.t().products.colPresentation }}</th>
                @if (isAdmin()) { <th class="right">{{ i18n.t().products.cost }}</th> }
                <th class="right">{{ i18n.t().common.price }}</th>
                <th>{{ i18n.t().common.status }}</th>
                @if (isAdmin()) { <th></th> }
              </tr>
            </thead>
            <tbody>
              @for (product of products(); track product.id) {
                <tr>
                  <td><strong>{{ product.name }}</strong><br><span class="muted">{{ product.brand }}</span></td>
                  <td>{{ product.category }}</td>
                  <td>{{ product.unitValue }} · {{ unitLabel(product.unitType) }}</td>
                  @if (isAdmin()) { <td class="right">{{ product.cost | money }}</td> }
                  <td class="right">{{ product.salePrice | money }}</td>
                  <td>
                    <span class="badge" [class.success]="product.isActive" [class.danger]="!product.isActive">
                      {{ product.isActive ? i18n.t().common.active : i18n.t().common.inactive }}
                    </span>
                  </td>
                  @if (isAdmin()) {
                    <td class="right nowrap">
                      <button type="button" class="btn small" (click)="openEdit(product)">{{ i18n.t().common.edit }}</button>
                      @if (product.isActive) {
                        <button type="button" class="btn small danger" (click)="deactivate(product)">{{ i18n.t().products.deactivate }}</button>
                      }
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
export class ProductsPage implements OnInit {
  private readonly service = inject(ProductService);
  private readonly auth = inject(AuthService);

  protected readonly i18n = inject(LanguageService);
  protected readonly isAdmin = this.auth.isAdministrator;
  protected readonly products = signal<Product[]>([]);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly notice = signal<string | null>(null);
  protected readonly showInactive = signal(false);
  protected readonly formOpen = signal(false);
  protected readonly editing = signal<Product | null>(null);

  protected readonly unitTypes = UNIT_TYPES;

  protected readonly form = inject(NonNullableFormBuilder).group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    brand: ['', [Validators.required, Validators.maxLength(100)]],
    category: ['', [Validators.required, Validators.maxLength(100)]],
    unitType: ['Unit' as UnitType],
    unitValue: [1, [Validators.required, Validators.min(0.001)]],
    cost: [0, [Validators.required, Validators.min(0)]],
    salePrice: [0, [Validators.required, Validators.min(0)]],
    isActive: [true],
  });

  ngOnInit(): void {
    this.load();
  }

  protected unitLabel(type: UnitType): string {
    return this.i18n.t().products.unitTypes[type];
  }

  protected invalid(name: keyof typeof this.form.controls): boolean {
    const control = this.form.controls[name];
    return control.touched && control.invalid;
  }

  protected priceBelowCost(): boolean {
    const { cost, salePrice } = this.form.getRawValue();
    return this.form.controls.salePrice.touched && salePrice < cost;
  }

  protected toggleInactive(value: boolean): void {
    this.showInactive.set(value);
    this.load();
  }

  protected openCreate(): void {
    this.editing.set(null);
    this.form.reset({ unitType: 'Unit', unitValue: 1, cost: 0, salePrice: 0, isActive: true });
    this.formOpen.set(true);
    this.notice.set(null);
  }

  protected openEdit(product: Product): void {
    this.editing.set(product);
    this.form.reset({
      name: product.name,
      brand: product.brand,
      category: product.category,
      unitType: product.unitType,
      unitValue: product.unitValue,
      cost: product.cost ?? 0,
      salePrice: product.salePrice,
      isActive: product.isActive,
    });
    this.formOpen.set(true);
    this.notice.set(null);
  }

  protected closeForm(): void {
    this.formOpen.set(false);
    this.editing.set(null);
  }

  protected save(): void {
    const value = this.form.getRawValue();

    if (this.form.invalid || value.salePrice < value.cost) {
      this.form.markAllAsTouched();
      return;
    }

    const { isActive, ...payload } = value;
    const current = this.editing();
    const request = current
      ? this.service.update(current.id, { ...payload, isActive })
      : this.service.create(payload);

    this.saving.set(true);
    this.error.set(null);

    request.subscribe({
      next: () => {
        this.saving.set(false);
        this.notice.set(current ? this.i18n.t().products.updated : this.i18n.t().products.created);
        this.closeForm();
        this.load();
      },
      error: (err: unknown) => {
        this.saving.set(false);
        this.error.set(friendlyMessage(err, this.i18n.t().errors));
      },
    });
  }

  protected deactivate(product: Product): void {
    if (!confirm(this.i18n.t().products.confirmDeactivate(product.name))) {
      return;
    }

    this.error.set(null);
    this.service.deactivate(product.id).subscribe({
      next: () => {
        this.notice.set(this.i18n.t().products.deactivated);
        this.load();
      },
      error: (err: unknown) => this.error.set(friendlyMessage(err, this.i18n.t().errors)),
    });
  }

  private load(): void {
    this.loading.set(true);
    this.service.list(this.showInactive() && this.isAdmin()).subscribe({
      next: (items) => {
        this.products.set(items);
        this.loading.set(false);
      },
      error: (err: unknown) => {
        this.error.set(friendlyMessage(err, this.i18n.t().errors));
        this.loading.set(false);
      },
    });
  }
}
