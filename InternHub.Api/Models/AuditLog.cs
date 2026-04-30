namespace InternHub.Api.Models;

public class AuditLog
{
    public int Id { get; set; }
    public required string Actor { get; set; }
    public required string Action { get; set; }
    public required string EntityName { get; set; }
    public int? EntityId { get; set; }
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
