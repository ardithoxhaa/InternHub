namespace InternHub.Api.Models;

public class CompanyAsset
{
    public int Id { get; set; }
    public required string Tag { get; set; }
    public required string Name { get; set; }
    public required string Category { get; set; }
    public decimal Value { get; set; }
    public DateOnly AssignedDate { get; set; }
    public DateOnly? ReturnDate { get; set; }
    public AssetStatus Status { get; set; } = AssetStatus.Assigned;
    public AssetCondition Condition { get; set; } = AssetCondition.Good;
    public int? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
}

public enum AssetCondition
{
    New,
    Good,
    NeedsRepair,
    Retired
}

public enum AssetStatus
{
    Available,
    Assigned,
    Returned,
    Retired,
    Lost
}
