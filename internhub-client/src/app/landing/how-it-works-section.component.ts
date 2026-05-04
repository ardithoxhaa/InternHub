import { Component } from '@angular/core';

@Component({
  selector: 'app-how-it-works-section',
  template: `
    <section class="landing-section steps-section" id="templates">
      <div class="section-heading">
        <span>How it works</span>
        <h2>Launch a professional website in three focused steps.</h2>
      </div>
      <div class="steps-grid">
        <article><b>1</b><h3>Choose a template</h3><p>Pick a professional starting point that matches your project.</p></article>
        <article><b>2</b><h3>Customize your website</h3><p>Edit content, structure, sections, SEO settings, and responsive behavior.</p></article>
        <article><b>3</b><h3>Preview and publish</h3><p>Review the final experience, check readiness, and publish with confidence.</p></article>
      </div>
    </section>
  `
})
export class HowItWorksSectionComponent {}
