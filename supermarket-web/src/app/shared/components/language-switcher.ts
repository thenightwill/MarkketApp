import { Component, inject } from '@angular/core';
import { LanguageService } from '../../core/i18n/language.service';
import { Lang } from '../../core/i18n/translations';

@Component({
  selector: 'app-language-switcher',
  styles: `
    select { width: auto; padding: .3rem .5rem; font-size: .8125rem; }
  `,
  template: `
    <select [attr.aria-label]="i18n.t().language.label" [value]="i18n.lang()" (change)="onChange($any($event.target).value)">
      <option value="es">{{ i18n.t().language.spanish }}</option>
      <option value="en">{{ i18n.t().language.english }}</option>
    </select>
  `,
})
export class LanguageSwitcher {
  protected readonly i18n = inject(LanguageService);

  protected onChange(value: Lang): void {
    this.i18n.setLanguage(value);
  }
}
