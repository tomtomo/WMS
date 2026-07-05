using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Outbound.Application.Abstractions;
using Wms.Outbound.Application.EventTranslation;
using Wms.Outbound.Contracts.Payloads;
using Wms.Outbound.Domain;

namespace Wms.Outbound.Application.Features.CreateWave;

// load order backlog lalu JoinWave (New ke InProgress), Wave.Create (Active), rilis WaveReleased ke Outbox
// Commit oleh TransactionBehavior.
internal sealed class CreateWaveHandler(
    IOutboundOrderRepository orderRepository,
    IWaveRepository waveRepository,
    IWarehouseReader warehouseReader,
    OutboundEventTranslator translator) : ICommandHandler<CreateWaveCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateWaveCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Lookup master data.
        if (!await warehouseReader.ExistsAsync(command.WarehouseId, cancellationToken))
        {
            return Result.Invalid<Guid>(new Error("wave.warehouse_unknown", "WarehouseId tidak dikenal di Master Data."));
        }

        var orderIds = new List<OutboundOrderId>(command.OrderIds.Count);
        foreach (var raw in command.OrderIds)
        {
            var id = OutboundOrderId.Create(raw);
            if (id.IsFailure)
            {
                return id.ForwardFailure<Guid>();
            }

            orderIds.Add(id.Value);
        }

        var orders = await orderRepository.ListByIdsAsync(orderIds, cancellationToken);
        if (orders.Count != orderIds.Count)
        {
            return Result.NotFound<Guid>(new Error("wave.order_not_found", "Sebagian order tidak ditemukan."));
        }

        var waveId = WaveId.Create(Guid.NewGuid()).Value;

        // Tiap order backlog masuk wave (New ke InProgress). Gagal bila ada yang bukan New.
        foreach (var order in orders)
        {
            var assigned = order.AssignToWave(waveId);
            if (assigned.IsFailure)
            {
                return assigned.ForwardFailure<Guid>();
            }
        }

        var wave = Wave.Create(waveId, command.WarehouseId, orderIds, []);
        if (wave.IsFailure)
        {
            return wave.ForwardFailure<Guid>();
        }

        await waveRepository.AddAsync(wave.Value, cancellationToken);

        // WaveReleased ke Inventory: satu WaveLine per demand line.
        var lines = orders
            .SelectMany(order => order.OrderLines.Select(line => new WaveLine(order.Id.Value, line.Sku, line.Qty)))
            .ToList();
        await translator.ReleaseWaveAsync(wave.Value, lines, cancellationToken);

        return Result.Success(waveId.Value);
    }
}
