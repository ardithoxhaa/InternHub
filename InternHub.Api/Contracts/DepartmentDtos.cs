using System.ComponentModel.DataAnnotations;

namespace InternHub.Api.Contracts;

public record DepartmentDto(int Id, string Name, string Code, string LeadName, string? Description, decimal Budget, int EmployeeCount);

public record DepartmentUpsertDto(
    [Required, MaxLength(120)] string Name,
    [Required, MaxLength(16)] string Code,
    [Required, MaxLength(120)] string LeadName,
    [MaxLength(500)] string? Description,
    [Range(0, 99999999)] decimal Budget);
