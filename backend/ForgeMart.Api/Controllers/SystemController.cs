using Microsoft.AspNetCore.Mvc;

namespace ForgeMart.Api.Controllers;

[ApiController]
[Route("api/v1/system")]
public sealed class SystemController : ControllerBase
{
    [HttpGet("info")]
    [ProducesResponseType<SystemInfoResponse>(StatusCodes.Status200OK)]
    public ActionResult<SystemInfoResponse> GetInfo() => Ok(new SystemInfoResponse(
        "ForgeMart",
        "Tools & Building Supplies",
        "v1"));
}

public sealed record SystemInfoResponse(string Name, string DisplayName, string ApiVersion);
