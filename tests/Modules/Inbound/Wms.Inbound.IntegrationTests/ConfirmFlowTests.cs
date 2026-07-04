using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Inbound.Application.Abstractions;
using Wms.Inbound.Application.EventTranslation;
using Wms.Inbound.Application.Features.ConfirmGoodsReceipt;
using Wms.Inbound.Contracts;
using Wms.Inbound.Domain;
using Wms.Inbound.Domain.Enums;
using Wms.Inbound.Domain.ValueObjects;
using Wms.Inbound.IntegrationTests.TestSupport;
using Xunit;
using ContractEnums = Wms.Inbound.Contracts.Enums;
using ContractLines = Wms.Inbound.Contracts.Payloads;

namespace Wms.Inbound.IntegrationTests;

// Write state GR dan baris Outbox dalam satu transaksi
[Collection(PostgresCollection.Name)]
public sealed class ConfirmFlowTests(PostgresFixture postgres) : IAsyncLifetime
{
    private ServiceProvider _provider = null!;

    public async Task InitializeAsync()
    {
        var connectionString = await postgres.CreateFreshDatabaseAsync();
        _provider = InboundTestHost.Build(connectionString);
        await InboundTestHost.MigrateAsync(_provider);
    }

    public async Task DisposeAsync() => await _provider.DisposeAsync();

    [Fact]
    public async Task Confirm_menulis_state_confirmed_dan_outbox_grconfirmed_sekali_commit()
    {
        // Over delivery SKU-A (12 > 10), short dan QC SKU-B (3 QcHold < 5), stray SKU-X.
        var grId = await GoodsReceiptScenarios.CreateAsync(_provider, ("SKU-A", 10m), ("SKU-B", 5m));
        await GoodsReceiptScenarios.ScanAsync(_provider, grId, "SKU-A", 8m, batch: "B1");
        await GoodsReceiptScenarios.ScanAsync(_provider, grId, "SKU-A", 4m, batch: "B2");
        await GoodsReceiptScenarios.ScanAsync(_provider, grId, "SKU-B", 3m, LineStatus.QcHold);
        await GoodsReceiptScenarios.ScanAsync(_provider, grId, "SKU-X", 4m, LineStatus.WrongItem);
        await GoodsReceiptScenarios.CompleteScanAsync(_provider, grId);
        await GoodsReceiptScenarios.ResolveAllAsync(_provider, grId);

        var confirmed = await PipelineRunner.SendAsync(_provider, new ConfirmGoodsReceiptCommand(grId));

        confirmed.IsSuccess.Should().BeTrue(confirmed.IsFailure ? confirmed.Error.Message : string.Empty);

        var detail = await GoodsReceiptScenarios.ReadDetailAsync(_provider, grId);
        detail.Status.Should().Be(nameof(GoodsReceiptStatus.Confirmed));

        var outboxRows = await PipelineRunner.OutboxRowsAsync(_provider, GRConfirmed.LogicalName);
        outboxRows.Should().HaveCount(1, "Confirm sukses = tepat satu baris Outbox GRConfirmed");
        outboxRows[0].DeliveryClass.Should().Be(DeliveryClass.CoreFlow);
        outboxRows[0].ProcessedAt.Should().BeNull("belum ada dispatch lintas proses");

        // Published language asyncapi.yaml: camelCase dan enum nama.
        outboxRows[0].Payload.Should().Contain("\"grId\"").And.Contain("\"receivedLines\"")
            .And.Contain("\"status\":\"QcHold\"").And.Contain("\"reason\":\"OverDelivery\"");

        var payload = System.Text.Json.JsonSerializer.Deserialize<GRConfirmed>(
            outboxRows[0].Payload,
            Wms.BuildingBlocks.Infrastructure.Messaging.MessageEnvelope.PayloadSerializerOptions)!;
        payload.GrId.Should().Be(grId);
        payload.WarehouseId.Should().Be(GoodsReceiptScenarios.WarehouseId);
        payload.SupplierId.Should().Be(GoodsReceiptScenarios.SupplierId, "supplierId wajib non-null (Reporting per-supplier)");
        payload.ReceivedLines.Should().BeEquivalentTo(new[]
        {
            new ContractLines.ReceivedLine("SKU-B", 3m, null, null, ContractEnums.ReceivedLineStatus.QcHold),
            new ContractLines.ReceivedLine("SKU-A", 8m, "B1", null, ContractEnums.ReceivedLineStatus.Good),
            new ContractLines.ReceivedLine("SKU-A", 2m, "B2", null, ContractEnums.ReceivedLineStatus.Good),
        });
        payload.RejectedLines.Should().BeEquivalentTo(new[]
        {
            new ContractLines.RejectedLine("SKU-A", 2m, ContractEnums.RejectionReason.OverDelivery),
            new ContractLines.RejectedLine("SKU-X", 4m, ContractEnums.RejectionReason.WrongItem),
        });
    }

