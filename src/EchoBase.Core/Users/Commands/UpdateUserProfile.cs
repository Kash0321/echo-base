using EchoBase.Core.Common;
using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Interfaces;
using MediatR;

namespace EchoBase.Core.Users.Commands;

/// <summary>
/// Comando para actualizar el perfil editable de un usuario (línea de negocio,
/// teléfono y preferencias de notificación).
/// </summary>
/// <param name="UserId">Identificador del usuario que actualiza su perfil.</param>
/// <param name="BusinessLine">Nueva línea de negocio del usuario.</param>
/// <param name="PhoneNumber">Número de teléfono de contacto (opcional).</param>
/// <param name="EmailNotifications">Habilitar notificaciones por correo electrónico.</param>
/// <param name="TeamsNotifications">Habilitar notificaciones por Microsoft Teams.</param>
public sealed record UpdateUserProfileCommand(
    Guid UserId,
    BusinessLine BusinessLine,
    string? PhoneNumber,
    bool EmailNotifications,
    bool TeamsNotifications) : IRequest<Result>;

/// <summary>
/// Handler que implementa las reglas de negocio para la actualización del perfil de usuario.
/// </summary>
/// <remarks>
/// Reglas validadas:
/// <list type="number">
///   <item>El usuario debe existir en el sistema.</item>
///   <item>El número de teléfono, si se proporciona, no puede superar 30 caracteres.</item>
/// </list>
/// </remarks>
public sealed class UpdateUserProfileHandler(IUserRepository repository)
    : IRequestHandler<UpdateUserProfileCommand, Result>
{
    private const int MaxPhoneLength = 30;

    /// <inheritdoc />
    public async Task<Result> Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
    {
        var trimmedPhone = request.PhoneNumber?.Trim() is { Length: > 0 } t ? t : null;

        if (trimmedPhone is not null && trimmedPhone.Length > MaxPhoneLength)
            return Result.Failure(UserErrors.PhoneNumberTooLong);

        var user = await repository.GetForUpdateAsync(request.UserId, cancellationToken);

        if (user is null)
            return Result.Failure(UserErrors.UserNotFound);

        user.UpdateProfile(request.BusinessLine, trimmedPhone);
        user.UpdateNotificationPreferences(request.EmailNotifications, request.TeamsNotifications);

        await repository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
