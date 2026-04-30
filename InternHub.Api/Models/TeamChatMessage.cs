namespace InternHub.Api.Models;

public class TeamChatMessage
{
    public int Id { get; set; }
    public required string SenderName { get; set; }
    public required string SenderEmail { get; set; }
    public required string Body { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
