import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';

type Tab = 'home' | 'mywork' | 'employees' | 'departments' | 'tasks' | 'assets' | 'calendar' | 'templates' | 'wizard' | 'invites' | 'reports' | 'accounts' | 'settings' | 'audit';

@Component({
  selector: 'app-sidebar',
  imports: [CommonModule],
  template: `
    <aside class="sidebar">
      <div class="brand"><span class="brand-mark">IH</span><div><strong>InternHub</strong><small>{{ role }} command center</small></div></div>
      <div class="sidebar-section-title">Workspace</div>
      <nav>
        <button *ngFor="let item of visibleItems" [class.active]="activeTab === item.tab" (click)="navigate.emit(item.tab)">
          <span class="nav-icon">{{ item.icon }}</span>
          <span>{{ item.label }}</span>
        </button>
      </nav>
      <div class="sidebar-actions">
        <button class="refresh" (click)="refresh.emit()" [disabled]="loading">Refresh data</button>
        <button class="ghost" (click)="openAi.emit()">AI Assistant</button>
        <button class="ghost" (click)="openTeam.emit()">Team Chat</button>
        <button class="ghost logout" (click)="logout.emit()">Sign out</button>
      </div>
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

  get visibleItems(): { tab: Tab; label: string; icon: string }[] {
    return [
      { tab: 'home', label: 'Command Center', icon: 'C' },
      { tab: 'mywork', label: 'My Workspace', icon: 'M' },
      { tab: 'employees', label: 'Interns', icon: 'I' },
      { tab: 'departments', label: 'Departments', icon: 'D' },
      { tab: 'tasks', label: 'Tasks', icon: 'T' },
      { tab: 'assets', label: 'Assets', icon: 'A' },
      { tab: 'calendar', label: 'Calendar', icon: 'K' },
      ...(this.canWrite ? [{ tab: 'wizard' as const, label: 'Launch Intern', icon: 'L' }, { tab: 'templates' as const, label: 'Templates', icon: 'P' }, { tab: 'reports' as const, label: 'Analytics', icon: 'R' }] : []),
      ...(this.canAdmin ? [{ tab: 'invites' as const, label: 'Invites', icon: 'V' }, { tab: 'accounts' as const, label: 'Members', icon: 'U' }, { tab: 'settings' as const, label: 'Settings', icon: 'S' }] : []),
      ...(this.canWrite ? [{ tab: 'audit' as const, label: 'Audit', icon: 'H' }] : [])
    ];
  }
}
