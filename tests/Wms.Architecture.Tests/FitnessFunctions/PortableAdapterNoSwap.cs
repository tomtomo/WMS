using System.Reflection;
using AwesomeAssertions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Xunit;

namespace Wms.Architecture.Tests.FitnessFunctions;

// Adapter portable seperti Argon2id, Redis, FCM, dan SignalR tetap ditempatkan di Platform.Shared.
// Setiap platform cloud cukup meregistrasikan adapter yang sama tanpa membuat implementasi baru.
public sealed class PortableAdapterNoSwap
{
    private const string PortableAdapterAssembly = "Wms.Platform.Shared";

    // Port yang implementasinya identik lintas cloud.
    private static readonly Type[] _portableAdapterPorts =
    [
        typeof(IPasswordHasher),
        typeof(ICacheStore),
        typeof(IApiIdempotencyStore),
        typeof(IPushNotifier),
        typeof(IInAppNotifier),
    ];

    [Fact]
    public void Password_hashing_is_implemented_exactly_once_and_lives_in_the_portable_adapter()
    {
        var hashers = ImplementationsOf(typeof(IPasswordHasher)).ToList();

        hashers.Should().ContainSingle("hashing password tak boleh ditulis ulang per cloud");
        hashers[0].Assembly.GetName().Name.Should().Be(PortableAdapterAssembly);
    }

    [Fact]
    public void Cloud_platforms_reuse_the_portable_adapters_instead_of_reimplementing_them()
    {
        var cloudPlatforms = ArchitectureFixture.PlatformAssemblies
            .Where(assembly => ArchitectureFixture.Name(assembly) is "Wms.Platform.Azure" or "Wms.Platform.Gcp")
            .Select(ArchitectureFixture.Name)
            .ToHashSet(StringComparer.Ordinal);

        // Pastikan minimal satu platform cloud terload agar test melakukan pengecekan.
        cloudPlatforms.Should().NotBeEmpty("minimal Wms.Platform.Azure harus terload");

        var violations = _portableAdapterPorts
            .SelectMany(port => ImplementationsOf(port)
                .Where(implementation => cloudPlatforms.Contains(implementation.Assembly.GetName().Name ?? string.Empty))
                .Select(implementation => $"{implementation.FullName} implement {port.Name}"))
            .ToList();

        violations.Should().BeEmpty($"adapter portable tinggal di {PortableAdapterAssembly} dan cukup diregistrasi ulang");
    }

    private static IEnumerable<Type> ImplementationsOf(Type port) =>
        ArchitectureFixture.WmsAssemblies
            .SelectMany(GetLoadableTypes)
            .Where(type => type is { IsClass: true, IsAbstract: false } && port.IsAssignableFrom(type));

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException partiallyLoaded)
        {
            return partiallyLoaded.Types.OfType<Type>();
        }
    }
}
