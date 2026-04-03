using EchoBase.Core.Entities;
using EchoBase.Core.Entities.Enums;

namespace EchoBase.Core.Interfaces;

/// <summary>DTO para una entrada de auditoría en las consultas paginadas.</summary>
public sealed record AuditLogDto(
    Guid Id,
    Guid? PerformedByUserId,
    string? PerformedByName,
    AuditAction Action,
    string Details,
    DateTimeOffset Timestamp);

/// <summary>
/// Abstracción para el acceso a datos del log de auditoría.
/// </summary>
public interface IAuditLogRepository
{
    /// <summary>Añade una nueva entrada al log de auditoría.</summary>
    Task AddAsync(AuditLog entry, CancellationToken ct = default);

    /// <summary>
    /// Obtiene una página de entradas del log de auditoría aplicando los filtros indicados.
    /// </summary>
    /// <param name="fromDate">Fecha de inicio del filtro (inclusiva). <see langword="null"/> para sin límite inferior.</param>
    /// <param name="toDate">Fecha de fin del filtro (inclusiva). <see langword="null"/> para sin límite superior.</param>
    /// <param name="actionFilter">Acción específica a filtrar. <see langword="null"/> para todas.</param>
    /// <param name="userSearch">Texto libre para buscar en el nombre del usuario. <see langword="null"/> para todos.</param>
    /// <param name="page">Número de página (base 1).</param>
    /// <param name="pageSize">Cantidad de registros por página.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task<(IReadOnlyList<AuditLogDto> Items, int TotalCount)> GetPagedAsync(
        DateOnly? fromDate,
        DateOnly? toDate,
        AuditAction? actionFilter,
        string? userSearch,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>Persiste los cambios pendientes en la unidad de trabajo.</summary>
    Task SaveChangesAsync(CancellationToken ct = default);
}
