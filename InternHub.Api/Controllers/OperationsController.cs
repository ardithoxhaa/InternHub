using InternHub.Api.Contracts;
using InternHub.Api.Data;
using InternHub.Api.Models;
using InternHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;

namespace InternHub.Api.Controllers;

[ApiController]
[Route("api")]
public class OperationsController(InternHubDbContext db, AuditService audit, IWebHostEnvironment environment, EmailService emailService) : ControllerBase
{
    [HttpGet("audit")]
    [Authorize(Roles = "Admin,HR,Manager")]
    public async Task<ActionResult<IEnumerable<AuditLogDto>>> Audit()
    {
        var logs = await db.AuditLogs
            .AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .Take(50)
            .Select(a => new AuditLogDto(a.Id, a.Actor, a.Action, a.EntityName, a.EntityId, a.Details, a.CreatedAt))
            .ToListAsync();

        return Ok(logs);
    }

    [HttpGet("notifications")]
    [Authorize(Roles = "Admin,HR,Manager")]
    public async Task<ActionResult<IEnumerable<NotificationDto>>> Notifications()
    {
        var notifications = await db.Notifications
            .AsNoTracking()
            .OrderByDescending(n => n.CreatedAt)
            .Take(80)
            .Select(n => ToDto(n))
            .ToListAsync();

        return Ok(notifications);
    }

    [HttpGet("my-work")]
    public async Task<ActionResult<MyWorkDto>> MyWork()
    {
        var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");
        var user = await db.AppUsers.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
        var employee = user?.EmployeeId is null
            ? await db.Employees.AsNoTracking().Include(e => e.Department).Include(e => e.Tasks).Include(e => e.Assets).FirstOrDefaultAsync(e => e.Email == email)
            : await db.Employees.AsNoTracking().Include(e => e.Department).Include(e => e.Tasks).Include(e => e.Assets).FirstOrDefaultAsync(e => e.Id == user.EmployeeId);

        if (employee is null)
        {
            return Ok(new MyWorkDto(null, [], [], [], await MyNotifications(email)));
        }

        var documents = await db.EmployeeDocuments.AsNoTracking().Where(d => d.EmployeeId == employee.Id).Select(d => ToDto(d)).ToListAsync();
        var notifications = await MyNotifications(employee.Email);
        return Ok(new MyWorkDto(
            new EmployeeDto(employee.Id, employee.FirstName, employee.LastName, employee.FirstName + " " + employee.LastName, employee.Email, employee.Role, employee.StartDate, employee.EndDate, employee.Status, employee.DepartmentId, employee.Department?.Name ?? "", employee.Tasks.Count(t => t.Status != OnboardingTaskStatus.Done), employee.Assets.Count),
            employee.Tasks.OrderBy(t => t.DueDate).Select(t => new OnboardingTaskDto(t.Id, t.Title, t.Notes, t.DueDate, t.Priority, t.Status, employee.Id, employee.FirstName + " " + employee.LastName)),
            employee.Assets.OrderBy(a => a.Tag).Select(a => new CompanyAssetDto(a.Id, a.Tag, a.Name, a.Category, a.Value, a.AssignedDate, a.ReturnDate, a.Status, a.Condition, a.EmployeeId, employee.FirstName + " " + employee.LastName)),
            documents,
            notifications));
    }

    [HttpPatch("notifications/{id:int}/read")]
    public async Task<IActionResult> MarkNotificationRead(int id)
    {
        var notification = await db.Notifications.FindAsync(id);
        if (notification is null)
        {
            return NotFound();
        }

        notification.IsRead = true;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("employees/{employeeId:int}/welcome-email")]
    [Authorize(Roles = "Admin,HR,Manager")]
    public async Task<IActionResult> SendWelcomeEmail(int employeeId)
    {
        var employee = await db.Employees.FindAsync(employeeId);
        if (employee is null)
        {
            return NotFound();
        }

        var body = $"Hi {employee.FirstName}, your onboarding plan is ready.";
        var sent = await emailService.SendAsync(new EmailMessage(
            employee.Email,
            "Welcome to InternHub",
            $"<h2>Welcome, {employee.FirstName}</h2><p>Your onboarding plan is ready in InternHub.</p>"));

        var notification = new Notification
        {
            EmployeeId = employee.Id,
            RecipientEmail = employee.Email,
            Subject = "Welcome to InternHub",
            Body = body,
            Status = sent ? NotificationStatus.Sent : NotificationStatus.Queued,
            SentAt = sent ? DateTime.UtcNow : null
        };
        db.Notifications.Add(notification);
        await db.SaveChangesAsync();
        await audit.RecordAsync("EmailSent", nameof(Employee), employee.Id, notification.Subject);
        return Ok(ToDto(notification));
    }

