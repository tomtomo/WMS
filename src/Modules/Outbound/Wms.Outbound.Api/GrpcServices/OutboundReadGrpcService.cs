using Grpc.Core;
using Wms.BuildingBlocks.Domain.Results;
using Wms.BuildingBlocks.Web.GrpcInterceptors;
using Wms.Outbound.Api.Grpc.V1;
using Wms.Outbound.Application.Abstractions;

namespace Wms.Outbound.Api.GrpcServices;

// Read only gRPC internal — Result.Failure dibawa ResultFailureException, ErrorMappingInterceptor memetakan ke status gRPC.
public sealed class OutboundReadGrpcService(IWaveReader reader)
    : OutboundReadService.OutboundReadServiceBase
{
    public override async Task<WaveSummary> GetWave(GetWaveRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        if (!Guid.TryParse(request.WaveId, out var waveId))
        {
            throw new ResultFailureException(
                ResultErrorType.Validation,
                new Error("wave.id_invalid", "waveId bukan GUID valid."));
        }

        var wave = await reader.GetByIdAsync(waveId, context.CancellationToken);
        if (wave is null)
        {
            throw new ResultFailureException(
                ResultErrorType.NotFound,
                new Error("wave.not_found", "Wave tidak ditemukan."));
        }

        return new WaveSummary
        {
            WaveId = wave.WaveId.ToString(),
            WarehouseId = wave.WarehouseId.ToString(),
            Status = wave.Status,
            PickingTaskCount = wave.PickingTaskCount,
            CompletedPickingTaskCount = wave.CompletedPickingTaskCount,
        };
    }
}
