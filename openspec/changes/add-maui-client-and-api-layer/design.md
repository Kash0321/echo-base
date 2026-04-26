## Context

EchoBase es un sistema de reservas de puestos de trabajo (hot-desking) con lógica de negocio en `EchoBase.Core` (Clean Architecture, MediatR, patrón Repository) y persistencia en `EchoBase.Infrastructure` (EF Core + SQLite/SQL Server). La capa de presentación actual es `EchoBase.Web` (Blazor Server con Azure AD via OIDC + cookies).

Este cambio añade dos capas nuevas:
1. `EchoBase.Api`: servidor HTTP REST que expone los handlers existentes de MediatR como endpoints, autenticado con Bearer tokens (Azure AD/MSAL).
2. `EchoBase.Maui`: cliente nativo multi-plataforma que consume la API.

`EchoBase.Core` e `EchoBase.Infrastructure` no se modifican.

## Goals / Non-Goals

**Goals:**
- Exponer todos los comandos y queries de Core como endpoints REST en `EchoBase.Api`.
- Autenticar las peticiones de la API con Bearer JWT (Azure AD), con un stub para desarrollo local.
- Implementar `EchoBase.Maui` para Windows, Android (tablet y móvil) e iOS (tablet y móvil) con las funcionalidades de usuario estándar: mapa de puestos, reservas propias, perfil y reporte de incidencias.
- Reutilizar `EchoBase.Infrastructure` sin modificaciones desde la API.
- Mantener `EchoBase.Web` completamente intacto.

**Non-Goals:**
- Funcionalidades de administración (SystemAdmin, gestión de zonas y roles) en MAUI — solo acceso a través de la Web.
- Modo offline en MAUI: la app requiere conexión activa.
- Versionado de la API (v1, v2): se añade si es necesario en el futuro.
- Compartir ViewModels o componentes UI entre Blazor y MAUI.
- Tests de integración para `EchoBase.Api` en este change (se abordan en un change de testing posterior).

## Decisions

### Decisión 1: Proyecto API separado (`EchoBase.Api`) en lugar de extender `EchoBase.Web`

**Elegida**: Proyecto separado.

**Razón**: `EchoBase.Web` usa OIDC con cookies como esquema de autenticación principal; la API necesita Bearer JWT. Mezclar ambos esquemas en un solo proyecto añade complejidad al `Program.cs`, al middleware de antiforgery de Blazor y a la configuración de autorización. Un proyecto independiente tiene su propio ciclo de vida, puede escalar por separado y permite mantener `EchoBase.Web` intacto.

**Alternativa descartada**: Añadir endpoints Minimal API a `EchoBase.Web`. Se descarta porque mezclaría dos paradigmas de autenticación distintos en un solo `Program.cs` y obligaría a modificar la configuración de `UseAntiforgery()` de Blazor para excluir las rutas API.

---

### Decisión 2: ASP.NET Core Minimal API (no Controllers)

**Elegida**: Minimal API con grupos de endpoints por dominio (`/api/reservations`, `/api/incidences`, etc.).

**Razón**: Coherente con el enfoque .NET 10 moderno. Los handlers ya están en Core; los endpoints son solo enrutadores delgados. No hay lógica de presentación compleja que justifique controllers.

**Alternativa descartada**: Controllers con `[ApiController]`. Más verboso, no añade valor dado el diseño actual.

---

### Decisión 3: Un único App Registration de Azure AD con scopes para la API

**Elegida**: El App Registration existente de EchoBase expone scopes (`api://echobase/access`) y MAUI solicita un token para ese scope mediante MSAL.

**Razón**: Un único registro simplifica la gestión de permisos en Azure AD. MAUI obtiene un Bearer token válido para la API con el mismo tenant.

**Implementación**:
- App Registration existente: añadir redirect URIs para MAUI (Android, iOS, Windows) y exponer el scope `access`.
- `EchoBase.Api/Program.cs`: validar tokens con `AddMicrosoftIdentityWebApi`.
- `EchoBase.Maui`: usar `PublicClientApplicationBuilder` de MSAL con el `ClientId` del App Registration.

---

### Decisión 4: `ApiCurrentUserService` implementa `ICurrentUserService` con `HttpContext`

**Elegida**: Nueva clase `ApiCurrentUserService` en `EchoBase.Api` que lee claims del `HttpContext.User` del JWT validado, y llama a `IUserRepository.EnsureUserAsync` igual que `BlazorCurrentUserService`.

