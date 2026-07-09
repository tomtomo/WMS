using AwesomeAssertions;
using Wms.Contracts.Abstractions;
using Wms.Inbound.Application.EventTranslation;
using Wms.Inbound.Contracts;
using Wms.Inbound.Domain;
using Wms.Inbound.Domain.Enums;
using Wms.Inbound.Domain.ValueObjects;
using Wms.Inbound.IntegrationTests.TestSupport;
using Xunit;

namespace Wms.Inbound.IntegrationTests;

// Translator tanpa DB
public sealed class GoodsReceiptEventTranslatorTests
{
    private readonly InMemoryIntegrationEventOutbox _outbox = new();

    [Fact]
    public async Task Confirmed_diterjemahkan_ke_grconfirmed_coreflow()
    {
        var gr = BuildPendingGoodsReceipt(scanQty: 10m);
        gr.Confirm().IsSuccess.Should().BeTrue();

        await new GoodsReceiptEventTranslator(_outbox).TranslateAndClearAsync(gr);

        _outbox.Entries.Should().HaveCount(2, "PendingReviewRaised (A3) + Confirmed (A4) sama-sama tertranslate");
        var confirmed = _outbox.Entries.Single(entry => entry.Event is GRConfirmed);
        confirmed.DeliveryClass.Should().Be(DeliveryClass.CoreFlow);

        var payload = (GRConfirmed)confirmed.Event;
        payload.GrId.Should().Be(gr.Id.Value);
        payload.SupplierId.Should().NotBeEmpty();
        payload.ReceivedLines.Should().ContainSingle(line => line.Sku == "SKU-A" && line.Qty == 10m);
        payload.RejectedLines.Should().BeEmpty();

        gr.DomainEvents.Should().BeEmpty("translator men-drain event agar tidak tertulis dua kali");
    }

    [Fact]
    public async Task PendingReview_diterjemahkan_ke_notification()
    {
        var gr = BuildPendingGoodsReceipt(scanQty: 12m);

        await new GoodsReceiptEventTranslator(_outbox).TranslateAndClearAsync(gr);

        _outbox.Entries.Should().ContainSingle();
        _outbox.Entries[0].DeliveryClass.Should().Be(DeliveryClass.Notification);
        var payload = (GoodsReceiptPendingReview)_outbox.Entries[0].Event;
        payload.HasOverDelivery.Should().BeTrue();
        payload.DiscrepancyCount.Should().Be(1);
    }

    [Fact]
    public async Task Held_tidak_menghasilkan_integration_event_tapi_event_tetap_terdrain()
    {
        var gr = BuildPendingGoodsReceipt(scanQty: 10m);
        gr.ClearDomainEvents();
        gr.Hold(HoldReason.Create("lot tercampur").Value).IsSuccess.Should().BeTrue();

        await new GoodsReceiptEventTranslator(_outbox).TranslateAndClearAsync(gr);

        _outbox.Entries.Should().BeEmpty();
        gr.DomainEvents.Should().BeEmpty();
    }

    private static GoodsReceipt BuildPendingGoodsReceipt(decimal scanQty)
    {
        var gr = GoodsReceipt.Create(
            GoodsReceiptId.Create(Guid.NewGuid()).Value,
            "PO-1",
            Guid.NewGuid(),
            Guid.NewGuid(),
            DockDoor.Create("DOCK-1").Value,
            [ExpectedLine.Create("SKU-A", 10m, "EA").Value]).Value;

        gr.Scan(ScannedLine.Create("SKU-A", scanQty, null, null, LineStatus.Good).Value).IsSuccess.Should().BeTrue();
        gr.CompleteScan().IsSuccess.Should().BeTrue();

        foreach (var discrepancy in gr.Discrepancies)
        {
            var action = discrepancy.Type == DiscrepancyType.OverDelivery
                ? ResolutionAction.RejectExcess
                : ResolutionAction.AcceptPartial;
            gr.Resolve(discrepancy.Id, action).IsSuccess.Should().BeTrue();
        }

        return gr;
    }
}
