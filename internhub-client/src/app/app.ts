import { CommonModule } from '@angular/common';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Component, OnInit, ViewEncapsulation, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import * as signalR from '@microsoft/signalr';
import { forkJoin } from 'rxjs';
import { InternHubApiService } from './core/internhub-api.service';
import { DashboardStatsComponent } from './dashboard/dashboard-stats.component';
import { InternCardComponent } from './dashboard/intern-card.component';
import { FeatureSectionComponent } from './landing/feature-section.component';
import { HowItWorksSectionComponent } from './landing/how-it-works-section.component';
import { LandingFooterComponent } from './landing/landing-footer.component';
import { LandingHeroComponent } from './landing/landing-hero.component';
import { LandingNavbarComponent } from './landing/landing-navbar.component';
import { SidebarComponent } from './layout/sidebar.component';
import { EmptyStateComponent } from './shared/empty-state.component';

type Tab = 'home' | 'mywork' | 'employees' | 'departments' | 'tasks' | 'assets' | 'calendar' | 'templates' | 'wizard' | 'invites' | 'reports' | 'accounts' | 'settings' | 'audit';
type EmploymentStatus = 'Candidate' | 'Onboarding' | 'Active' | 'Completed' | 'OnLeave' | 'Exited';
type TaskPriority = 'Low' | 'Medium' | 'High' | 'Critical';
type TaskStatus = 'ToDo' | 'InProgress' | 'Done' | 'Blocked';
type AssetCondition = 'New' | 'Good' | 'NeedsRepair' | 'Retired';
type AssetStatus = 'Available' | 'Assigned' | 'Returned' | 'Retired' | 'Lost';
type WorkTone = 'late' | 'critical' | 'blocked' | 'normal';

interface AuthUser { token: string; fullName: string; email: string; role: string; }
interface Dashboard { employees: number; departments: number; openTasks: number; overdueTasks: number; assets: number; assetValue: number; completionRate: number; departmentLoad: { department: string; employees: number }[]; upcomingTasks: { id: number; title: string; dueDate: string; employeeName: string; priority: string; status: string }[]; upcomingStarts: { id: number; employeeName: string; startDate: string; department: string }[]; }
interface Department { id: number; name: string; code: string; leadName: string; description?: string; budget: number; employeeCount: number; }
interface Employee { id: number; firstName: string; lastName: string; fullName: string; email: string; role: string; startDate: string; endDate?: string; status: EmploymentStatus; departmentId: number; departmentName: string; openTaskCount: number; assetCount: number; }
interface OnboardingTask { id: number; title: string; notes?: string; dueDate: string; priority: TaskPriority; status: TaskStatus; employeeId: number; employeeName: string; }
interface CompanyAsset { id: number; tag: string; name: string; category: string; value: number; assignedDate: string; returnDate?: string; status: AssetStatus; condition: AssetCondition; employeeId?: number; employeeName?: string; }
interface EmployeeDocument { id: number; fileName: string; documentType: string; sizeBytes: number; uploadedAt: string; approvalStatus: string; reviewedBy?: string; reviewedAt?: string; rejectionReason?: string; employeeId: number; }
interface NotificationItem { id: number; recipientEmail: string; subject: string; body: string; status: string; createdAt: string; sentAt?: string; isRead: boolean; employeeId?: number; }
interface AuditLog { id: number; actor: string; action: string; entityName: string; entityId?: number; details?: string; createdAt: string; }
interface CalendarItem { id: number; title: string; date: string; type: string; status: string; owner: string; }
interface OnboardingTemplate { id: number; name: string; departmentScope: string; isActive: boolean; items: { id: number; title: string; notes?: string; dueOffsetDays: number; priority: string }[]; }
interface TaskComment { id: number; author: string; body: string; createdAt: string; onboardingTaskId: number; }
interface EmployeeProfile { employee: Employee; tasks: OnboardingTask[]; assets: CompanyAsset[]; documents: EmployeeDocument[]; notifications: NotificationItem[]; }
interface MyWork { employee?: Employee; tasks: OnboardingTask[]; assets: CompanyAsset[]; documents: EmployeeDocument[]; notifications: NotificationItem[]; }
interface ChatMessage { role: 'user' | 'assistant'; content: string; usedOpenAi?: boolean; }
interface RoleHome { role: string; focusItems: string[]; metrics: Record<string, number>; }
interface SearchResult { type: string; id: number; title: string; subtitle: string; }
interface SettingsStatus { emailEnabled: boolean; openAiEnabled: boolean; openAiModel: string; companyName: string; }
interface TeamChatMessage { id: number; senderName: string; senderEmail: string; body: string; createdAt: string; }
interface Invite { id: number; email: string; fullName: string; role: string; token: string; createdAt: string; expiresAt: string; acceptedAt?: string; employeeId?: number; }
interface CompanySettings { companyName: string; defaultTemplateId?: number | null; reminderFrequencyDays: number; emailSenderName: string; }
interface Analytics { departmentProgress: { department: string; done: number; total: number; rate: number }[]; taskTrend: { date: string; created: number; due: number; completed: number }[]; assetsByCategory: { category: string; count: number; value: number }[]; employeeProgress: { employeeId: number; employeeName: string; department: string; done: number; total: number; rate: number }[]; pendingDocuments: number; }
interface JourneyPhase { key: string; title: string; signal: string; employees: (Employee & { progress: number; late: number; nextTask?: OnboardingTask })[]; }
interface ActionQueueItem { type: 'task' | 'employee' | 'asset' | 'document'; tone: WorkTone; title: string; detail: string; action: string; targetId?: number; }
interface InternSummaryCard { id: number; title: string; detail: string; status: string; statusTone: string; progress: number; }

