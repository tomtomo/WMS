using AwesomeAssertions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.Notifications.Persistence;
using Xunit;

namespace Wms.Notifications.IntegrationTests;

// Test arsitektur untuk memastikan modul ini tetap read only terhadap core.
public sealed class ReadOnlyToCoreArchTests
{
    [Fact]
    public void Notifications_references_only_contracts_across_modules()
    {
        var referenced = typeof(NotificationsDbContext).Assembly.GetReferencedAssemblies()
            .Select(assembly => assembly.Name ?? string.Empty)
            .Where(name => name.StartsWith("Wms.", StringComparison.Ordinal))
            .ToList();

        var forbidden = referenced
            .Where(name => !name.StartsWith("Wms.BuildingBlocks.", StringComparison.Ordinal)
                && !name.EndsWith(".Contracts", StringComparison.OrdinalIgnoreCase))
            .ToList();

        forbidden.Should().BeEmpty(
            "Notifications read-only ke core: lintas-modul hanya via *.Contracts, nol Domain/Infrastructure/Api modul lain (FF#3)");
    }

    [Fact]
    public void Notifications_does_not_emit_integration_events()
    {
        // Modul Notifications hanya mengonsumsi event, tidak menerbitkan event baru.
        var offenders = SourceFiles()
            .Where(file => File.ReadAllText(file).Contains("IIntegrationEventOutbox", StringComparison.Ordinal))
            .Select(Path.GetFileName)
            .ToList();

        offenders.Should().BeEmpty("modul Notifications nol emit balik ke core");
    }

    [Fact]
    public void Channel_ports_follow_interface_segregation()
    {
        // Setiap channel punya interface sendiri dengan satu tanggung jawab.
        Type[] channelPorts = [typeof(IEmailSender), typeof(IInAppNotifier), typeof(IPushNotifier)];
        channelPorts.Should().OnlyContain(port => port.IsInterface && port.GetMethods().Length == 1);
    }

    private static IEnumerable<string> SourceFiles()
    {
        return Directory.EnumerateFiles(NotificationsSourceRoot(), "*.cs", SearchOption.AllDirectories)
            .Where(file => !IsGenerated(file));
    }

    private static bool IsGenerated(string file) =>
        file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal);

    private static string NotificationsSourceRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "Wms.sln")))
        {
            current = current.Parent;
        }

        var root = current?.FullName ?? throw new InvalidOperationException("Wms.sln tidak ditemukan di jalur induk.");
        return Path.Combine(root, "src", "Modules", "Notifications", "Wms.Notifications");
    }
}
