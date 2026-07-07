using System.Collections.Immutable;
using System.Reflection;

namespace Wms.Architecture.Tests;

// Model assembly Wms.* per-peran.
internal static class ArchitectureFixture
{
    private static readonly string[] _moduleLayers = ["Domain", "Application", "Infrastructure", "Api", "Contracts", "Grpc"];

    // Project host/tool yang tidak ikut aturan dependency modul.
    private static readonly string[] _excludedFromGovernance = ["Wms.Architecture.Tests", "Wms.AppHost", "Wms.MigrationRunner"];

    public static string RepoRoot { get; } = FindRepoRoot();

    public static ImmutableArray<Assembly> WmsAssemblies { get; } = LoadGovernedWmsAssemblies();

    public static ImmutableArray<Assembly> BuildingBlocksAssemblies { get; } =
        [.. WmsAssemblies.Where(a => Name(a).StartsWith("Wms.BuildingBlocks.", StringComparison.Ordinal))];

    public static ImmutableArray<Assembly> PlatformAssemblies { get; } =
        [.. WmsAssemblies.Where(a => Name(a).StartsWith("Wms.Platform.", StringComparison.Ordinal))];

    public static ImmutableArray<Assembly> ModuleAssemblies { get; } = [.. WmsAssemblies.Where(IsModuleAssembly)];

    // Semua assembly berlayer Domain (kernel BuildingBlocks.Domain dan Domain modul)
    public static ImmutableArray<Assembly> DomainAssemblies { get; } = [.. WmsAssemblies.Where(a => HasLayer(a, "Domain"))];

    public static ImmutableArray<Assembly> ApiAssemblies { get; } = [.. WmsAssemblies.Where(a => HasLayer(a, "Api"))];

    public static ImmutableArray<Assembly> ContractsAssemblies { get; } = [.. WmsAssemblies.Where(a => HasLayer(a, "Contracts"))];

    // Nama assembly Wms.* yang direferensikan asm — dasar cek batas antar assembly
    public static IReadOnlySet<string> ReferencedWmsAssemblies(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        return assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .Where(name => name.StartsWith("Wms.", StringComparison.Ordinal))
            .ToImmutableHashSet(StringComparer.Ordinal);
    }

    public static bool IsModuleAssembly(Assembly assembly)
    {
        var name = Name(assembly);
        return name.StartsWith("Wms.", StringComparison.Ordinal)
            && !name.StartsWith("Wms.BuildingBlocks.", StringComparison.Ordinal)
            && !name.StartsWith("Wms.Platform.", StringComparison.Ordinal)
            && !_excludedFromGovernance.Contains(name)
            && _moduleLayers.Contains(LastSegment(name));
    }

    public static string ModuleKey(Assembly assembly)
    {
        var name = Name(assembly);
        var lastDot = name.LastIndexOf('.');
        return lastDot > 0 ? name[..lastDot] : name;
    }

    public static string LayerOf(Assembly assembly) => LastSegment(Name(assembly));

    public static string Name(Assembly assembly) => assembly.GetName().Name ?? string.Empty;

    // Cek apakah type ini turunan AggregateRoot<TId>.
    public static bool IsAggregateRoot(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType
                && current.GetGenericTypeDefinition() == typeof(Wms.BuildingBlocks.Domain.Primitives.AggregateRoot<>))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasLayer(Assembly assembly, string layer)
    {
        var name = Name(assembly);
        return !_excludedFromGovernance.Contains(name) && LastSegment(name) == layer;
    }

    private static string LastSegment(string assemblyName)
    {
        var lastDot = assemblyName.LastIndexOf('.');
        return lastDot >= 0 ? assemblyName[(lastDot + 1)..] : assemblyName;
    }

    private static ImmutableArray<Assembly> LoadGovernedWmsAssemblies()
    {
        var loaded = new List<Assembly>();
        foreach (var dll in Directory.EnumerateFiles(AppContext.BaseDirectory, "Wms.*.dll"))
        {
            if (_excludedFromGovernance.Contains(Path.GetFileNameWithoutExtension(dll)))
            {
                continue;
            }

            try
            {
                loaded.Add(Assembly.Load(AssemblyName.GetAssemblyName(dll)));
            }
            catch (Exception ex) when (ex is BadImageFormatException or FileLoadException or FileNotFoundException)
            {
                // Assembly yang tidak bisa dimuat.
            }
        }

        return [.. loaded];
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "Wms.sln")))
        {
            current = current.Parent;
        }

        return current?.FullName
            ?? throw new InvalidOperationException("Wms.sln tidak ditemukan di jalur induk test host.");
    }
}
