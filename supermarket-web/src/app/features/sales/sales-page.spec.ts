import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Product } from '../products/product.models';
import { SalesPage } from './sales-page';

const API = 'http://localhost:5203/api';

function product(id: string, name: string, unitType: Product['unitType'] = 'Volume', salePrice = 3200): Product {
  return {
    id,
    name,
    brand: 'Marca',
    category: 'Cat',
    unitType,
    unitValue: 1,
    cost: 1000,
    salePrice,
    isActive: true,
    createdAt: '2026-09-01T00:00:00Z',
    updatedAt: null,
  };
}

interface Internals {
  form: { patchValue(v: object): void };
  addToCart(): void;
  submit(): void;
  cart(): { product: Product; quantity: number }[];
  cartError(): string | null;
  estimatedTotal(): number;
}

describe('SalesPage', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  async function render() {
    const fixture = TestBed.createComponent(SalesPage);
    fixture.detectChanges();
    http.expectOne(`${API}/products?includeInactive=false`).flush([
      product('p1', 'Leche'),
      product('p2', 'Pan', 'Unit', 6200),
    ]);
    http.expectOne(`${API}/sales`).flush([]);
    await fixture.whenStable();
    fixture.detectChanges();
    return { fixture, page: fixture.componentInstance as unknown as Internals };
  }

  it('merges repeated products in the cart', async () => {
    const { page } = await render();

    page.form.patchValue({ productId: 'p1', quantity: 2 });
    page.addToCart();
    page.form.patchValue({ productId: 'p1', quantity: 3 });
    page.addToCart();

    expect(page.cart()).toHaveLength(1);
    expect(page.cart()[0].quantity).toBe(5);
  });

  it('rejects fractional quantities for unit products before calling the server', async () => {
    const { page } = await render();

    page.form.patchValue({ productId: 'p2', quantity: 1.5 });
    page.addToCart();

    expect(page.cart()).toHaveLength(0);
    expect(page.cartError()).toContain('enteras');
  });

  it('computes only an estimated total from reference prices', async () => {
    const { page } = await render();

    page.form.patchValue({ productId: 'p1', quantity: 2 });
    page.addToCart();

    expect(page.estimatedTotal()).toBe(6400);
  });

  it('sends only product ids and quantities: never prices, lots or the user', async () => {
    const { page } = await render();
    page.form.patchValue({ productId: 'p1', quantity: 15 });
    page.addToCart();

    page.submit();

    const request = http.expectOne(`${API}/sales`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ items: [{ productId: 'p1', quantity: 15 }] });
    request.flush({
      id: 's1',
      userId: 'u1',
      saleDate: '2026-09-19T12:00:00Z',
      total: 48000,
      status: 'Completed',
      items: [{ productId: 'p1', productName: 'Leche', quantity: 15, unitPrice: 3200, subtotal: 48000 }],
    });
    http.expectOne(`${API}/sales`).flush([]);
  });

  it('shows the receipt returned by the server and clears the cart', async () => {
    const { fixture, page } = await render();
    page.form.patchValue({ productId: 'p1', quantity: 1 });
    page.addToCart();
    page.submit();

    http.expectOne(`${API}/sales`).flush({
      id: 's1',
      userId: 'u1',
      saleDate: '2026-09-19T12:00:00Z',
      total: 3200,
      status: 'Completed',
      items: [{ productId: 'p1', productName: 'Leche', quantity: 1, unitPrice: 3200, subtotal: 3200 }],
    });
    http.expectOne(`${API}/sales`).flush([]);
    await fixture.whenStable();
    fixture.detectChanges();

    expect(page.cart()).toHaveLength(0);
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Venta registrada');
    expect((fixture.nativeElement as HTMLElement).querySelector('[aria-label=Comprobante]')?.textContent).toContain('Completada');
  });

  it('shows a friendly message and keeps the cart when there is not enough stock', async () => {
    const { fixture, page } = await render();
    page.form.patchValue({ productId: 'p1', quantity: 99 });
    page.addToCart();
    page.submit();

    http.expectOne(`${API}/sales`).flush({ code: 'INSUFFICIENT_STOCK' }, { status: 409, statusText: 'Conflict' });
    await fixture.whenStable();
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).querySelector('.alert')?.textContent).toContain('stock');
    expect(page.cart()).toHaveLength(1);
  });
});
