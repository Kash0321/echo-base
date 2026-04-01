using EchoBase.Core.Entities;
using EchoBase.Core.Interfaces;
using MediatR;

namespace EchoBase.Core.Common;

/// <summary>
/// Behavior de MediatR que escribe una entrada en el log de auditoría
/// tras la ejecución exitosa de cualquier comando que implemente <see cref="IAuditableRequest"/>.
/// </summary>
/// <remarks>
/// Solo registra acciones exitosas para mantener el log limpio: los intentos
/// fallidos (validaciones de negocio, errores de autorización) no se persisten.
/// El behavior es genérico abierto y se registra una sola vez como
/// <c>IPipelineBehavior&lt;,&gt;</c> en el contenedor DI.
/// </remarks>
public sealed class AuditLoggingBehavior<TRequest, TResponse>(
    IAuditLogRepository auditLogRepository,
    TimeProvider timeProvider)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next();

        if (request is IAuditableRequest auditable && IsSuccess(response))
        {
            var entry = new AuditLog(Guid.CreateVersion7())
            {
                PerformedByUserId = auditable.PerformedByUserId,
                Action = auditable.AuditAction,
                Details = auditable.BuildAuditDetails(),
                Timestamp = timeProvider.GetUtcNow(),
            };

            await auditLogRepository.AddAsync(entry, cancellationToken);
            await auditLogRepository.SaveChangesAsync(cancellationToken);
        }

        return response;
    }

    /// <summary>
    /// Determina si la respuesta corresponde a una operación exitosa.
    /// Compatible con <see cref="Result"/> y <see cref="Result{T}"/>.
    /// </summary>
    private static bool IsSuccess(TResponse response) => response switch
    {
        Result r => r.IsSuccess,
        _ when response is not null => IsResultTSuccess(response),
        _ => false
    };

    private static bool IsResultTSuccess(TResponse response)
    {
        var type = response!.GetType();
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var prop = type.GetProperty(nameof(Result.IsSuccess));
            return prop?.GetValue(response) is true;
        }
        return false;
    }
}
