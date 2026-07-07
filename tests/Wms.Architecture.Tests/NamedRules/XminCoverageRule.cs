using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Wms.Architecture.Tests.NamedRules;

// Memastikan setiap aggregate EF punya token optimistic concurrency.
public sealed class XminCoverageRule
{
    [Fact]
    public void Semua_aggregate_root_memakai_xmin_concurrency_token()
    {
        var violations = new List<string>();
        var scannedPerModule = new Dictionary<string, int>();

        foreach (var (module, model) in ModuleModels())
        {
            var aggregates = model.GetEntityTypes()
                .Where(entity => ArchitectureFixture.IsAggregateRoot(entity.ClrType))
                .ToList();
            scannedPerModule[module] = aggregates.Count;

            foreach (var entity in aggregates)
            {
                var xmin = entity.FindProperty("xmin");
                if (xmin is null || !xmin.IsConcurrencyToken)
                {
                    violations.Add($"{module}: {entity.ClrType.Name}");
                }
            }
        }

        violations.Should().BeEmpty("tiap aggregate root wajib UseXminAsConcurrencyToken (named FF)");

        // Pastikan rule benar-benar mengecek aggregate di tiap modul.
        scannedPerModule.Should().OnlyContain(entry => entry.Value > 0, "tiap modul ter-governance punya aggregate root");
    }

    [Fact]
    public void Seam_concurrency_bebas_retry_otomatis()
    {
        var seamFiles = new[]
        {
            SourceScan.SrcPath("BuildingBlocks", "Wms.BuildingBlocks.Application", "Behaviors", "TransactionBehavior.cs"),
            SourceScan.SrcPath("BuildingBlocks", "Wms.BuildingBlocks.Infrastructure", "Persistence", "UnitOfWork.cs"),
        };

        foreach (var file in seamFiles)
        {
            File.Exists(file).Should().BeTrue($"seam concurrency {Path.GetFileName(file)} harus ada");
            var text = File.ReadAllText(file);
            text.Should().NotContain("Polly", because: "konflik xmin di-surface ke caller, bukan diretry");
            text.Should().NotContain("for (", because: "seam commit tidak boleh punya loop retry");
            text.Should().NotContain("while (", because: "seam commit tidak boleh punya loop retry");
        }
    }

    private static IEnumerable<(string Module, IModel Model)> ModuleModels()
    {
        yield return ("Inbound", ModelOf<Wms.Inbound.Infrastructure.InboundDbContext>(options => new(options)));
        yield return ("Inventory", ModelOf<Wms.Inventory.Infrastructure.InventoryDbContext>(options => new(options)));
        yield return ("Outbound", ModelOf<Wms.Outbound.Infrastructure.OutboundDbContext>(options => new(options)));
        yield return ("MasterData", ModelOf<Wms.MasterData.Infrastructure.MasterDataDbContext>(options => new(options)));
        yield return ("Auth", ModelOf<Wms.Auth.Infrastructure.AuthDbContext>(options => new(options)));
    }

    // Model dibangun offline (tanpa koneksi) — cukup untuk inspeksi metadata concurrency token.
    private static IModel ModelOf<TContext>(Func<DbContextOptions<TContext>, TContext> factory)
        where TContext : DbContext
    {
        var options = new DbContextOptionsBuilder<TContext>().UseNpgsql().Options;
        using var context = factory(options);
        return context.Model;
    }
}
