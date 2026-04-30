using InternHub.Api.Contracts;
using InternHub.Api.Data;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace InternHub.Api.Services;

public class AiChatService(IConfiguration configuration, IHttpClientFactory httpClientFactory, InternHubDbContext db, ILogger<AiChatService> logger)
{
    public async Task<AiChatResponseDto> ChatAsync(AiChatRequestDto request)
    {
        var context = await BuildBusinessContextAsync();
        var apiKey = configuration["OpenAI:ApiKey"];
        var model = configuration["OpenAI:Model"] ?? "gpt-5.4-mini";

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new AiChatResponseDto(LocalFallback(request.Message, context), false);
        }

        try
        {
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var input = new List<object>
            {
                new
                {
                    role = "system",
                    content = "You are InternHub Assistant, a concise HR operations assistant inside an intern onboarding app. Use the provided app context when useful. Do not invent private data."
                },
                new
                {
                    role = "user",
                    content = $"Current InternHub context:\n{context}"
                }
            };

            foreach (var item in request.History?.TakeLast(8) ?? [])
            {
                input.Add(new { role = item.Role, content = item.Content });
            }

            input.Add(new { role = "user", content = request.Message });

            var payload = JsonSerializer.Serialize(new
            {
                model,
                input,
                max_output_tokens = 500
            });

            using var response = await client.PostAsync(
                "https://api.openai.com/v1/responses",
                new StringContent(payload, Encoding.UTF8, "application/json"));

            var json = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("OpenAI chat request failed: {Status} {Body}", response.StatusCode, json);
                return new AiChatResponseDto(LocalFallback(request.Message, context), false);
            }

            using var doc = JsonDocument.Parse(json);
            var text = ExtractOutputText(doc.RootElement);
            return new AiChatResponseDto(string.IsNullOrWhiteSpace(text) ? LocalFallback(request.Message, context) : text, true);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "OpenAI chat request failed");
            return new AiChatResponseDto(LocalFallback(request.Message, context), false);
        }
    }

    private async Task<string> BuildBusinessContextAsync()
    {
        var employeeCount = await db.Employees.CountAsync();
        var openTasks = await db.OnboardingTasks.CountAsync(t => t.Status != Models.OnboardingTaskStatus.Done);
        var overdueTasks = await db.OnboardingTasks.CountAsync(t => t.Status != Models.OnboardingTaskStatus.Done && t.DueDate < DateOnly.FromDateTime(DateTime.Today));
        var assetsAssigned = await db.CompanyAssets.CountAsync(a => a.Status == Models.AssetStatus.Assigned);
        var pendingDocuments = await db.EmployeeDocuments.CountAsync(d => d.ApprovalStatus == Models.ApprovalStatus.Pending);
        var upcoming = await db.Employees
            .AsNoTracking()
            .Where(e => e.StartDate >= DateOnly.FromDateTime(DateTime.Today))
            .OrderBy(e => e.StartDate)
            .Take(5)
            .Select(e => e.FirstName + " " + e.LastName + " starts " + e.StartDate)
            .ToListAsync();

        return $"""
        Employees: {employeeCount}
        Open onboarding tasks: {openTasks}
        Overdue tasks: {overdueTasks}
        Assigned assets: {assetsAssigned}
        Pending document approvals: {pendingDocuments}
        Upcoming starts: {(upcoming.Count == 0 ? "None" : string.Join("; ", upcoming))}
        """;
    }

    private static string LocalFallback(string message, string context)
    {
        if (message.Contains("overdue", StringComparison.OrdinalIgnoreCase))
        {
            return $"I can help with overdue work. Here is the current app context:\n\n{context}\n\nConfigure OpenAI in appsettings to enable richer AI answers.";
        }

        if (message.Contains("email", StringComparison.OrdinalIgnoreCase))
        {
            return "Real email support is wired through SMTP. Update the Email section in appsettings.Development.json with your SMTP host, username, and app password, then set Enabled to true.";
        }

        return $"I am running in local fallback mode because OpenAI is not configured yet. I can still summarize the current app state:\n\n{context}";
    }

    private static string ExtractOutputText(JsonElement root)
    {
        if (root.TryGetProperty("output_text", out var outputText))
        {
            return outputText.GetString() ?? "";
        }

        if (!root.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array)
        {
            return "";
        }

        var builder = new StringBuilder();
        foreach (var item in output.EnumerateArray())
        {
            if (!item.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var contentItem in content.EnumerateArray())
            {
                if (contentItem.TryGetProperty("text", out var text))
                {
                    builder.AppendLine(text.GetString());
                }
            }
        }

        return builder.ToString().Trim();
    }
}
