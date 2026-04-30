using InternHub.Api.Models;
using System.ComponentModel.DataAnnotations;

namespace InternHub.Api.Contracts;

public record OnboardingTaskDto(int Id, string Title, string? Notes, DateOnly DueDate, TaskPriority Priority, OnboardingTaskStatus Status, int EmployeeId, string EmployeeName);

public record OnboardingTaskUpsertDto(
    [Required, MaxLength(160)] string Title,
    [MaxLength(800)] string? Notes,
    DateOnly DueDate,
    TaskPriority Priority,
    OnboardingTaskStatus Status,
    [Range(1, int.MaxValue)] int EmployeeId);
