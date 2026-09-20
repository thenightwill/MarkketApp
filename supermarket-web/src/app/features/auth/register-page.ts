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
  templateUrl: './register-page.html',
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
