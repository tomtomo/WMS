using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.BuildingBlocks.Application.Behaviors;

// validasi terpusat, pipeline pertama
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var failures = await CollectFailuresAsync(request, cancellationToken);
        if (failures.Count == 0)
        {
            return await next(cancellationToken);
        }

        var message = string.Join("; ", failures.Select(failure => failure.ErrorMessage));
        return PipelineFailure.Create<TResponse>(ResultErrorType.Validation, new Error("validation.failed", message));
    }

    private async Task<IReadOnlyList<ValidationFailure>> CollectFailuresAsync(
        TRequest request,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return [];
        }

        var results = await Task.WhenAll(
            validators.Select(validator => validator.ValidateAsync(request, cancellationToken)));
        return results.SelectMany(result => result.Errors).ToList();
    }
}
