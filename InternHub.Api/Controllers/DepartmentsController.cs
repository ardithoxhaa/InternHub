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
public class DepartmentsController(InternHubDbContext db, AuditService audit) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<DepartmentDto>>> GetAll()
    {
        var departments = await db.Departments
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentDto(d.Id, d.Name, d.Code, d.LeadName, d.Description, d.Budget, d.Employees.Count))
            .ToListAsync();

        return Ok(departments);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DepartmentDto>> GetById(int id)
    {
        var department = await db.Departments
            .AsNoTracking()
            .Where(d => d.Id == id)
            .Select(d => new DepartmentDto(d.Id, d.Name, d.Code, d.LeadName, d.Description, d.Budget, d.Employees.Count))
            .FirstOrDefaultAsync();

        return department is null ? NotFound() : Ok(department);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,HR")]
    public async Task<ActionResult<DepartmentDto>> Create(DepartmentUpsertDto dto)
    {
        if (await db.Departments.AnyAsync(d => d.Code == dto.Code))
        {
            return Conflict("A department with this code already exists.");
        }

        var department = new Department
        {
            Name = dto.Name,
            Code = dto.Code.ToUpperInvariant(),
            LeadName = dto.LeadName,
            Description = dto.Description,
            Budget = dto.Budget
        };

        db.Departments.Add(department);
        await db.SaveChangesAsync();
        await audit.RecordAsync("Created", nameof(Department), department.Id, department.Code);

        var result = new DepartmentDto(department.Id, department.Name, department.Code, department.LeadName, department.Description, department.Budget, 0);
        return CreatedAtAction(nameof(GetById), new { id = department.Id }, result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> Update(int id, DepartmentUpsertDto dto)
    {
        var department = await db.Departments.FindAsync(id);
        if (department is null)
        {
            return NotFound();
        }

        if (await db.Departments.AnyAsync(d => d.Id != id && d.Code == dto.Code))
        {
            return Conflict("A department with this code already exists.");
        }

        department.Name = dto.Name;
        department.Code = dto.Code.ToUpperInvariant();
        department.LeadName = dto.LeadName;
        department.Description = dto.Description;
        department.Budget = dto.Budget;

        await db.SaveChangesAsync();
        await audit.RecordAsync("Updated", nameof(Department), id, department.Code);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> Delete(int id)
    {
        var department = await db.Departments.Include(d => d.Employees).FirstOrDefaultAsync(d => d.Id == id);
        if (department is null)
        {
            return NotFound();
        }

        if (department.Employees.Count > 0)
        {
            return Conflict("Move employees out of this department before deleting it.");
        }

        db.Departments.Remove(department);
        await db.SaveChangesAsync();
        await audit.RecordAsync("Deleted", nameof(Department), id, department.Code);
        return NoContent();
    }
}
