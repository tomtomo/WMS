using System.Text.RegularExpressions;
using AwesomeAssertions;
using Xunit;

namespace Wms.Architecture.Tests.MetaRules;

// Meta-FF (governance of governance) — tiap module project (Wms.<Module>.<Layer> di src/Modules) wajib
// di ProjectReference oleh suite ini.
public sealed partial class ModuleCoverageGuard
{
    private static readonly string[] _governedLayers = ["Domain", "Application", "Infrastructure", "Api", "Contracts", "Grpc"];

    [Fact]
    public void Architecture_tests_reference_every_module_project()
    {
        var moduleProjects = SourceScan.ProjectFiles(SourceScan.SrcPath("Modules"))
            .Where(IsGovernedModuleProject)
            .Select(path => Path.GetFileNameWithoutExtension(path)!)
            .ToHashSet(StringComparer.Ordinal);

        if (moduleProjects.Count == 0)
        {
            return;
        }

        var referenced = ReferencedProjectNames();
        var uncovered = moduleProjects.Where(name => !referenced.Contains(name)).ToList();

        uncovered.Should().BeEmpty(
            "tiap module project wajib di-ProjectReference Wms.Architecture.Tests.csproj agar FF assembly-graph melihatnya (meta-FF)");
    }

    // Project di src/Modules yang tunduk dependency-rule.
    private static bool IsGovernedModuleProject(string projectFile)
    {
        var name = Path.GetFileNameWithoutExtension(projectFile);
        var lastDot = name.LastIndexOf('.');
        return lastDot >= 0 && _governedLayers.Contains(name[(lastDot + 1)..]);
    }

    private static IReadOnlySet<string> ReferencedProjectNames()
    {
        var testProject = Path.Combine(
            ArchitectureFixture.RepoRoot, "tests", "Wms.Architecture.Tests", "Wms.Architecture.Tests.csproj");

        return File.ReadLines(testProject)
            .Select(line => ProjectReferenceRegex().Match(line))
            .Where(match => match.Success)
            .Select(match => Path.GetFileNameWithoutExtension(match.Groups[1].Value)!)
            .ToHashSet(StringComparer.Ordinal);
    }

    [GeneratedRegex(@"<ProjectReference\s+Include=""([^""]+)""")]
    private static partial Regex ProjectReferenceRegex();
}
