namespace EchoBase.Core.Entities;

/// <summary>
/// Clase base abstracta para todas las entidades del dominio.
/// Centraliza la validación de identificadores de tipo <see cref="Guid"/>.
/// </summary>
public abstract class EntityBase
{
    /// <summary>
    /// Valida que <paramref name="value"/> no sea <see cref="Guid.Empty"/>.
    /// </summary>
    /// <param name="value">Valor del identificador a validar.</param>
    /// <param name="paramName">
    /// Nombre del parámetro que se incluirá en el mensaje de la excepción.
    /// Por defecto es <c>"id"</c>.
    /// </param>
    /// <returns>El mismo <paramref name="value"/> si es válido.</returns>
    /// <exception cref="ArgumentException">
    /// Se lanza cuando <paramref name="value"/> es <see cref="Guid.Empty"/>.
    /// </exception>
    protected static Guid EnsureValidId(Guid value, string paramName = "id")
    {
        return value == Guid.Empty
            ? throw new ArgumentException("Identifier cannot be empty.", paramName)
            : value;
    }
}
