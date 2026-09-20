import { Injectable, computed, signal } from '@angular/core';
import { Lang, TRANSLATIONS } from './translations';

const STORAGE_KEY = 'supermarket.lang';
const DEFAULT_LANG: Lang = 'es';

@Injectable({ providedIn: 'root' })
export class LanguageService {
  private readonly language = signal<Lang>(this.restore());

  readonly lang = this.language.asReadonly();
  readonly t = computed(() => TRANSLATIONS[this.language()]);

  setLanguage(lang: Lang): void {
    this.language.set(lang);
    try {
      localStorage.setItem(STORAGE_KEY, lang);
    } catch {
      return;
    }
  }

  toggle(): void {
    this.setLanguage(this.language() === 'es' ? 'en' : 'es');
  }

  private restore(): Lang {
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      return stored === 'es' || stored === 'en' ? stored : DEFAULT_LANG;
    } catch {
      return DEFAULT_LANG;
    }
  }
}
