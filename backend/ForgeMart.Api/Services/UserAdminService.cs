using ForgeMart.Api.Contracts;
using ForgeMart.Api.Data;
using ForgeMart.Api.Domain;
using ForgeMart.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ForgeMart.Api.Services;

public sealed class UserAdminService(ForgeMartDbContext db, TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<UserAdminResponse>> ListAsync(CancellationToken cancellationToken) =>
        await db.Users.AsNoTracking().OrderBy(user => user.Email)
            .Select(user => new UserAdminResponse(
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.Role,
                user.IsActive,
                user.CreatedAt))
            .ToListAsync(cancellationToken);

    public async Task<UserAdminResponse> SetActiveAsync(
        Guid userId,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken)
            ?? throw ApiException.NotFound("User was not found.");
        if (user.IsActive == isActive)
        {
            throw ApiException.Conflict(isActive ? "User is already active." : "User is already disabled.");
        }

        if (!isActive && user.Role == UserRole.Admin)
        {
            var activeAdminCount = await db.Users.CountAsync(
                candidate => candidate.Role == UserRole.Admin && candidate.IsActive,
                cancellationToken);
            if (activeAdminCount <= 1)
            {
                throw ApiException.Conflict("The final active administrator cannot be disabled.");
            }
        }

        user.IsActive = isActive;
        user.UpdatedAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(cancellationToken);
        return new UserAdminResponse(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Role,
            user.IsActive,
            user.CreatedAt);
    }
}

