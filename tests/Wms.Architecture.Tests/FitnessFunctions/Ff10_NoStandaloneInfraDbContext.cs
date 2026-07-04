using AwesomeAssertions;
using Xunit;

namespace Wms.Architecture.Tests.FitnessFunctions;

// FF#10 — BuildingBlocks.Infrastructure nol DbContext standalone. Tabel infra (Outbox/Inbox/DLQ/audit) menumpang
// DbContext modul via AddInfrastructureTables.
public sealed class Ff10_NoStandaloneInfraDbContext
{
    [Fact]
    public void Infrastructure_building_block_has_no_standalone_dbcontext()
    {
        var infrastructure = ArchitectureFixture.BuildingBlocksAssemblies
            .SingleOrDefault(assembly => ArchitectureFixture.Name(assembly) == "Wms.BuildingBlocks.Infrastructure");
        if (infrastructure is null)
        {
            return;
        }

        var dbContexts = infrastructure.GetTypes()
            .Where(InheritsDbContext)
            .Select(type => type.FullName)
            .ToList();

        dbContexts.Should().BeEmpty("infra tables harus menumpang DbContext modul, bukan DbContext standalone (FF#10)");
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
}
