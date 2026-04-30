namespace InternHub.Api.Models;

public class OnboardingTask
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public string? Notes { get; set; }
    public DateOnly DueDate { get; set; }
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public OnboardingTaskStatus Status { get; set; } = OnboardingTaskStatus.ToDo;
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public List<TaskComment> Comments { get; set; } = [];
}

public enum TaskPriority
{
    Low,
    Medium,
    High,
    Critical
}

public enum OnboardingTaskStatus
{
    ToDo,
    InProgress,
    Done,
    Blocked
}
