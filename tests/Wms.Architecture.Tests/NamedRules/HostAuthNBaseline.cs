using AwesomeAssertions;
using Xunit;

namespace Wms.Architecture.Tests.NamedRules;

// Named FF — host REST wajib memasang authN baseline: AddJwtBearer (via AddJwtBearerRs256) dan AddHttpContextCurrentUser.
public sealed class HostAuthNBaseline
{
    [Fact]
    public void Rest_hosts_wire_authentication_baseline()
    {
        var hostsRoot = SourceScan.SrcPath("Hosts");
        if (!Directory.Exists(hostsRoot))
        {
            return;
        }

        var violations = new List<string>();
        foreach (var hostDir in Directory.EnumerateDirectories(hostsRoot, "*.Host.*", SearchOption.AllDirectories))
        {
            var text = string.Concat(SourceScan.SourceFiles(hostDir).Select(File.ReadAllText));

            // Pure consumer tanpa endpoint REST exempt.
            var exposesRest = text.Contains("MapControllers", StringComparison.Ordinal)
                || text.Contains("IEndpoint", StringComparison.Ordinal)
                || text.Contains("MapEndpoints", StringComparison.Ordinal);
            if (!exposesRest)
            {
                continue;
            }

            if (!text.Contains("AddJwtBearer", StringComparison.Ordinal)
                || !text.Contains("AddHttpContextCurrentUser", StringComparison.Ordinal))
            {
                violations.Add(hostDir);
            }
        }

        violations.Should().BeEmpty("host REST wajib AddJwtBearer + AddHttpContextCurrentUser (named FF, OWASP)");
    }
}
