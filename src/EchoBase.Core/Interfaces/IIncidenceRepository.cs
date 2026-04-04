using EchoBase.Core.Entities;
using EchoBase.Core.Entities.Enums;

namespace EchoBase.Core.Interfaces;

/// <summary>
/// DTO de incidencia para la vista propia del usuario (solo sus propias incidencias).
/// </summary>
/// <param name="Id">Identificador del reporte.</param>
/// <param name="DockCode">Código del puesto afectado.</param>
/// <param name="DockLocation">Descripción de la ubicación del puesto.</param>
/// <param name="Description">Descripción de la incidencia reportada.</param>
/// <param name="Status">Estado actual del reporte.</param>
/// <param name="CreatedAt">Fecha y hora UTC de creación del reporte.</param>
/// <param name="UpdatedAt">Fecha y hora UTC de la última actualización. <see langword="null"/> si no ha sido actualizado.</param>
/// <param name="ManagerComment">Comentario del Manager. <see langword="null"/> si no hay comentario.</param>
public sealed record IncidenceReportDto(
    Guid Id,
    string DockCode,
    string DockLocation,
    string Description,
    IncidenceStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    string? ManagerComment);

/// <summary>
/// DTO de incidencia enriquecido para la vista de gestión del Manager.
/// Incluye datos del usuario que reportó la incidencia.
/// </summary>
/// <param name="Id">Identificador del reporte.</param>
/// <param name="DockCode">Código del puesto afectado.</param>
/// <param name="DockLocation">Descripción de la ubicación del puesto.</param>
/// <param name="Description">Descripción de la incidencia reportada.</param>
/// <param name="Status">Estado actual del reporte.</param>
/// <param name="CreatedAt">Fecha y hora UTC de creación del reporte.</param>
/// <param name="UpdatedAt">Fecha y hora UTC de la última actualización. <see langword="null"/> si no ha sido actualizado.</param>
/// <param name="ManagerComment">Comentario del Manager. <see langword="null"/> si no hay comentario.</param>
/// <param name="ReporterName">Nombre del usuario que reportó la incidencia.</param>
/// <param name="ReporterEmail">Correo electrónico del usuario que reportó la incidencia.</param>
public sealed record IncidenceReportManagerDto(
    Guid Id,
    string DockCode,
    string DockLocation,
    string Description,
    IncidenceStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    string? ManagerComment,
    string ReporterName,
    string ReporterEmail);

/// <summary>
/// Abstracción del repositorio de reportes de incidencias.
/// </summary>
public interface IIncidenceRepository
{
    /// <summary>Obtiene un reporte de incidencia por su identificador, incluyendo el puesto y el usuario que lo reportó.</summary>
    Task<IncidenceReport?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Añade un nuevo reporte de incidencia a la unidad de trabajo.</summary>
    Task AddAsync(IncidenceReport report, CancellationToken ct = default);

    /// <summary>Comprueba si un puesto de trabajo existe por su identificador.</summary>
    Task<bool> DockExistsAsync(Guid dockId, CancellationToken ct = default);

    /// <summary>Obtiene el código alfanumérico de un puesto de trabajo. Devuelve <see langword="null"/> si no existe.</summary>
    Task<string?> GetDockCodeAsync(Guid dockId, CancellationToken ct = default);

    /// <summary>
    /// Obtiene todos los reportes de incidencias de un usuario ordenados de más reciente a más antiguo.
    /// Incluye el puesto de trabajo asociado.
    /// </summary>
    Task<List<IncidenceReport>> GetUserIncidencesAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Obtiene todos los reportes de incidencias del sistema, paginados y ordenados de más reciente a más antiguo.
    /// Incluye el puesto de trabajo y el usuario que lo reportó.
    /// </summary>
    Task<(List<IncidenceReport> Items, int TotalCount)> GetAllIncidencesAsync(int page, int pageSize, CancellationToken ct = default);

    /// <summary>
    /// Obtiene el conteo de incidencias agrupadas por puesto de trabajo y estado.
    /// Devuelve un diccionario donde la clave es el DockId y el valor es otro diccionario con IncidenceStatus como clave y conteo como valor.
    /// </summary>
    Task<Dictionary<Guid, Dictionary<IncidenceStatus, int>>> GetIncidenceCountsByDockAsync(CancellationToken ct = default);

    /// <summary>Persiste los cambios pendientes en la unidad de trabajo.</summary>
    Task SaveChangesAsync(CancellationToken ct = default);
}
