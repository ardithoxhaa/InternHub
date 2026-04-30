using InternHub.Api.Models;
using System.ComponentModel.DataAnnotations;

namespace InternHub.Api.Contracts;

public record CompanyAssetDto(int Id, string Tag, string Name, string Category, decimal Value, DateOnly AssignedDate, DateOnly? ReturnDate, AssetStatus Status, AssetCondition Condition, int? EmployeeId, string? EmployeeName);

public record CompanyAssetUpsertDto(
    [Required, MaxLength(40)] string Tag,
    [Required, MaxLength(140)] string Name,
    [Required, MaxLength(80)] string Category,
    [Range(0, 99999999)] decimal Value,
    DateOnly AssignedDate,
    DateOnly? ReturnDate,
    AssetStatus Status,
    AssetCondition Condition,
    int? EmployeeId);
