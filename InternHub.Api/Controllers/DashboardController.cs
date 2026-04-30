using InternHub.Api.Contracts;
using InternHub.Api.Data;
using InternHub.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController(InternHubDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DashboardDto>> Get()
    {
        var departmentLoad = await db.Departments
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentLoadDto(d.Name, d.Employees.Count))
            .ToListAsync();

        var upcomingTasks = await db.OnboardingTasks
            .AsNoTracking()
            .Include(t => t.Employee)
            .Where(t => t.Status != OnboardingTaskStatus.Done)
            .OrderBy(t => t.DueDate)
            .Take(6)
            .Select(t => new UpcomingTaskDto(
                t.Id,
                t.Title,
                t.DueDate,
                t.Employee == null ? "Unassigned" : t.Employee.FirstName + " " + t.Employee.LastName,
                t.Priority.ToString(),
                t.Status.ToString()))
            .ToListAsync();

        var totalTasks = await db.OnboardingTasks.CountAsync();
        var completedTasks = await db.OnboardingTasks.CountAsync(t => t.Status == OnboardingTaskStatus.Done);
        var upcomingStarts = await db.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .Where(e => e.StartDate >= DateOnly.FromDateTime(DateTime.Today))
            .OrderBy(e => e.StartDate)
            .Take(6)
            .Select(e => new UpcomingStartDto(e.Id, e.FirstName + " " + e.LastName, e.StartDate, e.Department!.Name))
            .ToListAsync();

        return Ok(new DashboardDto(
            await db.Employees.CountAsync(),
            await db.Departments.CountAsync(),
            await db.OnboardingTasks.CountAsync(t => t.Status != OnboardingTaskStatus.Done),
            await db.OnboardingTasks.CountAsync(t => t.Status != OnboardingTaskStatus.Done && t.DueDate < DateOnly.FromDateTime(DateTime.Today)),
            await db.CompanyAssets.CountAsync(),
            await db.CompanyAssets.SumAsync(a => a.Value),
            totalTasks == 0 ? 0 : Math.Round((double)completedTasks / totalTasks * 100, 1),
            departmentLoad,
            upcomingTasks,
            upcomingStarts));
    }
}
