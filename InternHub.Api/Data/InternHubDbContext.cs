using InternHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace InternHub.Api.Data;

public class InternHubDbContext(DbContextOptions<InternHubDbContext> options) : DbContext(options)
{
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<OnboardingTask> OnboardingTasks => Set<OnboardingTask>();
    public DbSet<CompanyAsset> CompanyAssets => Set<CompanyAsset>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<EmployeeDocument> EmployeeDocuments => Set<EmployeeDocument>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<OnboardingTemplate> OnboardingTemplates => Set<OnboardingTemplate>();
    public DbSet<OnboardingTemplateItem> OnboardingTemplateItems => Set<OnboardingTemplateItem>();
    public DbSet<TaskComment> TaskComments => Set<TaskComment>();
    public DbSet<TeamChatMessage> TeamChatMessages => Set<TeamChatMessage>();
    public DbSet<EmployeeInvite> EmployeeInvites => Set<EmployeeInvite>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasIndex(d => d.Code).IsUnique();
            entity.Property(d => d.Name).HasMaxLength(120).IsRequired();
            entity.Property(d => d.Code).HasMaxLength(16).IsRequired();
            entity.Property(d => d.LeadName).HasMaxLength(120).IsRequired();
            entity.Property(d => d.Budget).HasColumnType("decimal(12,2)");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.FirstName).HasMaxLength(80).IsRequired();
            entity.Property(e => e.LastName).HasMaxLength(80).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(160).IsRequired();
            entity.Property(e => e.Role).HasMaxLength(120).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);
        });

        modelBuilder.Entity<OnboardingTask>(entity =>
        {
            entity.Property(t => t.Title).HasMaxLength(160).IsRequired();
            entity.Property(t => t.Priority).HasConversion<string>().HasMaxLength(32);
            entity.Property(t => t.Status).HasConversion<string>().HasMaxLength(32);
        });

        modelBuilder.Entity<CompanyAsset>(entity =>
        {
            entity.HasIndex(a => a.Tag).IsUnique();
            entity.Property(a => a.Tag).HasMaxLength(40).IsRequired();
            entity.Property(a => a.Name).HasMaxLength(140).IsRequired();
            entity.Property(a => a.Category).HasMaxLength(80).IsRequired();
            entity.Property(a => a.Value).HasColumnType("decimal(12,2)");
            entity.Property(a => a.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(a => a.Condition).HasConversion<string>().HasMaxLength(32);
        });

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.FullName).HasMaxLength(140).IsRequired();
            entity.Property(u => u.Email).HasMaxLength(160).IsRequired();
            entity.Property(u => u.PasswordHash).HasMaxLength(260).IsRequired();
            entity.Property(u => u.Role).HasMaxLength(40).IsRequired();
            entity.HasOne(u => u.Employee).WithMany().HasForeignKey(u => u.EmployeeId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Employee>()
            .HasOne(e => e.ManagerUser)
            .WithMany()
            .HasForeignKey(e => e.ManagerUserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.Property(a => a.Actor).HasMaxLength(160).IsRequired();
            entity.Property(a => a.Action).HasMaxLength(80).IsRequired();
            entity.Property(a => a.EntityName).HasMaxLength(80).IsRequired();
        });

        modelBuilder.Entity<EmployeeDocument>(entity =>
        {
            entity.Property(d => d.FileName).HasMaxLength(220).IsRequired();
            entity.Property(d => d.DocumentType).HasMaxLength(80).IsRequired();
            entity.Property(d => d.StoredPath).HasMaxLength(500).IsRequired();
            entity.Property(d => d.ApprovalStatus).HasConversion<string>().HasMaxLength(32);
            entity.Property(d => d.RejectionReason).HasMaxLength(500);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.Property(n => n.RecipientEmail).HasMaxLength(160).IsRequired();
            entity.Property(n => n.Subject).HasMaxLength(220).IsRequired();
            entity.Property(n => n.Status).HasConversion<string>().HasMaxLength(32);
        });

        modelBuilder.Entity<OnboardingTemplate>(entity =>
        {
            entity.Property(t => t.Name).HasMaxLength(140).IsRequired();
            entity.Property(t => t.DepartmentScope).HasMaxLength(80).IsRequired();
        });

        modelBuilder.Entity<OnboardingTemplateItem>(entity =>
        {
            entity.Property(i => i.Title).HasMaxLength(160).IsRequired();
            entity.Property(i => i.Priority).HasConversion<string>().HasMaxLength(32);
        });

        modelBuilder.Entity<TaskComment>(entity =>
        {
            entity.Property(c => c.Author).HasMaxLength(160).IsRequired();
            entity.Property(c => c.Body).HasMaxLength(1200).IsRequired();
        });

        modelBuilder.Entity<TeamChatMessage>(entity =>
        {
            entity.Property(m => m.SenderName).HasMaxLength(140).IsRequired();
            entity.Property(m => m.SenderEmail).HasMaxLength(160).IsRequired();
            entity.Property(m => m.Body).HasMaxLength(1200).IsRequired();
        });

        modelBuilder.Entity<EmployeeInvite>(entity =>
        {
            entity.HasIndex(i => i.Token).IsUnique();
            entity.Property(i => i.Email).HasMaxLength(160).IsRequired();
            entity.Property(i => i.FullName).HasMaxLength(140).IsRequired();
            entity.Property(i => i.Role).HasMaxLength(40).IsRequired();
            entity.Property(i => i.Token).HasMaxLength(80).IsRequired();
            entity.HasOne(i => i.Employee).WithMany().HasForeignKey(i => i.EmployeeId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AppSetting>(entity =>
        {
            entity.HasIndex(s => s.Key).IsUnique();
            entity.Property(s => s.Key).HasMaxLength(80).IsRequired();
            entity.Property(s => s.Value).HasMaxLength(1000).IsRequired();
            entity.Property(s => s.UpdatedBy).HasMaxLength(160);
        });
    }
}