    [HttpPost("employees/{employeeId:int}/onboarding-plan")]
    [Authorize(Roles = "Admin,HR,Manager")]
    public async Task<IActionResult> GenerateOnboardingPlan(int employeeId, [FromQuery] int? templateId)
    {
        var employee = await db.Employees
            .Include(e => e.Tasks)
            .FirstOrDefaultAsync(e => e.Id == employeeId);

        if (employee is null)
        {
            return NotFound();
        }

        var template = await db.OnboardingTemplates
            .Include(t => t.Items)
            .Where(t => t.IsActive && (!templateId.HasValue || t.Id == templateId.Value))
            .OrderBy(t => t.DepartmentScope == "All" ? 0 : 1)
            .FirstOrDefaultAsync();

        if (template is null)
        {
            return BadRequest("No active onboarding template is available.");
        }

        var existingTitles = employee.Tasks.Select(t => t.Title).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var item in template.Items.Where(t => !existingTitles.Contains(t.Title)))
        {
            db.OnboardingTasks.Add(new OnboardingTask
            {
                EmployeeId = employee.Id,
                Title = item.Title,
                Notes = item.Notes,
                DueDate = employee.StartDate.AddDays(item.DueOffsetDays),
                Priority = item.Priority,
                Status = OnboardingTaskStatus.ToDo
            });
        }

        var planSent = await emailService.SendAsync(new EmailMessage(
            employee.Email,
            "Your onboarding plan is ready",
            $"<h2>Your onboarding plan is ready</h2><p>Hi {employee.FirstName}, your internship checklist has been prepared in InternHub.</p>"));

        db.Notifications.Add(new Notification
        {
            EmployeeId = employee.Id,
            RecipientEmail = employee.Email,
            Subject = "Your onboarding plan is ready",
            Body = $"Hi {employee.FirstName}, your internship checklist has been prepared.",
            Status = planSent ? NotificationStatus.Sent : NotificationStatus.Queued,
            SentAt = planSent ? DateTime.UtcNow : null
        });

        employee.Status = EmploymentStatus.Onboarding;
        await db.SaveChangesAsync();
        await audit.RecordAsync("OnboardingPlanGenerated", nameof(Employee), employee.Id, $"{employee.Email} using {template.Name}");

