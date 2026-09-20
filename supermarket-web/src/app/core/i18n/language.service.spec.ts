import { TestBed } from '@angular/core/testing';
import { LanguageService } from './language.service';

describe('LanguageService', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({});
  });

  it('defaults to Spanish when nothing was stored', () => {
    const service = TestBed.inject(LanguageService);

    expect(service.lang()).toBe('es');
    expect(service.t().nav.sales).toBe('Ventas');
  });

  it('switches the whole translation object when the language changes', () => {
    const service = TestBed.inject(LanguageService);

    service.setLanguage('en');

    expect(service.lang()).toBe('en');
    expect(service.t().nav.sales).toBe('Sales');
  });

  it('toggle alternates between es and en', () => {
    const service = TestBed.inject(LanguageService);

    service.toggle();
    expect(service.lang()).toBe('en');

    service.toggle();
    expect(service.lang()).toBe('es');
  });

  it('persists the choice to localStorage', () => {
    const service = TestBed.inject(LanguageService);

    service.setLanguage('en');

    expect(localStorage.getItem('supermarket.lang')).toBe('en');
  });

  it('restores a previously stored language on a fresh instance', () => {
    localStorage.setItem('supermarket.lang', 'en');

    const service = TestBed.inject(LanguageService);

    expect(service.lang()).toBe('en');
  });

  it('falls back to Spanish for a corrupted stored value', () => {
    localStorage.setItem('supermarket.lang', 'fr');

    const service = TestBed.inject(LanguageService);

    expect(service.lang()).toBe('es');
  });
});
