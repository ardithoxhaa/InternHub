using InternHub.Api.Contracts;
using InternHub.Api.Data;
using InternHub.Api.Models;
using InternHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(InternHubDbContext db, PasswordService passwords, TokenService tokens, EmailService emailService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResultDto>> Login(LoginDto dto)
    {
        var user = await db.AppUsers.FirstOrDefaultAsync(u => u.Email == dto.Email && u.IsActive);
        if (user is null || !passwords.Verify(dto.Password, user.PasswordHash))
        {
            return Unauthorized("Invalid email or password.");
        }

        return Ok(new AuthResultDto(tokens.CreateToken(user), user.FullName, user.Email, user.Role));
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResultDto>> Register(RegisterDto dto)
    {
        var isPrivilegedCreator = User.Identity?.IsAuthenticated == true && User.IsInRole("Admin");
        var requestedRole = isPrivilegedCreator ? dto.Role : "Intern";

        if (await db.AppUsers.AnyAsync(u => u.Email == dto.Email))
        {
            return Conflict("A user with this email already exists.");
        }

        var user = new AppUser
        {
            FullName = dto.FullName,
            Email = dto.Email,
            Role = requestedRole,
            PasswordHash = passwords.Hash(dto.Password),
            EmployeeId = (await db.Employees.FirstOrDefaultAsync(e => e.Email == dto.Email))?.Id
        };

        db.AppUsers.Add(user);
        db.Notifications.Add(new Notification
        {
            EmployeeId = user.EmployeeId,
            RecipientEmail = user.Email,
            Subject = "Welcome to InternHub",
            Body = $"Hi {user.FullName}, your InternHub account is ready.",
            Status = NotificationStatus.Queued
        });
        await db.SaveChangesAsync();

        var sent = await emailService.SendAsync(new EmailMessage(
            user.Email,
            "Welcome to InternHub",
            $"""
            <h2>Welcome to InternHub, {user.FullName}</h2>
            <p>Your account has been created with the <strong>{user.Role}</strong> role.</p>
            <p>You can now sign in and follow your onboarding work from the My Work page.</p>
            """));

        var notification = await db.Notifications
            .OrderByDescending(n => n.Id)
            .FirstAsync(n => n.RecipientEmail == user.Email && n.Subject == "Welcome to InternHub");
        notification.Status = sent ? NotificationStatus.Sent : NotificationStatus.Queued;
        notification.SentAt = sent ? DateTime.UtcNow : null;
        await db.SaveChangesAsync();

        return Ok(new AuthResultDto(tokens.CreateToken(user), user.FullName, user.Email, user.Role));
    }

    [AllowAnonymous]
    [HttpPost("accept-invite")]
    public async Task<ActionResult<AuthResultDto>> AcceptInvite(AcceptInviteDto dto)
    {
        var invite = await db.EmployeeInvites.FirstOrDefaultAsync(i => i.Token == dto.Token);
        if (invite is null || invite.AcceptedAt is not null || invite.ExpiresAt < DateTime.UtcNow)
        {
            return BadRequest("Invite is invalid or expired.");
        }

        if (await db.AppUsers.AnyAsync(u => u.Email == invite.Email))
        {
            return Conflict("A user with this email already exists.");
        }

        var user = new AppUser
        {
            FullName = invite.FullName,
            Email = invite.Email,
            Role = invite.Role,
            PasswordHash = passwords.Hash(dto.Password),
            EmployeeId = invite.EmployeeId
        };

        invite.AcceptedAt = DateTime.UtcNow;
        db.AppUsers.Add(user);
        db.Notifications.Add(new Notification
        {
            EmployeeId = invite.EmployeeId,
            RecipientEmail = invite.Email,
            Subject = "InternHub invite accepted",
            Body = $"Hi {invite.FullName}, your account is now active.",
            Status = NotificationStatus.Queued
        });
        await db.SaveChangesAsync();

        return Ok(new AuthResultDto(tokens.CreateToken(user), user.FullName, user.Email, user.Role));
    }
}
