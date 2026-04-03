using System.Security.Claims;
using EchoBase.Core.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;

namespace EchoBase.Web.Services;

/// <summary>
/// Implementación de <see cref="ICurrentUserService"/> para Blazor Server
/// que lee los claims del usuario autenticado vía Azure AD.
/// </summary>
internal sealed class BlazorCurrentUserService(
    AuthenticationStateProvider authStateProvider,
    IUserRepository userRepository)
    : ICurrentUserService
{
    private ClaimsPrincipal? _user;

    public Guid UserId => GetClaimAsGuid("http://schemas.microsoft.com/identity/claims/objectidentifier")
                       ?? GetClaimAsGuid(ClaimTypes.NameIdentifier)
                       ?? Guid.Empty;

    public string UserName => _user?.FindFirst("name")?.Value
                           ?? _user?.FindFirst(ClaimTypes.Name)?.Value
                           ?? string.Empty;

    public string Email => _user?.FindFirst("preferred_username")?.Value
                        ?? _user?.FindFirst(ClaimTypes.Email)?.Value
                        ?? string.Empty;

    public bool IsAuthenticated => _user?.Identity?.IsAuthenticated ?? false;

    /// <summary>
    /// Carga los claims del usuario autenticado y garantiza que exista en la base de datos.
    /// Debe invocarse una vez por ciclo de vida del componente.
    /// </summary>
    public async Task InitializeAsync()
    {
        var state = await authStateProvider.GetAuthenticationStateAsync();
        _user = state.User;

        if (IsAuthenticated && UserId != Guid.Empty)
        {
            await userRepository.EnsureUserAsync(UserId, UserName, Email);
        }
    }

    private Guid? GetClaimAsGuid(string claimType)
    {
        var value = _user?.FindFirst(claimType)?.Value;
        return Guid.TryParse(value, out var guid) ? guid : null;
    }
}
