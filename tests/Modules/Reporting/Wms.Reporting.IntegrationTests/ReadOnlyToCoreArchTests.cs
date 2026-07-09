using AwesomeAssertions;
using Wms.Reporting.Persistence;
using Xunit;

namespace Wms.Reporting.IntegrationTests;

public sealed class ReadOnlyToCoreArchTests
{
    [Fact]
    public void Reporting_references_only_contracts_across_modules()
    {
        var referenced = typeof(ReportingDbContext).Assembly.GetReferencedAssemblies()
            .Select(assembly => assembly.Name ?? string.Empty)
            .Where(name => name.StartsWith("Wms.", StringComparison.Ordinal))
            .ToList();

        // Lintas modul hanya boleh via lewat kontrak (*.Contracts).
        var forbidden = referenced
            .Where(name => !name.StartsWith("Wms.BuildingBlocks.", StringComparison.Ordinal)
                && !name.EndsWith(".Contracts", StringComparison.Ordinal)
                && !name.Equals("Wms.Contracts.Abstractions", StringComparison.Ordinal))
            .ToList();

        forbidden.Should().BeEmpty(
            "Reporting read-only ke core: lintas-modul hanya via *.Contracts, nol Domain/Infrastructure/Api modul lain (FF#3)");
    }

    [Fact]
    public void Reporting_does_not_emit_or_own_full_rail()
    {
        var offenders = new List<string>();
        foreach (var file in Directory.EnumerateFiles(ReportingSourceRoot(), "*.cs", SearchOption.AllDirectories))
        {
            if (IsGenerated(file))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            if (text.Contains("IIntegrationEventOutbox", StringComparison.Ordinal)
                || text.Contains("AddInfrastructureTables", StringComparison.Ordinal))
            {
                offenders.Add(Path.GetFileName(file));
            }
        }

        offenders.Should().BeEmpty(
            "pure-consumer read-side: nol emit (IIntegrationEventOutbox) & nol Outbox/DLQ rail penuh");
    }

    private static bool IsGenerated(string file) =>
        file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal);

    private static string ReportingSourceRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "Wms.sln")))
        {
            current = current.Parent;
        }

        var root = current?.FullName ?? throw new InvalidOperationException("Wms.sln tidak ditemukan di jalur induk.");
        return Path.Combine(root, "src", "Modules", "Reporting", "Wms.Reporting");
    }
}
