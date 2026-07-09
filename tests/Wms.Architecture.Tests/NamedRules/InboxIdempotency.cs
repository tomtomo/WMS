using AwesomeAssertions;
using Xunit;

namespace Wms.Architecture.Tests.NamedRules;

// Setiap consumer event dicek lewat Inbox sebelum diproses. Kalau broker mengirim event yang sama lagi, handler cukup skip.
public sealed class InboxIdempotency
{
    // Guard harus lengkap: cek sudah diproses dan tandai setelah berhasil. Kalau bagian cek hilang, event ganda masih bisa lolos.
    private const string InboxReadMarker = "HasProcessedAsync";
    private const string InboxWriteMarker = "MarkProcessedAsync";

    [Fact]
    public void Integration_event_consumers_use_inbox_guard()
    {
        var violations = new List<string>();
        foreach (var consumerFile in ConsumerFiles(SourceScan.SrcPath("Modules")))
        {
            var source = File.ReadAllText(consumerFile);
            if (!source.Contains(InboxReadMarker, StringComparison.Ordinal)
                || !source.Contains(InboxWriteMarker, StringComparison.Ordinal))
            {
                violations.Add(consumerFile);
            }
        }

        violations.Should().BeEmpty(
            "consumer integration event wajib pakai Inbox guard dua sisi: HasProcessedAsync (baca) + MarkProcessedAsync (tulis) (named FF)");
    }

    // Konvensi handler integration event: *EventHandler.cs / *Consumer.cs di modul.
    private static IEnumerable<string> ConsumerFiles(string modulesRoot) =>
        SourceScan.SourceFiles(modulesRoot)
            .Where(path =>
                path.EndsWith("EventHandler.cs", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith("Consumer.cs", StringComparison.OrdinalIgnoreCase));
}
