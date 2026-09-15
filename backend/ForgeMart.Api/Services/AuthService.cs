using System.Text.RegularExpressions;
using ForgeMart.Api.Authentication;
using ForgeMart.Api.Contracts;
using ForgeMart.Api.Data;
using ForgeMart.Api.Domain;
using ForgeMart.Api.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ForgeMart.Api.Services;

public sealed partial class AuthService(
    ForgeMartDbContext db,
    IPasswordHasher<User> passwordHasher,
    ITokenService tokenService)
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(user => user.Email == email, cancellationToken))
        {
            throw ApiException.Conflict("An account with this email address already exists.");
        }

        if (!PasswordComplexityRegex().IsMatch(request.Password))
        {
            throw ApiException.BadRequest("Password must contain an uppercase letter, lowercase letter, number, and symbol.");
        }

        var user = new User
        {
            Email = email,
            PasswordHash = string.Empty,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Role = UserRole.Customer
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        user.Cart = new Cart { UserId = user.Id };
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return tokenService.Create(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(candidate => candidate.Email == email, cancellationToken);
        if (user is null)
        {
            throw ApiException.Unauthorized("Email or password is incorrect.");
        }

        if (!user.IsActive)
        {
            throw ApiException.Forbidden("This account has been disabled.");
        }

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            throw ApiException.Unauthorized("Email or password is incorrect.");
        }

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
            user.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }

        return tokenService.Create(user);
    }

    public async Task<CurrentUserResponse> GetCurrentAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken)
            ?? throw ApiException.Unauthorized("The authenticated user no longer exists.");
        if (!user.IsActive)
        {
            throw ApiException.Forbidden("This account has been disabled.");
        }

        return ToResponse(user);
    }

    public static CurrentUserResponse ToResponse(User user) => new(
        user.Id,
        user.Email,
        user.FirstName,
        user.LastName,
        user.Role.ToString(),
        user.IsActive);

    [GeneratedRegex("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[^A-Za-z0-9]).{8,100}$")]
    private static partial Regex PasswordComplexityRegex();
}

