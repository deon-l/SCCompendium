namespace SCCompendium.Application.DbAccess;

/// <summary>
/// Interface for creating settings (either default or from input) for how to access database.
/// </summary>
public interface IDbSettingsManager
{
    /// <summary>Get settings, as specified type.</summary>
    /// <remarks>Meant for use for classes in <c>SCCompendium.Infrastructure</c></remarks>
    /// <exception cref="NotSupportedException">Can't give setting as specified type <typeparamref name="T"/>.</exception>
    public T Settings<T>();

    /// <summary>Set instance default settings.</summary>
    public void Initialize();

    /// <summary>Set instance to use settings, based on input <paramref name="reader"/>.</summary>
    /// <exception cref="FormatException"><paramref name="reader"/> is in an invalid format.</exception>
    public void Initialize(TextReader reader);
}
