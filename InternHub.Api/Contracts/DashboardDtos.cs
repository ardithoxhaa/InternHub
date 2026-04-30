namespace InternHub.Api.Contracts;

public record DashboardDto(int Employees, int Departments, int OpenTasks, int OverdueTasks, int Assets, decimal AssetValue, double CompletionRate, IEnumerable<DepartmentLoadDto> DepartmentLoad, IEnumerable<UpcomingTaskDto> UpcomingTasks, IEnumerable<UpcomingStartDto> UpcomingStarts);
public record DepartmentLoadDto(string Department, int Employees);
public record UpcomingTaskDto(int Id, string Title, DateOnly DueDate, string EmployeeName, string Priority, string Status);
public record UpcomingStartDto(int Id, string EmployeeName, DateOnly StartDate, string Department);
