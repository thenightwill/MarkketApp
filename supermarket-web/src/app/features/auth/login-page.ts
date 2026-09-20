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
  templateUrl: './login-page.html',
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
