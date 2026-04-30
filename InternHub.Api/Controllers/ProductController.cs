using InternHub.Api.Contracts;
using InternHub.Api.Data;
using InternHub.Api.Models;
using InternHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace InternHub.Api.Controllers;

[ApiController]
[Route("api/product")]
public class ProductController(InternHubDbContext db, IConfiguration configuration, EmailService emailService, AuditService audit) : ControllerBase
{
    [HttpGet("home")]
    public async Task<ActionResult<RoleHomeDto>> Home()
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? "Intern";
        var today = DateOnly.FromDateTime(DateTime.Today);
        var metrics = new
        {
            Employees = await db.Employees.CountAsync(),
            PendingDocuments = await db.EmployeeDocuments.CountAsync(d => d.ApprovalStatus == ApprovalStatus.Pending),
            OverdueTasks = await db.OnboardingTasks.CountAsync(t => t.Status != OnboardingTaskStatus.Done && t.DueDate < today),
            BlockedTasks = await db.OnboardingTasks.CountAsync(t => t.Status == OnboardingTaskStatus.Blocked),
            UnreadNotifications = await db.Notifications.CountAsync(n => !n.IsRead)
        };

        var focus = role switch
        {
            "Admin" => new[] { "Review system settings", "Check audit activity", "Export progress reports" },
            "HR" => new[] { "Approve pending documents", "Prepare upcoming starts", "Generate onboarding plans" },
            "Manager" => new[] { "Review blocked tasks", "Follow up with assigned interns", "Return unneeded assets" },
            _ => new[] { "Finish your onboarding tasks", "Read your notifications", "Upload required documents" }
        };

