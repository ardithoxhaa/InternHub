import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-empty-state',
  imports: [CommonModule],
  template: `
    <section class="empty-state">
      <b>{{ title }}</b>
      <p>{{ detail }}</p>
      <button *ngIf="actionLabel" type="button" (click)="action.emit()">{{ actionLabel }}</button>
    </section>
  `,
  styles: [`
    .empty-state {
      border: 1px dashed #c9d6dc;
      border-radius: 8px;
      background: #f8fafb;
      padding: 20px;
      display: grid;
      justify-items: start;
      gap: 8px;
      color: #41515a;
    }

    b {
      color: #172128;
    }

    p {
      margin: 0;
      color: #60717b;
    }

    button {
      min-height: 34px;
      border: 1px solid #c9d6dc;
      border-radius: 8px;
      padding: 8px 11px;
      background: #fff;
      color: #1769aa;
      font: inherit;
      font-weight: 760;
      cursor: pointer;
    }
  `]
})
export class EmptyStateComponent {
  @Input() title = 'Nothing here yet';
  @Input() detail = 'Create a record to get started.';
  @Input() actionLabel = '';
  @Output() action = new EventEmitter<void>();
}
