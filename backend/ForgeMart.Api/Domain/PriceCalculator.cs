namespace ForgeMart.Api.Domain;

public static class PriceCalculator
{
    public static bool IsDiscountActive(
        decimal discountPercentage,
        DateTimeOffset? start,
        DateTimeOffset? end,
        DateTimeOffset now) =>
        discountPercentage > 0m &&
        (!start.HasValue || start.Value <= now) &&
        (!end.HasValue || end.Value >= now);

    public static decimal DiscountedNetPrice(
        decimal price,
        decimal discountPercentage,
        DateTimeOffset? start,
        DateTimeOffset? end,
        DateTimeOffset now)
    {
        var multiplier = IsDiscountActive(discountPercentage, start, end, now)
            ? 1m - discountPercentage / 100m
            : 1m;
        return RoundMoney(price * multiplier);
    }

    public static decimal Vat(decimal netAmount, decimal vatRate) =>
        RoundMoney(netAmount * vatRate / 100m);

    public static decimal RoundMoney(decimal value) =>
        decimal.Round(value, 2, MidpointRounding.AwayFromZero);
}

