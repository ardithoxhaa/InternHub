import { Component } from '@angular/core';

@Component({
  selector: 'app-how-it-works-section',
  template: `
    <section class="landing-section steps-section" id="templates">
      <div class="section-heading">
        <span>How it works</span>
        <h2>Launch an intern onboarding plan in three focused steps.</h2>
      </div>
      <div class="steps-grid">
        <article><b>1</b><h3>Create the profile</h3><p>Add the intern, department, role, start date, and account invite details.</p></article>
        <article><b>2</b><h3>Apply a template</h3><p>Generate onboarding tasks, assign owners, and attach required documents or assets.</p></article>
        <article><b>3</b><h3>Track readiness</h3><p>Monitor progress, blocked tasks, notifications, and audit history through completion.</p></article>
      </div>
    </section>
  `
})
export class HowItWorksSectionComponent {}
