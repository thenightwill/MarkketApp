import { Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { friendlyMessage } from '../../core/api/api-error';
import { AuthService } from '../../core/auth/auth.service';
import { LanguageService } from '../../core/i18n/language.service';
import { Alert } from '../../shared/components/alert';
import { LanguageSwitcher } from '../../shared/components/language-switcher';

@Component({
  selector: 'app-register-page',
  imports: [ReactiveFormsModule, RouterLink, Alert, LanguageSwitcher],
  styles: `
    .auth { max-width: 400px; margin: 6vh auto 0; padding: 0 1rem; }
    form { display: grid; gap: .9rem; }
    .top { display: flex; justify-content: flex-end; margin-bottom: .5rem; }
  `,
  template: `
    <div class="auth">
      <div class="top"><app-language-switcher /></div>
      <div class="card">
        <h1>{{ i18n.t().auth.register.title }}</h1>
        <p class="muted">{{ i18n.t().auth.register.subtitle }}</p>
        <app-alert [message]="error()" />
        <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
          <div class="field">
            <label for="name">{{ i18n.t().auth.register.name }}</label>
            <input id="name" formControlName="name" autocomplete="name"
              [class.invalid]="form.controls.name.touched && form.controls.name.invalid">
            @if (form.controls.name.touched && form.controls.name.invalid) {
              <span class="error">{{ i18n.t().auth.register.nameRequired }}</span>
            }
          </div>
          <div class="field">
            <label for="email">{{ i18n.t().auth.register.email }}</label>
            <input id="email" type="email" formControlName="email" autocomplete="username"
              [class.invalid]="form.controls.email.touched && form.controls.email.invalid">
            @if (form.controls.email.touched && form.controls.email.invalid) {
              <span class="error">{{ i18n.t().auth.register.emailError }}</span>
            }
          </div>
          <div class="field">
            <label for="password">{{ i18n.t().auth.register.password }}</label>
            <input id="password" type="password" formControlName="password" autocomplete="new-password"
              [class.invalid]="form.controls.password.touched && form.controls.password.invalid">
            <span class="hint">{{ i18n.t().auth.register.passwordHint }}</span>
            @if (form.controls.password.touched && form.controls.password.invalid) {
              <span class="error">{{ i18n.t().auth.register.passwordError }}</span>
            }
          </div>
          <button class="btn primary" type="submit" [disabled]="loading()">
            {{ loading() ? i18n.t().auth.register.submitting : i18n.t().auth.register.submit }}
          </button>
        </form>
        <p class="muted">{{ i18n.t().auth.register.haveAccount }} <a routerLink="/login">{{ i18n.t().auth.register.login }}</a></p>
      </div>
    </div>
  `,
})
export class RegisterPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly i18n = inject(LanguageService);
  protected readonly error = signal<string | null>(null);
  protected readonly loading = signal(false);
  protected readonly form = inject(NonNullableFormBuilder).group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
    password: [
      '',
      [
        Validators.required,
        Validators.minLength(8),
        Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$/),
      ],
    ],
  });

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    this.auth.register(this.form.getRawValue()).subscribe({
      next: () => this.router.navigate(['/sales']),
      error: (err: unknown) => {
        this.error.set(friendlyMessage(err, this.i18n.t().errors));
        this.loading.set(false);
      },
    });
  }
}
