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
public class CompanyAssetsController(InternHubDbContext db, AuditService audit) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CompanyAssetDto>>> GetAll([FromQuery] int? employeeId)
    {
        var query = db.CompanyAssets.AsNoTracking().Include(a => a.Employee).AsQueryable();

        if (employeeId.HasValue)
        {
            query = query.Where(a => a.EmployeeId == employeeId.Value);
        }

        var assets = await query
            .OrderBy(a => a.Tag)
            .Select(a => ToDto(a))
            .ToListAsync();

        return Ok(assets);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CompanyAssetDto>> GetById(int id)
    {
        var asset = await db.CompanyAssets.AsNoTracking().Include(a => a.Employee).FirstOrDefaultAsync(a => a.Id == id);
        return asset is null ? NotFound() : Ok(ToDto(asset));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,HR,Manager")]
    public async Task<ActionResult<CompanyAssetDto>> Create(CompanyAssetUpsertDto dto)
    {
        if (dto.EmployeeId.HasValue && !await db.Employees.AnyAsync(e => e.Id == dto.EmployeeId.Value))
        {
            return BadRequest("Employee does not exist.");
        }

        if (await db.CompanyAssets.AnyAsync(a => a.Tag == dto.Tag))
        {
            return Conflict("An asset with this tag already exists.");
        }

        var asset = new CompanyAsset
        {
            Tag = dto.Tag.ToUpperInvariant(),
            Name = dto.Name,
            Category = dto.Category,
            Value = dto.Value,
            AssignedDate = dto.AssignedDate,
            ReturnDate = dto.ReturnDate,
            Status = dto.Status,
            Condition = dto.Condition,
            EmployeeId = dto.EmployeeId
        };

        db.CompanyAssets.Add(asset);
        await db.SaveChangesAsync();
        await audit.RecordAsync("Created", nameof(CompanyAsset), asset.Id, asset.Tag);

        return CreatedAtAction(nameof(GetById), new { id = asset.Id }, await GetById(asset.Id));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,HR,Manager")]
    public async Task<IActionResult> Update(int id, CompanyAssetUpsertDto dto)
    {
        var asset = await db.CompanyAssets.FindAsync(id);
        if (asset is null)
        {
            return NotFound();
        }

        if (dto.EmployeeId.HasValue && !await db.Employees.AnyAsync(e => e.Id == dto.EmployeeId.Value))
        {
            return BadRequest("Employee does not exist.");
        }

        if (await db.CompanyAssets.AnyAsync(a => a.Id != id && a.Tag == dto.Tag))
        {
            return Conflict("An asset with this tag already exists.");
        }

        asset.Tag = dto.Tag.ToUpperInvariant();
        asset.Name = dto.Name;
        asset.Category = dto.Category;
        asset.Value = dto.Value;
        asset.AssignedDate = dto.AssignedDate;
        asset.ReturnDate = dto.ReturnDate;
        asset.Status = dto.Status;
        asset.Condition = dto.Condition;
        asset.EmployeeId = dto.EmployeeId;

        await db.SaveChangesAsync();
        await audit.RecordAsync("Updated", nameof(CompanyAsset), id, asset.Tag);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> Delete(int id)
    {
        var asset = await db.CompanyAssets.FindAsync(id);
        if (asset is null)
        {
            return NotFound();
        }

        db.CompanyAssets.Remove(asset);
        await db.SaveChangesAsync();
        await audit.RecordAsync("Deleted", nameof(CompanyAsset), id, asset.Tag);
        return NoContent();
    }

    private static CompanyAssetDto ToDto(CompanyAsset asset) =>
        new(asset.Id, asset.Tag, asset.Name, asset.Category, asset.Value, asset.AssignedDate, asset.ReturnDate, asset.Status, asset.Condition, asset.EmployeeId, asset.Employee is null ? null : $"{asset.Employee.FirstName} {asset.Employee.LastName}");
}
