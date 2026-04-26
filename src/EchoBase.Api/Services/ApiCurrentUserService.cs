using System.Security.Claims;
using EchoBase.Core.Interfaces;

namespace EchoBase.Api.Services;

/// <summary>
/// Implementación de <see cref="ICurrentUserService"/> para la API REST.
/// Lee los claims del JWT Bearer validado por ASP.NET Core y garantiza
/// que el usuario exista en la base de datos.
/// </summary>
internal sealed class ApiCurrentUserService(
    IHttpContextAccessor httpContextAccessor,
    IUserRepository userRepository)
    : ICurrentUserService
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    /// <inheritdoc />
    public Guid UserId =>
        GetClaimAsGuid("http://schemas.microsoft.com/identity/claims/objectidentifier")
        ?? GetClaimAsGuid(ClaimTypes.NameIdentifier)
        ?? Guid.Empty;

    /// <inheritdoc />
    public string UserName =>
        User?.FindFirst("name")?.Value
        ?? User?.FindFirst(ClaimTypes.Name)?.Value
        ?? string.Empty;

    /// <inheritdoc />
    public string Email =>
        User?.FindFirst("preferred_username")?.Value
        ?? User?.FindFirst(ClaimTypes.Email)?.Value
        ?? string.Empty;

    /// <inheritdoc />
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    /// <summary>
    /// Garantiza que el usuario autenticado exista en la base de datos.
    /// Debe invocarse al inicio de cada endpoint que requiera el usuario en sesión.
    /// </summary>
    public async Task EnsureUserAsync(CancellationToken cancellationToken = default)
    {
        if (IsAuthenticated && UserId != Guid.Empty)
            await userRepository.EnsureUserAsync(UserId, UserName, Email, cancellationToken);
    }

    private Guid? GetClaimAsGuid(string claimType)
    {
        var value = User?.FindFirst(claimType)?.Value;
        return Guid.TryParse(value, out var guid) ? guid : null;
    }
}