    [Fact]
    public async Task CompleteScan_menulis_outbox_pending_review_dengan_delivery_class_notification()
    {
        var grId = await GoodsReceiptScenarios.CreateAsync(_provider, ("SKU-A", 10m));
        await GoodsReceiptScenarios.ScanAsync(_provider, grId, "SKU-A", 12m);

        await GoodsReceiptScenarios.CompleteScanAsync(_provider, grId);

        var rows = await PipelineRunner.OutboxRowsAsync(_provider, GoodsReceiptPendingReview.LogicalName);
        rows.Should().HaveCount(1);
        rows[0].DeliveryClass.Should().Be(DeliveryClass.Notification);

        var payload = System.Text.Json.JsonSerializer.Deserialize<GoodsReceiptPendingReview>(
            rows[0].Payload,
            Wms.BuildingBlocks.Infrastructure.Messaging.MessageEnvelope.PayloadSerializerOptions)!;
        payload.GrId.Should().Be(grId);
        payload.HasOverDelivery.Should().BeTrue();
        payload.DiscrepancyCount.Should().Be(1);
    }

    [Fact]
    public async Task Confirm_di_state_salah_conflict_dan_tidak_menulis_outbox()
    {
        var grId = await GoodsReceiptScenarios.CreateAsync(_provider, ("SKU-A", 10m));

        var confirmed = await PipelineRunner.SendAsync(_provider, new ConfirmGoodsReceiptCommand(grId));

        confirmed.IsFailure.Should().BeTrue();
        confirmed.ErrorType.Should().Be(ResultErrorType.Conflict);
        confirmed.Error.Code.Should().Be("goods_receipt.not_pending");

        (await PipelineRunner.OutboxRowsAsync(_provider, GRConfirmed.LogicalName)).Should().BeEmpty();
        (await GoodsReceiptScenarios.ReadDetailAsync(_provider, grId)).Status
            .Should().Be(nameof(GoodsReceiptStatus.InProgress));
    }

    [Fact]
    public async Task Hold_tidak_menghasilkan_integration_event()
    {
        var grId = await GoodsReceiptScenarios.CreateAsync(_provider, ("SKU-A", 10m));
        await GoodsReceiptScenarios.ScanAsync(_provider, grId, "SKU-A", 10m);
        await GoodsReceiptScenarios.CompleteScanAsync(_provider, grId);

        var held = await PipelineRunner.SendAsync(
            _provider,
            new Application.Features.HoldGoodsReceipt.HoldGoodsReceiptCommand(grId, "dokumen bermasalah"));

        held.IsSuccess.Should().BeTrue();
        var all = await PipelineRunner.OutboxRowsAsync(_provider);
        all.Should().ContainSingle(row => row.LogicalName == GoodsReceiptPendingReview.LogicalName)
            .And.NotContain(row => row.LogicalName == GRConfirmed.LogicalName);
    }

    [Fact]
    public async Task Konflik_xmin_membatalkan_state_dan_outbox_bersama_anti_dual_write()
    {
        // GR bersih sampai Pending.
        var grId = await GoodsReceiptScenarios.CreateAsync(_provider, ("SKU-A", 10m));
        await GoodsReceiptScenarios.ScanAsync(_provider, grId, "SKU-A", 10m);
        await GoodsReceiptScenarios.CompleteScanAsync(_provider, grId);

        using var staleScope = _provider.CreateScope();
        var staleRepo = staleScope.ServiceProvider.GetRequiredService<IGoodsReceiptRepository>();
        var staleGr = (await staleRepo.GetAsync(GoodsReceiptId.Create(grId).Value))!;

        var heldByOther = await PipelineRunner.SendAsync(
            _provider,
            new Application.Features.HoldGoodsReceipt.HoldGoodsReceiptCommand(grId, "hold oleh SPV lain"));
        heldByOther.IsSuccess.Should().BeTrue();

        staleGr.Confirm().IsSuccess.Should().BeTrue("cek state domain memakai snapshot stale");
        await staleScope.ServiceProvider.GetRequiredService<GoodsReceiptEventTranslator>()
            .TranslateAndClearAsync(staleGr);
        var conflicted = await staleScope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();

        conflicted.IsFailure.Should().BeTrue();
        conflicted.ErrorType.Should().Be(ResultErrorType.Conflict);
        conflicted.Error.Code.Should().Be("concurrency.conflict");

        // Tak ada dual write parsial: GRConfirmed tidak menyentuh Outbox, status tetap Hold.
        (await PipelineRunner.OutboxRowsAsync(_provider, GRConfirmed.LogicalName)).Should().BeEmpty();
        (await GoodsReceiptScenarios.ReadDetailAsync(_provider, grId)).Status
            .Should().Be(nameof(GoodsReceiptStatus.Hold));
    }

    [Fact]
    public async Task Create_menolak_sku_tak_dikenal_master_data()
    {
        using var scope = _provider.CreateScope();
        var productReader = (FakeProductReader)scope.ServiceProvider.GetRequiredService<IProductReader>();
        productReader.MarkUnknown("SKU-GHOST");

        var created = await PipelineRunner.SendAsync(
            _provider,
            new Application.Features.CreateGoodsReceiptHeader.CreateGoodsReceiptHeaderCommand(
                "PO-X",
                GoodsReceiptScenarios.SupplierId,
                GoodsReceiptScenarios.WarehouseId,
                "DOCK-1",
                [new Application.Features.CreateGoodsReceiptHeader.ExpectedLineInput("SKU-GHOST", 1m, "EA")]));

        created.IsFailure.Should().BeTrue();
        created.Error.Code.Should().Be("goods_receipt.sku_unknown");
    }
}
