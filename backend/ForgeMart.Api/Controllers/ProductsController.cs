using ForgeMart.Api.Authentication;
using ForgeMart.Api.Contracts;
using ForgeMart.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ForgeMart.Api.Controllers;

[ApiController]
[Route("api/v1/products")]
public sealed class ProductsController(ProductService products, ICurrentUser currentUser) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductResponse>>> Search([FromQuery] ProductQuery query, CancellationToken cancellationToken) =>
        Ok(await products.SearchAsync(query, User.Identity?.IsAuthenticated == true && currentUser.IsAdmin, cancellationToken));

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDetailResponse>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await products.GetAsync(id, User.Identity?.IsAuthenticated == true && currentUser.IsAdmin, cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<ProductDetailResponse>> Create(UpsertProductRequest request, CancellationToken cancellationToken)
    {
        var result = await products.CreateAsync(request, currentUser.Id, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductDetailResponse>> Update(Guid id, UpsertProductRequest request, CancellationToken cancellationToken) =>
        Ok(await products.UpdateAsync(id, request, cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await products.SetActiveAsync(id, false, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:guid}/reactivate")]
    public async Task<IActionResult> Reactivate(Guid id, CancellationToken cancellationToken)
    {
        await products.SetActiveAsync(id, true, cancellationToken);
        return NoContent();
    }
}

