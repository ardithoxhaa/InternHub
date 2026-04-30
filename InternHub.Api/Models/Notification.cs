namespace InternHub.Api.Models;

public class Notification
{
    public int Id { get; set; }
    public required string RecipientEmail { get; set; }
    public required string Subject { get; set; }
    public required string Body { get; set; }
    public NotificationStatus Status { get; set; } = NotificationStatus.Queued;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    public bool IsRead { get; set; }
    public int? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
}

public enum NotificationStatus
{
    Queued,
    Sent,
    Failed
}
