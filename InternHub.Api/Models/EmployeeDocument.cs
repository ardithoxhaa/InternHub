namespace InternHub.Api.Models;

public class EmployeeDocument
{
    public int Id { get; set; }
    public required string FileName { get; set; }
    public required string DocumentType { get; set; }
    public required string StoredPath { get; set; }
    public long SizeBytes { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Pending;
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? RejectionReason { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
}

public enum ApprovalStatus
{
    Pending,
    Approved,
    Rejected
}
