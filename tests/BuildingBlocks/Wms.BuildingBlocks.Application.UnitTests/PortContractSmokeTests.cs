using System.Reflection;
using AwesomeAssertions;
using Xunit;

namespace Wms.BuildingBlocks.Application.UnitTests;

// Setiap tipe di namespace Ports adalah interface abstrak, dan assembly Application tidak terdapat SDK cloud atau infra.
public sealed class PortContractSmokeTests
{
    private const string PortsNamespace = "Wms.BuildingBlocks.Application.Abstractions.Ports";

    private static readonly Assembly _applicationAssembly = typeof(IApplicationBuildingBlocksMarker).Assembly;

    [Fact]
    public void Every_type_in_the_ports_namespace_is_an_interface()
    {
        var portTypes = _applicationAssembly
            .GetTypes()
            .Where(type => type.Namespace == PortsNamespace)
            .ToList();

        portTypes.Should().NotBeEmpty();
        portTypes.Should().OnlyContain(type => type.IsInterface);
    }

    [Fact]
    public void Notification_ports_are_three_separate_interfaces_without_a_god_notifier()
    {
        var portNames = _applicationAssembly
            .GetTypes()
            .Where(type => type.Namespace == PortsNamespace && type.IsInterface)
            .Select(type => type.Name)
            .ToList();

        portNames.Should().Contain("IEmailSender");
        portNames.Should().Contain("IPushNotifier");
        portNames.Should().Contain("IInAppNotifier");
        portNames.Should().NotContain("INotifier");
    }

    [Fact]
    public void The_application_assembly_references_no_cloud_or_infrastructure_sdk()
    {
        var referencedAssemblies = _applicationAssembly
            .GetReferencedAssemblies()
            .Select(assembly => assembly.Name ?? string.Empty)
            .ToList();

        referencedAssemblies.Should().NotContain(name =>
            name.StartsWith("Azure.", StringComparison.Ordinal)
            || name.StartsWith("Google.Cloud.", StringComparison.Ordinal)
            || name.StartsWith("RabbitMQ", StringComparison.Ordinal)
            || name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)
            || name.StartsWith("Npgsql", StringComparison.Ordinal));
    }
}
