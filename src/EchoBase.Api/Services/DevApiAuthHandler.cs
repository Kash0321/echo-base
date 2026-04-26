using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace EchoBase.Api.Services;

/// <summary>
/// Esquema de autenticación para desarrollo local que auto-autentica
/// un usuario simulado sin necesidad de Azure AD ni token Bearer.
/// Equivalente al <c>DevAuthHandler</c> de <c>EchoBase.Web</c>.
/// Solo se activa cuando <c>Authentication:UseDevelopmentStub</c> es <c>true</c>.
/// </summary>
internal sealed class DevApiAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    System.Text.Encodings.Web.UrlEncoder encoder,
    IConfiguration configuration)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    internal const string SchemeName = "DevApiAuth";

    /// <inheritdoc />
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "00000000-0000-0000-0000-000000000001"),
            new("http://schemas.microsoft.com/identity/claims/objectidentifier", "00000000-0000-0000-0000-000000000001"),
            new(ClaimTypes.Name, "Dev User"),
            new("name", "Dev User"),
            new(ClaimTypes.Email, "dev@localhost"),
            new("preferred_username", "dev@localhost"),
        };

        if (configuration.GetValue<bool>("Authentication:DevUserIsManager"))
            claims.Add(new Claim(ClaimTypes.Role, "Manager"));

        if (configuration.GetValue<bool>("Authentication:DevUserIsSystemAdmin"))
            claims.Add(new Claim(ClaimTypes.Role, "SystemAdmin"));

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
