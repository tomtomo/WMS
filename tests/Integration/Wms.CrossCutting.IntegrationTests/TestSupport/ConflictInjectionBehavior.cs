using MediatR;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.CrossCutting.IntegrationTests.TestSupport;

// Dipasang paling dekat dengan handler agar bisa menyisipkan conflict sebelum audit dan commit.
internal sealed class ConflictInjectionBehavior<TRequest, TResponse>(ConflictInjector injector)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next(cancellationToken);
        if (response.IsSuccess)
        {
            await injector.FireAsync();
        }

        return response;
    }
}
