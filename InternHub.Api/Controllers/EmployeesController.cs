using InternHub.Api.Contracts;
using InternHub.Api.Data;
using InternHub.Api.Models;
using InternHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController(InternHubDbContext db, AuditService audit) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmployeeDto>>> GetAll([FromQuery] string? search, [FromQuery] EmploymentStatus? status)
    {
        var query = db.Employees.AsNoTracking().Include(e => e.Department).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(e => e.FirstName.Contains(search) || e.LastName.Contains(search) || e.Email.Contains(search) || e.Role.Contains(search));
        }

        if (status.HasValue)
        {
            query = query.Where(e => e.Status == status.Value);
        }

        var employees = await query
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .Select(e => ToDto(e))
            .ToListAsync();

        return Ok(employees);
    }

    [HttpGet("{id:int}/profile")]
    public async Task<ActionResult<EmployeeProfileDto>> Profile(int id)
    {
        var employee = await db.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.Tasks)
            .Include(e => e.Assets)
            .Include(e => e.Documents)
            .Include(e => e.Notifications)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee is null)
        {
            return NotFound();
        }

        return Ok(new EmployeeProfileDto(
            ToDto(employee),
            employee.Tasks.OrderBy(t => t.DueDate).Select(t => new OnboardingTaskDto(t.Id, t.Title, t.Notes, t.DueDate, t.Priority, t.Status, employee.Id, employee.FirstName + " " + employee.LastName)),
            employee.Assets.OrderBy(a => a.Tag).Select(a => new CompanyAssetDto(a.Id, a.Tag, a.Name, a.Category, a.Value, a.AssignedDate, a.ReturnDate, a.Status, a.Condition, a.EmployeeId, employee.FirstName + " " + employee.LastName)),
            employee.Documents.OrderByDescending(d => d.UploadedAt).Select(d => new EmployeeDocumentDto(d.Id, d.FileName, d.DocumentType, d.SizeBytes, d.UploadedAt, d.ApprovalStatus.ToString(), d.ReviewedBy, d.ReviewedAt, d.RejectionReason, employee.Id)),
            employee.Notifications.OrderByDescending(n => n.CreatedAt).Select(n => new NotificationDto(n.Id, n.RecipientEmail, n.Subject, n.Body, n.Status.ToString(), n.CreatedAt, n.SentAt, n.IsRead, employee.Id))));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmployeeDto>> GetById(int id)
    {
        var employee = await db.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.Tasks)
            .Include(e => e.Assets)
            .FirstOrDefaultAsync(e => e.Id == id);

        return employee is null ? NotFound() : Ok(ToDto(employee));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,HR,Manager")]
    public async Task<ActionResult<EmployeeDto>> Create(EmployeeUpsertDto dto)
    {
        if (!await db.Departments.AnyAsync(d => d.Id == dto.DepartmentId))
        {
            return BadRequest("Department does not exist.");
        }

        if (await db.Employees.AnyAsync(e => e.Email == dto.Email))
        {
            return Conflict("An employee with this email already exists.");
        }

        var employee = new Employee
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            Role = dto.Role,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Status = dto.Status,
            DepartmentId = dto.DepartmentId
        };

        db.Employees.Add(employee);
        await db.SaveChangesAsync();
        await audit.RecordAsync("Created", nameof(Employee), employee.Id, employee.Email);

        return CreatedAtAction(nameof(GetById), new { id = employee.Id }, await GetById(employee.Id));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,HR,Manager")]
    public async Task<IActionResult> Update(int id, EmployeeUpsertDto dto)
    {
        var employee = await db.Employees.FindAsync(id);
        if (employee is null)
        {
            return NotFound();
        }

        if (!await db.Departments.AnyAsync(d => d.Id == dto.DepartmentId))
        {
            return BadRequest("Department does not exist.");
        }

        if (await db.Employees.AnyAsync(e => e.Id != id && e.Email == dto.Email))
        {
            return Conflict("An employee with this email already exists.");
        }

        employee.FirstName = dto.FirstName;
        employee.LastName = dto.LastName;
        employee.Email = dto.Email;
        employee.Role = dto.Role;
        employee.StartDate = dto.StartDate;
        employee.EndDate = dto.EndDate;
        employee.Status = dto.Status;
        employee.DepartmentId = dto.DepartmentId;

        await db.SaveChangesAsync();
        await audit.RecordAsync("Updated", nameof(Employee), id, employee.Email);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> Delete(int id)
    {
        var employee = await db.Employees.FindAsync(id);
        if (employee is null)
        {
            return NotFound();
        }

        db.Employees.Remove(employee);
        await db.SaveChangesAsync();
        await audit.RecordAsync("Deleted", nameof(Employee), id, employee.Email);
        return NoContent();
    }

    private static EmployeeDto ToDto(Employee e) =>
        new(e.Id, e.FirstName, e.LastName, $"{e.FirstName} {e.LastName}", e.Email, e.Role, e.StartDate, e.EndDate, e.Status, e.DepartmentId, e.Department?.Name ?? "Unassigned", e.Tasks.Count(t => t.Status != OnboardingTaskStatus.Done), e.Assets.Count);
}
