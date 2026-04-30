namespace InternHub.Api.Models;

public class Employee
{
    public int Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public required string Role { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public EmploymentStatus Status { get; set; } = EmploymentStatus.Onboarding;
    public int DepartmentId { get; set; }
    public int? ManagerUserId { get; set; }
    public AppUser? ManagerUser { get; set; }
    public Department? Department { get; set; }
    public List<OnboardingTask> Tasks { get; set; } = [];
    public List<CompanyAsset> Assets { get; set; } = [];
    public List<EmployeeDocument> Documents { get; set; } = [];
    public List<Notification> Notifications { get; set; } = [];
}

public enum EmploymentStatus
{
    Candidate,
    Onboarding,
    Active,
    Completed,
    OnLeave,
    Exited
}
