import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { API_URL } from '../config';
import { Session } from './auth.models';
import { AuthService } from './auth.service';

const API = 'http://localhost:5203/api';

function session(role: 'Employee' | 'Administrator', expiresInMs = 60_000): Session {
  return {
    accessToken: 'token-123',
    expiresAt: new Date(Date.now() + expiresInMs).toISOString(),
    user: { id: 'u1', name: 'Ana', email: 'ana@supermarket.local', role },
  };
}

describe('AuthService', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
  });

  afterEach(() => TestBed.inject(HttpTestingController).verify());

  it('starts without a session', () => {
    const auth = TestBed.inject(AuthService);

    expect(auth.user()).toBeNull();
    expect(auth.token).toBeNull();
    expect(auth.hasValidSession()).toBe(false);
  });

  it('stores the session after a successful login', () => {
    const auth = TestBed.inject(AuthService);
    const http = TestBed.inject(HttpTestingController);

    auth.login({ email: 'ana@supermarket.local', password: 'Secret123' }).subscribe();
    http.expectOne(`${API}/auth/login`).flush(session('Employee'));

    expect(auth.user()?.name).toBe('Ana');
    expect(auth.token).toBe('token-123');
    expect(auth.isAdministrator()).toBe(false);
    expect(localStorage.getItem('supermarket.session')).toContain('token-123');
  });

  it('detects administrators', () => {
    const auth = TestBed.inject(AuthService);
    const http = TestBed.inject(HttpTestingController);

    auth.login({ email: 'admin@supermarket.local', password: 'x' }).subscribe();
    http.expectOne(`${API}/auth/login`).flush(session('Administrator'));

    expect(auth.isAdministrator()).toBe(true);
  });

  it('stores the session after registering', () => {
    const auth = TestBed.inject(AuthService);
    const http = TestBed.inject(HttpTestingController);

    auth.register({ name: 'Ana', email: 'ana@supermarket.local', password: 'Secret123' }).subscribe();
    http.expectOne(`${API}/auth/register`).flush(session('Employee'));

    expect(auth.hasValidSession()).toBe(true);
  });

  it('clears the session on logout', () => {
    const auth = TestBed.inject(AuthService);
    const http = TestBed.inject(HttpTestingController);
    auth.login({ email: 'a@a.com', password: 'x' }).subscribe();
    http.expectOne(`${API}/auth/login`).flush(session('Employee'));

    auth.logout();

    expect(auth.user()).toBeNull();
    expect(auth.token).toBeNull();
    expect(localStorage.getItem('supermarket.session')).toBeNull();
  });

  it('restores a saved session', () => {
    localStorage.setItem('supermarket.session', JSON.stringify(session('Employee')));

    const auth = TestBed.inject(AuthService);

    expect(auth.user()?.email).toBe('ana@supermarket.local');
    expect(auth.hasValidSession()).toBe(true);
  });

  it('ignores an expired saved session', () => {
    localStorage.setItem('supermarket.session', JSON.stringify(session('Employee', -1000)));

    const auth = TestBed.inject(AuthService);

    expect(auth.hasValidSession()).toBe(false);
    expect(auth.token).toBeNull();
  });

  it('ignores a corrupted saved session', () => {
    localStorage.setItem('supermarket.session', '{not json');

    const auth = TestBed.inject(AuthService);

    expect(auth.user()).toBeNull();
  });

  it('uses the API url token', () => {
    expect(TestBed.inject(API_URL)).toBe('http://localhost:5203/api');
  });
});
