using System.Security.Claims;
using ForgeMart.Api.Infrastructure;

namespace ForgeMart.Api.Authentication;

public interface ICurrentUser
{
    Guid Id { get; }
    bool IsAdmin { get; }
}

public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid Id
    {
        get
        {
            var raw = accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(raw, out var id) ? id : throw ApiException.Unauthorized("A valid user identity is required.");
        }
    }

    public bool IsAdmin => accessor.HttpContext?.User.IsInRole("Admin") == true;
}

