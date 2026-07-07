namespace Wms.CrossCutting.IntegrationTests.TestSupport;

// Hook sekali pakai untuk memaksa conflict tepat setelah handler sukses.
internal sealed class ConflictInjector
{
    private Func<Task>? _onHandlerSucceeded;

    public void ArmOnce(Func<Task> hook) => _onHandlerSucceeded = hook;

    public Task FireAsync()
    {
        var hook = Interlocked.Exchange(ref _onHandlerSucceeded, null);
        return hook?.Invoke() ?? Task.CompletedTask;
    }
}
