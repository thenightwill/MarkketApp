import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Product } from '../products/product.models';
import { InventoryPage } from './inventory-page';
import { Stock } from './inventory.models';

const API = 'http://localhost:5203/api';

function product(id: string, name: string): Product {
  return {
    id,
    name,
    brand: 'Marca',
    category: 'Cat',
    unitType: 'Volume',
    unitValue: 1,
    cost: null,
    salePrice: 3200,
    isActive: true,
    createdAt: '2026-09-01T00:00:00Z',
    updatedAt: null,
  };
}

function stock(id: string, overrides: Partial<Stock> = {}): Stock {
  return {
    id,
    productId: 'p1',
    productName: 'Leche',
    batchNumber: 'L001',
    location: 'A-01',
    quantity: 10,
    minimumStock: 2,
    receivedDate: '2026-09-01T00:00:00Z',
    expirationDate: '2026-12-01T00:00:00Z',
    isActive: true,
    isLowStock: false,
    isExpired: false,
    createdAt: '2026-09-01T00:00:00Z',
    ...overrides,
  };
}

function loginAs(role: 'Employee' | 'Administrator'): void {
  localStorage.setItem(
    'supermarket.session',
    JSON.stringify({
      accessToken: 't',
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
      user: { id: 'u1', name: 'Ana', email: 'ana@supermarket.local', role },
    }),
  );
}

describe('InventoryPage', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  async function render(role: 'Employee' | 'Administrator', stocks: Stock[] = [stock('s1')]) {
    loginAs(role);
    const fixture = TestBed.createComponent(InventoryPage);
    fixture.detectChanges();
    http.expectOne(`${API}/products?includeInactive=false`).flush([product('p1', 'Leche')]);
    http.expectOne(`${API}/inventory`).flush(stocks);
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  it('hides management actions from employees', async () => {
    const fixture = await render('Employee');
    const text = (fixture.nativeElement as HTMLElement).textContent!;

    expect(text).toContain('L001');
    expect(text).not.toContain('Nuevo lote');
    expect(text).not.toContain('Agregar');
  });

  it('shows the add-quantity control to administrators', async () => {
    const fixture = await render('Administrator');
    const text = (fixture.nativeElement as HTMLElement).textContent!;

    expect(text).toContain('Nuevo lote');
    expect(text).toContain('Agregar');
  });

  it('posts the entered quantity to add-quantity and reloads the list', async () => {
    const fixture = await render('Administrator');
    const page = fixture.componentInstance as unknown as { addQuantity(s: Stock, q: number): void };

    page.addQuantity(stock('s1'), 5);

    const request = http.expectOne(`${API}/inventory/s1/add-quantity`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ quantity: 5 });
    request.flush(stock('s1', { quantity: 15 }));
    http.expectOne(`${API}/inventory`).flush([stock('s1', { quantity: 15 })]);
  });

  it('rejects a non-positive quantity without calling the server', async () => {
    const fixture = await render('Administrator');
    const page = fixture.componentInstance as unknown as { addQuantity(s: Stock, q: number): void };

    page.addQuantity(stock('s1'), 0);
    fixture.detectChanges();

    http.expectNone(`${API}/inventory/s1/add-quantity`);
    expect((fixture.nativeElement as HTMLElement).querySelector('.alert')?.textContent).toContain('mayor que cero');
  });

  it('shows a friendly message when the server rejects the quantity', async () => {
    const fixture = await render('Administrator');
    const page = fixture.componentInstance as unknown as { addQuantity(s: Stock, q: number): void };

    page.addQuantity(stock('s1'), 3);
    http.expectOne(`${API}/inventory/s1/add-quantity`).flush({ code: 'INVALID_QUANTITY' }, { status: 400, statusText: 'Bad Request' });
    await fixture.whenStable();
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).querySelector('.alert')?.textContent).toContain('cantidad');
  });

  it('flags expired and low-stock batches', async () => {
    const fixture = await render('Employee', [
      stock('low', { batchNumber: 'LOW', isLowStock: true }),
      stock('old', { batchNumber: 'OLD', isExpired: true }),
    ]);
    const text = (fixture.nativeElement as HTMLElement).textContent!;

    expect(text).toContain('Stock bajo');
    expect(text).toContain('Vencido');
  });
});
