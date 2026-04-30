using InternHub.Api.Models;
using System.ComponentModel.DataAnnotations;

namespace InternHub.Api.Contracts;

public record EmployeeDto(int Id, string FirstName, string LastName, string FullName, string Email, string Role, DateOnly StartDate, DateOnly? EndDate, EmploymentStatus Status, int DepartmentId, string DepartmentName, int OpenTaskCount, int AssetCount);

public record EmployeeUpsertDto(
    [Required, MaxLength(80)] string FirstName,
    [Required, MaxLength(80)] string LastName,
    [Required, EmailAddress, MaxLength(160)] string Email,
    [Required, MaxLength(120)] string Role,
    DateOnly StartDate,
    DateOnly? EndDate,
    EmploymentStatus Status,
    [Range(1, int.MaxValue)] int DepartmentId);
