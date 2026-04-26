## Why

EchoBase es actualmente una aplicación web interna (Blazor Server). Los empleados necesitan acceder al sistema de reservas de puestos desde sus dispositivos móviles (iOS y Android) y desde Windows, sin depender del navegador. Añadir una capa de presentación nativa con Microsoft MAUI requiere exponer la lógica de negocio existente a través de una API HTTP propia (`EchoBase.Api`), ya que el modelo de datos es compartido y en tiempo real: no puede haber consistencia sin un servidor centralizado.

## What Changes

- **Nuevo proyecto `EchoBase.Api`**: API REST con ASP.NET Core Minimal API (net10.0) que expone todos los comandos y queries existentes de `EchoBase.Core` como endpoints HTTP protegidos con Bearer tokens (Azure AD / MSAL). Reutiliza `EchoBase.Infrastructure` sin modificaciones.
- **Nuevo proyecto `EchoBase.Maui`**: Aplicación nativa multi-plataforma (Windows, Android tablet, Android móvil, iOS tablet, iOS móvil) que consume `EchoBase.Api` mediante HTTPS + MSAL. Implementa las pantallas principales del flujo de usuario: mapa de puestos, reservas propias, perfil y reporte de incidencias.
- **Autenticación unificada**: Se añaden los redirect URIs necesarios para MAUI al App Registration de Azure AD existente. Un único App Registration con scopes para la API.
- **Desarrollo local**: `EchoBase.Api` incluye un stub de autenticación Bearer para desarrollo sin Azure AD (equivalente al `DevAuthHandler` de Web).
- `EchoBase.Web`, `EchoBase.Core` y `EchoBase.Infrastructure` **no se modifican**.

## Capabilities

### New Capabilities

- `rest-api`: Capa HTTP REST (`EchoBase.Api`) que expone los comandos y queries de Core como endpoints Minimal API, con autenticación Bearer (Azure AD), autorización por roles, y documentación OpenAPI. Incluye `ApiCurrentUserService` y stub de dev auth.
- `maui-client`: Aplicación .NET MAUI multi-plataforma (Windows, Android, iOS) con autenticación MSAL, cliente HTTP tipado para `EchoBase.Api`, y pantallas para las funcionalidades de usuario estándar (mapa de puestos, reservas, perfil, incidencias).

### Modified Capabilities

<!-- Sin cambios en requisitos de capacidades existentes. La Web no se toca. -->

## Impact

- **Nuevos proyectos**: `src/EchoBase.Api/` y `src/EchoBase.Maui/` se añaden a la solución `EchoBase.slnx`.
- **`EchoBase.Infrastructure`**: Referenciado desde la API exactamente igual que desde Web. Sin cambios.
- **`EchoBase.Core`**: Sin cambios. Los handlers MediatR existentes son el backend de la API.
- **Azure AD**: El App Registration existente necesita nuevos redirect URIs para MAUI (Android, iOS, Windows) y exponer scopes de API.
- **Despliegue**: La API requiere su propio pipeline/servicio de hosting independiente de EchoBase.Web.
- **Dependencias nuevas**: `Microsoft.Identity.Client` (MSAL) en MAUI; `Microsoft.AspNetCore.Authentication.JwtBearer` en la API; `Microsoft.Identity.Web` para la validación de tokens en la API.
