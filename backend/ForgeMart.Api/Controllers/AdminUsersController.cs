using ForgeMart.Api.Contracts;
using ForgeMart.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ForgeMart.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/users")]
public sealed class AdminUsersController(UserAdminService users) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserAdminResponse>>> List(CancellationToken cancellationToken) =>
        Ok(await users.ListAsync(cancellationToken));

    [HttpPut("{id:guid}/active")]
    public async Task<ActionResult<UserAdminResponse>> SetActive(
        Guid id,
        SetUserActiveRequest request,
        CancellationToken cancellationToken) =>
        Ok(await users.SetActiveAsync(id, request.IsActive, cancellationToken));
}
