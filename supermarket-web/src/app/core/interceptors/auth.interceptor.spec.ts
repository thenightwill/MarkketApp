import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { AuthService } from '../auth/auth.service';
import { authInterceptor } from './auth.interceptor';

const API = 'http://localhost:5203/api';

function seedSession(): void {
  localStorage.setItem(
    'supermarket.session',
    JSON.stringify({
      accessToken: 'abc.def.ghi',
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
      user: { id: 'u1', name: 'Ana', email: 'ana@supermarket.local', role: 'Employee' },
    }),
  );
}

describe('authInterceptor', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ],
    });
  });

  afterEach(() => TestBed.inject(HttpTestingController).verify());

  it('adds the bearer token to API calls', () => {
    seedSession();
    const http = TestBed.inject(HttpClient);
    const controller = TestBed.inject(HttpTestingController);

    http.get(`${API}/products`).subscribe();

    const request = controller.expectOne(`${API}/products`);
    expect(request.request.headers.get('Authorization')).toBe('Bearer abc.def.ghi');
    request.flush([]);
  });

  it('does not add a token when there is no session', () => {
    const http = TestBed.inject(HttpClient);
    const controller = TestBed.inject(HttpTestingController);

    http.get(`${API}/products`).subscribe();

    const request = controller.expectOne(`${API}/products`);
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush([]);
  });

  it('does not leak the token to other hosts', () => {
    seedSession();
    const http = TestBed.inject(HttpClient);
    const controller = TestBed.inject(HttpTestingController);

    http.get('https://example.com/data').subscribe();

    const request = controller.expectOne('https://example.com/data');
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush({});
  });

  it('logs out and redirects to login on a 401 from a protected endpoint', () => {
    seedSession();
    const http = TestBed.inject(HttpClient);
    const controller = TestBed.inject(HttpTestingController);
    const router = TestBed.inject(Router);
    const navigate = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    http.get(`${API}/products`).subscribe({ error: () => undefined });
    controller.expectOne(`${API}/products`).flush({ code: 'UNAUTHORIZED' }, { status: 401, statusText: 'Unauthorized' });

    expect(TestBed.inject(AuthService).hasValidSession()).toBe(false);
    expect(navigate).toHaveBeenCalledWith(['/login'], expect.anything());
  });

  it('keeps the session when the login endpoint itself answers 401', () => {
    seedSession();
    const http = TestBed.inject(HttpClient);
    const controller = TestBed.inject(HttpTestingController);
    const router = TestBed.inject(Router);
    const navigate = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    http.post(`${API}/auth/login`, {}).subscribe({ error: () => undefined });
    controller.expectOne(`${API}/auth/login`).flush({ code: 'INVALID_CREDENTIALS' }, { status: 401, statusText: 'Unauthorized' });

    expect(navigate).not.toHaveBeenCalled();
  });

  it('does not log out on a 403', () => {
    seedSession();
    const http = TestBed.inject(HttpClient);
    const controller = TestBed.inject(HttpTestingController);

    http.post(`${API}/products`, {}).subscribe({ error: () => undefined });
    controller.expectOne(`${API}/products`).flush({ code: 'FORBIDDEN' }, { status: 403, statusText: 'Forbidden' });

    expect(TestBed.inject(AuthService).hasValidSession()).toBe(true);
  });
});
