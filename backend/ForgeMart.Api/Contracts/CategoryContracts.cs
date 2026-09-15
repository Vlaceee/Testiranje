using System.ComponentModel.DataAnnotations;

namespace ForgeMart.Api.Contracts;

public sealed record CategoryResponse(Guid Id, string Name, string Description, bool IsActive, int ProductCount);

public sealed record UpsertCategoryRequest(
    [Required, MinLength(2), MaxLength(100)] string Name,
    [Required, MinLength(10), MaxLength(600)] string Description);
