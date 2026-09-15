using ForgeMart.Api.Contracts;
using ForgeMart.Api.Data;
using ForgeMart.Api.Domain;
using ForgeMart.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ForgeMart.Api.Services;

public sealed class CategoryService(ForgeMartDbContext db)
{
    public async Task<IReadOnlyList<CategoryResponse>> ListAsync(bool includeInactive, CancellationToken cancellationToken) =>
        await db.Categories.AsNoTracking()
            .Where(category => includeInactive || category.IsActive)
            .OrderBy(category => category.Name)
            .Select(category => new CategoryResponse(category.Id, category.Name, category.Description, category.IsActive, category.Products.Count))
            .ToListAsync(cancellationToken);

    public async Task<CategoryResponse> CreateAsync(UpsertCategoryRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (await db.Categories.AnyAsync(category => EF.Functions.ILike(category.Name, name), cancellationToken))
        {
            throw ApiException.Conflict("A category with this name already exists.");
        }

        var category = new Category { Name = name, Description = request.Description.Trim() };
        db.Categories.Add(category);
        await db.SaveChangesAsync(cancellationToken);
        return new CategoryResponse(category.Id, category.Name, category.Description, category.IsActive, 0);
    }

    public async Task<CategoryResponse> UpdateAsync(Guid id, UpsertCategoryRequest request, CancellationToken cancellationToken)
    {
        var category = await db.Categories.Include(candidate => candidate.Products)
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken)
            ?? throw ApiException.NotFound("Category was not found.");
        var name = request.Name.Trim();
        if (await db.Categories.AnyAsync(candidate => candidate.Id != id && EF.Functions.ILike(candidate.Name, name), cancellationToken))
        {
            throw ApiException.Conflict("A category with this name already exists.");
        }

        category.Name = name;
        category.Description = request.Description.Trim();
        await db.SaveChangesAsync(cancellationToken);
        return new CategoryResponse(category.Id, category.Name, category.Description, category.IsActive, category.Products.Count);
    }

    public async Task SetActiveAsync(Guid id, bool active, CancellationToken cancellationToken)
    {
        var category = await db.Categories.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken)
            ?? throw ApiException.NotFound("Category was not found.");
        if (category.IsActive == active)
        {
            throw ApiException.Conflict(active ? "Category is already active." : "Category is already inactive.");
        }

        category.IsActive = active;
        await db.SaveChangesAsync(cancellationToken);
    }
}
