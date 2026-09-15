using ForgeMart.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace ForgeMart.ComponentTests.Infrastructure;

public sealed class ForgeMartApiFactory : WebApplicationFactory<Program>
{
    private const string TestConnection =
        "Host=localhost;Port=5500;Database=hardware_store_component_tests;Username=forgemart;Password=forgemart_dev";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(
            new Dictionary<string, string?> { ["ConnectionStrings:DefaultConnection"] = TestConnection }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ForgeMartDbContext>>();
            services.RemoveAll<ForgeMartDbContext>();
            services.AddDbContext<ForgeMartDbContext>(options => options.UseNpgsql(TestConnection));
        });
    }

    public async Task ResetAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ForgeMartDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
        await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync();
    }
}
