using ForgeMart.Api.Authentication;
using ForgeMart.Api.Contracts;
using ForgeMart.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ForgeMart.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/orders")]
public sealed class AdminOrdersController(OrderService orders, ICurrentUser currentUser) : ControllerBase
{
    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<OrderResponse>> UpdateStatus(
        Guid id,
        UpdateOrderStatusRequest request,
        CancellationToken cancellationToken) =>
        Ok(await orders.UpdateStatusAsync(id, request.Status, currentUser.Id, cancellationToken));
}

