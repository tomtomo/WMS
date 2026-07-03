using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Hosting;
using NetArchTest.Rules;
using Wms.BuildingBlocks.Application;
using Wms.BuildingBlocks.Infrastructure;
using Wms.Platform.Local.Cache;
using Xunit;

namespace Wms.Architecture.Tests;

// Test Fitness function
public sealed partial class FitnessFunctionTests
{
    private static readonly string[] _cloudSdkPrefixes = ["Azure.", "Microsoft.Azure", "Google.Cloud", "Amazon", "AWSSDK"];

    // SDK cloud hanya boleh hidup di Platform.<Cloud> + Hosts. Core dan Platform agnostic (Hosting/Local) — diverifikasi di source (PackageReference + using), bukan cuma assembly ter-load.
    [Fact]
    public void Ff01_no_cloud_sdk_in_building_blocks_modules_and_agnostic_platform()
    {
        var repoRoot = FindRepoRoot();
        string[] scanRoots =
        [
            Path.Combine(repoRoot, "src", "BuildingBlocks"),
            Path.Combine(repoRoot, "src", "Modules"),
            Path.Combine(repoRoot, "src", "Platform", "Wms.Platform.Hosting"),
            Path.Combine(repoRoot, "src", "Platform", "Wms.Platform.Local"),
        ];

        var violations = new List<string>();
        foreach (var scanRoot in scanRoots.Where(Directory.Exists))
        {
            foreach (var projectFile in Directory.EnumerateFiles(scanRoot, "*.csproj", SearchOption.AllDirectories))
            {
                violations.AddRange(
                    File.ReadLines(projectFile)
                        .Select(line => PackageReferenceRegex().Match(line))
                        .Where(match => match.Success && _cloudSdkPrefixes.Any(
                            prefix => match.Groups[1].Value.StartsWith(prefix, StringComparison.Ordinal)))
                        .Select(match => $"{projectFile}: PackageReference {match.Groups[1].Value}"));
            }

            foreach (var sourceFile in EnumerateSourceFiles(scanRoot))
            {
                violations.AddRange(
                    File.ReadLines(sourceFile)
                        .Where(line => CloudUsingRegex().IsMatch(line))
                        .Select(line => $"{sourceFile}: {line.Trim()}"));
            }
        }

        Assert.Empty(violations);
    }

    // BuildingBlocks tidak boleh depend ke Modules atau Platform — menjaga arah dependency inti tetap ke dalam.
    [Fact]
    public void Ff04_application_building_block_does_not_depend_on_modules_or_platform()
    {
        var result = Types
            .InAssembly(typeof(IApplicationBuildingBlocksMarker).Assembly)
            .Should()
            .NotHaveDependencyOnAny("Wms.Modules", "Wms.Platform")
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    // Adapter hanya mengimplementasi port abstrak — Platform.* tak pernah menyentuh Modules.*.
    [Fact]
    public void Ff06_platform_does_not_reference_modules()
    {
        var platformAssemblies = new[]
        {
            typeof(ServiceDefaults).Assembly,
            typeof(InMemoryCacheStore).Assembly,
        };

        var result = Types
            .InAssemblies(platformAssemblies)
            .Should()
            .NotHaveDependencyOnAny("Wms.Modules")
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    // Infrastructure tidak boleh punya DbContext standalone.
    [Fact]
    public void Ff10_infrastructure_building_block_has_no_standalone_dbcontext()
    {
        var dbContexts = typeof(InfrastructureModelBuilderExtensions).Assembly
            .GetTypes()
            .Where(InheritsDbContext)
            .ToList();

        Assert.Empty(dbContexts);
    }

    private static bool InheritsDbContext(Type type)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.FullName == "Microsoft.EntityFrameworkCore.DbContext")
            {
                return true;
            }
        }

        return false;
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "Wms.sln")))
        {
            current = current.Parent!;
        }

        return current?.FullName
            ?? throw new InvalidOperationException("Wms.sln tidak ditemukan di jalur induk test host.");
    }

    private static IEnumerable<string> EnumerateSourceFiles(string root) =>
        Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));

    [GeneratedRegex(@"<PackageReference\s+Include=""([^""]+)""")]
    private static partial Regex PackageReferenceRegex();

    [GeneratedRegex(@"^\s*(global\s+)?using\s+(static\s+)?(Azure|Microsoft\.Azure|Google\.Cloud|Amazon)[\.;]")]
    private static partial Regex CloudUsingRegex();
}
