import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { LanguageService } from '../../core/i18n/language.service';
import { ProductsPage } from './products-page';
import { Product } from './product.models';

const API = 'http://localhost:5203/api';

const PRODUCT: Product = {
  id: 'p1',
  name: 'Leche Entera',
  brand: 'Alpina',
  category: 'Lácteos',
  unitType: 'Volume',
  unitValue: 1,
  cost: 2500,
  salePrice: 3200,
  isActive: true,
  createdAt: '2026-09-01T00:00:00Z',
  updatedAt: null,
};

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

describe('ProductsPage', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  async function render(role: 'Employee' | 'Administrator') {
    loginAs(role);
    const fixture = TestBed.createComponent(ProductsPage);
    fixture.detectChanges();
    http.expectOne(`${API}/products?includeInactive=false`).flush([PRODUCT]);
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  it('hides management actions and costs from employees', async () => {
    const fixture = await render('Employee');
    const text = (fixture.nativeElement as HTMLElement).textContent!;

    expect(text).toContain('Leche Entera');
    expect(text).not.toContain('Nuevo producto');
    expect(text).not.toContain('Costo');
    expect(text).not.toContain('Desactivar');
  });

  it('shows management actions to administrators', async () => {
    const fixture = await render('Administrator');
    const text = (fixture.nativeElement as HTMLElement).textContent!;

    expect(text).toContain('Nuevo producto');
    expect(text).toContain('Costo');
    expect(text).toContain('Desactivar');
  });

  it('blocks saving when the sale price is lower than the cost', async () => {
    const fixture = await render('Administrator');
    const page = fixture.componentInstance as unknown as {
      openCreate(): void;
      form: { patchValue(v: object): void };
      save(): void;
    };

    page.openCreate();
    page.form.patchValue({ name: 'X', brand: 'Y', category: 'Z', unitValue: 1, cost: 2500, salePrice: 2000 });
    page.save();

    http.expectNone(`${API}/products`);
  });

  it('creates a product with the form values', async () => {
    const fixture = await render('Administrator');
    const page = fixture.componentInstance as unknown as {
      openCreate(): void;
      form: { patchValue(v: object): void };
      save(): void;
    };

    page.openCreate();
    page.form.patchValue({ name: 'Yogurt', brand: 'Alpina', category: 'Lácteos', unitType: 'Volume', unitValue: 1, cost: 1000, salePrice: 1500 });
    page.save();

    const request = http.expectOne(`${API}/products`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body.name).toBe('Yogurt');
    expect(request.request.body).not.toHaveProperty('isActive');
    request.flush({ ...PRODUCT, id: 'p2', name: 'Yogurt' });
    http.expectOne(`${API}/products?includeInactive=false`).flush([PRODUCT]);
  });

  it('re-renders in English when the language is switched', async () => {
    const fixture = await render('Administrator');

    TestBed.inject(LanguageService).setLanguage('en');
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent!;
    expect(text).toContain('Products');
    expect(text).toContain('New product');
    expect(text).toContain('Cost');
    expect(text).toContain('Deactivate');
    expect(text).not.toContain('Costo');
  });
});
