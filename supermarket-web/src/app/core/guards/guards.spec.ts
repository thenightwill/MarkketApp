import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';
import { adminGuard, authGuard, guestGuard } from './guards';

function seedSession(role: 'Employee' | 'Administrator', expiresInMs = 60_000): void {
  localStorage.setItem(
    'supermarket.session',
    JSON.stringify({
      accessToken: 't',
      expiresAt: new Date(Date.now() + expiresInMs).toISOString(),
      user: { id: 'u1', name: 'Ana', email: 'ana@supermarket.local', role },
    }),
  );
}

function run(guard: typeof authGuard, url = '/products') {
  return TestBed.runInInjectionContext(() =>
    guard({} as ActivatedRouteSnapshot, { url } as RouterStateSnapshot),
  );
}

describe('guards', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
  });

  it('authGuard lets authenticated users through', () => {
    seedSession('Employee');

    expect(run(authGuard)).toBe(true);
  });

  it('authGuard redirects anonymous users to login keeping the return url', () => {
    const result = run(authGuard, '/inventory') as UrlTree;

    expect(TestBed.inject(Router).serializeUrl(result)).toBe('/login?returnUrl=%2Finventory');
  });

  it('authGuard rejects an expired session', () => {
    seedSession('Employee', -1000);

    expect(run(authGuard)).toBeInstanceOf(UrlTree);
  });

  it('adminGuard allows administrators', () => {
    seedSession('Administrator');

    expect(run(adminGuard)).toBe(true);
  });

  it('adminGuard sends employees away', () => {
    seedSession('Employee');

    const result = run(adminGuard) as UrlTree;

    expect(TestBed.inject(Router).serializeUrl(result)).toBe('/sales');
  });

  it('guestGuard lets anonymous users see the login page', () => {
    expect(run(guestGuard)).toBe(true);
  });

  it('guestGuard redirects logged users away from the login page', () => {
    seedSession('Employee');

    expect(run(guestGuard)).toBeInstanceOf(UrlTree);
  });
});
