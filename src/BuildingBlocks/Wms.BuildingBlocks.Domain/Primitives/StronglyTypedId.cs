using Wms.BuildingBlocks.Domain.Results;

namespace Wms.BuildingBlocks.Domain.Primitives;

// Typed ID — tidak tertukar antar aggregate saat compile, valid by construction lewat Create. GoF Factory Method
public abstract record StronglyTypedId<TSelf, TValue>
    where TSelf : StronglyTypedId<TSelf, TValue>
    where TValue : notnull
{
    protected StronglyTypedId(TValue value) => Value = value;

    public TValue Value { get; }

    protected static Result<TSelf> Create(TValue value, Func<TValue, TSelf> factory)
    {
        if (IsEmpty(value))
        {
            return Result.Invalid<TSelf>(new Error("id.invalid", "ID tidak boleh kosong."));
        }

        return Result.Success(factory(value));
    }

    private static bool IsEmpty(TValue value) => value switch
    {
        Guid guid => guid == Guid.Empty,
        string text => string.IsNullOrWhiteSpace(text),
        _ => false,
    };
}
