namespace ForgeMart.Api.Services;

public interface ICheckoutConcurrencyHook
{
    Task AfterInventoryReadAsync(CancellationToken cancellationToken);
}

public sealed class NoOpCheckoutConcurrencyHook : ICheckoutConcurrencyHook
{
    public Task AfterInventoryReadAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

