## 1. Proyecto EchoBase.Api — Estructura y configuración

- [x] 1.1 Crear el proyecto `src/EchoBase.Api/EchoBase.Api.csproj` (ASP.NET Core, net10.0) y añadirlo a `EchoBase.slnx`
- [x] 1.2 Añadir referencias a `EchoBase.Infrastructure` y los paquetes NuGet: `Microsoft.Identity.Web`, `Microsoft.AspNetCore.Authentication.JwtBearer`, `Swashbuckle.AspNetCore`
- [x] 1.3 Crear `appsettings.json` y `appsettings.Development.json` con las secciones: `AzureAd`, `ConnectionStrings`, `Database`, `Smtp`, `MicrosoftGraph`, `Notifications`, `Authentication` y `Features`
- [x] 1.4 Crear `Program.cs` con el pipeline básico: registro de servicios de Infrastructure, autenticación Bearer (Azure AD), autorización por roles, Swagger (solo en Development) y mapeo de grupos de endpoints

## 2. Autenticación y servicio de usuario en la API

- [x] 2.1 Crear `Services/ApiCurrentUserService.cs` que implemente `ICurrentUserService` leyendo claims del `HttpContext.User` (claim `oid` como `UserId`, `name` como `UserName`, `preferred_username` como `Email`) y llame a `IUserRepository.EnsureUserAsync`
- [x] 2.2 Crear `Services/DevApiAuthHandler.cs`, un `AuthenticationHandler` que, cuando `Authentication:UseDevelopmentStub` es `true`, auto-autentica un usuario simulado con roles configurables (equivalente al `DevAuthHandler` de Web)
- [x] 2.3 Registrar en `Program.cs` la lógica de selección de esquema de autenticación: Bearer JWT (producción) vs. `DevApiAuthHandler` (desarrollo)
- [x] 2.4 Registrar `ApiCurrentUserService` como `ICurrentUserService` con scope `Scoped` y como `IHttpContextAccessor`-aware

## 3. Endpoints REST — Reservas

- [x] 3.1 Crear `Endpoints/ReservationsEndpoints.cs` con grupo `/api/v1/reservations`
- [x] 3.2 Implementar `GET /api/v1/docks/map` → `GetDockMapQuery`, requiere usuario autenticado
- [x] 3.3 Implementar `GET /api/v1/reservations` → `GetUserReservationsQuery` con `UserId` del usuario en sesión
- [x] 3.4 Implementar `POST /api/v1/reservations` → `CreateReservationCommand`; responde 201 con `id` o 422 con el error de dominio
- [x] 3.5 Implementar `DELETE /api/v1/reservations/{id}` → `CancelReservationCommand`; responde 204 o 403/422 según el error de dominio
- [x] 3.6 Añadir helper `ResultToHttpResult` que mapea `Result`/`Result<T>` a `IResult` HTTP (200/201/204/403/422)

## 4. Endpoints REST — Incidencias

- [x] 4.1 Crear `Endpoints/IncidencesEndpoints.cs` con grupo `/api/v1/incidences`
- [x] 4.2 Implementar `POST /api/v1/incidences` → `ReportIncidenceCommand`; responde 201 con `id`
- [x] 4.3 Implementar `GET /api/v1/incidences/mine` → `GetUserIncidencesQuery` con `UserId` del usuario en sesión

## 5. Endpoints REST — Perfil de usuario

- [x] 5.1 Crear `Endpoints/UsersEndpoints.cs` con grupo `/api/v1/users`
- [x] 5.2 Implementar `GET /api/v1/users/me` → `GetUserProfileQuery`
- [x] 5.3 Implementar `PUT /api/v1/users/me` → `UpdateUserProfileCommand`; responde 204

## 6. Endpoints REST — Operaciones de Manager

- [x] 6.1 Crear `Endpoints/BlockedDocksEndpoints.cs` con grupo `/api/v1/blocked-docks`, restringido al rol `Manager`
- [x] 6.2 Implementar `POST /api/v1/blocked-docks` → `BlockDocksCommand`
- [x] 6.3 Implementar `DELETE /api/v1/blocked-docks` → `UnblockDocksCommand`

## 7. Verificación y smoke test de la API

- [x] 7.1 Verificar que la solución compila con `dotnet build` sin errores
- [x] 7.2 Ejecutar la API en local con el stub de desarrollo y comprobar que Swagger UI carga en `/swagger`
- [x] 7.3 Probar manualmente los endpoints principales desde Swagger UI con el usuario de desarrollo

## 8. Proyecto EchoBase.Maui — Estructura y configuración