        return Ok(new RoleHomeDto(role, focus, metrics));
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<SearchResultDto>>> Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return Ok(Array.Empty<SearchResultDto>());
        }

        var employees = await db.Employees.AsNoTracking()
            .Where(e => e.FirstName.Contains(q) || e.LastName.Contains(q) || e.Email.Contains(q) || e.Role.Contains(q))
            .Take(6)
            .Select(e => new SearchResultDto("Employee", e.Id, e.FirstName + " " + e.LastName, e.Email))
            .ToListAsync();

        var tasks = await db.OnboardingTasks.AsNoTracking().Include(t => t.Employee)
            .Where(t => t.Title.Contains(q))
            .Take(6)
            .Select(t => new SearchResultDto("Task", t.Id, t.Title, t.Employee == null ? "" : t.Employee.FirstName + " " + t.Employee.LastName))
            .ToListAsync();

        var assets = await db.CompanyAssets.AsNoTracking()
            .Where(a => a.Tag.Contains(q) || a.Name.Contains(q))
            .Take(6)
            .Select(a => new SearchResultDto("Asset", a.Id, a.Tag, a.Name))
            .ToListAsync();

        return Ok(employees.Concat(tasks).Concat(assets));
    }

    [HttpGet("settings-status")]
    [Authorize(Roles = "Admin,HR")]
    public ActionResult<SettingsStatusDto> SettingsStatus()
    {
        return Ok(new SettingsStatusDto(
            configuration.GetValue<bool>("Email:Enabled"),
            !string.IsNullOrWhiteSpace(configuration["OpenAI:ApiKey"]),
            configuration["OpenAI:Model"] ?? "",
            configuration["Company:Name"] ?? "InternHub"));
    }

    [HttpGet("settings")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<ActionResult<CompanySettingsDto>> Settings()
    {
        var settings = await db.AppSettings.AsNoTracking().ToDictionaryAsync(s => s.Key, s => s.Value);
        return Ok(new CompanySettingsDto(
            Setting(settings, "CompanyName", configuration["Company:Name"] ?? "InternHub"),
            int.TryParse(Setting(settings, "DefaultTemplateId", ""), out var templateId) ? templateId : null,
            int.TryParse(Setting(settings, "ReminderFrequencyDays", "3"), out var days) ? days : 3,
            Setting(settings, "EmailSenderName", "InternHub People Team")));
    }

    [HttpPut("settings")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> SaveSettings(CompanySettingsDto dto)
    {
        await UpsertSetting("CompanyName", dto.CompanyName);
        await UpsertSetting("DefaultTemplateId", dto.DefaultTemplateId?.ToString() ?? "");
        await UpsertSetting("ReminderFrequencyDays", Math.Max(1, dto.ReminderFrequencyDays).ToString());
        await UpsertSetting("EmailSenderName", dto.EmailSenderName);
        await db.SaveChangesAsync();
        await audit.RecordAsync("SettingsUpdated", nameof(AppSetting), null, dto.CompanyName);
        return NoContent();
    }

    [HttpGet("analytics")]
    [Authorize(Roles = "Admin,HR,Manager")]
    public async Task<ActionResult<AnalyticsDto>> Analytics()
    {
        var employees = await db.Employees.AsNoTracking().Include(e => e.Department).Include(e => e.Tasks).ToListAsync();
        var tasks = await db.OnboardingTasks.AsNoTracking().ToListAsync();
        var assets = await db.CompanyAssets.AsNoTracking().ToListAsync();
        var today = DateOnly.FromDateTime(DateTime.Today);

        var departmentProgress = employees.GroupBy(e => e.Department?.Name ?? "Unassigned").Select(g =>
        {
            var total = g.Sum(e => e.Tasks.Count);
            var done = g.Sum(e => e.Tasks.Count(t => t.Status == OnboardingTaskStatus.Done));
            return new DepartmentProgressDto(g.Key, done, total, total == 0 ? 0 : Math.Round((double)done / total * 100, 1));
        }).OrderBy(d => d.Department);

        var taskTrend = Enumerable.Range(-13, 14)
            .Select(offset => today.AddDays(offset))
            .Select(date => new TaskTrendDto(
                date.ToString("yyyy-MM-dd"),
                tasks.Count(t => t.DueDate == date),
                tasks.Count(t => t.DueDate == date && t.Status != OnboardingTaskStatus.Done),
                tasks.Count(t => t.DueDate == date && t.Status == OnboardingTaskStatus.Done)));

        var assetsByCategory = assets.GroupBy(a => a.Category)
            .Select(g => new AssetCategoryDto(g.Key, g.Count(), g.Sum(a => a.Value)))
            .OrderByDescending(a => a.Value);

        var employeeProgress = employees.Select(e =>
        {
            var total = e.Tasks.Count;
            var done = e.Tasks.Count(t => t.Status == OnboardingTaskStatus.Done);
            return new EmployeeProgressDto(e.Id, e.FirstName + " " + e.LastName, e.Department?.Name ?? "Unassigned", done, total, total == 0 ? 0 : Math.Round((double)done / total * 100, 1));
        }).OrderBy(e => e.Rate);

        return Ok(new AnalyticsDto(departmentProgress, taskTrend, assetsByCategory, employeeProgress, await db.EmployeeDocuments.CountAsync(d => d.ApprovalStatus == ApprovalStatus.Pending)));
    }

    [HttpGet("invites")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<ActionResult<IEnumerable<InviteDto>>> Invites()
    {
        return Ok(await db.EmployeeInvites.AsNoTracking()
            .OrderByDescending(i => i.CreatedAt)
            .Take(60)
            .Select(i => new InviteDto(i.Id, i.Email, i.FullName, i.Role, i.Token, i.CreatedAt, i.ExpiresAt, i.AcceptedAt, i.EmployeeId))
            .ToListAsync());
    }

    [HttpPost("invites")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<ActionResult<InviteDto>> CreateInvite(InviteCreateDto dto)
    {
        var employee = await db.Employees.FindAsync(dto.EmployeeId);
        if (employee is null)
        {
            return NotFound("Employee does not exist.");
        }

        var invite = new EmployeeInvite
        {
            EmployeeId = employee.Id,
            Email = employee.Email,
            FullName = employee.FirstName + " " + employee.LastName,
            Role = string.IsNullOrWhiteSpace(dto.Role) ? "Intern" : dto.Role,
            Token = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24)),
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        db.EmployeeInvites.Add(invite);
        var inviteLink = $"http://localhost:4200?invite={invite.Token}";
        var sent = await emailService.SendAsync(new EmailMessage(invite.Email, "Your InternHub invite", $"<p>Hi {invite.FullName}, use this invite link to create your account:</p><p>{inviteLink}</p>"));
        db.Notifications.Add(new Notification
        {
            EmployeeId = employee.Id,
            RecipientEmail = invite.Email,
            Subject = "Your InternHub invite",
            Body = inviteLink,
            Status = sent ? NotificationStatus.Sent : NotificationStatus.Queued,
            SentAt = sent ? DateTime.UtcNow : null
        });
        await db.SaveChangesAsync();
        await audit.RecordAsync("InviteCreated", nameof(Employee), employee.Id, invite.Email);
        return Ok(new InviteDto(invite.Id, invite.Email, invite.FullName, invite.Role, invite.Token, invite.CreatedAt, invite.ExpiresAt, invite.AcceptedAt, invite.EmployeeId));
    }

    [HttpPost("run-reminders")]
    [Authorize(Roles = "Admin,HR,Manager")]
    public async Task<IActionResult> RunReminders()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var overdue = await db.OnboardingTasks.Include(t => t.Employee)
            .Where(t => t.Status != OnboardingTaskStatus.Done && t.DueDate < today && t.Employee != null)
            .ToListAsync();

        var sent = 0;
        foreach (var task in overdue)
        {
            var email = task.Employee!.Email;
            var subject = "Overdue onboarding task";
            var body = $"Hi {task.Employee.FirstName}, your task '{task.Title}' is overdue.";
            var delivered = await emailService.SendAsync(new EmailMessage(email, subject, $"<p>{body}</p>"));
            db.Notifications.Add(new Notification
            {
                EmployeeId = task.EmployeeId,
                RecipientEmail = email,
                Subject = subject,
                Body = body,
                Status = delivered ? NotificationStatus.Sent : NotificationStatus.Queued,
                SentAt = delivered ? DateTime.UtcNow : null
            });
            sent++;
        }

        await db.SaveChangesAsync();
        await audit.RecordAsync("ReminderRun", "OnboardingTask", null, $"{sent} reminders queued/sent");
        return Ok(new { reminders = sent });
    }

    [HttpGet("employees/{employeeId:int}/timeline")]
    public async Task<ActionResult<IEnumerable<AuditLogDto>>> EmployeeTimeline(int employeeId)
    {
        var logs = await db.AuditLogs.AsNoTracking()
            .Where(a => a.EntityId == employeeId || a.Details != null && a.Details.Contains(employeeId.ToString()))
            .OrderByDescending(a => a.CreatedAt)
            .Take(40)
            .Select(a => new AuditLogDto(a.Id, a.Actor, a.Action, a.EntityName, a.EntityId, a.Details, a.CreatedAt))
            .ToListAsync();
        return Ok(logs);
    }

    [HttpGet("chat/history")]
    public async Task<ActionResult<IEnumerable<TeamChatMessageDto>>> ChatHistory()
    {
        return Ok(await db.TeamChatMessages.AsNoTracking()
            .OrderByDescending(m => m.CreatedAt)
            .Take(50)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new TeamChatMessageDto(m.Id, m.SenderName, m.SenderEmail, m.Body, m.CreatedAt))
            .ToListAsync());
    }

    private static string Setting(IReadOnlyDictionary<string, string> settings, string key, string fallback) =>
        settings.TryGetValue(key, out var value) ? value : fallback;

    private async Task UpsertSetting(string key, string value)
    {
        var setting = await db.AppSettings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting is null)
        {
            db.AppSettings.Add(new AppSetting { Key = key, Value = value, UpdatedBy = User.Identity?.Name });
            return;
        }

        setting.Value = value;
        setting.UpdatedAt = DateTime.UtcNow;
        setting.UpdatedBy = User.Identity?.Name;
    }
}
