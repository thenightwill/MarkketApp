import { TestBed } from '@angular/core/testing';
import { LanguageService } from '../../core/i18n/language.service';
import { LanguageSwitcher } from './language-switcher';

describe('LanguageSwitcher', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({});
  });

  it('reflects the current language as the selected option', () => {
    const fixture = TestBed.createComponent(LanguageSwitcher);
    fixture.detectChanges();

    const select = fixture.nativeElement.querySelector('select') as HTMLSelectElement;
    expect(select.value).toBe('es');
  });

  it('changes the language when a different option is selected', () => {
    const fixture = TestBed.createComponent(LanguageSwitcher);
    fixture.detectChanges();
    const select = fixture.nativeElement.querySelector('select') as HTMLSelectElement;

    select.value = 'en';
    select.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(TestBed.inject(LanguageService).lang()).toBe('en');
    expect(select.value).toBe('en');
  });
});
