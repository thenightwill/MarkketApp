import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { LanguageSwitcher } from '../../shared/components/language-switcher';
import { AuthService } from '../auth/auth.service';
import { LanguageService } from '../i18n/language.service';

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, LanguageSwitcher],
  styles: `
    .topbar { background: var(--surface); border-bottom: 1px solid var(--border); position: sticky; top: 0; z-index: 10; }
    .bar { max-width: 1120px; margin: 0 auto; padding: .6rem 1rem; display: flex; flex-wrap: wrap; align-items: center; gap: 1rem; }
    .brand { font-weight: 700; font-size: 1.05rem; text-decoration: none; color: var(--text); }
    nav { display: flex; flex-wrap: wrap; gap: .25rem; flex: 1; }
    nav a { text-decoration: none; color: var(--muted); padding: .35rem .7rem; border-radius: 8px; font-weight: 600; }
    nav a:hover { background: var(--surface-2); }
    nav a.active { color: var(--primary); background: var(--info-bg); }
    .who { display: flex; align-items: center; gap: .6rem; }
  `,
  template: `
    <header class="topbar">
      <div class="bar">
        <a class="brand" routerLink="/sales">Supermarket</a>
        <nav [attr.aria-label]="i18n.t().nav.mainLabel">
          <a routerLink="/sales" routerLinkActive="active">{{ i18n.t().nav.sales }}</a>
          <a routerLink="/products" routerLinkActive="active">{{ i18n.t().nav.products }}</a>
          <a routerLink="/inventory" routerLinkActive="active">{{ i18n.t().nav.inventory }}</a>
          <a routerLink="/tasks" routerLinkActive="active">{{ i18n.t().nav.tasks }}</a>
        </nav>
        <div class="who">
          <app-language-switcher />
          <span>{{ auth.user()?.name }}</span>
          <span class="badge info">{{ auth.isAdministrator() ? i18n.t().nav.administrator : i18n.t().nav.employee }}</span>
          <button type="button" class="btn small" (click)="logout()">{{ i18n.t().nav.logout }}</button>
        </div>
      </div>
    </header>
    <main class="container">
      <router-outlet />
    </main>
  `,
})
export class Shell {
  protected readonly auth = inject(AuthService);
  protected readonly i18n = inject(LanguageService);
  private readonly router = inject(Router);

  logout(): void {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
