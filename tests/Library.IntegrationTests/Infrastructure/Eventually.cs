namespace Library.IntegrationTests.Infrastructure;

public static class Eventually
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan PollingInterval = TimeSpan.FromMilliseconds(250);

    public static async Task<bool> SatisfiesAsync(Func<Task<bool>> condition, CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow + DefaultTimeout;

        while (DateTime.UtcNow < deadline)
        {
            if (await condition())
            {
                return true;
            }

            await Task.Delay(PollingInterval, cancellationToken);
        }

        return false;
    }

    public static Task<bool> SatisfiesAsync(Func<bool> condition, CancellationToken cancellationToken) =>
        SatisfiesAsync(() => Task.FromResult(condition()), cancellationToken);
}