**Razón**: El contrato `ICurrentUserService` en Core está diseñado para ser independiente del mecanismo de autenticación. Solo necesitamos una implementación nueva; cero cambios en Core o Infrastructure.

El claim `oid` (object identifier de Azure AD) es el `UserId`. En desarrollo local, el stub inyecta claims equivalentes.

---

### Decisión 5: Stub de autenticación Bearer para desarrollo local en la API

**Elegida**: `DevApiAuthHandler` — un `AuthenticationHandler<AuthenticationSchemeOptions>` que, cuando está activo (`Authentication:UseDevelopmentStub: true`), devuelve automáticamente un `ClaimsPrincipal` con los mismos claims que el `DevAuthHandler` de Web.

**Razón**: Permite ejecutar y depurar la API localmente sin Azure AD, siguiendo el patrón ya establecido en `EchoBase.Web`.

---

### Decisión 6: Funcionalidades de MAUI — solo flujo de usuario estándar

**Elegida**: MAUI implementa las pantallas: mapa de puestos (ver y reservar), mis reservas (ver y cancelar), perfil de usuario (ver y editar), y reporte de incidencias.

**Excluido de MAUI**: Administración de zonas/puestos, gestión de usuarios y roles, logs de auditoría, modo de mantenimiento. Estas funciones requieren pantallas complejas mejor adaptadas al navegador de escritorio.

---

### Decisión 7: Arquitectura de `EchoBase.Maui` — MVVM con cliente HTTP tipado

**Elegida**: Patrón MVVM con `CommunityToolkit.Mvvm` para ViewModels. Cliente HTTP mediante `HttpClient` + `IHttpClientFactory` con un `DelegatingHandler` que inyecta el Bearer token de MSAL en cada petición.

**Razón**: MVVM es el patrón estándar de MAUI. `CommunityToolkit.Mvvm` reduce boilerplate con source generators. El `DelegatingHandler` desacopla la autenticación del código de negocio del cliente.

## Risks / Trade-offs

- **Riesgo: Configuración de App Registration en Azure AD** → La adición de redirect URIs para Android (`msauth://com.empresa.echobase/callback`) e iOS requiere permisos en el tenant. Mitigación: documentar los URIs necesarios en el `README` de `EchoBase.Maui` para que el administrador de Azure AD los añada.

- **Riesgo: MAUI en iOS requiere Mac para compilar** → El pipeline de CI/CD necesita un agente macOS para builds de iOS. Mitigación: en la primera iteración, validar iOS manualmente desde un Mac de desarrollo; el pipeline se configura como tarea separada.

- **Riesgo: Gestión del token MSAL en background (refresco)** → MSAL gestiona el refresco automático del token, pero en MAUI hay que configurar el `KeychainAccessGroup` en iOS y el almacenamiento seguro en Android. Mitigación: usar `MsalCacheHelper` de la librería `Microsoft.Identity.Client.Extensions.Msal`.

- **Trade-off: API online-only** → No hay soporte offline. Si la red no está disponible, la app muestra un mensaje de error. Aceptado conscientemente: la consistencia de los datos compartidos es más importante que la disponibilidad offline.

- **Trade-off: Sin versionado de API en v1** → Si en el futuro `EchoBase.Web` consume también la API (en lugar de Infrastructure directamente), un cambio de contrato requeriría versionar. Se acepta la deuda: añadir `/api/v1/` prefix desde el inicio como convención, sin middleware de versionado por ahora.

## Migration Plan

1. **Sin migración de datos**: No hay cambios en el modelo de datos ni en la BD.
2. **Despliegue incremental**:
   - `EchoBase.Api` se despliega de forma independiente (Azure App Service o contenedor).
   - `EchoBase.Web` sigue funcionando sin interrupciones.
   - `EchoBase.Maui` se distribuye internamente (Intune / sideload enterprise) una vez la API está operativa.
3. **Rollback**: Retirar `EchoBase.Api` del despliegue no afecta a `EchoBase.Web` ni a los datos.

## Open Questions

- ¿El `ClientId` del App Registration de MAUI es el mismo que el de Web, o se crea uno nuevo? (Decisión 3 asume el mismo, pero el administrador de Azure AD debe confirmarlo.)
- ¿Qué plataforma de distribución se usará para MAUI en producción? (Intune para distribución enterprise, o App Store interno.) Afecta al package name y redirect URIs de MSAL.
- ¿Se requieren tests de integración para `EchoBase.Api` en este change, o se difieren?
