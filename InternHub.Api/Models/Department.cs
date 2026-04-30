namespace InternHub.Api.Models;

public class Department
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Code { get; set; }
    public required string LeadName { get; set; }
    public string? Description { get; set; }
    public decimal Budget { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Employee> Employees { get; set; } = [];
}
