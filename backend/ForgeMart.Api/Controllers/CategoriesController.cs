using ForgeMart.Api.Contracts;
using ForgeMart.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ForgeMart.Api.Controllers;

[ApiController]
[Route("api/v1/categories")]
public sealed class CategoriesController(CategoryService categories) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryResponse>>> List(bool includeInactive, CancellationToken cancellationToken) =>
        Ok(await categories.ListAsync(User.IsInRole("Admin") && includeInactive, cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<CategoryResponse>> Create(UpsertCategoryRequest request, CancellationToken cancellationToken)
    {
        var response = await categories.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(List), response);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CategoryResponse>> Update(Guid id, UpsertCategoryRequest request, CancellationToken cancellationToken) =>
        Ok(await categories.UpdateAsync(id, request, cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await categories.SetActiveAsync(id, false, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:guid}/reactivate")]
    public async Task<IActionResult> Reactivate(Guid id, CancellationToken cancellationToken)
    {
        await categories.SetActiveAsync(id, true, cancellationToken);
        return NoContent();
    }
}
