using ForgeMart.Api.Data;
using ForgeMart.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace ForgeMart.KnownDefectTests.Infrastructure;

public sealed class KnownDefectApiFactory : WebApplicationFactory<Program>
{
    private const string Connection =
        "Host=localhost;Port=5500;Database=hardware_store_known_defects;Username=forgemart;Password=forgemart_dev";

    public TwoRequestCheckoutBarrier Barrier { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(
            new Dictionary<string, string?> { ["ConnectionStrings:DefaultConnection"] = Connection }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ForgeMartDbContext>>();
            services.RemoveAll<ForgeMartDbContext>();
            services.AddDbContext<ForgeMartDbContext>(options => options.UseNpgsql(Connection));
            services.RemoveAll<ICheckoutConcurrencyHook>();
            services.AddSingleton<ICheckoutConcurrencyHook>(Barrier);
        });
    }

    public async Task ResetAsync()
    {
        Barrier.Reset();
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ForgeMartDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
        await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync();
    }
}

public sealed class TwoRequestCheckoutBarrier : ICheckoutConcurrencyHook
{
    private TaskCompletionSource<bool> release = NewRelease();
    private int arrivals;

    public async Task AfterInventoryReadAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Increment(ref arrivals) == 2)
        {
            release.TrySetResult(true);
        }

        await release.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
    }

    public void Reset()
    {
        arrivals = 0;
        release = NewRelease();
    }

    private static TaskCompletionSource<bool> NewRelease() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}
