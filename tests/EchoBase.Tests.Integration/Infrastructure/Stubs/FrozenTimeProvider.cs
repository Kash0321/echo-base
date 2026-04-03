namespace EchoBase.Tests.Integration.Infrastructure.Stubs;

/// <summary>
/// Implementación de <see cref="TimeProvider"/> con el tiempo congelado en un instante determinístico.
/// Permite controlar la fecha "de hoy" que ven los handlers de negocio sin depender del reloj real.
/// </summary>
internal sealed class FrozenTimeProvider(DateTimeOffset frozenUtc) : TimeProvider
{
    /// <summary>Crea un proveedor congelado en el inicio del día UTC actual.</summary>
    public static FrozenTimeProvider AtStartOfToday()
        => new(new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero));

    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow() => frozenUtc;
}
