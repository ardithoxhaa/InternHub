import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';

type Tab = 'home' | 'mywork' | 'employees' | 'departments' | 'tasks' | 'assets' | 'calendar' | 'templates' | 'wizard' | 'invites' | 'reports' | 'accounts' | 'settings' | 'audit';

@Component({
  selector: 'app-sidebar',
  imports: [CommonModule],
  template: `
    <aside class="sidebar">
      <div class="brand"><span class="brand-mark">IH</span><div><strong>InternHub</strong><small>{{ role }} workspace</small></div></div>
      <nav>
        <button *ngFor="let item of visibleItems" [class.active]="activeTab === item.tab" (click)="navigate.emit(item.tab)">{{ item.label }}</button>
      </nav>
      <button class="refresh" (click)="refresh.emit()" [disabled]="loading">Refresh</button>
      <button class="ghost" (click)="openAi.emit()">AI Assistant</button>
      <button class="ghost" (click)="openTeam.emit()">Team Chat</button>
      <button class="ghost logout" (click)="logout.emit()">Sign out</button>
    </aside>
  `
})
export class SidebarComponent {
  @Input({ required: true }) activeTab!: Tab;
  @Input() role = 'User';
  @Input() canWrite = false;
  @Input() canAdmin = false;
  @Input() loading = false;
  @Output() navigate = new EventEmitter<Tab>();
  @Output() refresh = new EventEmitter<void>();
  @Output() openAi = new EventEmitter<void>();
  @Output() openTeam = new EventEmitter<void>();
  @Output() logout = new EventEmitter<void>();

  get visibleItems(): { tab: Tab; label: string }[] {
    return [
      { tab: 'home', label: 'Home' },
      { tab: 'mywork', label: 'My Workspace' },
      { tab: 'employees', label: 'Interns' },
      { tab: 'departments', label: 'Departments' },
      { tab: 'tasks', label: 'Tasks' },
      { tab: 'assets', label: 'Assets' },
      { tab: 'calendar', label: 'Calendar' },
      ...(this.canWrite ? [{ tab: 'wizard' as const, label: 'Launch Intern' }, { tab: 'templates' as const, label: 'Templates' }, { tab: 'reports' as const, label: 'Analytics' }] : []),
      ...(this.canAdmin ? [{ tab: 'invites' as const, label: 'Invites' }, { tab: 'accounts' as const, label: 'Members' }, { tab: 'settings' as const, label: 'Settings' }] : []),
      ...(this.canWrite ? [{ tab: 'audit' as const, label: 'Audit' }] : [])
    ];
  }
}