        return Ok(new { message = "Onboarding plan generated." });
    }

    [HttpGet("onboarding-templates")]
    [Authorize(Roles = "Admin,HR,Manager")]
    public async Task<ActionResult<IEnumerable<OnboardingTemplateDto>>> Templates()
    {
        return Ok(await db.OnboardingTemplates
            .AsNoTracking()
            .Include(t => t.Items)
            .OrderBy(t => t.Name)
            .Select(t => new OnboardingTemplateDto(
                t.Id,
                t.Name,
                t.DepartmentScope,
                t.IsActive,
                t.Items.OrderBy(i => i.DueOffsetDays).Select(i => new OnboardingTemplateItemDto(i.Id, i.Title, i.Notes, i.DueOffsetDays, i.Priority.ToString()))))
            .ToListAsync());
    }

    [HttpPost("onboarding-templates")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<ActionResult<OnboardingTemplateDto>> CreateTemplate(TemplateUpsertDto dto)
    {
        var template = new OnboardingTemplate
        {
            Name = dto.Name,
            DepartmentScope = dto.DepartmentScope,
            IsActive = dto.IsActive,
            Items = dto.Items.Select(i => new OnboardingTemplateItem { Title = i.Title, Notes = i.Notes, DueOffsetDays = i.DueOffsetDays, Priority = Enum.Parse<TaskPriority>(i.Priority) }).ToList()
        };
        db.OnboardingTemplates.Add(template);
        await db.SaveChangesAsync();
        await audit.RecordAsync("Created", nameof(OnboardingTemplate), template.Id, template.Name);
        return Ok(ToTemplateDto(template));
    }

    [HttpPut("onboarding-templates/{id:int}")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> UpdateTemplate(int id, TemplateUpsertDto dto)
    {
        var template = await db.OnboardingTemplates.Include(t => t.Items).FirstOrDefaultAsync(t => t.Id == id);
        if (template is null)
        {
            return NotFound();
        }

        template.Name = dto.Name;
        template.DepartmentScope = dto.DepartmentScope;
        template.IsActive = dto.IsActive;
        db.OnboardingTemplateItems.RemoveRange(template.Items);
        template.Items = dto.Items.Select(i => new OnboardingTemplateItem { Title = i.Title, Notes = i.Notes, DueOffsetDays = i.DueOffsetDays, Priority = Enum.Parse<TaskPriority>(i.Priority) }).ToList();
        await db.SaveChangesAsync();
        await audit.RecordAsync("Updated", nameof(OnboardingTemplate), template.Id, template.Name);
        return NoContent();
    }

    [HttpGet("tasks/{taskId:int}/comments")]
    public async Task<ActionResult<IEnumerable<TaskCommentDto>>> TaskComments(int taskId)
    {
        return Ok(await db.TaskComments
            .AsNoTracking()
            .Where(c => c.OnboardingTaskId == taskId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new TaskCommentDto(c.Id, c.Author, c.Body, c.CreatedAt, c.OnboardingTaskId))
            .ToListAsync());
    }

    [HttpPost("tasks/{taskId:int}/comments")]
    public async Task<ActionResult<TaskCommentDto>> AddTaskComment(int taskId, TaskCommentCreateDto dto)
    {
        if (!await db.OnboardingTasks.AnyAsync(t => t.Id == taskId))
        {
            return NotFound();
        }

        var comment = new TaskComment
        {
            OnboardingTaskId = taskId,
            Body = dto.Body,
            Author = User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Name) ?? "User"
        };
        db.TaskComments.Add(comment);
        await db.SaveChangesAsync();
        await audit.RecordAsync("Commented", nameof(OnboardingTask), taskId, dto.Body);
        return Ok(new TaskCommentDto(comment.Id, comment.Author, comment.Body, comment.CreatedAt, taskId));
    }

    [HttpGet("documents/{documentId:int}/download")]
    public async Task<IActionResult> DownloadDocument(int documentId)
    {
        var document = await db.EmployeeDocuments.FindAsync(documentId);
        if (document is null || !System.IO.File.Exists(document.StoredPath))
        {
            return NotFound();
        }

        return PhysicalFile(document.StoredPath, "application/octet-stream", document.FileName);
    }

    [HttpPatch("documents/{documentId:int}/approval/{status}")]
    [Authorize(Roles = "Admin,HR,Manager")]
    public async Task<IActionResult> ReviewDocument(int documentId, ApprovalStatus status)
    {
        var document = await db.EmployeeDocuments.FindAsync(documentId);
        if (document is null)
        {
            return NotFound();
        }

        document.ApprovalStatus = status;
        document.ReviewedBy = User.Identity?.Name ?? "Reviewer";
        document.ReviewedAt = DateTime.UtcNow;
        document.RejectionReason = status == ApprovalStatus.Rejected ? "Rejected during review." : null;
        await db.SaveChangesAsync();
        await audit.RecordAsync("DocumentReviewed", nameof(EmployeeDocument), document.Id, status.ToString());
        return NoContent();
    }

    [HttpPatch("documents/{documentId:int}/review")]
    [Authorize(Roles = "Admin,HR,Manager")]
    public async Task<IActionResult> ReviewDocumentWithReason(int documentId, DocumentReviewDto dto)
    {
        var document = await db.EmployeeDocuments.FindAsync(documentId);
        if (document is null)
        {
            return NotFound();
        }

        if (!Enum.TryParse<ApprovalStatus>(dto.Status, true, out var status))
        {
            return BadRequest("Status must be Pending, Approved, or Rejected.");
        }

        document.ApprovalStatus = status;
        document.ReviewedBy = User.Identity?.Name ?? "Reviewer";
        document.ReviewedAt = DateTime.UtcNow;
        document.RejectionReason = status == ApprovalStatus.Rejected ? dto.Reason : null;
        await db.SaveChangesAsync();
        await audit.RecordAsync("DocumentReviewed", nameof(EmployeeDocument), document.Id, status == ApprovalStatus.Rejected ? dto.Reason : status.ToString());
        return NoContent();
    }

    [HttpPatch("assets/{assetId:int}/return")]
    [Authorize(Roles = "Admin,HR,Manager")]
    public async Task<IActionResult> ReturnAsset(int assetId)
    {
        var asset = await db.CompanyAssets.FindAsync(assetId);
        if (asset is null)
        {
            return NotFound();
        }

        asset.Status = AssetStatus.Returned;
        asset.ReturnDate = DateOnly.FromDateTime(DateTime.Today);
        asset.EmployeeId = null;
        await db.SaveChangesAsync();
        await audit.RecordAsync("AssetReturned", nameof(CompanyAsset), asset.Id, asset.Tag);
        return NoContent();
    }

    [HttpPatch("employees/{employeeId:int}/lifecycle/{status}")]
    [Authorize(Roles = "Admin,HR,Manager")]
    public async Task<IActionResult> UpdateLifecycle(int employeeId, EmploymentStatus status)
    {
        var employee = await db.Employees.FindAsync(employeeId);
        if (employee is null)
        {
            return NotFound();
        }

        employee.Status = status;
        if (status is EmploymentStatus.Completed or EmploymentStatus.Exited)
        {
            employee.EndDate = DateOnly.FromDateTime(DateTime.Today);
        }

        await db.SaveChangesAsync();
        await audit.RecordAsync("LifecycleChanged", nameof(Employee), employee.Id, status.ToString());
        return NoContent();
    }

    [HttpGet("reports/{type}")]
    [Authorize(Roles = "Admin,HR,Manager")]
    public async Task<IActionResult> ExportReport(string type)
    {
        var csv = new StringBuilder();
        var fileName = $"{type}-report.csv";

        if (type.Equals("employees", StringComparison.OrdinalIgnoreCase))
        {
            csv.AppendLine("Name,Email,Role,Department,Status,OpenTasks,Assets");
            var rows = await db.Employees.AsNoTracking().Include(e => e.Department).Include(e => e.Tasks).Include(e => e.Assets).ToListAsync();
            foreach (var e in rows)
            {
                csv.AppendLine($"{Escape(e.FirstName + " " + e.LastName)},{Escape(e.Email)},{Escape(e.Role)},{Escape(e.Department?.Name ?? "")},{e.Status},{e.Tasks.Count(t => t.Status != OnboardingTaskStatus.Done)},{e.Assets.Count}");
            }
        }
        else if (type.Equals("tasks", StringComparison.OrdinalIgnoreCase))
        {
            csv.AppendLine("Title,Employee,DueDate,Priority,Status");
            var rows = await db.OnboardingTasks.AsNoTracking().Include(t => t.Employee).ToListAsync();
            foreach (var t in rows)
            {
                csv.AppendLine($"{Escape(t.Title)},{Escape(t.Employee is null ? "" : t.Employee.FirstName + " " + t.Employee.LastName)},{t.DueDate},{t.Priority},{t.Status}");
            }
        }
        else if (type.Equals("assets", StringComparison.OrdinalIgnoreCase))
        {
            csv.AppendLine("Tag,Name,Category,Status,Condition,Owner,AssignedDate,ReturnDate,Value");
            var rows = await db.CompanyAssets.AsNoTracking().Include(a => a.Employee).ToListAsync();
            foreach (var a in rows)
            {
                csv.AppendLine($"{Escape(a.Tag)},{Escape(a.Name)},{Escape(a.Category)},{a.Status},{a.Condition},{Escape(a.Employee is null ? "" : a.Employee.FirstName + " " + a.Employee.LastName)},{a.AssignedDate},{a.ReturnDate},{a.Value}");
            }
        }
        else if (type.Equals("progress", StringComparison.OrdinalIgnoreCase))
        {
            csv.AppendLine("Employee,Department,CompletedTasks,TotalTasks,CompletionRate");
            var rows = await db.Employees.AsNoTracking().Include(e => e.Department).Include(e => e.Tasks).ToListAsync();
            foreach (var e in rows)
            {
                var total = e.Tasks.Count;
                var done = e.Tasks.Count(t => t.Status == OnboardingTaskStatus.Done);
                var rate = total == 0 ? 0 : Math.Round((double)done / total * 100, 1);
                csv.AppendLine($"{Escape(e.FirstName + " " + e.LastName)},{Escape(e.Department?.Name ?? "")},{done},{total},{rate}");
            }
        }
        else
        {
            return BadRequest("Supported report types are employees, tasks, assets, and progress.");
        }

        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", fileName);
    }

    [HttpGet("calendar")]
    public async Task<ActionResult<IEnumerable<CalendarItemDto>>> Calendar()
    {
        var starts = await db.Employees.AsNoTracking()
            .Select(e => new CalendarItemDto(e.Id, e.FirstName + " " + e.LastName + " starts", e.StartDate.ToString("yyyy-MM-dd"), "StartDate", e.Status.ToString(), e.Department!.Name))
            .ToListAsync();

        var due = await db.OnboardingTasks.AsNoTracking().Include(t => t.Employee)
            .Select(t => new CalendarItemDto(t.Id, t.Title, t.DueDate.ToString("yyyy-MM-dd"), "TaskDue", t.Status.ToString(), t.Employee!.FirstName + " " + t.Employee.LastName))
            .ToListAsync();

        return Ok(starts.Concat(due).OrderBy(i => i.Date));
    }

    [HttpGet("employees/{employeeId:int}/documents")]
    public async Task<ActionResult<IEnumerable<EmployeeDocumentDto>>> Documents(int employeeId)
    {
        return Ok(await db.EmployeeDocuments
            .AsNoTracking()
            .Where(d => d.EmployeeId == employeeId)
            .OrderByDescending(d => d.UploadedAt)
            .Select(d => ToDto(d))
            .ToListAsync());
    }

    [HttpPost("employees/{employeeId:int}/documents")]
    [Authorize(Roles = "Admin,HR,Manager")]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult<EmployeeDocumentDto>> UploadDocument(int employeeId, IFormFile file, [FromForm] string documentType)
    {
        if (!await db.Employees.AnyAsync(e => e.Id == employeeId))
        {
            return NotFound();
        }

        if (file.Length == 0)
        {
            return BadRequest("Choose a file to upload.");
        }

        var uploadRoot = Path.Combine(environment.ContentRootPath, "App_Data", "uploads");
        Directory.CreateDirectory(uploadRoot);
        var safeName = $"{Guid.NewGuid():N}_{Path.GetFileName(file.FileName)}";
        var storedPath = Path.Combine(uploadRoot, safeName);

        await using (var stream = System.IO.File.Create(storedPath))
        {
            await file.CopyToAsync(stream);
        }

        var document = new EmployeeDocument
        {
            EmployeeId = employeeId,
            FileName = file.FileName,
            DocumentType = documentType,
            SizeBytes = file.Length,
            StoredPath = storedPath
        };
        db.EmployeeDocuments.Add(document);
        await db.SaveChangesAsync();
        await audit.RecordAsync("DocumentUploaded", nameof(Employee), employeeId, file.FileName);

        return Ok(ToDto(document));
    }

    private static NotificationDto ToDto(Notification n) =>
        new(n.Id, n.RecipientEmail, n.Subject, n.Body, n.Status.ToString(), n.CreatedAt, n.SentAt, n.IsRead, n.EmployeeId);

    private static EmployeeDocumentDto ToDto(EmployeeDocument d) =>
        new(d.Id, d.FileName, d.DocumentType, d.SizeBytes, d.UploadedAt, d.ApprovalStatus.ToString(), d.ReviewedBy, d.ReviewedAt, d.RejectionReason, d.EmployeeId);

    private static OnboardingTemplateDto ToTemplateDto(OnboardingTemplate t) =>
        new(t.Id, t.Name, t.DepartmentScope, t.IsActive, t.Items.OrderBy(i => i.DueOffsetDays).Select(i => new OnboardingTemplateItemDto(i.Id, i.Title, i.Notes, i.DueOffsetDays, i.Priority.ToString())));

    private async Task<List<NotificationDto>> MyNotifications(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return [];
        }

        return await db.Notifications.AsNoTracking()
            .Where(n => n.RecipientEmail == email)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => ToDto(n))
            .ToListAsync();
    }

    private static string Escape(string value) =>
        "\"" + value.Replace("\"", "\"\"") + "\"";
}
