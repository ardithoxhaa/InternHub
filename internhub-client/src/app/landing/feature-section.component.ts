import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-feature-section',
  imports: [CommonModule],
  template: `
    <section class="landing-section" id="features">
      <div class="section-heading">
        <span>Features</span>
        <h2>Everything you need to take a website from draft to launch.</h2>
      </div>
      <div class="feature-grid">
        <article *ngFor="let feature of features">
          <small>{{ feature.badge }}</small>
          <h3>{{ feature.title }}</h3>
          <p>{{ feature.detail }}</p>
        </article>
      </div>
    </section>
  `
})
export class FeatureSectionComponent {
  features = [
    { badge: 'Builder', title: 'Drag-and-drop builder', detail: 'Arrange sections, content, and page blocks without touching code.' },
    { badge: 'Design', title: 'Professional templates', detail: 'Start from polished layouts built for SaaS, portfolios, services, and campaigns.' },
    { badge: 'Responsive', title: 'Responsive editing', detail: 'Tune desktop and mobile layouts from the same workspace.' },
    { badge: 'Growth', title: 'SEO settings', detail: 'Manage metadata, page titles, descriptions, and launch readiness.' },
    { badge: 'Launch', title: 'Preview and publish', detail: 'Review work before publishing and keep launch checks visible.' },
    { badge: 'History', title: 'Version history', detail: 'Track changes and keep a clear timeline of project progress.' }
  ];
}
