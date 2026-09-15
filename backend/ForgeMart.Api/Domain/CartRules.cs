using ForgeMart.Api.Infrastructure;

namespace ForgeMart.Api.Domain;

public static class CartRules
{
    public static void ValidateQuantity(decimal requested, decimal available, UnitOfMeasure unit)
    {
        if (requested <= 0)
        {
            throw ApiException.BadRequest("Cart quantity must be greater than zero.");
        }

        if (requested > available)
        {
            throw ApiException.Conflict($"Only {available} {unit} is currently in stock.");
        }

        if (unit is UnitOfMeasure.Piece or UnitOfMeasure.Pack && requested != decimal.Truncate(requested))
        {
            throw ApiException.BadRequest("Piece and pack quantities must be whole numbers.");
        }
    }
}

