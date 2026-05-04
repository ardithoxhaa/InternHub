import { Component, EventEmitter, Output } from '@angular/core';

@Component({
  selector: 'app-landing-navbar',
  template: `
    <header class="landing-nav">
      <a class="landing-brand" href="#home" aria-label="InternHub home"><span>IH</span><b>InternHub</b></a>
      <nav>
        <a href="#home">Home</a>
        <a href="#features">Features</a>
        <a href="#templates">Templates</a>
        <a href="#pricing">About</a>
      </nav>
      <div class="landing-actions">
        <button type="button" class="nav-secondary" (click)="login.emit()">Login</button>
        <button type="button" (click)="register.emit()">Register</button>
      </div>
    </header>
  `
})
export class LandingNavbarComponent {
  @Output() login = new EventEmitter<void>();
  @Output() register = new EventEmitter<void>();
}