@Component({
  selector: 'app-root',
  imports: [CommonModule, ReactiveFormsModule, SidebarComponent, EmptyStateComponent, LandingNavbarComponent, LandingHeroComponent, FeatureSectionComponent, HowItWorksSectionComponent, LandingFooterComponent, DashboardStatsComponent, InternCardComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss',
  encapsulation: ViewEncapsulation.None
})
export class App implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly fb = inject(FormBuilder);
  private readonly internHub = inject(InternHubApiService);
  private readonly api = this.internHub.api;
  private readonly hubUrl = this.internHub.hubUrl;
  private chatConnection?: signalR.HubConnection;

  activeTab = signal<Tab>('home');
  loading = signal(false);
  message = signal('');
  editingId = signal<number | null>(null);
  showRegister = signal(false);
  authPanelOpen = signal(false);
  user = signal<AuthUser | null>(this.restoreUser());
  profile = signal<EmployeeProfile | null>(null);
  profileTimeline = signal<AuditLog[]>([]);

  dashboard = signal<Dashboard | null>(null);
  departments = signal<Department[]>([]);
  employees = signal<Employee[]>([]);
  tasks = signal<OnboardingTask[]>([]);
  assets = signal<CompanyAsset[]>([]);
  notifications = signal<NotificationItem[]>([]);
  audit = signal<AuditLog[]>([]);
  calendar = signal<CalendarItem[]>([]);
  templates = signal<OnboardingTemplate[]>([]);
  myWork = signal<MyWork | null>(null);
  comments = signal<TaskComment[]>([]);
  selectedTask = signal<OnboardingTask | null>(null);
  chatOpen = signal(false);
  chatBusy = signal(false);
  chatMessages = signal<ChatMessage[]>([
    { role: 'assistant', content: 'Hi, I am InternHub Assistant. Ask me about onboarding status, overdue tasks, email setup, assets, or what to do next.' }
  ]);
  teamChatOpen = signal(false);
  teamMessages = signal<TeamChatMessage[]>([]);
  roleHome = signal<RoleHome | null>(null);
  settings = signal<SettingsStatus | null>(null);
  companySettings = signal<CompanySettings | null>(null);
  analytics = signal<Analytics | null>(null);
  invites = signal<Invite[]>([]);
  searchResults = signal<SearchResult[]>([]);
  selectedTemplateId = signal<number | null>(null);
  calendarMonth = signal(this.today().slice(0, 7));
  rejectionReason = signal('');
  wizardStep = signal(1);
  wizardBusy = signal(false);

  employeeSearch = signal('');
  statusFilter = signal('');
  taskStatusFilter = signal('');
  taskPriorityFilter = signal('');

  readonly employeeStatuses: EmploymentStatus[] = ['Onboarding', 'Active', 'Completed', 'OnLeave', 'Exited'] as EmploymentStatus[];
  readonly taskPriorities: TaskPriority[] = ['Low', 'Medium', 'High', 'Critical'];
  readonly taskStatuses: TaskStatus[] = ['ToDo', 'InProgress', 'Done', 'Blocked'];
  readonly assetConditions: AssetCondition[] = ['New', 'Good', 'NeedsRepair', 'Retired'];
  readonly assetStatuses: AssetStatus[] = ['Available', 'Assigned', 'Returned', 'Retired', 'Lost'];
  readonly accountRoles = ['Intern', 'Manager', 'HR', 'Admin'];

  loginForm = this.fb.nonNullable.group({
    email: ['admin@internhub.test', [Validators.required, Validators.email]],
    password: ['Admin123!', Validators.required]
  });

  registerForm = this.fb.nonNullable.group({
    fullName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
    role: ['Intern', Validators.required]
  });

  employeeForm = this.fb.nonNullable.group({
    firstName: ['', [Validators.required, Validators.maxLength(80)]],
    lastName: ['', [Validators.required, Validators.maxLength(80)]],
    email: ['', [Validators.required, Validators.email]],
    role: ['', Validators.required],
    startDate: [this.today(), Validators.required],
    endDate: [''],
    status: ['Onboarding' as EmploymentStatus, Validators.required],
    departmentId: [0, [Validators.required, Validators.min(1)]]
  });

  departmentForm = this.fb.nonNullable.group({
    name: ['', Validators.required],
    code: ['', Validators.required],
    leadName: ['', Validators.required],
    description: [''],
    budget: [0, [Validators.required, Validators.min(0)]]
  });

  taskForm = this.fb.nonNullable.group({
    title: ['', Validators.required],
    notes: [''],
    dueDate: [this.today(), Validators.required],
    priority: ['Medium' as TaskPriority, Validators.required],
    status: ['ToDo' as TaskStatus, Validators.required],
    employeeId: [0, [Validators.required, Validators.min(1)]]
  });

  assetForm = this.fb.nonNullable.group({
    tag: ['', Validators.required],
    name: ['', Validators.required],
    category: ['Laptop', Validators.required],
    value: [0, [Validators.required, Validators.min(0)]],
    assignedDate: [this.today(), Validators.required],
    returnDate: [''],
    status: ['Assigned' as AssetStatus, Validators.required],
    condition: ['Good' as AssetCondition, Validators.required],
    employeeId: [0]
  });

  documentForm = this.fb.nonNullable.group({
    documentType: ['Contract', Validators.required]
  });

  commentForm = this.fb.nonNullable.group({
    body: ['', Validators.required]
  });

  templateForm = this.fb.nonNullable.group({
    name: ['Standard Intern Onboarding', Validators.required],
    departmentScope: ['All', Validators.required],
    isActive: [true],
    itemsText: ['Sign internship agreement|-1|Critical|Upload the signed agreement\nComplete security and privacy training|1|High|Required before access is granted', Validators.required]
  });

  chatForm = this.fb.nonNullable.group({
    message: ['', Validators.required]
  });

  teamChatForm = this.fb.nonNullable.group({
    body: ['', Validators.required]
  });

  searchForm = this.fb.nonNullable.group({
    q: ['']
  });

  inviteForm = this.fb.nonNullable.group({
    employeeId: [0, [Validators.required, Validators.min(1)]],
    role: ['Intern', Validators.required]
  });

  settingsForm = this.fb.nonNullable.group({
    companyName: ['InternHub', Validators.required],
    defaultTemplateId: [0],
    reminderFrequencyDays: [3, [Validators.required, Validators.min(1)]],
    emailSenderName: ['InternHub People Team', Validators.required]
  });

  launchForm = this.fb.nonNullable.group({
    firstName: ['', [Validators.required, Validators.maxLength(80)]],
    lastName: ['', [Validators.required, Validators.maxLength(80)]],
    email: ['', [Validators.required, Validators.email]],
    role: ['Software Engineering Intern', Validators.required],
    startDate: [this.today(), Validators.required],
    departmentId: [0, [Validators.required, Validators.min(1)]],
    templateId: [0],
    inviteRole: ['Intern', Validators.required],
    assetTag: [''],
    assetName: [''],
    assetCategory: ['Laptop'],
    assetValue: [0]
  });

  filteredEmployees = computed(() => {
    const search = this.employeeSearch().toLowerCase();
    const status = this.statusFilter();
    return this.employees().filter(e =>
      (!status || e.status === status) &&
      (!search || `${e.fullName} ${e.email} ${e.role} ${e.departmentName}`.toLowerCase().includes(search)));
  });

  filteredTasks = computed(() => {
    const status = this.taskStatusFilter();
    const priority = this.taskPriorityFilter();
    return this.tasks().filter(t => (!status || t.status === status) && (!priority || t.priority === priority));
  });

  maxDepartmentCount = computed(() => Math.max(1, ...this.dashboard()?.departmentLoad.map(d => d.employees) ?? [1]));
  dueSoonTasks = computed(() => this.tasks()
    .filter(t => t.status !== 'Done')
    .sort((a, b) => new Date(a.dueDate).getTime() - new Date(b.dueDate).getTime())
    .slice(0, 8));
  blockedTasks = computed(() => this.tasks().filter(t => t.status === 'Blocked'));
  criticalOpenTasks = computed(() => this.tasks().filter(t => t.status !== 'Done' && (t.priority === 'Critical' || this.isOverdue(t.dueDate))));
  onboardingStats = computed(() => {
    const published = this.employees().filter(e => e.status === 'Active' || e.status === 'Completed').length;
    const drafts = Math.max(0, this.employees().length - published);
    const total = Math.max(this.employees().length, published + drafts);
    return [
      { label: 'Total interns', value: total, detail: 'Profiles in onboarding', tone: 'blue' },
      { label: 'Active interns', value: published, detail: 'Active or completed profiles', tone: 'green' },
      { label: 'New interns', value: drafts, detail: 'Awaiting more progress', tone: 'amber' },
      { label: 'Templates available', value: Math.max(this.templates().length, 6), detail: 'Reusable onboarding plans', tone: 'violet' }
    ];
  });
  recentInternCards = computed<InternSummaryCard[]>(() => this.employees().slice(0, 6).map(employee => {
    const progress = this.employeeProgress(employee.id);
    const published = employee.status === 'Active' || employee.status === 'Completed' || progress >= 85;
    return {
      id: employee.id,
      title: employee.fullName,
      detail: `${employee.fullName} - ${employee.role}`,
      status: published ? 'On track' : progress > 0 ? 'In progress' : 'New',
      statusTone: published ? 'published' : progress > 0 ? 'draft' : 'new',
      progress
    };
  }));
  actionQueue = computed<ActionQueueItem[]>(() => {
    const taskActions = this.tasks()
      .filter(t => t.status !== 'Done' && (t.status === 'Blocked' || this.isOverdue(t.dueDate) || t.priority === 'Critical'))
      .sort((a, b) => Number(b.status === 'Blocked') - Number(a.status === 'Blocked') || new Date(a.dueDate).getTime() - new Date(b.dueDate).getTime())
      .slice(0, 4)
      .map(task => ({
        type: 'task' as const,
        tone: this.taskTone(task),
        title: task.title,
        detail: `${task.employeeName} - ${this.dueLabel(task)} - ${task.priority}`,
        action: task.status === 'Blocked' ? 'Open blocker' : 'Review task',
        targetId: task.id
      }));

    const employeeActions = this.atRiskEmployees().slice(0, 3).map(employee => ({
      type: 'employee' as const,
      tone: employee.late ? 'late' as const : 'normal' as const,
      title: employee.fullName,
      detail: `${employee.departmentName} - ${employee.progress}% complete - ${employee.openTaskCount} open`,
      action: 'Open profile',
      targetId: employee.id
    }));

    const assetActions = this.assets()
      .filter(asset => asset.condition === 'NeedsRepair' || asset.status === 'Lost')
      .slice(0, 2)
      .map(asset => ({
        type: 'asset' as const,
        tone: asset.status === 'Lost' ? 'late' as const : 'critical' as const,
        title: `${asset.tag} - ${asset.name}`,
        detail: `${asset.category} - ${asset.status} - ${asset.condition}`,
        action: 'Review asset',
        targetId: asset.id
      }));

    return [...taskActions, ...employeeActions, ...assetActions].slice(0, 8);
  });
  atRiskEmployees = computed(() => this.employees()
    .filter(e => e.status === 'Onboarding' || e.openTaskCount > 0)
    .map(e => ({ ...e, progress: this.employeeProgress(e.id), late: this.tasks().filter(t => t.employeeId === e.id && t.status !== 'Done' && this.isOverdue(t.dueDate)).length }))
    .sort((a, b) => b.late - a.late || b.openTaskCount - a.openTaskCount)
    .slice(0, 8));
  journeyMap = computed<JourneyPhase[]>(() => {
    const phases: JourneyPhase[] = [
      { key: 'preboarding', title: 'Preboarding', signal: 'Before day one', employees: [] },
      { key: 'launch', title: 'Launch week', signal: 'First 7 days', employees: [] },
      { key: 'momentum', title: 'Momentum', signal: 'Active work', employees: [] },
      { key: 'ready', title: 'Ready to close', signal: 'Nearly complete', employees: [] }
    ];

    for (const employee of this.employees()) {
      const progress = this.employeeProgress(employee.id);
      const late = this.tasks().filter(t => t.employeeId === employee.id && t.status !== 'Done' && this.isOverdue(t.dueDate)).length;
      const nextTask = this.tasks()
        .filter(t => t.employeeId === employee.id && t.status !== 'Done')
        .sort((a, b) => new Date(a.dueDate).getTime() - new Date(b.dueDate).getTime())[0];
      const enriched = { ...employee, progress, late, nextTask };
      const daysFromStart = this.daysFromToday(employee.startDate) * -1;

      if (employee.status === 'Completed' || progress >= 90) {
        phases[3].employees.push(enriched);
      } else if (daysFromStart < 0) {
        phases[0].employees.push(enriched);
      } else if (daysFromStart <= 7) {
        phases[1].employees.push(enriched);
      } else {
        phases[2].employees.push(enriched);
      }
    }

    return phases.map(phase => ({
      ...phase,
      employees: phase.employees
        .sort((a, b) => b.late - a.late || a.progress - b.progress)
        .slice(0, 4)
    }));
  });
  assetSummary = computed(() => ({
    assigned: this.assets().filter(a => a.status === 'Assigned').length,
    available: this.assets().filter(a => a.status === 'Available').length,
    needsRepair: this.assets().filter(a => a.condition === 'NeedsRepair').length,
    returned: this.assets().filter(a => a.status === 'Returned').length
  }));
  calendarDays = computed(() => {
    const [year, month] = this.calendarMonth().split('-').map(Number);
    const first = new Date(year, month - 1, 1);
    const start = new Date(first);
    start.setDate(first.getDate() - first.getDay());
    return Array.from({ length: 42 }, (_, index) => {
      const date = new Date(start);
      date.setDate(start.getDate() + index);
      const iso = date.toISOString().slice(0, 10);
      return {
        date: iso,
        day: date.getDate(),
        muted: date.getMonth() !== month - 1,
        items: this.calendar().filter(item => item.date === iso)
      };
    });
  });
  canWrite = computed(() => ['Admin', 'HR', 'Manager'].includes(this.user()?.role ?? ''));
  canAdmin = computed(() => ['Admin', 'HR'].includes(this.user()?.role ?? ''));
  profileHealth = computed(() => {
    const profile = this.profile();
    if (!profile) return null;
    const total = profile.tasks.length;
    const done = profile.tasks.filter(t => t.status === 'Done').length;
    const late = profile.tasks.filter(t => t.status !== 'Done' && this.isOverdue(t.dueDate)).length;
    const blocked = profile.tasks.filter(t => t.status === 'Blocked').length;
    const progress = total ? Math.round(done / total * 100) : 0;
    const score = Math.max(0, Math.min(100, progress - late * 12 - blocked * 8));
    const nextTask = profile.tasks.filter(t => t.status !== 'Done').sort((a, b) => new Date(a.dueDate).getTime() - new Date(b.dueDate).getTime())[0];
    return { total, done, late, blocked, progress, score, nextTask };
  });

  ngOnInit(): void {
    if (this.user()) {
      this.loadAll();
      this.startTeamChat();
    }
  }

  login(): void {
    this.http.post<AuthUser>(`${this.api}/auth/login`, this.loginForm.getRawValue()).subscribe({
      next: user => {
        localStorage.setItem('internhub-user', JSON.stringify(user));
        this.user.set(user);
        this.message.set(`Welcome, ${user.fullName}.`);
        this.loadAll();
        this.startTeamChat();
      },
      error: err => this.message.set(err.error || 'Login failed.')
    });
  }

  register(): void {
    const options = this.user() ? this.auth() : {};
    this.http.post<AuthUser>(`${this.api}/auth/register`, this.registerForm.getRawValue(), options).subscribe({
      next: account => {
        this.message.set(this.user() ? `Account created for ${account.fullName}.` : `Account created. Welcome, ${account.fullName}.`);
        if (!this.user()) {
          localStorage.setItem('internhub-user', JSON.stringify(account));
          this.user.set(account);
          this.loadAll();
          this.startTeamChat();
        }
        this.registerForm.reset({ fullName: '', email: '', password: '', role: 'Intern' });
        this.showRegister.set(false);
      },
      error: err => this.message.set(err.error || 'Registration failed.')
    });
  }

  logout(): void {
    localStorage.removeItem('internhub-user');
    this.user.set(null);
    this.profile.set(null);
    this.chatConnection?.stop();
  }

  loadAll(): void {
    this.loading.set(true);
    this.message.set('');
    this.http.get<Dashboard>(`${this.api}/dashboard`, this.auth()).subscribe(d => this.dashboard.set(d));
    this.http.get<Department[]>(`${this.api}/departments`, this.auth()).subscribe(d => this.departments.set(d));
    this.http.get<Employee[]>(`${this.api}/employees`, this.auth()).subscribe(e => this.employees.set(e));
    this.http.get<OnboardingTask[]>(`${this.api}/onboardingtasks`, this.auth()).subscribe(t => this.tasks.set(t));
    this.http.get<CompanyAsset[]>(`${this.api}/companyassets`, this.auth()).subscribe(a => this.assets.set(a));
    this.http.get<CalendarItem[]>(`${this.api}/calendar`, this.auth()).subscribe(c => this.calendar.set(c));
    this.http.get<MyWork>(`${this.api}/my-work`, this.auth()).subscribe(w => this.myWork.set(w));
    this.http.get<RoleHome>(`${this.api}/product/home`, this.auth()).subscribe(h => this.roleHome.set(h));
    this.http.get<TeamChatMessage[]>(`${this.api}/product/chat/history`, this.auth()).subscribe(m => this.teamMessages.set(m));
    if (this.canWrite()) {
      this.http.get<NotificationItem[]>(`${this.api}/notifications`, this.auth()).subscribe(n => this.notifications.set(n));
      this.http.get<AuditLog[]>(`${this.api}/audit`, this.auth()).subscribe(a => this.audit.set(a));
      this.http.get<OnboardingTemplate[]>(`${this.api}/onboarding-templates`, this.auth()).subscribe(t => this.templates.set(t));
      this.http.get<Analytics>(`${this.api}/product/analytics`, this.auth()).subscribe(a => this.analytics.set(a));
      this.http.get<SettingsStatus>(`${this.api}/product/settings-status`, this.auth()).subscribe(s => this.settings.set(s));
      if (this.canAdmin()) {
        this.http.get<Invite[]>(`${this.api}/product/invites`, this.auth()).subscribe(i => this.invites.set(i));
        this.http.get<CompanySettings>(`${this.api}/product/settings`, this.auth()).subscribe(s => {
          this.companySettings.set(s);
          this.settingsForm.patchValue({
            companyName: s.companyName,
            defaultTemplateId: s.defaultTemplateId ?? 0,
            reminderFrequencyDays: s.reminderFrequencyDays,
            emailSenderName: s.emailSenderName
          });
        });
      }
    }
    this.loading.set(false);
  }

  globalSearch(): void {
    const q = this.searchForm.getRawValue().q;
    if (!q.trim()) {
      this.searchResults.set([]);
      return;
    }
    this.http.get<SearchResult[]>(`${this.api}/product/search?q=${encodeURIComponent(q)}`, this.auth()).subscribe(r => this.searchResults.set(r));
  }

  openSearchResult(result: SearchResult): void {
    if (result.type === 'Employee') {
      const employee = this.employees().find(e => e.id === result.id);
      if (employee) {
        this.setTab('employees');
        this.openProfile(employee);
      }
      return;
    }

    if (result.type === 'Task') {
      const task = this.tasks().find(t => t.id === result.id);
      if (task) {
        this.setTab('tasks');
        this.taskStatusFilter.set(task.status);
        this.taskPriorityFilter.set('');
        this.openTaskComments(task);
      }
      return;
    }

    if (result.type === 'Asset') {
      this.setTab('assets');
    }
  }

  runReminders(): void {
    this.http.post<{ reminders: number }>(`${this.api}/product/run-reminders`, {}, this.auth()).subscribe({
      next: result => {
        this.message.set(`${result.reminders} reminders queued or sent.`);
        this.loadAll();
      },
      error: err => this.message.set(err.error || 'Reminder run failed.')
    });
  }

  setTab(tab: Tab): void {
    this.activeTab.set(tab);
    this.profile.set(null);
    this.profileTimeline.set([]);
    this.selectedTask.set(null);
    this.comments.set([]);
    this.cancelEdit();
  }

  showLogin(): void {
    this.showRegister.set(false);
    this.authPanelOpen.set(true);
  }

  showSignup(): void {
    this.showRegister.set(true);
    this.authPanelOpen.set(true);
  }

  openProfile(employee: Employee): void {
    this.selectedTask.set(null);
    this.comments.set([]);
    this.profileTimeline.set([]);
    this.http.get<EmployeeProfile>(`${this.api}/employees/${employee.id}/profile`, this.auth()).subscribe(p => this.profile.set(p));
    this.http.get<AuditLog[]>(`${this.api}/product/employees/${employee.id}/timeline`, this.auth()).subscribe(t => this.profileTimeline.set(t));
  }

  openEmployeeById(employeeId: number): void {
    const employee = this.employees().find(e => e.id === employeeId);
    if (employee) {
      this.activeTab.set('employees');
      this.cancelEdit();
      this.openProfile(employee);
    }
  }

  openAction(item: ActionQueueItem): void {
    if (item.type === 'task') {
      const task = this.tasks().find(t => t.id === item.targetId);
      if (task) {
        this.setTab('tasks');
        this.openTaskComments(task);
      }
      return;
    }

    if (item.type === 'employee') {
      this.openEmployeeById(item.targetId ?? 0);
      return;
    }

    if (item.type === 'asset') {
      this.setTab('assets');
    }
  }

  continueEditing(): void {
    const intern = this.recentInternCards()[0];
    if (intern) {
      this.openEmployeeById(intern.id);
      return;
    }

    this.setTab('wizard');
  }

  closeProfile(): void {
    this.profile.set(null);
    this.profileTimeline.set([]);
    this.selectedTask.set(null);
  }

  sendWelcome(employeeId: number): void {
    this.http.post(`${this.api}/employees/${employeeId}/welcome-email`, {}, this.auth()).subscribe({
      next: () => {
        this.message.set('Welcome email logged as sent.');
        this.loadAll();
        const employee = this.employees().find(e => e.id === employeeId);
        if (employee) this.openProfile(employee);
      },
      error: err => this.message.set(err.error || 'Could not send email.')
    });
  }

  generateOnboardingPlan(employeeId: number): void {
    const templateId = this.selectedTemplateId();
    const query = templateId ? `?templateId=${templateId}` : '';
    this.http.post(`${this.api}/employees/${employeeId}/onboarding-plan${query}`, {}, this.auth()).subscribe({
      next: () => {
        this.message.set('Onboarding checklist generated.');
        this.loadAll();
        const employee = this.employees().find(e => e.id === employeeId);
        if (employee) this.openProfile(employee);
      },
      error: err => this.message.set(err.error || 'Could not generate onboarding plan.')
    });
  }

  exportReport(type: 'employees' | 'tasks'): void {
    this.http.get(`${this.api}/reports/${type}`, { ...this.auth(), responseType: 'blob' }).subscribe(blob => {
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `${type}-report.csv`;
      link.click();
      URL.revokeObjectURL(url);
    });
  }

  exportAdvancedReport(type: 'employees' | 'tasks' | 'assets' | 'progress'): void {
    this.http.get(`${this.api}/reports/${type}`, { ...this.auth(), responseType: 'blob' }).subscribe(blob => {
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `${type}-report.csv`;
      link.click();
      URL.revokeObjectURL(url);
    });
  }

  markNotificationRead(id: number): void {
    this.http.patch(`${this.api}/notifications/${id}/read`, {}, this.auth()).subscribe(() => this.loadAll());
  }

  reviewDocument(documentId: number, status: 'Approved' | 'Rejected'): void {
    this.http.patch(`${this.api}/documents/${documentId}/approval/${status}`, {}, this.auth()).subscribe({
      next: () => {
        this.message.set(`Document ${status.toLowerCase()}.`);
        this.loadAll();
        const employee = this.profile()?.employee;
        if (employee) this.openProfile(employee);
      },
      error: err => this.message.set(err.error || 'Document review failed.')
    });
  }

  returnAsset(assetId: number): void {
    this.http.patch(`${this.api}/assets/${assetId}/return`, {}, this.auth()).subscribe({
      next: () => {
        this.message.set('Asset marked returned.');
        this.loadAll();
      },
      error: err => this.message.set(err.error || 'Asset return failed.')
    });
  }

  changeLifecycle(employeeId: number, status: EmploymentStatus): void {
    this.http.patch(`${this.api}/employees/${employeeId}/lifecycle/${status}`, {}, this.auth()).subscribe({
      next: () => {
        this.message.set(`Employee marked ${status}.`);
        this.loadAll();
      },
      error: err => this.message.set(err.error || 'Lifecycle update failed.')
    });
  }

  saveTemplate(): void {
    const value = this.templateForm.getRawValue();
    const items = value.itemsText.split('\n').filter(Boolean).map(line => {
      const [title, dueOffsetDays, priority, notes] = line.split('|');
      return { title, dueOffsetDays: Number(dueOffsetDays ?? 0), priority: priority || 'Medium', notes: notes || '' };
    });
    this.http.post(`${this.api}/onboarding-templates`, { name: value.name, departmentScope: value.departmentScope, isActive: value.isActive, items }, this.auth()).subscribe({
      next: () => {
        this.message.set('Template created.');
        this.loadAll();
      },
      error: err => this.message.set(err.error || 'Template save failed.')
    });
  }

  createInvite(): void {
    if (this.inviteForm.invalid) return;
    this.http.post<Invite>(`${this.api}/product/invites`, this.inviteForm.getRawValue(), this.auth()).subscribe({
      next: invite => {
        this.message.set(`Invite created for ${invite.fullName}.`);
        this.invites.set([invite, ...this.invites()]);
      },
      error: err => this.message.set(err.error || 'Invite could not be created.')
    });
  }

  nextWizardStep(): void {
    this.wizardStep.set(Math.min(4, this.wizardStep() + 1));
  }

  previousWizardStep(): void {
    this.wizardStep.set(Math.max(1, this.wizardStep() - 1));
  }

  launchIntern(): void {
    if (this.launchForm.invalid || this.wizardBusy()) return;
    this.wizardBusy.set(true);
    const value = this.launchForm.getRawValue();
    const employeeBody = {
      firstName: value.firstName,
      lastName: value.lastName,
      email: value.email,
      role: value.role,
      startDate: value.startDate,
      endDate: null,
      status: 'Candidate',
      departmentId: value.departmentId
    };

    this.http.post<Employee>(`${this.api}/employees`, employeeBody, this.auth()).subscribe({
      next: employee => {
        const jobs = [
          this.http.post(`${this.api}/employees/${employee.id}/onboarding-plan${value.templateId ? `?templateId=${value.templateId}` : ''}`, {}, this.auth()),
          this.http.post(`${this.api}/product/invites`, { employeeId: employee.id, role: value.inviteRole }, this.auth())
        ];

        if (value.assetTag.trim() && value.assetName.trim()) {
          jobs.push(this.http.post(`${this.api}/companyassets`, {
            tag: value.assetTag,
            name: value.assetName,
            category: value.assetCategory,
            value: value.assetValue,
            assignedDate: value.startDate,
            returnDate: null,
            status: 'Assigned',
            condition: 'Good',
            employeeId: employee.id
          }, this.auth()));
        }

        forkJoin(jobs).subscribe({
          next: () => {
            this.message.set(`${employee.fullName} launched with plan, invite, and setup workflow.`);
            this.wizardBusy.set(false);
            this.wizardStep.set(1);
            this.launchForm.reset({
              firstName: '',
              lastName: '',
              email: '',
              role: 'Software Engineering Intern',
              startDate: this.today(),
              departmentId: this.departments()[0]?.id ?? 0,
              templateId: this.templates()[0]?.id ?? 0,
              inviteRole: 'Intern',
              assetTag: '',
              assetName: '',
              assetCategory: 'Laptop',
              assetValue: 0
            });
            this.loadAll();
          },
          error: err => {
            this.wizardBusy.set(false);
            this.message.set(err.error || 'Intern was created, but launch steps need review.');
            this.loadAll();
          }
        });
      },
      error: err => {
        this.wizardBusy.set(false);
        this.message.set(err.error || 'Could not create intern.');
      }
    });
  }

  saveSettings(): void {
    if (this.settingsForm.invalid) return;
    const value = this.settingsForm.getRawValue();
    const body: CompanySettings = {
      companyName: value.companyName,
      defaultTemplateId: value.defaultTemplateId || null,
      reminderFrequencyDays: value.reminderFrequencyDays,
      emailSenderName: value.emailSenderName
    };
    this.http.put(`${this.api}/product/settings`, body, this.auth()).subscribe({
      next: () => {
        this.message.set('Settings saved.');
        this.companySettings.set(body);
      },
      error: err => this.message.set(err.error || 'Settings could not be saved.')
    });
  }

  reviewDocumentWithReason(documentId: number, status: 'Approved' | 'Rejected'): void {
    this.http.patch(`${this.api}/documents/${documentId}/review`, { status, reason: status === 'Rejected' ? this.rejectionReason() : null }, this.auth()).subscribe({
      next: () => {
        this.message.set(`Document ${status.toLowerCase()}.`);
        this.rejectionReason.set('');
        this.loadAll();
        const employee = this.profile()?.employee;
        if (employee) this.openProfile(employee);
      },
      error: err => this.message.set(err.error || 'Document review failed.')
    });
  }

  previousMonth(): void {
    const [year, month] = this.calendarMonth().split('-').map(Number);
    const date = new Date(year, month - 2, 1);
    this.calendarMonth.set(date.toISOString().slice(0, 7));
  }

  nextMonth(): void {
    const [year, month] = this.calendarMonth().split('-').map(Number);
    const date = new Date(year, month, 1);
    this.calendarMonth.set(date.toISOString().slice(0, 7));
  }

  sendChat(): void {
    if (this.chatForm.invalid || this.chatBusy()) return;
    const message = this.chatForm.getRawValue().message.trim();
    if (!message) return;

    const nextMessages = [...this.chatMessages(), { role: 'user' as const, content: message }];
    this.chatMessages.set(nextMessages);
    this.chatForm.reset({ message: '' });
    this.chatBusy.set(true);

    this.http.post<{ reply: string; usedOpenAi: boolean }>(`${this.api}/ai/chat`, {
      message,
      history: nextMessages.slice(-8)
    }, this.auth()).subscribe({
      next: response => {
        this.chatMessages.set([...this.chatMessages(), { role: 'assistant', content: response.reply, usedOpenAi: response.usedOpenAi }]);
        this.chatBusy.set(false);
      },
      error: err => {
        this.chatMessages.set([...this.chatMessages(), { role: 'assistant', content: err.error || 'I could not reach the assistant endpoint.' }]);
        this.chatBusy.set(false);
      }
    });
  }

  async startTeamChat(): Promise<void> {
    if (!this.user()) return;

    if (this.chatConnection?.state === signalR.HubConnectionState.Connected) {
      return;
    }

    if (!this.chatConnection) {
      this.chatConnection = new signalR.HubConnectionBuilder()
        .withUrl(this.hubUrl, { accessTokenFactory: () => this.user()?.token ?? '' })
        .withAutomaticReconnect()
        .build();

      this.chatConnection.on('messageReceived', (message: TeamChatMessage) => {
        this.teamMessages.set([...this.teamMessages(), message].slice(-80));
      });
    }

    try {
      if (this.chatConnection.state === signalR.HubConnectionState.Disconnected) {
        await this.chatConnection.start();
      }
    } catch {
      this.message.set('Live team chat could not connect. Check that the backend is running and try again.');
    }
  }

  async sendTeamMessage(): Promise<void> {
    if (this.teamChatForm.invalid) return;
    await this.startTeamChat();
    const body = this.teamChatForm.getRawValue().body.trim();
    if (!body || !this.chatConnection || this.chatConnection.state !== signalR.HubConnectionState.Connected) {
      this.message.set('Team chat is not connected yet. Try sending again in a moment.');
      return;
    }

    try {
      await this.chatConnection.invoke('SendMessage', body);
      this.teamChatForm.reset({ body: '' });
    } catch {
      this.message.set('Message was not sent. Reconnecting to team chat...');
      await this.chatConnection.stop();
      this.chatConnection = undefined;
      await this.startTeamChat();
    }
  }

  downloadDocument(documentId: number): void {
    this.http.get(`${this.api}/documents/${documentId}/download`, { ...this.auth(), responseType: 'blob' }).subscribe(blob => {
      const url = URL.createObjectURL(blob);
      window.open(url, '_blank');
    });
  }

  openTaskComments(task: OnboardingTask): void {
    this.selectedTask.set(task);
    this.http.get<TaskComment[]>(`${this.api}/tasks/${task.id}/comments`, this.auth()).subscribe(c => this.comments.set(c));
  }

  addTaskComment(): void {
    const task = this.selectedTask();
    if (!task || this.commentForm.invalid) return;
    this.http.post<TaskComment>(`${this.api}/tasks/${task.id}/comments`, this.commentForm.getRawValue(), this.auth()).subscribe({
      next: () => {
        this.commentForm.reset({ body: '' });
        this.openTaskComments(task);
        this.message.set('Comment added.');
      },
      error: err => this.message.set(err.error || 'Could not add comment.')
    });
  }

  uploadDocument(employeeId: number, files: FileList | null): void {
    if (!files?.length) return;
    const form = new FormData();
    form.append('file', files[0]);
    form.append('documentType', this.documentForm.getRawValue().documentType);
    this.http.post(`${this.api}/employees/${employeeId}/documents`, form, this.auth()).subscribe({
      next: () => {
        this.message.set('Document uploaded.');
        const employee = this.employees().find(e => e.id === employeeId);
        if (employee) this.openProfile(employee);
      },
      error: err => this.message.set(err.error || 'Upload failed.')
    });
  }

  changeTaskStatus(task: OnboardingTask, status: TaskStatus): void {
    this.http.patch(`${this.api}/onboardingtasks/${task.id}/status/${status}`, {}, this.auth()).subscribe({
      next: () => {
        this.message.set(`Task marked ${status}.`);
        this.loadAll();
      },
      error: err => this.message.set(err.error || 'Task update failed.')
    });
  }

  tasksForStatus(status: TaskStatus): OnboardingTask[] {
    return this.filteredTasks().filter(t => t.status === status);
  }

  isOverdue(date: string): boolean {
    return new Date(date) < new Date(this.today());
  }

  dueLabel(task: OnboardingTask): string {
    if (task.status === 'Done') return 'Completed';
    if (this.isOverdue(task.dueDate)) return `Overdue since ${task.dueDate}`;
    return `Due ${task.dueDate}`;
  }

  taskTone(task: OnboardingTask): WorkTone {
    if (task.status === 'Blocked') return 'blocked';
    if (this.isOverdue(task.dueDate)) return 'late';
    if (task.priority === 'Critical' || task.priority === 'High') return 'critical';
    return 'normal';
  }

  employeeProgress(employeeId: number): number {
    const employeeTasks = this.tasks().filter(t => t.employeeId === employeeId);
    if (!employeeTasks.length) return 0;
    return Math.round(employeeTasks.filter(t => t.status === 'Done').length / employeeTasks.length * 100);
  }

  private daysFromToday(date: string): number {
    const target = new Date(date).getTime();
    const today = new Date(this.today()).getTime();
    return Math.round((target - today) / 86_400_000);
  }

  editEmployee(employee: Employee): void { this.activeTab.set('employees'); this.editingId.set(employee.id); this.employeeForm.patchValue(employee); }
  saveEmployee(): void {
    if (this.employeeForm.invalid) return;
    const value = this.employeeForm.getRawValue();
    this.save('employees', { ...value, endDate: value.endDate || null });
  }
  editDepartment(department: Department): void { this.activeTab.set('departments'); this.editingId.set(department.id); this.departmentForm.patchValue(department); }
  saveDepartment(): void { if (!this.departmentForm.invalid) this.save('departments', this.departmentForm.getRawValue()); }
  editTask(task: OnboardingTask): void { this.activeTab.set('tasks'); this.editingId.set(task.id); this.taskForm.patchValue(task); }
  saveTask(): void { if (!this.taskForm.invalid) this.save('onboardingtasks', this.taskForm.getRawValue()); }
  editAsset(asset: CompanyAsset): void { this.activeTab.set('assets'); this.editingId.set(asset.id); this.assetForm.patchValue({ ...asset, employeeId: asset.employeeId ?? 0 }); }
  saveAsset(): void {
    if (this.assetForm.invalid) return;
    const value = this.assetForm.getRawValue();
    this.save('companyassets', { ...value, returnDate: value.returnDate || null, employeeId: value.employeeId || null });
  }

  delete(resource: string, id: number): void {
    this.http.delete(`${this.api}/${resource}/${id}`, this.auth()).subscribe({
      next: () => {
        this.message.set('Record deleted.');
        this.loadAll();
      },
      error: err => this.message.set(err.error || 'Delete failed.')
    });
  }

  cancelEdit(): void {
    this.editingId.set(null);
    this.employeeForm.reset({ firstName: '', lastName: '', email: '', role: '', startDate: this.today(), endDate: '', status: 'Onboarding', departmentId: this.departments()[0]?.id ?? 0 });
    this.departmentForm.reset({ name: '', code: '', leadName: '', description: '', budget: 0 });
    this.taskForm.reset({ title: '', notes: '', dueDate: this.today(), priority: 'Medium', status: 'ToDo', employeeId: this.employees()[0]?.id ?? 0 });
    this.assetForm.reset({ tag: '', name: '', category: 'Laptop', value: 0, assignedDate: this.today(), returnDate: '', status: 'Assigned', condition: 'Good', employeeId: this.employees()[0]?.id ?? 0 });
  }

  private save(resource: string, body: object): void {
    const id = this.editingId();
    const request = id
      ? this.http.put(`${this.api}/${resource}/${id}`, body, this.auth())
      : this.http.post(`${this.api}/${resource}`, body, this.auth());

    request.subscribe({
      next: () => {
        this.message.set(id ? 'Record updated.' : 'Record created.');
        this.cancelEdit();
        this.loadAll();
      },
      error: err => this.message.set(err.error || 'Save failed. Check required fields, permissions, and unique values.')
    });
  }

  private auth(): { headers: HttpHeaders } {
    return this.internHub.auth(this.user());
  }

  private restoreUser(): AuthUser | null {
    const raw = localStorage.getItem('internhub-user');
    return raw ? JSON.parse(raw) as AuthUser : null;
  }

  private today(): string {
    return new Date().toISOString().slice(0, 10);
  }
}
