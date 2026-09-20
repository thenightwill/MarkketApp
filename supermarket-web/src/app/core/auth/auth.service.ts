import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { API_URL } from '../config';
import { AuthUser, LoginPayload, RegisterPayload, Session } from './auth.models';

const STORAGE_KEY = 'supermarket.session';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = inject(API_URL);
  private readonly session = signal<Session | null>(this.restore());

  readonly user = computed<AuthUser | null>(() => this.session()?.user ?? null);
  readonly isAdministrator = computed(() => this.user()?.role === 'Administrator');

  get token(): string | null {
    return this.hasValidSession() ? this.session()!.accessToken : null;
  }

  hasValidSession(): boolean {
    const current = this.session();
    return !!current && new Date(current.expiresAt).getTime() > Date.now();
  }

  login(payload: LoginPayload): Observable<Session> {
    return this.http.post<Session>(`${this.apiUrl}/auth/login`, payload).pipe(tap((s) => this.store(s)));
  }

  register(payload: RegisterPayload): Observable<Session> {
    return this.http.post<Session>(`${this.apiUrl}/auth/register`, payload).pipe(tap((s) => this.store(s)));
  }

  logout(): void {
    this.session.set(null);
    try {
      localStorage.removeItem(STORAGE_KEY);
    } catch {
      return;
    }
  }

  private store(session: Session): void {
    this.session.set(session);
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(session));
    } catch {
      return;
    }
  }

  private restore(): Session | null {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      return raw ? (JSON.parse(raw) as Session) : null;
    } catch {
      return null;
    }
  }
}
