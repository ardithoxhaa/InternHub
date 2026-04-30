namespace InternHub.Api.Models;

public class OnboardingTemplate
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string DepartmentScope { get; set; }
    public bool IsActive { get; set; } = true;
    public List<OnboardingTemplateItem> Items { get; set; } = [];
}

public class OnboardingTemplateItem
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public string? Notes { get; set; }
    public int DueOffsetDays { get; set; }
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public int OnboardingTemplateId { get; set; }
    public OnboardingTemplate? OnboardingTemplate { get; set; }
}
