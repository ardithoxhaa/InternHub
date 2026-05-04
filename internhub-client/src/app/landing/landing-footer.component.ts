import { Component, EventEmitter, Output } from '@angular/core';

@Component({
  selector: 'app-landing-footer',
  template: `
    <footer class="landing-footer" id="pricing">
      <div><b>CreaStudio</b><p>A modern workspace for building, previewing, and launching professional web experiences.</p></div>
      <button type="button" (click)="start.emit()">Start Building</button>
    </footer>
  `
})
export class LandingFooterComponent {
  @Output() start = new EventEmitter<void>();
}
