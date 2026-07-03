using MediatR;
using Microsoft.Extensions.Logging;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.BuildingBlocks.Application.Behaviors;

// Logging terstruktur, pipeline ke empat.
public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger,
    TimeProvider timeProvider)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var startedAt = timeProvider.GetTimestamp();

        var response = await next(cancellationToken);

        var elapsedMs = timeProvider.GetElapsedTime(startedAt).TotalMilliseconds;
        if (response.IsSuccess)
        {
            logger.LogInformation("{Request} sukses dalam {ElapsedMs} ms", requestName, elapsedMs);
        }
        else
        {
            logger.LogWarning(
                "{Request} gagal dengan {ErrorCode} dalam {ElapsedMs} ms", requestName, response.Error.Code, elapsedMs);
        }

        return response;
    }
}
