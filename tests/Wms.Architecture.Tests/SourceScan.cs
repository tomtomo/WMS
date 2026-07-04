namespace Wms.Architecture.Tests;

// Util source scan untuk aturan di luar jangkauan analisis assembly (format/regex teks)
internal static class SourceScan
{
    public static string SrcPath(params string[] segments) =>
        Path.Combine(ArchitectureFixture.RepoRoot, "src", Path.Combine(segments));

    public static IEnumerable<string> SourceFiles(string root) =>
        Directory.Exists(root)
            ? Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories).Where(IsNotBuildArtifact)
            : [];

    public static IEnumerable<string> ProjectFiles(string root) =>
        Directory.Exists(root)
            ? Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories)
            : [];

    // File AsyncAPI katalog di mana pun dalam repo.
    public static IEnumerable<string> AsyncApiCatalogs() =>
        Directory.EnumerateFiles(ArchitectureFixture.RepoRoot, "asyncapi.y*ml", SearchOption.AllDirectories)
            .Where(IsNotBuildArtifact);

    private static bool IsNotBuildArtifact(string path) =>
        !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
        && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase);
}
