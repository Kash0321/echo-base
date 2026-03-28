using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace EchoBase.Web.Services;

/// <summary>
/// Esquema de autenticación para desarrollo local que auto-autentica
/// un usuario simulado sin necesidad de Azure AD.
/// </summary>
internal sealed class DevAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    System.Text.Encodings.Web.UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    internal const string SchemeName = "DevAuth";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "00000000-0000-0000-0000-000000000001"),
            new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", "00000000-0000-0000-0000-000000000001"),
            new Claim(ClaimTypes.Name, "Dev User"),
            new Claim("name", "Dev User"),
            new Claim(ClaimTypes.Email, "dev@localhost"),
            new Claim("preferred_username", "dev@localhost"),
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
