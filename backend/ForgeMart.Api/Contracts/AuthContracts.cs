using System.ComponentModel.DataAnnotations;

namespace ForgeMart.Api.Contracts;

public sealed record RegisterRequest(
    [Required, EmailAddress, MaxLength(254)] string Email,
    [Required, MinLength(8), MaxLength(100)] string Password,
    [Required, MinLength(2), MaxLength(100)] string FirstName,
    [Required, MinLength(2), MaxLength(100)] string LastName);

public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public sealed record AuthResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    CurrentUserResponse User);

public sealed record CurrentUserResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    bool IsActive);
