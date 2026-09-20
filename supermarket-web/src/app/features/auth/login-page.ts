import { Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { friendlyMessage } from '../../core/api/api-error';
import { AuthService } from '../../core/auth/auth.service';
import { LanguageService } from '../../core/i18n/language.service';
import { Alert } from '../../shared/components/alert';
import { LanguageSwitcher } from '../../shared/components/language-switcher';

@Component({
  selector: 'app-login-page',
  imports: [ReactiveFormsModule, RouterLink, Alert, LanguageSwitcher],
  styles: `
    .auth { max-width: 400px; margin: 8vh auto 0; padding: 0 1rem; }
    form { display: grid; gap: .9rem; }
    .top { display: flex; justify-content: flex-end; margin-bottom: .5rem; }
  `,
  template: `
    <div class="auth">
      <div class="top"><app-language-switcher /></div>
      <div class="card">
        <h1>{{ i18n.t().auth.login.title }}</h1>
        <p class="muted">{{ i18n.t().auth.login.subtitle }}</p>
        <app-alert [message]="error()" />
        <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
          <div class="field">
            <label for="email">{{ i18n.t().auth.login.email }}</label>
            <input id="email" type="email" formControlName="email" autocomplete="username"
              [class.invalid]="form.controls.email.touched && form.controls.email.invalid">
            @if (form.controls.email.touched && form.controls.email.invalid) {
              <span class="error">{{ i18n.t().auth.login.emailError }}</span>
            }
          </div>
          <div class="field">
            <label for="password">{{ i18n.t().auth.login.password }}</label>
            <input id="password" type="password" formControlName="password" autocomplete="current-password"
              [class.invalid]="form.controls.password.touched && form.controls.password.invalid">
            @if (form.controls.password.touched && form.controls.password.invalid) {
              <span class="error">{{ i18n.t().auth.login.passwordRequired }}</span>
            }
          </div>
          <button class="btn primary" type="submit" [disabled]="loading()">
            {{ loading() ? i18n.t().auth.login.submitting : i18n.t().auth.login.submit }}
          </button>
        </form>
        <p class="muted">{{ i18n.t().auth.login.noAccount }} <a routerLink="/register">{{ i18n.t().auth.login.register }}</a></p>
      </div>
    </div>
  `,
})
export class LoginPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly i18n = inject(LanguageService);
  protected readonly error = signal<string | null>(null);
  protected readonly loading = signal(false);
  protected readonly form = inject(NonNullableFormBuilder).group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
  });

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    this.auth.login(this.form.getRawValue()).subscribe({
      next: () => {
        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
        this.router.navigateByUrl(returnUrl && returnUrl.startsWith('/') ? returnUrl : '/sales');
      },
      error: (err: unknown) => {
        this.error.set(friendlyMessage(err, this.i18n.t().errors));
        this.loading.set(false);
      },
    });
  }
}
