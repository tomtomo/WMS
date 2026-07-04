using AwesomeAssertions;
using Xunit;

namespace Wms.Architecture.Tests.NamedRules;

// Named FF — tiap consumer integration event di back Inbox (HasProcessedAsync + MarkProcessedAsync) sehingga
// pengiriman ganda broker jadi no-op.
public sealed class InboxIdempotency
{
    private const string InboxGuardMarker = "MarkProcessedAsync";

    [Fact]
    public void Integration_event_consumers_use_inbox_guard()
    {
        var violations = new List<string>();
        foreach (var consumerFile in ConsumerFiles(SourceScan.SrcPath("Modules")))
        {
            if (!File.ReadAllText(consumerFile).Contains(InboxGuardMarker, StringComparison.Ordinal))
            {
                violations.Add(consumerFile);
            }
        }

        violations.Should().BeEmpty("consumer integration event wajib pakai Inbox guard (named FF)");
    }

    // Konvensi handler integration event: *EventHandler.cs / *Consumer.cs di modul.
    private static IEnumerable<string> ConsumerFiles(string modulesRoot) =>
        SourceScan.SourceFiles(modulesRoot)
            .Where(path =>
                path.EndsWith("EventHandler.cs", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith("Consumer.cs", StringComparison.OrdinalIgnoreCase));
}
