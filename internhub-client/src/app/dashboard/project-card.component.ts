import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-project-card',
  template: `
    <article class="project-card" [class]="statusTone">
      <div class="project-preview">
        <span></span><span></span><span></span>
      </div>
      <div class="project-body">
        <small>{{ status }}</small>
        <h3>{{ title }}</h3>
        <p>{{ detail }}</p>
        <div class="project-progress"><i [style.width.%]="progress"></i></div>
        <button type="button" (click)="open.emit()">Open project</button>
      </div>
    </article>
  `
})
export class ProjectCardComponent {
  @Input() title = '';
  @Input() detail = '';
  @Input() status = 'Draft';
  @Input() statusTone = 'draft';
  @Input() progress = 0;
  @Output() open = new EventEmitter<void>();
}
