using ForgeMart.Api.Authentication;
using ForgeMart.Api.Contracts;
using ForgeMart.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ForgeMart.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/inventory")]
public sealed class AdminInventoryController(InventoryService inventory, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<InventoryResponse>>> List(bool lowStockOnly, CancellationToken cancellationToken) =>
        Ok(await inventory.ListAsync(lowStockOnly, cancellationToken));

    [HttpPost("{productId:guid}/adjust")]
    public async Task<ActionResult<InventoryResponse>> Adjust(
        Guid productId,
        InventoryAdjustmentRequest request,
        CancellationToken cancellationToken) =>
        Ok(await inventory.AdjustAsync(productId, request, currentUser.Id, cancellationToken));

    [HttpGet("{productId:guid}/movements")]
    public async Task<ActionResult<IReadOnlyList<StockMovementResponse>>> History(Guid productId, CancellationToken cancellationToken) =>
        Ok(await inventory.HistoryAsync(productId, cancellationToken));
}

