import { Component, EventEmitter, Output } from '@angular/core';

@Component({
  selector: 'app-landing-footer',
  template: `
    <footer class="landing-footer" id="pricing">
      <div><b>InternHub</b><p>A modern workspace for launching interns, tracking onboarding, and keeping operations organized.</p></div>
      <button type="button" (click)="start.emit()">Launch Intern</button>
    </footer>
  `
})
export class LandingFooterComponent {
  @Output() start = new EventEmitter<void>();
}
