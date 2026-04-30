namespace InternHub.Api.Models;

public class TaskComment
{
    public int Id { get; set; }
    public required string Author { get; set; }
    public required string Body { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int OnboardingTaskId { get; set; }
    public OnboardingTask? OnboardingTask { get; set; }
}
