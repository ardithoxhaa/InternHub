import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-feature-section',
  imports: [CommonModule],
  template: `
    <section class="landing-section" id="features">
      <div class="section-heading">
        <span>Features</span>
        <h2>Everything you need to take an intern from invite to completion.</h2>
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
    { badge: 'People', title: 'Intern profiles', detail: 'Keep roles, departments, dates, status, documents, and ownership in one place.' },
    { badge: 'Plans', title: 'Onboarding templates', detail: 'Start from reusable task plans tailored to each department or program.' },
    { badge: 'Tasks', title: 'Progress tracking', detail: 'Track due dates, priorities, blockers, notes, and completion across every intern.' },
    { badge: 'Assets', title: 'Equipment assignment', detail: 'Manage laptops, badges, and other company assets from assignment to return.' },
    { badge: 'Invites', title: 'Account invitations', detail: 'Create employee invites and keep access, notifications, and onboarding flow visible.' },
    { badge: 'Audit', title: 'Operations history', detail: 'Review notifications, updates, and audit records for a clearer timeline.' }
  ];
}
