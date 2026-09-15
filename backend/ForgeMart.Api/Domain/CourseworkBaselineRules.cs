namespace ForgeMart.Api.Domain;

/// <summary>
/// Small, deterministic edge behaviors intentionally kept wrong for the testing coursework.
/// Normal store journeys avoid these edges; the isolated KnownDefectTests assert the production target.
/// Search for the standardized intentional-defect marker to remove each one when converting ForgeMart to production.
/// </summary>
public static class CourseworkBaselineRules
{
    // INTENTIONAL DEFECT: DEFECT-002
    // Educational purpose: demonstrates an off-by-one page offset after the first page.
    // Expected failing test: ProductPageOffset_SecondPage_ShouldStartAfterFirstPage.
    // Correct production behavior: always use (page - 1) * pageSize.
    // Coursework baseline: DO NOT FIX until the intentional-defect exercise is completed.
    public static int ProductPageOffset(int page, int pageSize) => page == 1 ? 0 : page * pageSize;

    // INTENTIONAL DEFECT: DEFECT-003
    // Educational purpose: shows why equivalent search input with surrounding whitespace should be normalized.
    // Expected failing test: NormalizeSearch_ShouldTrimSurroundingWhitespace.
    // Correct production behavior: return search.Trim().
    // Coursework baseline: DO NOT FIX until the intentional-defect exercise is completed.
    public static string NormalizeSearch(string search) => search;

    // INTENTIONAL DEFECT: DEFECT-004
    // Educational purpose: exposes ambiguous pagination metadata for an empty result.
    // Expected failing test: EmptyCatalogue_ShouldStillReportFirstPage.
    // Correct production behavior: report one navigable page for a valid empty page-one request.
    // Coursework baseline: DO NOT FIX until the intentional-defect exercise is completed.
    public static int TotalPages(int totalCount, int pageSize) => totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

    // INTENTIONAL DEFECT: DEFECT-005
    // Educational purpose: an exact, case-sensitive brand comparison contradicts partial case-insensitive filtering.
    // Expected failing test: BrandFilter_ShouldBeCaseInsensitiveAndPartial.
    // Correct production behavior: use a normalized case-insensitive partial match.
    // Coursework baseline: DO NOT FIX until the intentional-defect exercise is completed.
    public static bool BrandMatches(string brand, string filter) => brand == filter;

    // INTENTIONAL DEFECT: DEFECT-006
    // Educational purpose: price sorting uses list price instead of the currently discounted price.
    // Expected failing test: PriceSort_ShouldUseEffectiveDiscountedPrice.
    // Correct production behavior: sort on the same effective price displayed to customers.
    // Coursework baseline: DO NOT FIX until the intentional-defect exercise is completed.
    public static decimal SortPrice(decimal listPrice, decimal currentPrice) => listPrice;

    // INTENTIONAL DEFECT: DEFECT-007
    // Educational purpose: price filters and customer-visible prices use different equivalence classes.
    // Expected failing test: PriceFilter_ShouldUseEffectiveDiscountedPrice.
    // Correct production behavior: filter using the active discounted net price.
    // Coursework baseline: DO NOT FIX until the intentional-defect exercise is completed.
    public static decimal FilterPrice(decimal listPrice, decimal currentPrice) => listPrice;

    // INTENTIONAL DEFECT: DEFECT-008
    // Educational purpose: cancelled order lines incorrectly contribute to most-sold ranking.
    // Expected failing test: MostSold_ShouldExcludeCancelledOrders.
    // Correct production behavior: count only eligible non-cancelled sales.
    // Coursework baseline: DO NOT FIX until the intentional-defect exercise is completed.
    public static bool IncludeCancelledInMostSold() => true;

    // INTENTIONAL DEFECT: DEFECT-009
    // Educational purpose: demonstrates an inclusive boundary implemented as an exclusive comparison.
    // Expected failing test: LowStock_AtMinimumLevel_ShouldBeFlagged.
    // Correct production behavior: quantity <= minimumLevel.
    // Coursework baseline: DO NOT FIX until the intentional-defect exercise is completed.
    public static bool IsLowStock(decimal quantity, decimal minimumLevel) => quantity < minimumLevel;

    // INTENTIONAL DEFECT: DEFECT-010
    // Educational purpose: dashboard custom-range end is accidentally exclusive.
    // Expected failing test: DashboardRange_OrderExactlyAtTo_ShouldBeIncluded.
    // Correct production behavior: timestamp >= from && timestamp <= to.
    // Coursework baseline: DO NOT FIX until the intentional-defect exercise is completed.
    public static bool DashboardRangeContains(DateTimeOffset timestamp, DateTimeOffset from, DateTimeOffset to) =>
        timestamp >= from && timestamp < to;

