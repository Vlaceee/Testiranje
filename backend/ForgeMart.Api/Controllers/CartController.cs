using ForgeMart.Api.Authentication;
using ForgeMart.Api.Contracts;
using ForgeMart.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ForgeMart.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/cart")]
public sealed class CartController(CartService cart, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CartResponse>> Get(CancellationToken cancellationToken) =>
        Ok(await cart.GetAsync(currentUser.Id, cancellationToken));

    [HttpPut("items/{productId:guid}")]
    public async Task<ActionResult<CartResponse>> SetItem(
        Guid productId,
        SetCartItemRequest request,
        CancellationToken cancellationToken) =>
        Ok(await cart.SetItemAsync(currentUser.Id, productId, request.Quantity, cancellationToken));

    [HttpDelete("items/{productId:guid}")]
    public async Task<ActionResult<CartResponse>> RemoveItem(Guid productId, CancellationToken cancellationToken) =>
        Ok(await cart.RemoveItemAsync(currentUser.Id, productId, cancellationToken));

    [HttpPost("merge")]
    public async Task<ActionResult<CartResponse>> Merge(MergeCartRequest request, CancellationToken cancellationToken) =>
        Ok(await cart.MergeAsync(currentUser.Id, request, cancellationToken));
}