- [ ] 8.1 Crear el proyecto `src/EchoBase.Maui/EchoBase.Maui.csproj` (net10.0-windows/android/ios) y añadirlo a `EchoBase.slnx`
- [ ] 8.2 Añadir paquetes NuGet: `Microsoft.Identity.Client`, `Microsoft.Identity.Client.Extensions.Msal`, `CommunityToolkit.Mvvm`, `CommunityToolkit.Maui`
- [ ] 8.3 Configurar `MauiProgram.cs` con el pipeline básico: `UseMauiApp`, registro de ViewModels, `HttpClient` tipado con `IHttpClientFactory` y el `AuthTokenHandler`
- [ ] 8.4 Crear `appsettings.json` de MAUI con `AzureAd:ClientId`, `AzureAd:TenantId`, `AzureAd:Scopes` y la URL base de la API
- [ ] 8.5 Configurar los targets de plataforma en el `.csproj`: `net10.0-windows10.0.19041.0`, `net10.0-android`, `net10.0-ios`

## 9. Autenticación MSAL en MAUI

- [ ] 9.1 Crear `Services/MsalAuthService.cs` que encapsule `PublicClientApplicationBuilder`, `AcquireTokenSilentAsync` y `AcquireTokenInteractiveAsync`
- [ ] 9.2 Implementar `Services/AuthTokenHandler.cs` (DelegatingHandler) que inyecta el Bearer token de MSAL en la cabecera `Authorization` de cada petición HTTP
- [ ] 9.3 Configurar `MsalCacheHelper` para persistir el token de forma segura en Android (Keystore), iOS (Keychain) y Windows (DPAPI)
- [ ] 9.4 Documentar en `README.md` del proyecto los redirect URIs necesarios para el App Registration de Azure AD (Android, iOS, Windows)

## 10. Cliente HTTP tipado para la API

- [ ] 10.1 Crear `Services/EchoBaseApiClient.cs` con métodos tipados para cada endpoint: `GetDockMapAsync`, `GetMyReservationsAsync`, `CreateReservationAsync`, `CancelReservationAsync`, `GetMyProfileAsync`, `UpdateProfileAsync`, `ReportIncidenceAsync`, `GetMyIncidencesAsync`
- [ ] 10.2 Definir los DTOs de request/response en `Models/` (pueden reutilizar los mismos records de Core o ser DTOs de transferencia propios de MAUI)
- [ ] 10.3 Implementar manejo de errores HTTP: deserializar errores 422, capturar `HttpRequestException` para errores de red y propagar mensajes legibles a los ViewModels

## 11. ViewModels y navegación en MAUI

- [ ] 11.1 Crear `ViewModels/DockMapViewModel.cs` con propiedades observables para las zonas, la fecha seleccionada y el estado de carga; comandos `LoadMapCommand` y `ReserveCommand`
- [ ] 11.2 Crear `ViewModels/MyReservationsViewModel.cs` con lista observable de reservas y comando `CancelReservationCommand`
- [ ] 11.3 Crear `ViewModels/UserProfileViewModel.cs` con propiedades para los campos del perfil y comando `SaveProfileCommand`
- [ ] 11.4 Crear `ViewModels/IncidencesViewModel.cs` con lista observable de incidencias y comando `ReportIncidenceCommand`
- [ ] 11.5 Configurar la navegación Shell en `AppShell.xaml` con las cuatro secciones principales: Mapa, Reservas, Perfil e Incidencias

## 12. Páginas (Views) en MAUI

- [ ] 12.1 Crear `Pages/DockMapPage.xaml` y `.xaml.cs` con la vista del mapa de puestos, selector de fecha y lista de zonas con sus mesas y puestos
- [ ] 12.2 Crear `Pages/MyReservationsPage.xaml` y `.xaml.cs` con la lista de reservas y opción de cancelación
- [ ] 12.3 Crear `Pages/UserProfilePage.xaml` y `.xaml.cs` con los campos de perfil (lectura y edición)
- [ ] 12.4 Crear `Pages/IncidencesPage.xaml` y `.xaml.cs` con el formulario de reporte y la lista de incidencias propias
- [ ] 12.5 Crear `Pages/LoginPage.xaml` y `.xaml.cs` con el botón de inicio de sesión con Azure AD

## 13. Manejo de errores de red y UX en MAUI

- [ ] 13.1 Implementar un `ConnectivityService` que detecte ausencia de red y notifique a los ViewModels
- [ ] 13.2 Añadir mensajes de error accesibles en las páginas cuando la API no está disponible (sin conexión o error 5xx)
- [ ] 13.3 Implementar indicadores de carga (ActivityIndicator) en todas las páginas durante las llamadas a la API

## 14. Verificación multi-plataforma de MAUI

- [ ] 14.1 Compilar y ejecutar la app en Windows (local) y verificar todas las pantallas
- [ ] 14.2 Compilar y ejecutar la app en el emulador de Android y verificar todas las pantallas
- [ ] 14.3 Compilar y ejecutar la app en el simulador de iOS (requiere Mac) y verificar todas las pantallas
