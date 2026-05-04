import { Component, EventEmitter, Output } from '@angular/core';

@Component({
  selector: 'app-landing-hero',
  template: `
    <section class="landing-hero" id="home">
      <div class="hero-copy">
        <span class="hero-kicker">No-code site operations</span>
        <h1>Build, customize, preview, and publish modern websites without coding.</h1>
        <p>CreaStudio gives teams a clean workspace for launching pages, managing templates, tracking publishing readiness, and keeping every website project moving from idea to live.</p>
        <div class="hero-actions">
          <button type="button" (click)="start.emit()">Start Building</button>
          <a href="#templates">View Templates</a>
        </div>
      </div>
      <div class="hero-product" aria-label="Product preview">
        <div class="builder-top"><span></span><span></span><span></span></div>
        <div class="builder-grid">
          <aside><b>Pages</b><i>Home</i><i>Features</i><i>Pricing</i></aside>
          <main><strong>Homepage Canvas</strong><div></div><div></div><div></div></main>
          <section><b>Publish</b><em>SEO ready</em><em>Mobile ready</em><button type="button">Preview</button></section>
        </div>
      </div>
    </section>
  `
})
export class LandingHeroComponent {
  @Output() start = new EventEmitter<void>();
}
