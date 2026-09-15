using ForgeMart.Api.Contracts;
using ForgeMart.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ForgeMart.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/dashboard")]
public sealed class AdminDashboardController(DashboardService dashboard) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DashboardResponse>> Get(
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken) =>
        Ok(await dashboard.GetAsync(from, to, cancellationToken));
}

