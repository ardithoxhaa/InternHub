using InternHub.Api.Models;
using InternHub.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace InternHub.Api.Data;

public static class SeedData
{
    public static async Task EnsureSeededAsync(InternHubDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("UPDATE CompanyAssets SET Status = 'Assigned' WHERE Status = ''");
        await db.Database.ExecuteSqlRawAsync("UPDATE EmployeeDocuments SET ApprovalStatus = 'Pending' WHERE ApprovalStatus = ''");

        var passwordService = new PasswordService();
        if (!await db.AppUsers.AnyAsync())
        {
            var arditId = await db.Employees.Where(e => e.Email == "ardit.hoxha@internhub.test").Select(e => (int?)e.Id).FirstOrDefaultAsync();
            db.AppUsers.AddRange(
                new AppUser { FullName = "Admin User", Email = "admin@internhub.test", PasswordHash = passwordService.Hash("Admin123!"), Role = "Admin" },
                new AppUser { FullName = "HR Manager", Email = "hr@internhub.test", PasswordHash = passwordService.Hash("Hr12345!"), Role = "HR" },
                new AppUser { FullName = "Team Manager", Email = "manager@internhub.test", PasswordHash = passwordService.Hash("Manager123!"), Role = "Manager" },
                new AppUser { FullName = "Intern Demo", Email = "intern@internhub.test", PasswordHash = passwordService.Hash("Intern123!"), Role = "Intern", EmployeeId = arditId });
            await db.SaveChangesAsync();
        }

        if (!await db.OnboardingTemplates.AnyAsync())
        {
            db.OnboardingTemplates.Add(new OnboardingTemplate
            {
                Name = "Standard Intern Onboarding",
                DepartmentScope = "All",
                Items =
                [
                    new OnboardingTemplateItem { Title = "Sign internship agreement", Notes = "Upload the signed agreement in the employee profile.", DueOffsetDays = -1, Priority = TaskPriority.Critical },
                    new OnboardingTemplateItem { Title = "Complete security and privacy training", Notes = "Required before production access is granted.", DueOffsetDays = 1, Priority = TaskPriority.High },
                    new OnboardingTemplateItem { Title = "Set up laptop and development tools", Notes = ".NET SDK, Node.js, SQL Server LocalDB, IDE, Git access.", DueOffsetDays = 2, Priority = TaskPriority.High },
                    new OnboardingTemplateItem { Title = "Meet assigned mentor", Notes = "Review first sprint goals and communication expectations.", DueOffsetDays = 3, Priority = TaskPriority.Medium },
                    new OnboardingTemplateItem { Title = "Submit first-week reflection", Notes = "Capture blockers, learning goals, and support needs.", DueOffsetDays = 7, Priority = TaskPriority.Medium }
                ]
            });
            await db.SaveChangesAsync();
        }

        if (await db.Departments.AnyAsync())
        {
            var existingInternUser = await db.AppUsers.FirstOrDefaultAsync(u => u.Email == "intern@internhub.test" && u.EmployeeId == null);
            var ardit = await db.Employees.FirstOrDefaultAsync(e => e.Email == "ardit.hoxha@internhub.test");
            if (existingInternUser is not null && ardit is not null)
            {
                existingInternUser.EmployeeId = ardit.Id;
                await db.SaveChangesAsync();
            }

            return;
        }

        var engineering = new Department { Name = "Engineering", Code = "ENG", LeadName = "Mira Novak", Budget = 185000, Description = "Product engineering and platform delivery." };
        var people = new Department { Name = "People Operations", Code = "POPS", LeadName = "Elian Carter", Budget = 72000, Description = "Hiring, onboarding, and employee experience." };
        var sales = new Department { Name = "Commercial", Code = "COM", LeadName = "Sofia Weber", Budget = 115000, Description = "Sales operations and customer success." };

        db.Departments.AddRange(engineering, people, sales);

        var employees = new List<Employee>
        {
            new() { FirstName = "Ardit", LastName = "Hoxha", Email = "ardit.hoxha@internhub.test", Role = "Software Engineering Intern", StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-12)), Status = EmploymentStatus.Active, Department = engineering },
            new() { FirstName = "Lina", LastName = "Kovacs", Email = "lina.kovacs@internhub.test", Role = "QA Intern", StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5)), Status = EmploymentStatus.Onboarding, Department = engineering },
            new() { FirstName = "Noah", LastName = "Bennett", Email = "noah.bennett@internhub.test", Role = "People Ops Intern", StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-30)), Status = EmploymentStatus.Active, Department = people },
            new() { FirstName = "Sara", LastName = "Morina", Email = "sara.morina@internhub.test", Role = "Customer Success Intern", StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(2)), Status = EmploymentStatus.Onboarding, Department = sales }
        };

        db.Employees.AddRange(employees);

        db.OnboardingTasks.AddRange(
            new OnboardingTask { Title = "Sign internship agreement", Notes = "Upload signed PDF to HR folder.", DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)), Priority = TaskPriority.Critical, Status = OnboardingTaskStatus.InProgress, Employee = employees[1] },
            new OnboardingTask { Title = "Complete security training", DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(3)), Priority = TaskPriority.High, Status = OnboardingTaskStatus.ToDo, Employee = employees[1] },
            new OnboardingTask { Title = "Set up development environment", Notes = ".NET SDK, Node.js, SQL Server, IDE extensions.", DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-2)), Priority = TaskPriority.High, Status = OnboardingTaskStatus.Done, Employee = employees[0] },
            new OnboardingTask { Title = "Schedule mentor check-in", DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(4)), Priority = TaskPriority.Medium, Status = OnboardingTaskStatus.ToDo, Employee = employees[3] });

        db.CompanyAssets.AddRange(
            new CompanyAsset { Tag = "LPT-1001", Name = "Dell XPS 15", Category = "Laptop", Value = 1899, AssignedDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-10)), Condition = AssetCondition.Good, Employee = employees[0] },
            new CompanyAsset { Tag = "MON-2033", Name = "LG UltraFine 27", Category = "Monitor", Value = 449, AssignedDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-10)), Condition = AssetCondition.Good, Employee = employees[0] },
            new CompanyAsset { Tag = "LPT-1048", Name = "Lenovo ThinkPad T14", Category = "Laptop", Value = 1399, AssignedDate = DateOnly.FromDateTime(DateTime.Today), Condition = AssetCondition.New, Employee = employees[1] },
            new CompanyAsset { Tag = "PHN-3102", Name = "iPhone 15", Category = "Phone", Value = 899, AssignedDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-20)), Condition = AssetCondition.Good, Employee = employees[2] });

        db.Notifications.AddRange(
            new Notification { RecipientEmail = employees[1].Email, Subject = "Welcome to InternHub", Body = "Your onboarding plan is ready.", Status = NotificationStatus.Sent, SentAt = DateTime.UtcNow.AddDays(-1), Employee = employees[1] },
            new Notification { RecipientEmail = employees[3].Email, Subject = "Upcoming first day", Body = "Please review your first-day checklist.", Status = NotificationStatus.Queued, Employee = employees[3] });

        await db.SaveChangesAsync();

        var internUser = await db.AppUsers.FirstOrDefaultAsync(u => u.Email == "intern@internhub.test");
        if (internUser is not null && internUser.EmployeeId is null)
        {
            internUser.EmployeeId = employees[0].Id;
            await db.SaveChangesAsync();
        }
    }
}
