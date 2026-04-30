using InternHub.Api.Data;
using InternHub.Api.Models;
using System.Security.Claims;

namespace InternHub.Api.Services;

public class AuditService(InternHubDbContext db, IHttpContextAccessor accessor)
{
    public async Task RecordAsync(string action, string entityName, int? entityId, string? details = null)
    {
        var actor = accessor.HttpContext?.User.Identity?.Name ?? "System";
        db.AuditLogs.Add(new AuditLog
        {
            Actor = actor,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Details = details
        });
        await db.SaveChangesAsync();
    }

    public string CurrentRole => accessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role) ?? "Guest";
}
