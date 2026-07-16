using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Outbound.Application.Features.CreateOutboundOrder;
using Wms.Outbound.Domain.Enums;
using Wms.Outbound.IntegrationTests.TestSupport;
using Xunit;

namespace Wms.Outbound.IntegrationTests;

// Command ini hanya dipakai test untuk membuat OutboundOrder berstatus New di backlog.
// Intake order pelanggan tetap di luar scope. Setiap test memeriksa satu invariant.
[Collection(PostgresCollection.Name)]
public sealed class CreateOutboundOrderTests(PostgresFixture postgres) : IAsyncLifetime
{
    private ServiceProvider _provider = null!;

    public async Task InitializeAsync()
    {
        var connectionString = await postgres.CreateFreshDatabaseAsync();
        _provider = OutboundTestHost.Build(connectionString);
        await OutboundTestHost.MigrateAsync(_provider);
    }

    public async Task DisposeAsync() => await _provider.DisposeAsync();

    [Fact]
    public async Task Create_order_persists_new_backlog_order_with_pending_lines()
    {
        var result = await PipelineRunner.SendAsync(_provider, ValidCommand(("SKU-MILK", 10m)));

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.Message : string.Empty);

        var order = (await PipelineRunner.OrdersAsync(_provider)).Should().ContainSingle().Subject;
        order.Id.Value.Should().Be(result.Value);
        order.Status.Should().Be(OutboundOrderStatus.New);
        order.WaveId.Should().BeNull("order backlog belum di-wave");
        var line = order.OrderLines.Should().ContainSingle().Subject;
        line.Sku.Should().Be("SKU-MILK");
        line.Qty.Should().Be(10m);
        line.AllocatedQty.Should().Be(0m);
        line.AllocationStatus.Should().Be(AllocationStatus.Pending);
    }

    [Fact]
    public async Task Create_order_rejects_duplicate_sku_across_lines()
    {
        var result = await PipelineRunner.SendAsync(_provider, ValidCommand(("SKU-MILK", 10m), ("SKU-MILK", 5m)));

        result.ErrorType.Should().Be(ResultErrorType.Validation);
        result.Error.Code.Should().Be("outbound_order.sku_duplicated");
        (await PipelineRunner.OrdersAsync(_provider)).Should().BeEmpty();
    }

    [Fact]
    public async Task Create_order_rejects_empty_lines()
    {
        var command = new CreateOutboundOrderCommand(Guid.NewGuid(), "Toko Tom", "Jl. Merdeka 1", "Jakarta", []);

        var result = await PipelineRunner.SendAsync(_provider, command);

        result.IsFailure.Should().BeTrue();
        (await PipelineRunner.OrdersAsync(_provider)).Should().BeEmpty();
    }

    [Fact]
    public async Task Create_order_rejects_missing_customer()
    {
        var command = new CreateOutboundOrderCommand(
            Guid.Empty, "Toko Tom", "Jl. Merdeka 1", "Jakarta", [new CreateOutboundOrderLine("SKU-MILK", 10m, "CARTON")]);

        var result = await PipelineRunner.SendAsync(_provider, command);

        result.IsFailure.Should().BeTrue();
        (await PipelineRunner.OrdersAsync(_provider)).Should().BeEmpty();
    }

    private static CreateOutboundOrderCommand ValidCommand(params (string Sku, decimal Qty)[] lines) =>
        new(
            Guid.NewGuid(),
            "Toko Tom",
            "Jl. Merdeka 1",
            "Jakarta",
            [.. lines.Select(line => new CreateOutboundOrderLine(line.Sku, line.Qty, "CARTON"))]);
}
