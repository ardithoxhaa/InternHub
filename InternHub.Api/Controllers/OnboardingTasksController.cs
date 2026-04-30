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
public class OnboardingTasksController(InternHubDbContext db, AuditService audit) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<OnboardingTaskDto>>> GetAll([FromQuery] int? employeeId, [FromQuery] OnboardingTaskStatus? status)
    {
        var query = db.OnboardingTasks.AsNoTracking().Include(t => t.Employee).AsQueryable();

        if (employeeId.HasValue)
        {
            query = query.Where(t => t.EmployeeId == employeeId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        var tasks = await query
            .OrderBy(t => t.DueDate)
            .Select(t => ToDto(t))
            .ToListAsync();

        return Ok(tasks);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OnboardingTaskDto>> GetById(int id)
    {
        var task = await db.OnboardingTasks.AsNoTracking().Include(t => t.Employee).FirstOrDefaultAsync(t => t.Id == id);
        return task is null ? NotFound() : Ok(ToDto(task));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,HR,Manager")]
    public async Task<ActionResult<OnboardingTaskDto>> Create(OnboardingTaskUpsertDto dto)
    {
        if (!await db.Employees.AnyAsync(e => e.Id == dto.EmployeeId))
        {
            return BadRequest("Employee does not exist.");
        }

        var task = new OnboardingTask
        {
            Title = dto.Title,
            Notes = dto.Notes,
            DueDate = dto.DueDate,
            Priority = dto.Priority,
            Status = dto.Status,
            EmployeeId = dto.EmployeeId
        };

        db.OnboardingTasks.Add(task);
        await db.SaveChangesAsync();
        await audit.RecordAsync("Created", nameof(OnboardingTask), task.Id, task.Title);

        return CreatedAtAction(nameof(GetById), new { id = task.Id }, await GetById(task.Id));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,HR,Manager")]
    public async Task<IActionResult> Update(int id, OnboardingTaskUpsertDto dto)
    {
        var task = await db.OnboardingTasks.FindAsync(id);
        if (task is null)
        {
            return NotFound();
        }

        if (!await db.Employees.AnyAsync(e => e.Id == dto.EmployeeId))
        {
            return BadRequest("Employee does not exist.");
        }

        task.Title = dto.Title;
        task.Notes = dto.Notes;
        task.DueDate = dto.DueDate;
        task.Priority = dto.Priority;
        task.Status = dto.Status;
        task.EmployeeId = dto.EmployeeId;

        await db.SaveChangesAsync();
        await audit.RecordAsync("Updated", nameof(OnboardingTask), id, task.Title);
        return NoContent();
    }

    [HttpPatch("{id:int}/status/{status}")]
    [Authorize(Roles = "Admin,HR,Manager,Intern")]
    public async Task<IActionResult> ChangeStatus(int id, OnboardingTaskStatus status)
    {
        var task = await db.OnboardingTasks.FindAsync(id);
        if (task is null)
        {
            return NotFound();
        }

        task.Status = status;
        await db.SaveChangesAsync();
        await audit.RecordAsync("StatusChanged", nameof(OnboardingTask), id, status.ToString());
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,HR,Manager")]
    public async Task<IActionResult> Delete(int id)
    {
        var task = await db.OnboardingTasks.FindAsync(id);
        if (task is null)
        {
            return NotFound();
        }

        db.OnboardingTasks.Remove(task);
        await db.SaveChangesAsync();
        await audit.RecordAsync("Deleted", nameof(OnboardingTask), id, task.Title);
        return NoContent();
    }

    private static OnboardingTaskDto ToDto(OnboardingTask task) =>
        new(task.Id, task.Title, task.Notes, task.DueDate, task.Priority, task.Status, task.EmployeeId, task.Employee is null ? "Unassigned" : $"{task.Employee.FirstName} {task.Employee.LastName}");
}
