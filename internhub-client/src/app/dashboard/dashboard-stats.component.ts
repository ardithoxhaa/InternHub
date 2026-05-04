import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-dashboard-stats',
  imports: [CommonModule],
  template: `
    <section class="builder-stats">
      <article *ngFor="let stat of stats" [class]="stat.tone">
        <small>{{ stat.label }}</small>
        <strong>{{ stat.value }}</strong>
        <span>{{ stat.detail }}</span>
      </article>
    </section>
  `
})
export class DashboardStatsComponent {
  @Input() stats: { label: string; value: string | number; detail: string; tone: string }[] = [];
}
