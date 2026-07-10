namespace Wms.Platform.Azure.ParityTests.TestSupport;

internal static class ParityWait
{
    public static Task UntilAsync(Func<bool> condition, TimeSpan timeout, string because) =>
        UntilAsync(() => Task.FromResult(condition()), timeout, because);

    public static async Task UntilAsync(Func<Task<bool>> conditionAsync, TimeSpan timeout, string because)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            if (await conditionAsync())
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250));
        }

        throw new TimeoutException($"Kondisi tidak tercapai dalam {timeout.TotalSeconds:F0}s: {because}");
    }
}
