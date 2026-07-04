using AwesomeAssertions;
using Xunit;

namespace Wms.Architecture.Tests.NamedRules;

// Named FF — OutboxDispatcher dan turunannya wajib throw saat Result.Failure agar publish gagal masuk rail DLQ, lalu Dead Letter Channel.
public sealed class DispatcherFailLoud
{
    [Fact]
    public void Outbox_dispatchers_throw_on_failure()
    {
        var dispatcherFiles = DispatcherSourceFiles();

        var violations = new List<string>();
        foreach (var dispatcherFile in dispatcherFiles)
        {
            var text = File.ReadAllText(dispatcherFile);

            // Dispatcher yang mengevaluasi hasil publish wajib punya jalur throw fail loud.
            var evaluatesResult = text.Contains("IsFailure", StringComparison.Ordinal)
                || text.Contains("Result.Failure", StringComparison.Ordinal);
            if (evaluatesResult && !text.Contains("throw", StringComparison.Ordinal))
            {
                violations.Add(dispatcherFile);
            }
        }

        // Abstraksi base harus benar-benar terscan.
        dispatcherFiles.Should().NotBeEmpty("abstraksi OutboxDispatcher (P0.4) harus ter-scan");
        violations.Should().BeEmpty("dispatcher wajib throw saat Result.Failure (named FF)");
    }

    private static IReadOnlyList<string> DispatcherSourceFiles()
    {
        string[] roots =
        [
            SourceScan.SrcPath("BuildingBlocks"),
            SourceScan.SrcPath("Platform"),
            SourceScan.SrcPath("Modules"),
        ];

        return [.. roots
            .SelectMany(SourceScan.SourceFiles)
            .Where(path => path.EndsWith("Dispatcher.cs", StringComparison.OrdinalIgnoreCase))];
    }
}
