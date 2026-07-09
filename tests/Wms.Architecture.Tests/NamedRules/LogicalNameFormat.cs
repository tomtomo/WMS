using System.Text.RegularExpressions;
using AwesomeAssertions;
using Wms.BuildingBlocks.Infrastructure.Messaging;
using Wms.Contracts.Abstractions;
using Xunit;

namespace Wms.Architecture.Tests.NamedRules;

// Named FF — LogicalName tiap integration event berformat {module}.{event}.v{N}
// Identitas broker yang stabil & ber-versi.
public sealed partial class LogicalNameFormat
{
    [Fact]
    public void Integration_event_logical_names_follow_module_event_version_format()
    {
        var violations = new List<string>();
        foreach (var eventType in ArchitectureFixture.ContractsAssemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IIntegrationEvent).IsAssignableFrom(type)))
        {
            var logicalName = IntegrationEventLogicalName.Resolve(eventType);
            if (!LogicalNameRegex().IsMatch(logicalName))
            {
                violations.Add($"{eventType.FullName}: LogicalName '{logicalName}'");
            }
        }

        violations.Should().BeEmpty("LogicalName wajib {module}.{event}.v{N} (named FF)");
    }

    [GeneratedRegex(@"^[a-z][a-z0-9]*\.[a-z][a-z0-9_]*\.v[1-9][0-9]*$")]
    private static partial Regex LogicalNameRegex();
}
