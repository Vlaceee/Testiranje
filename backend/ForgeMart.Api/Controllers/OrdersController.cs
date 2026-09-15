using ForgeMart.Api.Authentication;
using ForgeMart.Api.Contracts;
using ForgeMart.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ForgeMart.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/orders")]
public sealed class OrdersController(OrderService orders, ICurrentUser currentUser) : ControllerBase
{
    [HttpPost("checkout")]
    public async Task<ActionResult<OrderResponse>> Checkout(CheckoutRequest request, CancellationToken cancellationToken)
    {
        var response = await orders.CheckoutAsync(currentUser.Id, request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = response.Id }, response);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<OrderResponse>>> List(
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await orders.ListAsync(currentUser.Id, currentUser.IsAdmin, page, pageSize, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderResponse>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await orders.GetAsync(id, currentUser.Id, currentUser.IsAdmin, cancellationToken));

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<OrderResponse>> Cancel(Guid id, CancellationToken cancellationToken) =>
        Ok(await orders.CancelAsync(id, currentUser.Id, cancellationToken));
}

