using System.Collections.Concurrent;
using System.Reflection;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.BuildingBlocks.Application.Behaviors;

// Membuat failure Result untuk TResponse generik di pipeline.
internal static class PipelineFailure
{
    private static readonly ConcurrentDictionary<Type, Dictionary<ResultErrorType, MethodInfo>> _typedFactories = new();

    public static TResponse Create<TResponse>(ResultErrorType errorType, Error error)
        where TResponse : Result
    {
        if (typeof(TResponse) == typeof(Result))
        {
            return (TResponse)(object)NonGenericFailure(errorType, error);
        }

        var factories = _typedFactories.GetOrAdd(typeof(TResponse), BuildTypedFactories);
        var factory = factories.TryGetValue(errorType, out var method) ? method : factories[ResultErrorType.Failure];
        return (TResponse)factory.Invoke(null, new object[] { error })!;
    }

    private static Result NonGenericFailure(ResultErrorType errorType, Error error) => errorType switch
    {
        ResultErrorType.Validation => Result.Invalid(error),
        ResultErrorType.Conflict => Result.Conflict(error),
        ResultErrorType.NotFound => Result.NotFound(error),
        ResultErrorType.Forbidden => Result.Forbidden(error),
        _ => Result.Failure(error),
    };

    private static Dictionary<ResultErrorType, MethodInfo> BuildTypedFactories(Type responseType)
    {
        var valueType = responseType.GetGenericArguments()[0];

        MethodInfo Closed(string name) => typeof(Result)
            .GetMethod(name, 1, BindingFlags.Public | BindingFlags.Static, binder: null, [typeof(Error)], modifiers: null)!
            .MakeGenericMethod(valueType);

        return new Dictionary<ResultErrorType, MethodInfo>
        {
            [ResultErrorType.Validation] = Closed(nameof(Result.Invalid)),
            [ResultErrorType.Conflict] = Closed(nameof(Result.Conflict)),
            [ResultErrorType.NotFound] = Closed(nameof(Result.NotFound)),
            [ResultErrorType.Forbidden] = Closed(nameof(Result.Forbidden)),
            [ResultErrorType.Failure] = Closed(nameof(Result.Failure)),
        };
    }
}
