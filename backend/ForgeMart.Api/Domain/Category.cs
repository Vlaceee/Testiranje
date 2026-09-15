namespace ForgeMart.Api.Domain;

public sealed class Category
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public required string Description { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<Product> Products { get; set; } = [];
}

