using InternHub.Api.Contracts;
using InternHub.Api.Data;
using InternHub.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace InternHub.Api.Hubs;

[Authorize]
public class TeamChatHub(InternHubDbContext db) : Hub
{
    public async Task SendMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return;
        }

        var name = Context.User?.Identity?.Name ?? "User";
        var email = Context.User?.FindFirstValue(ClaimTypes.Email) ?? "unknown";
        var message = new TeamChatMessage
        {
            SenderName = name,
            SenderEmail = email,
            Body = body.Trim()
        };

        db.TeamChatMessages.Add(message);
        await db.SaveChangesAsync();

        await Clients.All.SendAsync("messageReceived", new TeamChatMessageDto(message.Id, message.SenderName, message.SenderEmail, message.Body, message.CreatedAt));
    }
}
