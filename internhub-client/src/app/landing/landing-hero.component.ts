import { Component, EventEmitter, Output } from '@angular/core';

@Component({
  selector: 'app-landing-hero',
  template: `
    <section class="landing-hero" id="home">
      <div class="hero-copy">
        <span class="hero-kicker">Intern onboarding operations</span>
        <h1>Launch interns, track onboarding, and keep every first week moving.</h1>
        <p>InternHub gives teams a clean workspace for managing intern profiles, onboarding plans, tasks, documents, assets, invites, and progress from day one to completion.</p>
        <div class="hero-actions">
          <button type="button" (click)="start.emit()">Launch an Intern</button>
          <a href="#templates">View Onboarding Templates</a>
        </div>
      </div>
      <div class="hero-product" aria-label="Product preview">
        <div class="preview-top"><span></span><span></span><span></span></div>
        <div class="preview-grid">
          <aside><b>Workspace</b><i>Interns</i><i>Tasks</i><i>Assets</i></aside>
          <main><strong>Onboarding Plan</strong><div></div><div></div><div></div></main>
          <section><b>Readiness</b><em>Tasks ready</em><em>Docs checked</em><button type="button">Review</button></section>
        </div>
      </div>
    </section>
  `
})
export class LandingHeroComponent {
  @Output() start = new EventEmitter<void>();
}
