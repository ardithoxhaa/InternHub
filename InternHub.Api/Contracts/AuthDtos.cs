using System.ComponentModel.DataAnnotations;

namespace InternHub.Api.Contracts;

public record LoginDto([Required, EmailAddress] string Email, [Required] string Password);
public record RegisterDto([Required] string FullName, [Required, EmailAddress] string Email, [Required, MinLength(6)] string Password, [Required] string Role);
public record AuthResultDto(string Token, string FullName, string Email, string Role);
