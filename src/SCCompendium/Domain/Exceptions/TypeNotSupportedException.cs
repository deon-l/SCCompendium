namespace SCCompendium.Domain.Exceptions;

/// <summary>
/// The exception thrown when the type provided by an instance or generic isn't supported by an operation
/// </summary>
public class TypeNotSupportedException : SystemException
{
    /// <summary>
    /// The provided type that isn't supported by an operation.
    /// </summary>
    private Type? Type { get; } = null;

    public TypeNotSupportedException(Type type) : base("The generic value or instance's type is not supported.")
    {
        Type = type;
    }

    public TypeNotSupportedException(Type type, string message) : base(message)
    {
        Type = type;
    }

    public override string Message => base.Message + (Type is null ? String.Empty : $"(type: {Type.Name})");

    /// <summary>
    /// Cast <paramref name="obj"/> to type <typeparamref name="T"/> and return it,
    /// or throw a <see cref="TypeNotSupportedException"/> if can't
    /// </summary>
    /// <remarks>Does not consider user-defined conversions.</remarks>
    public static T CastOrThrowIfCantCast<T>(object? obj)
    {
        if (obj is T t) return t;
        throw new TypeNotSupportedException(typeof(T), $"Cannot cast object ({obj?.GetType().Name ?? "null"}) to target type.");
    }
}
