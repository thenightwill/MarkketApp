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
  templateUrl: './products-page.html',
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