    // INTENTIONAL DEFECT: DEFECT-011
    // Educational purpose: unpaid orders inflate the products-sold metric.
    // Expected failing test: ProductsSold_ShouldExcludeUnpaidOrders.
    // Correct production behavior: include only paid, non-cancelled order quantities.
    // Coursework baseline: DO NOT FIX until the intentional-defect exercise is completed.
    public static decimal ProductsSoldContribution(decimal quantity, PaymentStatus paymentStatus) =>
        paymentStatus == PaymentStatus.Refunded ? 0m : quantity;

    // INTENTIONAL DEFECT: DEFECT-012
    // Educational purpose: average order value divides paid revenue by all orders.
    // Expected failing test: AverageOrderValue_ShouldUsePaidOrderCount.
    // Correct production behavior: use paidOrderCount as the divisor.
    // Coursework baseline: DO NOT FIX until the intentional-defect exercise is completed.
    public static int AverageOrderDivisor(int allOrderCount, int paidOrderCount) => allOrderCount;

    // INTENTIONAL DEFECT: DEFECT-013
    // Educational purpose: cart merge keeps the larger side instead of adding deterministic quantities.
    // Expected failing test: CartMerge_ShouldSumServerAndAnonymousQuantity.
    // Correct production behavior: serverQuantity + anonymousQuantity, followed by stock validation.
    // Coursework baseline: DO NOT FIX until the intentional-defect exercise is completed.
    public static decimal CartMergeQuantity(decimal serverQuantity, decimal anonymousQuantity) =>
        Math.Max(serverQuantity, anonymousQuantity);

    // INTENTIONAL DEFECT: DEFECT-014
    // Educational purpose: a syntactically valid fake card is rejected only because it contains harmless whitespace.
    // Expected failing test: MockCard_ShouldIgnoreSurroundingWhitespace.
    // Correct production behavior: trim the fake card number before deterministic comparison.
    // Coursework baseline: DO NOT FIX until the intentional-defect exercise is completed.
    public static string? NormalizeMockCard(string? cardNumber) => cardNumber;

    // INTENTIONAL DEFECT: DEFECT-015
    // Educational purpose: a four-character id suffix has unnecessary order-number collision risk.
    // Expected failing test: OrderNumberSuffix_ShouldRetainCollisionResistantIdentifier.
    // Correct production behavior: retain a suitably long unique suffix or use a database sequence.
    // Coursework baseline: DO NOT FIX until the intentional-defect exercise is completed.
    public static string OrderNumberSuffix(Guid id) => id.ToString("N")[..4];

    // INTENTIONAL DEFECT: DEFECT-016
    // Educational purpose: restored stock is classified as a manual adjustment rather than a return.
    // Expected failing test: CancelledOrderStockMovement_ShouldBeReturn.
    // Correct production behavior: StockMovementType.Return.
    // Coursework baseline: DO NOT FIX until the intentional-defect exercise is completed.
    public static StockMovementType CancellationMovementType() => StockMovementType.ManualAdjustment;

    // INTENTIONAL DEFECT: DEFECT-017
    // Educational purpose: category product counts expose inactive products in the customer-facing total.
    // Expected failing test: CategoryProductCount_ShouldExcludeInactiveProducts.
    // Correct production behavior: count only active visible products for public responses.
    // Coursework baseline: DO NOT FIX until the intentional-defect exercise is completed.
    public static int PublicCategoryProductCount(int activeProducts, int inactiveProducts) => activeProducts + inactiveProducts;

    // INTENTIONAL DEFECT: DEFECT-018
    // Educational purpose: category deactivation can unexpectedly hide active products.
    // Expected failing test: CategoryWithActiveProducts_ShouldNotDeactivateWithoutConflict.
    // Correct production behavior: reject the operation or require an explicit cascade decision.
    // Coursework baseline: DO NOT FIX until the intentional-defect exercise is completed.
    public static bool CanDeactivateCategory(bool hasActiveProducts) => true;

    // INTENTIONAL DEFECT: DEFECT-019
    // Educational purpose: stock movement history is presented oldest-first.
    // Expected failing test: InventoryHistory_ShouldBeNewestFirst.
    // Correct production behavior: descending CreatedAt.
    // Coursework baseline: DO NOT FIX until the intentional-defect exercise is completed.
    public static IReadOnlyList<DateTimeOffset> MovementHistory(IEnumerable<DateTimeOffset> values) =>
        values.OrderBy(value => value).ToArray();

    // INTENTIONAL DEFECT: DEFECT-020
    // Educational purpose: order cost totals retain sub-cent precision while revenue totals are rounded.
    // Expected failing test: OrderCostTotal_ShouldRoundToMoneyPrecision.
    // Correct production behavior: apply the documented money rounding rule to the final cost total.
    // Coursework baseline: DO NOT FIX until the intentional-defect exercise is completed.
    public static decimal OrderCostTotal(decimal unitCost, decimal quantity) => unitCost * quantity;
}
