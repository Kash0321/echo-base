using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace EchoBase.Infrastructure.Data;

/// <summary>
/// Generador de valores para EF Core que produce UUID v7 (<see cref="Guid.CreateVersion7()"/>).
/// A diferencia de <c>Guid.NewGuid()</c> (v4), los UUID v7 incorporan un prefijo de
/// marca de tiempo de 48 bits, lo que los hace ordenables cronológicamente y reduce
/// la fragmentación de índices B-tree en bases de datos relacionales.
/// </summary>
/// <remarks>
/// Se registra mediante <c>HasValueGenerator&lt;UuidV7ValueGenerator&gt;()</c> en las
/// configuraciones Fluent API de las entidades con PKs de tipo <see cref="Guid"/>.
/// Actúa como fallback de EF Core cuando no se proporciona el ID explícitamente;
/// en la práctica, los handlers siempre suministran el ID a través de
/// <c>Guid.CreateVersion7()</c> antes de persistir la entidad.
/// </remarks>
internal sealed class UuidV7ValueGenerator : ValueGenerator<Guid>
{
    /// <inheritdoc />
    public override Guid Next(EntityEntry entry) => Guid.CreateVersion7();

    /// <inheritdoc />
    /// <remarks>
    /// <c>false</c>: el valor generado es definitivo y no necesita ser sustituido
    /// por un valor de BD tras la inserción.
    /// </remarks>
    public override bool GeneratesTemporaryValues => false;
}
