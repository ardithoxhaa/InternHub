namespace InternHub.Api.Contracts;

public record EmployeeProfileDto(
    EmployeeDto Employee,
    IEnumerable<OnboardingTaskDto> Tasks,
    IEnumerable<CompanyAssetDto> Assets,
    IEnumerable<EmployeeDocumentDto> Documents,
    IEnumerable<NotificationDto> Notifications);
