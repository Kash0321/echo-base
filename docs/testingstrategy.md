# 🧪 Estrategia de pruebas

## 🔬 Pruebas unitarias (`EchoBase.Tests.Unit`)

- **Framework:** xUnit + NSubstitute
- **Alcance:** Handlers MediatR de forma aislada; cada dependencia externa (repositorios, servicios de notificación) se sustituye por un doble de prueba con NSubstitute.
- **Cobertura actual:** Handlers de comandos/queries de Reservaciones, Usuarios y BlockedDocks; feature flags de Teams; orientación de zona y localizador de mesa en `GetDockMapHandler`.

## 🔗 Pruebas de integración (`EchoBase.Tests.Integration`)

### 🧰 Herramientas elegidas

| Capa | Decisión | Justificación |
|---|---|---|
| Framework de tests | xUnit (ya en uso) | Consistencia con el resto del proyecto; no se introduce nueva dependencia. |
| Base de datos | EF Core con **SQLite en memoria** (`Microsoft.Data.Sqlite`) | Misma librería ya referenciada en `EchoBase.Infrastructure`. Semántica SQL real (a diferencia del proveedor `InMemory` de EF Core, que no valida tipos ni restricciones). |
| Pipeline de negocio | **MediatR real** — sin mocks | Los tests ejercen el pipeline completo (validación, handler, notificaciones), de modo que cualquier regresión en el ensamblado de dependencias o en el flujo de un comando queda expuesta. |
| Servicios externos | Stubs no-operativos en `Infrastructure/Stubs/` | `NullEmailService` y `NullTeamsNotificationService` implementan las interfaces reales sin efecto secundario; permiten que los handlers de notificación se ejecuten sin SMTP ni Graph API. |
| Tiempo | `FrozenTimeProvider` | Subclase de `TimeProvider` congelada al inicio del día UTC. Hace deterministas las comprobaciones de "hoy" y "máximo 7 días vista". |

### 🔒 Patrón de aislamiento

Cada clase de tests hereda de `IntegrationTestBase : IAsyncLifetime`.

- En `InitializeAsync`: se abre una `SqliteConnection("Data Source=:memory:")` y se mantiene abierta durante toda la vida del objeto. EF Core recibe esa conexión directamente con `UseSqlite(connection)`, lo que garantiza que el esquema persiste entre operaciones (las bases de datos SQLite en memoria se destruyen en cuanto se cierran todas sus conexiones).
- Se construye un contenedor DI con los repositorios reales, MediatR, stubs y `FrozenTimeProvider`.
- `DbSeeder.SeedAsync` puebla zonas y puestos de la configuración real; a continuación se insertan los tres usuarios de prueba específicos de los tests.
- En `DisposeAsync`: se libera el `DbContext`, el proveedor de servicios y la conexión SQLite.

Cada instancia de clase de tests obtiene su propia base de datos en memoria, por lo que los tests son completamente independientes entre sí.