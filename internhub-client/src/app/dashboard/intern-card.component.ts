import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-intern-card',
  template: `
    <article class="intern-card" [class]="statusTone">
      <div class="intern-preview">
        <span></span><span></span><span></span>
      </div>
      <div class="intern-card-body">
        <small>{{ status }}</small>
        <h3>{{ title }}</h3>
        <p>{{ detail }}</p>
        <div class="intern-progress"><i [style.width.%]="progress"></i></div>
        <button type="button" (click)="open.emit()">Open profile</button>
      </div>
    </article>
  `
})
export class InternCardComponent {
  @Input() title = '';
  @Input() detail = '';
  @Input() status = 'Draft';
  @Input() statusTone = 'draft';
  @Input() progress = 0;
  @Output() open = new EventEmitter<void>();
}
