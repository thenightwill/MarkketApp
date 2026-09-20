import { Component, input } from '@angular/core';

@Component({
  selector: 'app-alert',
  template: `
    @if (message()) {
      <div class="alert" [class.error]="kind() === 'error'" [class.success]="kind() === 'success'" [class.info]="kind() === 'info'" role="alert">
        {{ message() }}
      </div>
    }
  `,
})
export class Alert {
  readonly message = input<string | null>(null);
  readonly kind = input<'error' | 'success' | 'info'>('error');
}
