using System.Text.RegularExpressions;
using AwesomeAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Wms.BuildingBlocks.Application.Behaviors;
using Xunit;

namespace Wms.Architecture.Tests.NamedRules;

// Memastikan command yang mengubah data tetap melewati pipeline audit.
public sealed partial class AuditCoverageRule
{
    [Fact]
    public void Kernel_pipeline_memasang_audit_log_behavior_pada_urutan_terkunci()
    {
        var services = new ServiceCollection();
        services.AddApplicationBuildingBlocks();

        var behaviors = services
            .Where(descriptor => descriptor.ServiceType == typeof(IPipelineBehavior<,>))
            .Select(descriptor => descriptor.ImplementationType)
            .ToList();

        behaviors.Should().Equal(
            [
                typeof(ValidationBehavior<,>),
                typeof(TransactionBehavior<,>),
                typeof(AuditLogBehavior<,>),
                typeof(LoggingBehavior<,>),
            ],
            "urutan pipeline Validation, Transaction, AuditLog, Logging (named FF)");
    }

    [Fact]
    public void Endpoint_mutating_wajib_dispatch_via_ISender()
    {
        var violations = new List<string>();
        foreach (var file in SourceScan.SourceFiles(SourceScan.SrcPath("Modules")))
        {
            var text = File.ReadAllText(file);
            if (MutatingMapRegex().IsMatch(text) && !text.Contains("ISender", StringComparison.Ordinal))
            {
                violations.Add(Path.GetRelativePath(ArchitectureFixture.RepoRoot, file));
            }
        }

        violations.Should().BeEmpty(
            "endpoint mutating wajib dispatch command via ISender agar tunduk AuditLogBehavior (named FF)");
    }

    [GeneratedRegex(@"Map(Post|Put|Delete|Patch)\s*\(")]
    private static partial Regex MutatingMapRegex();
}
