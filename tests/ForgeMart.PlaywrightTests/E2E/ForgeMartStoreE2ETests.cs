using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using System.Text.RegularExpressions;
using static Microsoft.Playwright.Assertions;

namespace ForgeMart.PlaywrightTests.E2E;

[TestFixture]
[Category("E2E")]
[NonParallelizable]
public sealed class ForgeMartStoreE2ETests : PageTest
{
    private static string WebUrl => Environment.GetEnvironmentVariable("FORGEMART_WEB_URL") ?? "http://localhost:4200";

    [SetUp]
    public async Task OpenStoreAsync() => await Page.GotoAsync(WebUrl);

    [Test]
    public async Task Home_ShowsProfessionalStoreIdentity()
    {
        await Expect(Page).ToHaveTitleAsync("ForgeMart | Profesionalni alat i građevinski materijal");
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "ForgeMart home" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Sve za posao. Na jednom mestu." })).ToBeVisibleAsync();
    }

    [Test]
    public async Task Home_ShowsSeededProductCards()
    {
        await Expect(Page.Locator("article.product-card").First).ToBeVisibleAsync();
        await Expect(Page.Locator("article.product-card")).ToHaveCountAsync(12);
    }

    [Test]
    public async Task Home_ExposesSearchFilterAndSortControls()
    {
        await Expect(Page.GetByLabel("Search name, SKU, brand…")).ToBeVisibleAsync();
        await Expect(Page.GetByLabel("Category")).ToBeVisibleAsync();
        await Expect(Page.GetByLabel("Sort")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Search_FindsPartialBrandMatches()
    {
        await Page.GetByLabel("Search name, SKU, brand…").FillAsync("vol");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Apply" }).ClickAsync();
        await Expect(Page.Locator("article.product-card").First).ToContainTextAsync("VoltCraft", new() { IgnoreCase = true });
    }

    [Test]
    public async Task Cart_InitiallyShowsEmptyState()
    {
        await Page.GetByRole(AriaRole.Link, new() { Name = "Shopping cart" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Your cart is ready for a job." })).ToBeVisibleAsync();
    }

    [Test]
    public async Task AnonymousCart_PersistsAcrossReload()
    {
        await Page.GetByRole(AriaRole.Button, new() { Name = "Add to cart" }).First.ClickAsync();
        await Page.ReloadAsync();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Shopping cart" }).ClickAsync();
        await Expect(Page.Locator(".items article")).ToHaveCountAsync(1);
    }

    [Test]
    public async Task LoginPage_ContainsValidatedCredentialsForm()
    {
        await Page.GetByRole(AriaRole.Link, new() { Name = "Login" }).ClickAsync();
        await Expect(Page.GetByLabel("Email")).ToBeVisibleAsync();
        await Expect(Page.GetByLabel("Password")).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Sign in" })).ToBeDisabledAsync();
    }

    [Test]
    public async Task Login_InvalidCredentials_ShowsApiError()
    {
        await LoginAsync("customer@forgemart.test", "WrongPassword1!", expectSuccess: false);
        await Expect(Page.GetByRole(AriaRole.Alert)).ToBeVisibleAsync();
    }

    [Test]
    public async Task Login_Customer_ShowsOrdersAndLogout()
    {
        await LoginAsync("customer@forgemart.test", "Customer123!");
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Orders" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Logout" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task Customer_CannotNavigateToAdminArea()
    {
        await LoginAsync("customer@forgemart.test", "Customer123!");
        await Page.GotoAsync($"{WebUrl}/admin");
        await Expect(Page).ToHaveURLAsync(WebUrl + "/");
    }

    [Test]
    public async Task Login_Admin_ShowsAdminNavigation()
    {
        await LoginAsync("admin@forgemart.test", "Admin123!");
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Admin" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task RegisterPage_RequiresIdentityAndStrongPasswordFields()
    {
        await Page.GetByRole(AriaRole.Link, new() { Name = "Login" }).ClickAsync();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Create an account" }).ClickAsync();
        await Expect(Page.GetByLabel("First name")).ToBeVisibleAsync();
        await Expect(Page.GetByLabel("Last name")).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Create account" })).ToBeDisabledAsync();
    }

    [Test]
    public async Task Home_PrimaryNavigationContainsAllPublicDestinations()
    {
        var navigation = Page.GetByRole(AriaRole.Navigation, new() { Name = "Primary navigation" });
        await Expect(navigation.GetByRole(AriaRole.Link, new() { Name = "Početna" })).ToBeVisibleAsync();
        await Expect(navigation.GetByRole(AriaRole.Link, new() { Name = "Prodavnica" })).ToBeVisibleAsync();
        await Expect(navigation.GetByRole(AriaRole.Link, new() { Name = "Brendovi" })).ToBeVisibleAsync();
        await Expect(navigation.GetByRole(AriaRole.Link, new() { Name = "O nama" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task Home_ShowsCategoryJobsAndPopularSections()
    {
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Šta danas tražiš?" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Za svaki posao postoji pravi alat." })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Proizvodi kojima se veruje." })).ToBeVisibleAsync();
    }

    [Test]
    public async Task Home_ProductTabsChangeSelectedState()
    {
        var newProducts = Page.GetByRole(AriaRole.Tab, new() { Name = "Novo" });
        await newProducts.ClickAsync();
        await Expect(newProducts).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Page.GetByRole(AriaRole.Tab, new() { Name = "Najprodavanije" })).ToHaveAttributeAsync("aria-selected", "false");
    }

    [Test]
    public async Task Home_BrowseProductsNavigatesToShop()
    {
        await Page.GetByRole(AriaRole.Link, new() { Name = "Pogledaj proizvode" }).ClickAsync();
        await Expect(Page).ToHaveURLAsync(WebUrl + "/shop");
    }

    [Test]
    public async Task Home_FooterExplainsThatPaymentsAreSimulated()
    {
        await Expect(Page.GetByText("Studentski demonstracioni projekat — nema stvarnih plaćanja.")).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Kontakt" })).ToHaveAttributeAsync("href", "mailto:podrska@forgemart.rs");
    }

    [Test]
    public async Task About_ShowsMissionAndStoreStatistics()
    {
        await Page.GotoAsync(WebUrl + "/about");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Alat za ljude koji prave stvari." })).ToBeVisibleAsync();
        await Expect(Page.GetByLabel("ForgeMart u brojevima")).ToContainTextAsync("15.000+");
        await Expect(Page.GetByLabel("ForgeMart u brojevima")).ToContainTextAsync("50+");
    }

    [Test]
    public async Task Brands_ShowsSearchAndBrandPrinciples()
    {
        await Page.GotoAsync(WebUrl + "/brands");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Najbolji alat počinje dobrim proizvođačem." })).ToBeVisibleAsync();
        await Expect(Page.GetByText("Pretraži brendove")).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Brend nije samo logo." })).ToBeVisibleAsync();
    }

    [Test]
    public async Task Brands_SearchFiltersTheVisibleBrandList()
    {
        await Page.GotoAsync(WebUrl + "/brands");
        await Page.GetByPlaceholder("Na primer: Bosch, aku alat, merenje…").FillAsync("Bosch");
        await Expect(Page.Locator(".brand-grid a")).ToHaveCountAsync(1);
        await Expect(Page.Locator(".brand-grid a")).ToContainTextAsync("Bosch");
    }

    [Test]
    public async Task Brands_UnknownSearchShowsEmptyState()
    {
        await Page.GotoAsync(WebUrl + "/brands");
        await Page.GetByPlaceholder("Na primer: Bosch, aku alat, merenje…").FillAsync("brand-does-not-exist");
        await Expect(Page.GetByText("Nema rezultata.")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Shop_ShowsSeededFirstPageAndPaginator()
    {
        await OpenShopAsync();
        await Expect(Page.Locator("article.product-card")).ToHaveCountAsync(12);
        await Expect(Page.GetByLabel("Product pages")).ToBeVisibleAsync();
        await Expect(Page.GetByText("38 proizvoda")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Shop_SearchBySkuReturnsMatchingProduct()
    {
        await OpenShopAsync();
        await Page.GetByLabel("Search name, SKU, brand…").FillAsync("FM-01-001");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Apply" }).ClickAsync();
        await Expect(Page.Locator("article.product-card")).ToHaveCountAsync(1);
        await Expect(Page.Locator("article.product-card")).ToContainTextAsync("18V Cordless Drill");
    }

    [Test]
    public async Task Shop_UnknownSearchShowsNoResultsMessage()
    {
        await OpenShopAsync();
        await Page.GetByLabel("Search name, SKU, brand…").FillAsync("definitely-not-a-forgemart-product");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Apply" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "No tools match those filters." })).ToBeVisibleAsync();
    }

    [Test]
    public async Task Shop_InStockFilterReturnsOnlyAvailableCards()
    {
        await OpenShopAsync();
        await Page.GetByRole(AriaRole.Checkbox, new() { Name = "In stock" }).CheckAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Apply" }).ClickAsync();
        var cards = Page.Locator("article.product-card");
        await Expect(cards.First).ToBeVisibleAsync();
        await Expect(cards.GetByText("Out of stock")).ToHaveCountAsync(0);
    }

    [Test]
    public async Task Shop_OnSaleFilterReturnsDiscountedCards()
    {
        await OpenShopAsync();
        await Page.GetByRole(AriaRole.Checkbox, new() { Name = "On sale" }).CheckAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Apply" }).ClickAsync();
        var cards = Page.Locator("article.product-card");
        await Expect(cards.First).ToBeVisibleAsync();
        await Expect(cards).ToHaveCountAsync(6);
        await Expect(Page.Locator("article.product-card .discount")).ToHaveCountAsync(6);
    }

    [Test]
    public async Task Shop_ProductCardOpensDetailPage()
    {
        await OpenShopAsync();
        var name = (await Page.Locator("article.product-card h3").First.InnerTextAsync()).Trim();
        await Page.Locator("article.product-card h3").First.ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex(@"/products/[0-9a-f-]+$", RegexOptions.IgnoreCase));
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = name })).ToBeVisibleAsync();
    }

    [Test]
    public async Task ProductDetail_ShowsCommercialAndInventoryInformation()
    {
        await OpenFirstProductAsync();
        await Expect(Page.GetByText("Net price · VAT", new() { Exact = false })).ToBeVisibleAsync();
        await Expect(Page.GetByText("Low stock at")).ToBeVisibleAsync();
        await Expect(Page.GetByText("This is a simulated university store.", new() { Exact = false })).ToBeVisibleAsync();
    }

    [Test]
    public async Task ProductDetail_BackLinkReturnsToCatalog()
    {
        await OpenFirstProductAsync();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Back to catalog" }).ClickAsync();
        await Expect(Page).ToHaveURLAsync(WebUrl + "/shop");
    }

    [Test]
    public async Task ProductDetail_AddToCartCreatesAnonymousCartLine()
    {
        await OpenFirstProductAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Add to cart" }).ClickAsync();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Shopping cart" }).ClickAsync();
        await Expect(Page.Locator(".items article")).ToHaveCountAsync(1);
    }

    [Test]
    public async Task AnonymousCart_ShowsOrderSummaryAndLoginAction()
    {
        await AddFirstCatalogItemAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Order summary" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Sign in to checkout" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task AnonymousCart_QuantityCanBeIncreased()
    {
        await AddFirstCatalogItemAsync();
        var quantity = Page.Locator(".items article .quantity").First;
        await quantity.GetByRole(AriaRole.Button, new() { Name = "+" }).ClickAsync();
        await Expect(quantity.Locator("span")).ToHaveTextAsync("2");
    }

    [Test]
    public async Task AnonymousCart_RemoveReturnsToEmptyState()
    {
        await AddFirstCatalogItemAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Remove item" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Your cart is ready for a job." })).ToBeVisibleAsync();
    }

    [Test]
    public async Task AnonymousCart_LoginActionNavigatesToLogin()
    {
        await AddFirstCatalogItemAsync();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Sign in to checkout" }).ClickAsync();
        await Expect(Page).ToHaveURLAsync(WebUrl + "/login");
    }

    [Test]
    public async Task AnonymousOrdersRouteRedirectsToLogin()
    {
        await Page.GotoAsync(WebUrl + "/orders");
        await Expect(Page).ToHaveURLAsync(WebUrl + "/login");
    }

    [Test]
    public async Task UnknownRouteRedirectsToHome()
    {
        await Page.GotoAsync(WebUrl + "/route-that-does-not-exist");
        await Expect(Page).ToHaveURLAsync(WebUrl + "/");
    }

    [Test]
    public async Task Customer_OrdersPageShowsSeededHistory()
    {
        await LoginAsync("customer@forgemart.test", "Customer123!");
        await Page.GetByRole(AriaRole.Link, new() { Name = "Orders" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Your orders." })).ToBeVisibleAsync();
        await Expect(Page.Locator("a.order")).ToHaveCountAsync(2);
    }

    [Test]
    public async Task Customer_OrderCardOpensOrderDetail()
    {
        await LoginAsync("customer@forgemart.test", "Customer123!");
        await Page.GotoAsync(WebUrl + "/orders");
        await Page.Locator("a.order").First.ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Order details." })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Delivery" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Payment" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task Customer_LogoutRestoresAnonymousNavigation()
    {
        await LoginAsync("customer@forgemart.test", "Customer123!");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Logout" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Login" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Orders" })).ToHaveCountAsync(0);
    }

    [Test]
    public async Task Admin_DashboardShowsMetricsAndPeriodControls()
    {
        await LoginAsync("admin@forgemart.test", "Admin123!");
        await Page.GotoAsync(WebUrl + "/admin");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Workshop pulse." })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Today" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "7 days" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "30 days" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task Admin_ProductsSectionShowsCatalogRows()
    {
        await OpenAdminSectionAsync("products", "Products");
        await Expect(Page.Locator(".table article").First).ToBeVisibleAsync();
        await Expect(Page.GetByText("Manage the complete active and inactive catalog.")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Admin_InventorySectionShowsTraceableStockRows()
    {
        await OpenAdminSectionAsync("inventory", "Inventory");
        await Expect(Page.Locator(".table article").First).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "+10 restock" }).First).ToBeVisibleAsync();
    }

    [Test]
    public async Task Home_LogoPointsToHomeRoute()
    {
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "ForgeMart home" })).ToHaveAttributeAsync("href", "/");
    }

    [Test]
    public async Task Home_CategoryScrollLinkPointsToCategorySection()
    {
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Idi na kategorije" })).ToHaveAttributeAsync("href", "#categories");
    }

    [Test]
    public async Task Home_FeaturedProductsExposeFourTabs()
    {
        await Expect(Page.GetByRole(AriaRole.Tab)).ToHaveCountAsync(4);
    }

    [Test]
    public async Task Home_ShowsFeaturedBrandWordmarks()
    {
        await Expect(Page.GetByLabel("Izdvojeni brendovi")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Home_BusinessOfferUsesSalesEmail()
    {
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Zatraži poslovnu ponudu" }))
            .ToHaveAttributeAsync("href", "mailto:prodaja@forgemart.rs");
    }

    [Test]
    public async Task About_HeroImageHasMeaningfulAlternativeText()
    {
        await Page.GotoAsync(WebUrl + "/about");
        await Expect(Page.GetByAltText("ForgeMart tim i isporuka materijala na savremenom gradilištu")).ToBeVisibleAsync();
    }

    [Test]
    public async Task About_ShowsAverageDeliveryTime()
    {
        await Page.GotoAsync(WebUrl + "/about");
        await Expect(Page.GetByLabel("ForgeMart u brojevima")).ToContainTextAsync("24–48 h");
    }

    [Test]
    public async Task About_WorkshopCallToActionPointsToShop()
    {
        await Page.GotoAsync(WebUrl + "/about");
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Pogledaj proizvode" })).ToHaveAttributeAsync("href", "/shop");
    }

    [Test]
    public async Task Brands_InitialListContainsBrandLinks()
    {
        await Page.GotoAsync(WebUrl + "/brands");
        await Expect(Page.Locator(".brand-grid a").First).ToBeVisibleAsync();
        Assert.That(await Page.Locator(".brand-grid a").CountAsync(), Is.GreaterThan(1));
    }

    [Test]
    public async Task Brands_BoschCategoryNavigationHasFourLinks()
    {
        await Page.GotoAsync(WebUrl + "/brands");
        await Expect(Page.GetByRole(AriaRole.Navigation, new() { Name = "Bosch kategorije" }).GetByRole(AriaRole.Link))
            .ToHaveCountAsync(4);
    }

    [Test]
    public async Task Shop_ShowsSearchCategoryAndSortLabels()
    {
        await OpenShopAsync();
        await Expect(Page.GetByLabel("Search name, SKU, brand…")).ToBeVisibleAsync();
        await Expect(Page.GetByLabel("Category")).ToBeVisibleAsync();
        await Expect(Page.GetByLabel("Sort")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Shop_FirstCardShowsAForgeMartSku()
    {
        await OpenShopAsync();
        await Expect(Page.Locator("article.product-card").First).ToContainTextAsync(new Regex(@"FM-\d{2}-\d{3}"));
    }

    [Test]
    public async Task Shop_FirstProductImageHasAlternativeText()
    {
        await OpenShopAsync();
        var image = Page.Locator("article.product-card img").First;
        await Expect(image).ToBeVisibleAsync();
        await Expect(image).ToHaveAttributeAsync("alt", new Regex(@"\S+"));
    }

    [Test]
    public async Task Shop_OutOfStockProductCannotBeAddedToCart()
    {
        await OpenShopAsync();
        await Expect(Page.Locator("button[aria-label='Add to cart']:disabled"))
            .ToHaveCountAsync(1);
    }

    [Test]
    public async Task ProductDetail_BackLinkPointsToShop()
    {
        await OpenFirstProductAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Back to catalog" })).ToHaveAttributeAsync("href", "/shop");
    }

    [Test]
    public async Task EmptyCart_BrowseProductsLinkPointsHome()
    {
        await Page.GotoAsync(WebUrl + "/cart");
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Browse products" })).ToHaveAttributeAsync("href", "/");
    }

    [Test]
    public async Task Login_ShowsWelcomeHeading()
    {
        await Page.GotoAsync(WebUrl + "/login");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Welcome back to the bench." })).ToBeVisibleAsync();
    }

    [Test]
    public async Task Login_CreateAccountLinkPointsToRegister()
    {
        await Page.GotoAsync(WebUrl + "/login");
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Create an account" })).ToHaveAttributeAsync("href", "/register");
    }

    [Test]
    public async Task Register_SignInLinkPointsBackToLogin()
    {
        await Page.GotoAsync(WebUrl + "/register");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Join ForgeMart" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Sign in" })).ToHaveAttributeAsync("href", "/login");
    }

    private async Task OpenShopAsync()
    {
        await Page.GotoAsync(WebUrl + "/shop");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Pronađi alat za sledeći posao." })).ToBeVisibleAsync();
    }

    private async Task OpenFirstProductAsync()
    {
        await OpenShopAsync();
        await Page.Locator("article.product-card a.image-wrap").First.ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex(@"/products/[0-9a-f-]+$", RegexOptions.IgnoreCase));
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Add to cart" })).ToBeVisibleAsync();
    }

    private async Task AddFirstCatalogItemAsync()
    {
        await OpenShopAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Add to cart" }).First.ClickAsync();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Shopping cart" }).ClickAsync();
        await Expect(Page.Locator(".items article")).ToHaveCountAsync(1);
    }

    private async Task LoginAsync(string email, string password, bool expectSuccess = true)
    {
        await Page.GotoAsync(WebUrl + "/login");
        await Page.GetByLabel("Email").FillAsync(email);
        await Page.GetByLabel("Password").FillAsync(password);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();
        if (expectSuccess)
        {
            await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Logout" })).ToBeVisibleAsync();
        }
    }

    private async Task OpenAdminSectionAsync(string section, string heading)
    {
        await LoginAsync("admin@forgemart.test", "Admin123!");
        await Page.GotoAsync($"{WebUrl}/admin/{section}");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = heading, Exact = true })).ToBeVisibleAsync();
    }
}
