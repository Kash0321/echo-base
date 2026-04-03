# 🚀 Spec: Sistema de Reserva de Puestos (Hot Desking)

## 📑 Tabla de contenido

- [🎯 Objetivo](#-objetivo)
- [🛠️ Stack técnico](#️-stack-técnico)
- [📐 Convenciones de implementación](#-convenciones-de-implementación)
- [🎨 Diseño y UX (UI Guidelines)](#-diseño-y-ux-ui-guidelines)
  - [🖼️ Galería de Pantallas](#️-galería-de-pantallas-uiux)
- [🗃️ Modelo de datos](#️-modelo-de-datos)
- [🔐 Modelo de autenticación y autorización](#-modelo-de-autenticación-y-autorización)
- [📋 Reglas de negocio generales](#-reglas-de-negocio-generales)
- [👥 Funcionalidad 0: Gestión cuentas de usuario](#-funcionalidad-0-gestión-cuentas-de-usuario-parcialmente-implementada-pendiente-de-configuración-de-acceso-al-tennant-de-azure-ad)
- [⚙️ Funcionalidad 1: Configuración de cuenta de usuario](#️-funcionalidad-1-configuración-de-cuenta-de-usuario-implementada)
- [📌 Funcionalidad 2: Reserva de puesto de trabajo](#-funcionalidad-2-reserva-de-puesto-de-trabajo-implementada)
- [🛡️ Funcionalidad 3: Cuadro de mando para administración](#️-funcionalidad-3-cuadro-de-mando-para-administración-implementada)
- [🗓️ Funcionalidad 4: Gestión de reservas](#️-funcionalidad-4-gestión-de-reservas-implementada)
- [🏗️ Funcionalidad 5: Configuración de Zonas, Mesas y Puestos de trabajo](#️-funcionalidad-5-configuración-del-zonas-mesas-y-puestos-de-trabajo-implementada)
- [📊 Funcionalidad 6: Reportes y estadísticas](#-funcionalidad-6-reportes-y-estadísticas-pendiente-de-implementación)
- [🔧 Funcionalidad 7: Reporte de incidencias](#-funcionalidad-7-reporte-de-incidencias-en-los-puestos-de-trabajo-pendiente-de-implementación)
- [🚩 Funcionalidad 8: Feature Flags de sistema](#-funcionalidad-8-feature-flags-de-sistema-implementada)
  - [🔔 Flag: TeamsNotificationsEnabled](#-flag-featuresteamsnotificationsenabled)
- [🛠️ Funcionalidad 9: Funciones de administración del sistema](#️-funcionalidad-9-funciones-de-administración-del-sistema-implementada)
- [🧪 Estrategia de pruebas](#-estrategia-de-pruebas)
  - [🔬 Pruebas unitarias](#-pruebas-unitarias-echobasetestsunit)
  - [🔗 Pruebas de integración](#-pruebas-de-integración-echobasetestsintegration)

---

## 🎯 Objetivo
App interna para que múltiples empleados reserven sobre un conjunto limitado de puestos físicos, permitiendo cada puesto dos franjas horarias, mañana o tarde.

## 🛠️ Stack técnico
- Framework: .NET 10
- Frontend: Blazor Web App (Interactive Server)
- Estilo: Bootstrap 5 + Bootstrap Icons + CSS scoped por componente
- Persistencia: Entity Framework Core con SQLite (Local) / Azure SQL (Prod)

## 📐 Convenciones de implementación
*   Usa Clean Code y sigue las convenciones de estilo de C#.
*   Aplica principios SOLID y patrones de diseño cuando sea apropiado.
*   Documenta con XML comments y mantén el código legible.
*   Escribe tests para cada nueva funcionalidad o cambio significativo.
*   Utiliza GitHub Copilot Pro para generar código boilerplate, pero siempre revisa y ajusta según el contexto específico del proyecto.
*   Mantén los archivos `spec.md` actualizados para que la IA tenga el contexto necesario para generar código relevante.
*   Clean Architecture: Mantén una separación clara entre las capas de dominio, infraestructura y presentación para facilitar la mantenibilidad y escalabilidad del proyecto.
*   Usa Microsoft.Identity.Web para Blazor Server y Blazor WebAssembly para implementar autenticación y autorización con Azure AD, asegurando que solo los usuarios autorizados puedan acceder a la aplicación y sus funcionalidades.
*   Usa el patrón Mediator para manejar la lógica de negocio y las interacciones entre componentes, promoviendo un código más limpio y desacoplado. Esto facilitará la gestión de comandos y consultas, así como la implementación de nuevas funcionalidades sin afectar otras partes del sistema. Usa la librería MediatR para facilitar la implementación de este patrón en toda la aplicación.
*   Para la capa de acceso a datos, implementa el patrón Repository para abstraer la lógica de acceso a la base de datos y facilitar la gestión de entidades. Esto permitirá una mayor flexibilidad y mantenibilidad del código, ya que las operaciones de acceso a datos estarán centralizadas y desacopladas del resto de la aplicación.
*   **Identificadores de entidad**: Todos los identificadores de entidades de dominio son `Guid` generados como **UUID v7** mediante `Guid.CreateVersion7()` (disponible de forma nativa desde .NET 9). Los UUID v7 incorporan un prefijo de marca de tiempo de 48 bits que los hace ordenables cronológicamente, reduciendo la fragmentación de índices B-tree. La generación se realiza en la capa de aplicación (handlers y behavior de auditoría); EF Core está configurado con `UuidV7ValueGenerator` en las configuraciones Fluent API de cada entidad como mecanismo de fallback. Las entidades de datos maestros con GUIDs determinísticos (`DbSeeder`) quedan excluidas de este mecanismo. `SystemSetting` también queda excluida por tener PK de tipo `string`.

## 🎨 Diseño y UX (UI Guidelines)
Para mantener coherencia en el rediseño y ampliaciones futuras de Echo Base, se establecieron las siguientes bases de UX/UI en el desarrollo interactivo de `Home.razor`, `DockMap.razor`, `MyReservations.razor`, `About.razor` y `UserProfile.razor`:

1. **Cabeceras de página (`eb-page-header`, `eb-hero`)**
   - Dominadas por un gradiente espacial/táctico oscuro: `linear-gradient(135deg, #0d1b2a 0%, #1a3a5c 60%, #0d1b2a 100%)`.
   - Textos en blanco para máximo contraste, integrando subtítulos ligeros tipo "eyebrow" (letras en mayúscula, pequeñas, con tracking/letter-spacing aumentado).
   - En vistas operativas (DockMap), la cabecera acomoda controles compactos (selector de fecha transparente y leyenda simplificada con `span` circulares) para maximizar el espacio vertical útil (above the fold).
   - **CSS Isolation (crítico):** Blazor aplica *scoped CSS*: los estilos definidos en `ComponenteX.razor.css` solo aplican al marcado de ese componente (se compilan con un atributo `b-xxxx` único). Por tanto, **cada nueva página que use `eb-page-header`, `eb-page-eyebrow` u otras clases compartidas debe tener su propio fichero `.razor.css`** con esas definiciones. No basta con que la clase exista en otro componente.

2. **Densidad y Espaciado (Desktop/Mobile)**
   - Elementos *compactos* (`py-2`, `mb-2`, `p-3`) en lugar del espaciado excesivo de Bootstrap por defecto. El objetivo de la interfaz es evitar el scroll vertical o minimizarlo, para que los usuarios vean el contexto visual (ej. el mapa de bahías interactivo) sin tener que desplazarse demasiado.
   - Botones sutiles e inputs con clases como `form-control-sm` o `btn-sm` para interacciones recurrentes.

3. **Uso de Iconografía (Bootstrap Icons)**
   - Iconos integrados sistemáticamente en los títulos de secciones/vistas y en acciones principales (`bi-*` junto a textos como reservas confirmadas, botones de cancelación con *spinners* o símbolos).
   - Modalidades de alerta: uso de alertas (`.alert`) acompañadas de íconos in-line y flexbox (`d-flex align-items-center gap-2`) para feedback de éxito o advertencia en vez de simples bloques de texto monótonos.

4. **Componentización Visual**
   - **Tarjetas sin borde y elevación sutil**: Uso de `.card.border-0.shadow-sm`.
   - **Hover effects táctiles**: Aplicados en tarjetas informativas (reservas futuras) y los puestos del mapa (`.eb-reservation-card:hover`, `.eb-dock-seat:not(:disabled):hover`) usando `transform: translateY(-2px); box-shadow: ...`.
   - **Distribución de mesas por zona (`DockZone.Orientation`)**: Cada zona puede renderizar sus mesas en sentido horizontal (`d-flex flex-row flex-wrap gap-3`, valor por defecto) o vertical (`d-flex flex-column gap-3`), controlado por la propiedad `ZoneOrientation` almacenada en BD. Esta lógica se aplica en `DockMap.razor` y `AdminDashboard.razor` sin CSS adicional, usando únicamente clases Flexbox de Bootstrap 5.
   - **Localizador de mesa (`DockTable.Locator`)**: El encabezado que aparece sobre cada bloque de mesa en el mapa muestra `Locator` si está definido, o el nombre inferido (ej. «Mesa 1») como fallback. `DockTable` es una entidad independiente que asocia un `TableKey` (clave lógica derivada del prefijo del código de puesto) con su texto informativo. Los botones de puesto individual siempre muestran `Code`.
   - **Interacciones enriquecidas (Formularios/Modales)**: Uso moderno de entradas, por ejemplo, convirtiendo opciones en botones (`.btn-check` + `.btn-outline-primary`) en lugar de radio buttons clásicos para mejorar las áreas táctiles en móviles y darle un look moderno.
   - **Datos históricos**: Manejo diferenciado visualmente. Los elementos cancelados o pasados bajan su opacidad y tienen efecto tachado sobre los listados.
   - **Formularios de perfil**: Separación clara entre datos corporativos de solo lectura (gestionados por Azure AD) y datos editables del usuario (línea de negocio, teléfono y preferencias), usando tarjetas diferenciadas, campos compactos y switches visuales para preferencias booleanas.
   - **Controles de edición y creación inline (patrón establecido en `SystemAdminDashboard`)**: Tanto el modo de edición de un registro existente como el formulario de creación de uno nuevo siguen el mismo patrón visual, aplicado en la pestaña "Zonas y puestos" para Zonas, Mesas y Puestos de trabajo:
     - Todos los campos se agrupan en **una única línea flex fluida** (`d-flex flex-wrap gap-2 align-items-center`) que ocupa todo el ancho disponible, sin columnas de cuadrícula ni etiquetas encima.
     - Cada campo de texto usa `input-group input-group-sm` con un botón `<button type="button">` al final que muestra `<i class="bi bi-x-lg" style="font-size:.7rem;">` y borra el contenido del campo al hacer clic (`title="Limpiar"`). En formularios de creación con muchos campos, el label del campo se integra como `<span class="input-group-text">` en lugar de una etiqueta flotante.
     - Cada campo se dimensiona con `flex` inline (ej. `style="flex:1 1 180px; min-width:160px;"`) para ajustarse fluidamente sin anchos máximos fijos que fragmenten la UI en resoluciones pequeñas.
     - Los botones de acción (Guardar/Cancelar o Añadir/Cancelar) se agrupan **al final** en `<div class="d-flex gap-1 flex-shrink-0">`, garantizando que no se separen de los campos.
     - En modo de **edición de fila de tabla**, se usa `<td colspan="N" class="py-1">` para que el contenedor flex ocupe todas las columnas sin celdas vacías.
     - En modo de **edición de `card-header`**, los campos siguen la misma lógica dentro de un `<div class="d-flex flex-wrap gap-2 align-items-center flex-grow-1">` con los botones de acción en un `<div class="d-flex gap-1 flex-shrink-0">` al extremo derecho del header.

### 🎨 Paleta de Colores (Design Tokens)

La paleta de colores oficial del proyecto está definida en `Colors.pdf` (ver `docs/Colors.pdf`) y se aplica mediante variables CSS personalizadas declaradas en `wwwroot/app.css`. Esto centraliza todos los tokens de color y permite mantener coherencia en cualquier ampliación futura.

#### Colores Primarios

| Token CSS | Hex | Uso principal |
|---|---|---|
| `--eb-dark-blue` | `#17233C` | Base oscura de cabeceras, gradiente inicio |
| `--eb-dark-petroleum` | `#0A404B` | Acento medio del gradiente de cabeceras |
| `--eb-stone-green` | `#4F5E5A` | Color neutro secundario / microdecoración |
| `--eb-graphite-black` | `#262626` | Texto oscuro / fondos muy oscuros |

#### Sistema de Colores Funcionales

| Familia | 400 (fuerte) | 300 | 200 | 100 (suave) |
|---|---|---|---|---|
| **Azul** | `#086cd9` | `#1d88fe` | `#8fc3ff` | `#eaf4ff` |
| **Verde** | `#11845b` | `#05c168` | `#7fdca4` | `#def2e6` |
| **Rojo** | `#dc2b2b` | `#ff5a65` | `#ffbec2` | `#ffeff0` |
| **Amarillo** | `#FFA800` | `#FDBD1A` | `#FFE39B` | `#FFF6E4` |

#### Colores Alternativos y Secundarios

| Rol | Alt (saturado) | Secondary (desaturado) |
|---|---|---|
| Color 1 – Turquesa | `#BEE2E4` | `#8ACCC3` |
| Color 2 – Lima | `#D1DA50` | `#B4B736` |
| Color 3 – Dorado | `#FED525` | `#D8B00E` |
| Color 4 – Naranja | `#F18820` | `#D36F15` |
| Color 5 – Coral/Rojo | `#EB595B` | `#C13E43` |
| Color 6 – Púrpura | `#9464A7` | `#6E4583` |
| Color 7 – Azul violáceo | `#706DB0` | `#535497` |

#### Aplicación de la Paleta en los ficheros CSS

| Fichero | Cambios realizados |
|---|---|
| `wwwroot/app.css` | Definición completa de variables `--eb-*`; sobrescritura de Bootstrap 5 (`--bs-primary`, `--bs-success`, `--bs-danger`, `--bs-warning`); colores de botones, enlaces, validación y focus ring |
| `MainLayout.razor.css` | Gradiente del sidebar: `#17233C → #0A404B` (Dark Blue → Dark Petroleum) |
| `Home.razor.css` | Gradiente hero + tinte de lore cards con color primario `#17233C` |
| `DockMap.razor.css` | Cabecera de página, header de modal, zone header |
| `MyReservations.razor.css` | Cabecera de página, fondo de encabezados de tabla |
| `About.razor.css` | Hero de "Acerca de", brillo radial del hero, lore cards, lore pillars, avatar AI |
| `UserProfile.razor.css` | Cabecera de página, estado activo de tarjetas de notificación |
| `SystemAdminDashboard.razor.css` | Cabecera de página, zone header, header de modal de emergencia |
| `AdminDashboard.razor.css` | Cabecera de página |
| `ReconnectModal.razor.css` | Botón de reconexión (Blue 200/Blue 400) |

#### Mapeo semántico de colores en controles Bootstrap 5

Los controles de Bootstrap 5 (botones, alertas, badges, formularios, tabs, paginación…) se sobreescriben a nivel de componente en `wwwroot/app.css`, usando en primera instancia los **Colores Primarios** del PDF para roles estructurales y los **Alternativos / Secundarios** (tonos desaturados) donde se busca que el elemento no sobresalga demasiado:

| Rol Bootstrap | Color de marca | Hex | Origen en PDF |
|---|---|---|---|
| `primary` | Blue 400 | `#086cd9` | System Blues |
| `secondary` | Stone Green | `#4F5E5A` | Primary Colors |
| `success` | Green 400 | `#11845b` | System Greens |
| `danger` | Red 400 | `#dc2b2b` | System Reds |
| `warning` | Yellow 400 | `#FFA800` | System Yellows |
| `info` | Dark Petroleum | `#0A404B` | Primary Colors |
| `dark` | Dark Blue | `#17233C` | Primary Colors |
| Body text | Graphite Black | `#262626` | Primary Colors |
| Borders | Neutro derivado | `#d4d8da` | — |

Los **backdrops de alertas** usan los tonos 100 (fondos) y 200 (bordes) de cada familia de sistema, resultando en paletas de alerta completamente calmas y on-brand. Para la alerta `info` se usa el Alt-1 turquesa desaturado (`#8ACCC3` — Sec-1) como borde.

Los **botones de warning** usan texto `#262626` (Graphite Black) en lugar de blanco para garantizar contraste mínimo WCAG AA sobre el fondo amarillo.

**Convenciones de sobreescritura** (Bootstrap 5):
- Se usan las *CSS custom properties de componente* de Bootstrap 5 (`--bs-btn-bg`, `--bs-btn-hover-bg`, etc.) para controlar con precisión cada estado (normal, hover, active, disabled) sin afectar a otros componentes.
- Los overrides de `:root` (`--bs-primary`, `--bs-secondary`…) sirven de fallback global y alimentan automáticamente clases utilitarias como `.text-primary`, `.border-success`, `.bg-danger`, etc.
- Los `.form-check-input:checked` y `.form-switch` activos usan Blue 400 coherentemente con el color de acción principal.

#### Convenciones de uso

- **Gradiente de cabeceras** unificado via `var(--eb-header-gradient)`: `linear-gradient(135deg, #17233C 0%, #0A404B 60%, #17233C 100%)`.
- **Overrides de Bootstrap 5** definidos en `:root` de `app.css` para que las clases utilitarias de Bootstrap (`.btn-primary`, `.text-success`, `.alert-danger`, etc.) reflejen la paleta de marca automáticamente.
- Los **tintes de fondo** para sub-cabeceras y elementos destacados usan `rgba(23, 35, 60, .03/.04/.06)` (derivado de `--eb-dark-blue`).
- Los **colores de estado** (validación de formularios, bordes activos) usan Green 400 y Red 400 respectivamente.

### 🖼️ Galería de Pantallas (UI/UX)

A continuación se muestran capturas de las pantallas principales del sistema, que reflejan la aplicación de los principios de diseño mencionados:

#### 🏠 1. Home (`Home.razor`)
![Home](001.Home.png)

#### 🗺️ 2. Mapa de Bahías (`DockMap.razor`)
![Mapa de bahías](002.DockMap.png)

#### 📅 3. Mis Reservas (`MyReservations.razor`)
![Mis Reservas](003.MyReservations.png)

#### ℹ️ 4. Acerca de (`About.razor`)
![Acerca de - Parte 1](004.About_01.png)
![Acerca de - Parte 2](004.About_02.png)

#### 👤 5. Perfil de Usuario (`UserProfile.razor`)
![Perfil de Usuario](005.UserProfile.png)

#### 🛡️ 6. Administración del Sistema (`SystemAdminDashboard.razor`)

##### 🔧 Modo Mantenimiento
![Administración - Modo Mantenimiento](006.SystemAdmin01_MaintenanceMode.png)

##### 🗑️ Cancelación Masiva
![Administración - Cancelación Masiva](006.SystemAdmin02_BulkCancel.png)

##### ⚡ Reserva de Emergencia
![Administración - Reserva de Emergencia](006.SystemAdmin03_EmergencyReservation.png)

##### 👥 Gestión de Usuarios
![Administración - Gestión de Usuarios](006.SystemAdmin04_UsersManagement.png)

##### 📋 Log de Auditoría
![Administración - Log de Auditoría](006.SystemAdmin05_LOG.png)

## 🗃️ Modelo de datos

### Entidades de negocio

#### `User` — Empleado
Representa a un empleado autenticado mediante Azure AD.

| Propiedad | Tipo | Descripción |
|---|---|---|
| `Id` | `Guid` (UUID v7) | Identificador único |
| `Name` | `string` | Nombre completo (sincronizado con Azure AD, solo lectura) |
| `Email` | `string` | Correo corporativo (sincronizado con Azure AD, solo lectura) |
| `BusinessLine` | `BusinessLine` (enum) | Línea de negocio: `Core`, `Energia`, `ScrapWaste`, `Transversal` |
| `PhoneNumber` | `string?` | Teléfono de contacto opcional |
| `EmailNotifications` | `bool` | Preferencia de notificación por correo (por defecto `true`) |
| `TeamsNotifications` | `bool` | Preferencia de notificación por Teams (por defecto `false`) |
| `Reservations` | nav. → `Reservation` | Reservas del usuario |
| `Roles` | nav. → `Role` | Roles de autorización asignados (relación muchos-a-muchos) |

> **Nota:** Las preferencias de notificación se persisten como columnas de `User` (no existe una tabla `UserPreferences` separada).

---

#### `Dock` — Puesto de trabajo
Puesto de trabajo físico reservable.

| Propiedad | Tipo | Descripción |
|---|---|---|
| `Id` | `Guid` (UUID v7) | Identificador único |
| `Code` | `string` | Código alfanumérico (ej.: `N-A01`) |
| `Location` | `string` | Descripción de la ubicación física |
| `Equipment` | `string` | Equipamiento disponible (texto libre) |
| `DockTableId` | `Guid` | FK de la mesa física a la que pertenece el puesto |
| `DockTable` | nav. → `DockTable` | Mesa física a la que pertenece el puesto |
| `Side` | `DockSide` (enum) | Lado de la mesa: `A` (0) o `B` (1) |
| `Reservations` | nav. → `Reservation` | Reservas realizadas sobre este puesto |

> **Nota:** La zona a la que pertenece un puesto se accede a través de la navegación `Dock.DockTable.DockZone`; no existe un `DockZoneId` directo en `Dock`.

---

#### `DockZone` — Zona de trabajo
Agrupa un conjunto de mesas físicas bajo una misma zona de la oficina.

| Propiedad | Tipo | Descripción |
|---|---|---|
| `Id` | `Guid` | Identificador único |
| `Name` | `string` | Nombre de la zona (`Nostromo`, `Derelict`) |
| `Description` | `string?` | Descripción opcional de la zona |
| `Orientation` | `ZoneOrientation` (enum) | Distribución visual de las mesas: `Horizontal` (en fila, por defecto) o `Vertical` (apiladas en columna) |
| `Order` | `int` | Posición de la zona en el mapa. Menor valor = aparece antes. Por defecto `0`. |
| `Tables` | nav. → `DockTable` | Mesas físicas que componen la zona |

> **Nota:** La jerarquía es `DockZone (1) → (M) DockTable (1) → (M) Dock`. Los puestos se acceden siempre a través de las mesas de la zona.

---

#### `DockTable` — Mesa física
Representa una mesa física dentro de una zona, agrupa sus puestos de trabajo en dos lados (A y B).

| Propiedad | Tipo | Descripción |
|---|---|---|
| `Id` | `Guid` (UUID v7) | Identificador único |
| `TableKey` | `string` | Clave de la mesa (ej.: `"N"`, `"D-1"`, `"D-2"`), máx. 20 caracteres. Única por zona. Editable por SystemAdmin. |
| `Locator` | `string?` | Texto de localización opcional que aparece sobre el bloque de mesa en el mapa. Si es `null`, se muestra el nombre generado automáticamente (ej.: «Mesa 1»). Máx. 100 caracteres. |
| `Order` | `int` | Posición de la mesa dentro de la zona. Menor valor = aparece antes. Por defecto `0`. |
| `DockZoneId` | `Guid` | FK de la zona a la que pertenece la mesa |
| `DockZone` | nav. → `DockZone` | Zona propietaria |
| `Docks` | nav. → `Dock` | Puestos de trabajo que pertenecen a esta mesa |

Semillas de BD (`DbSeeder`):

| Mesa | `TableKey` | Id semilla |
|---|---|---|
| Nostromo | `"N"` | `e0000000-0000-0000-0000-000000000001` |
| Derelict 1 | `"D-1"` | `e0000000-0000-0000-0000-000000000002` |
| Derelict 2 | `"D-2"` | `e0000000-0000-0000-0000-000000000003` |
| Derelict 3 | `"D-3"` | `e0000000-0000-0000-0000-000000000004` |

---

#### `Reservation` — Reserva de puesto
Reserva de un puesto por un usuario en una fecha y franja horaria concretas.

| Propiedad | Tipo | Descripción |
|---|---|---|
| `Id` | `Guid` (UUID v7) | Identificador único |
| `UserId` | `Guid` | FK del usuario que realizó la reserva |
| `DockId` | `Guid` | FK del puesto de trabajo reservado |
| `Date` | `DateOnly` | Fecha de la reserva (sin componente horario) |
| `TimeSlot` | `TimeSlot` (enum) | Franja horaria: `Morning`, `Afternoon`, `Both` |
| `Status` | `ReservationStatus` (enum) | Estado: `Active`, `Cancelled` |
| `User` | nav. → `User` | Propietario de la reserva |
| `Dock` | nav. → `Dock` | Puesto reservado |

---

#### `BlockedDock` — Bloqueo de puesto
Bloqueo administrativo de un puesto para un período de fechas.

| Propiedad | Tipo | Descripción |
|---|---|---|
| `Id` | `Guid` (UUID v7) | Identificador único |
| `DockId` | `Guid` | FK del puesto bloqueado |
| `BlockedByUserId` | `Guid` | FK del Manager que creó el bloqueo |
| `StartDate` | `DateOnly` | Fecha de inicio del bloqueo (inclusiva) |
| `EndDate` | `DateOnly` | Fecha de fin del bloqueo (inclusiva) |
| `Reason` | `string` | Motivo del bloqueo |
| `IsActive` | `bool` | `true` mientras el bloqueo esté vigente; `false` si fue desactivado |
| `Dock` | nav. → `Dock` | Puesto bloqueado |
| `BlockedByUser` | nav. → `User` | Manager que realizó el bloqueo |

---

#### `Role` — Rol de autorización

| Propiedad | Tipo | Descripción |
|---|---|---|
| `Id` | `Guid` | Identificador único |
| `Name` | `string` | Nombre del rol: `BasicUser`, `Manager`, `SystemAdmin` |
| `Users` | nav. → `User` | Usuarios con este rol (relación muchos-a-muchos) |

> **Nota:** La relación `User` ↔ `Role` es muchos-a-muchos gestionada por EF Core; no existe una entidad `UserRole` separada.

Semillas de BD (`DbSeeder`):

| Nombre | Id semilla |
|---|---|
| `BasicUser` | `d0000000-0000-0000-0000-000000000001` |
| `Manager` | `d0000000-0000-0000-0000-000000000002` |
| `SystemAdmin` | `d0000000-0000-0000-0000-000000000003` |

---

#### `AuditLog` — Registro de auditoría
Entrada inmutable creada automáticamente por `AuditLoggingBehavior` para cada comando exitoso que implemente `IAuditableRequest`.

| Propiedad | Tipo | Descripción |
|---|---|---|
| `Id` | `Guid` (UUID v7) | Identificador único |
| `PerformedByUserId` | `Guid?` | FK del usuario que realizó la acción (`null` para acciones de sistema) |
| `Action` | `AuditAction` (enum) | Tipo de acción auditada |
| `Details` | `string` | Descripción legible (puesto, fecha, franja, etc.) |
| `Timestamp` | `DateTimeOffset` | Momento UTC de la acción |

Valores del enum `AuditAction`:

| Valor | Descripción |
|---|---|
| `ReservationCreated` | Reserva creada |
| `ReservationCancelled` | Reserva cancelada |
| `DockBlocked` | Puesto(s) bloqueado(s) |
| `DockUnblocked` | Puesto(s) desbloqueado(s) |
| `BulkReservationsCancelled` | Cancelación masiva ejecutada |
| `MaintenanceModeChanged` | Modo mantenimiento activado/desactivado |
| `EmergencyReservationCreated` | Reserva de emergencia creada |
| `UserRoleAssigned` | Rol asignado a un usuario |
| `UserRoleRemoved` | Rol retirado a un usuario |
| `DockZoneCreated` | Zona de trabajo creada |
| `DockZoneUpdated` | Zona de trabajo actualizada |
| `DockZoneDeleted` | Zona de trabajo eliminada |
| `DockCreated` | Puesto de trabajo creado |
| `DockUpdated` | Puesto de trabajo actualizado |
| `DockDeleted` | Puesto de trabajo eliminado |
| `DockTableCreated` | Mesa lógica creada |
| `DockTableUpdated` | Mesa lógica actualizada |
| `DockTableDeleted` | Mesa lógica eliminada |
| `DockZonesReordered` | Orden de visualización de zonas modificado |
| `DockTablesReordered` | Orden de visualización de mesas de una zona modificado |

---

#### `SystemSetting` — Configuración del sistema
Par clave-valor persistido para configuración en caliente sin redespliegue. La clave actúa como clave primaria (`string`); **excluida** del mecanismo UUID v7.

| Propiedad | Tipo | Descripción |
|---|---|---|
| `Key` | `string` (PK) | Clave única del ajuste |
| `Value` | `string` | Valor serializado como cadena |
| `UpdatedAt` | `DateTimeOffset` | Fecha y hora UTC de la última modificación |
| `UpdatedByUserId` | `Guid?` | FK del usuario que realizó la última modificación |

Claves predefinidas:

| Clave | Descripción |
|---|---|
| `"MaintenanceMode"` | `"true"` / `"false"` |
| `"MaintenanceModeReason"` | Texto libre (vacío si está desactivado) |

---

### Enums del dominio

| Enum | Valores |
|---|---|
| `BusinessLine` | `Core = 1`, `Energia = 2`, `ScrapWaste = 3`, `Transversal = 4` |
| `TimeSlot` | `Morning = 1` (hasta 14:00 h), `Afternoon = 2` (14:00 h – fin jornada), `Both = 3` (jornada completa) |
| `ReservationStatus` | `Active = 1`, `Cancelled = 2` |
| `ZoneOrientation` | `Horizontal = 0` (mesas en fila, por defecto), `Vertical = 1` (mesas apiladas en columna) |
| `AuditAction` | (ver tabla de `AuditLog` arriba) |

---

### Entidades planificadas (no implementadas)

Las siguientes entidades fueron contempladas en el diseño inicial pero aún no han sido implementadas:

| Entidad | Funcionalidad asociada |
|---|---|
| `IncidenceReport` | Funcionalidad 6: Reporte de incidencias en puestos de trabajo |
| `Report` / `ReportData` | Funcionalidad 5: Reportes y estadísticas |
| `Notification` | Notificaciones internas persistidas en la aplicación |


## 🔐 Modelo de autenticación y autorización
- **Autenticación**: Azure AD (Single Sign-On)
- **Roles/Claims**: BasicUser (puede reservar), Manager (puede reservar de modo normal y bloquear puestos)

Integración con Azure AD (tennant de nuestra compañía) para autenticación y autorización basada en roles. Los usuarios con rol "Manager" tendrán acceso a funcionalidades adicionales para bloquear puestos de trabajo.

## 📋 Reglas de negocio generales
- Un usuario solo puede reservar 1 puesto por día, y puede indicar la franja o franjas horarias en la que va a usar el puesto (mañanas hasta las 14 y tardes de 14 fin de jornada), de modo que otro empleado pueda reservar el puesto en una franja horaria diferente el mismo día si está disponible, pero un empleado puede reservar como máximo dos franjas horarias en el mismo puesto de trabajo o en dos puestos de trabajo distintos.
- Las reservas se abren con 7 días de antelación.
- Capacidad máxima: no hay una capacidad máxima global, pero cada puesto de trabajo tiene una capacidad de 2 reservas por día (mañana y tarde). El sistema debe validar que no se exceda esta capacidad al crear reservas.
- Interfaz visual: Mapa de puestos de trabajo, agrupados en zonas, cada zona con su propio bloque visual, que dispone mesas con una orientación específica (vertical u horizontal). Cada puesto muestra su estado (libre, reservado mañana, reservado tarde, reservado ambas franjas, bloqueado) y quién tiene la reserva o el bloqueo.
- Existirá un cuadro de mando para usuarios con privilegios de administración, que permitirá bloquear varios puestos de trabajo en un día o un período de días más largo, lo que bloqueará (impedirá reservar) esos puestos de trabajo para su reserva al resto de usuarios con privilegios normales.

## 👥 Funcionalidad 0: Gestión cuentas de usuario [Parcialmente implementada, pendiente de configuración de acceso al tennant de Azure AD]
- El sistema se integra con Azure AD para la autenticación de usuarios.
- Los usuarios se asignan automáticamente al rol "BasicUser" al iniciar sesión por primera vez.
- Un administrador puede asignar el rol "Manager" a usuarios específicos desde el portal de Azure AD, lo que les otorga privilegios adicionales para gestionar reservas y bloquear puestos de trabajo.

## ⚙️ Funcionalidad 1: Configuración de cuenta de usuario [Implementada]
- El usuario inicia sesión en la aplicación utilizando su cuenta de Azure AD.
- El usuario accede a su perfil de usuario desde un enlace persistente en la barra superior, identificado con su nombre de usuario autenticado.
- El perfil muestra los datos corporativos básicos sincronizados con Azure AD (nombre y correo) en modo de solo lectura.
- El usuario puede actualizar su línea de negocio y su número de teléfono de contacto desde su perfil.
- El usuario puede configurar sus preferencias de notificación (correo electrónico y, si está habilitada, Microsoft Teams) para recibir confirmaciones de reservas, cancelaciones y recordatorios.
- La edición del perfil se realiza en una pantalla dedicada, coherente con la UI principal de la aplicación, con feedback visual inmediato de guardado y validaciones básicas de entrada.
- La opción de configuración de notificaciones por Teams solo se muestra al usuario si el feature flag `Features:TeamsNotificationsEnabled` está activado. Si está desactivado, la tarjeta Teams desaparece de la pantalla de perfil y no se persiste la preferencia.

## 📌 Funcionalidad 2: Reserva de puesto de trabajo [Implementada]
- El usuario inicia sesión en la aplicación utilizando su cuenta de Azure AD.
- El usuario ve un mapa visual de los puestos de trabajo, agrupados en las zonas configuradas.
- El usuario selecciona un puesto de trabajo disponible para la fecha deseada.
- El usuario indica la franja horaria (mañana, tarde o ambas) para su reserva.
- El sistema valida que el usuario no tenga otra reserva para ese día y que el puesto esté disponible en la franja horaria seleccionada.
- El sistema confirma la reserva y muestra un resumen de la misma. El usuario puede cancelar la reserva desde el mismo resumen. El usuario recibe una notificación por correo electrónico o chat de Microsoft Teams (según configuración) confirmando la reserva.
- El mapa de bahías muestra información contextual en los tooltips de cada puesto:
  - **Libre**: "Libre — haz clic para reservar".
  - **Parcialmente reservado**: franja ocupada y nombre del usuario que la tiene reservada.
  - **Completo**: nombre(s) del/de los usuario(s) que tienen cada franja (mañana / tarde), diferenciados si son distintos.
  - **Bloqueado**: nombre del Manager que realizó el bloqueo y el motivo.
- Al abrir el modal de reserva de un puesto parcialmente ocupado, se indica explícitamente quién tiene reservada la franja ya ocupada.

## 🛡️ Funcionalidad 3: Cuadro de mando para administración [Implementada]
- Un usuario con rol de Manager inicia sesión en la aplicación.
- El Manager accede a un cuadro de mando que muestra el mapa de puestos de trabajo con la capacidad de seleccionar uno o varios puestos de trabajo.
- El Manager selecciona los puestos de trabajo que desea bloquear para un día específico o un período de días.
- El sistema bloquea los puestos de trabajo seleccionados, impidiendo que los usuarios con rol BasicUser puedan reservar esos puestos para las fechas bloqueadas.
- El Manager puede desbloquear los puestos de trabajo bloqueados desde el mismo cuadro de mando.
- El mapa de selección del cuadro de mando muestra en los tooltips quién tiene reservado cada puesto y en qué franja, diferenciando reservas de mañana y tarde con el nombre del reservador. Esto permite al Manager conocer el impacto sobre las reservas existentes antes de proceder al bloqueo.

## 🗓️ Funcionalidad 4: Gestión de reservas [Implementada]
- El usuario puede ver un historial de sus reservas pasadas y futuras.
- El usuario puede cancelar una reserva activa desde su historial en cualquier momento, sin restricción de antelación. Las cancelaciones de última hora son bienvenidas, ya que liberan el puesto para otros compañeros.
- El sistema envía notificaciones por correo electrónico al usuario para confirmar la creación, modificación o cancelación de una reserva.
- El sistema envía recordatorios automáticos a los usuarios sobre sus reservas próximas, con opciones para modificar o cancelar la reserva directamente desde la notificación.
- Las notificaciones por Microsoft Teams solo se envían si el feature flag `Features:TeamsNotificationsEnabled` está activo a nivel global (ver Funcionalidad 7).

## 🏗️ Funcionalidad 5: Configuración del Zonas, Mesas y Puestos de trabajo [Implementada]
- El SystemAdmin puede configurar (crear, editar y eliminar) las zonas de trabajo, asignar puestos a cada zona y definir la orientación de las mesas (horizontal o vertical) para cada zona desde el cuadro de mando de administración.
- El SystemAdmin puede definir metadatos de localización para cada mesa (por ejemplo, "Mesa 1", "Mesa 2") que se muestran en el mapa de puestos de trabajo como parte del bloque visual de cada zona. Si no se define un localizador para una mesa, se muestra un nombre inferido basado en la clave lógica (ejemplo: "Mesa N" para la mesa con clave "N").
- El SystemAdmin puede editar la `TableKey` (clave de agrupación) de una mesa existente desde el formulario de edición inline. La clave debe ser única dentro de la zona.
- El SystemAdmin puede reordenar las zonas y las mesas dentro de cada zona mediante arrastrar y soltar (*drag-and-drop*) con iconos de agarre (`bi-grip-vertical`) en las tarjetas de zona y en las filas de mesas. El nuevo orden se persiste inmediatamente en la base de datos y se refleja en el mapa de reservas de todos los usuarios.
- El SystemAdmin puede editar la información de cada puesto de trabajo, incluyendo su código, ubicación y equipamiento disponible, para mantener la información actualizada y precisa para los usuarios al hacer sus reservas.
- El SystemAdmin puede eliminar puestos de trabajo obsoletos o que ya no estén disponibles, lo que los retirará del mapa de reservas y cancelará automáticamente cualquier reserva futura asociada a esos puestos, notificando a los usuarios afectados sobre la cancelación y el motivo.

### Detalles de implementación

#### Nuevos valores del enum `AuditAction`

| Valor | Descripción |
|---|---|
| `DockZoneCreated` (10) | Zona de trabajo creada |
| `DockZoneUpdated` (11) | Zona de trabajo actualizada |
| `DockZoneDeleted` (12) | Zona de trabajo eliminada |
| `DockCreated` (13) | Puesto de trabajo creado |
| `DockUpdated` (14) | Puesto de trabajo actualizado |
| `DockDeleted` (15) | Puesto de trabajo eliminado |
| `DockTableCreated` (16) | Mesa lógica creada |
| `DockTableUpdated` (17) | Mesa lógica actualizada |
| `DockTableDeleted` (18) | Mesa lógica eliminada |
| `DockZonesReordered` (19) | Orden de visualización de zonas modificado |
| `DockTablesReordered` (20) | Orden de visualización de mesas de una zona modificado |

#### Comandos y queries implementados

| Artefacto | Ubicación |
|---|---|
| `CreateDockZoneCommand` | `EchoBase.Core/DockAdmin/Commands/` |
| `UpdateDockZoneCommand` | `EchoBase.Core/DockAdmin/Commands/` |
| `DeleteDockZoneCommand` | `EchoBase.Core/DockAdmin/Commands/` |
| `CreateDockCommand` | `EchoBase.Core/DockAdmin/Commands/` |
| `UpdateDockCommand` | `EchoBase.Core/DockAdmin/Commands/` |
| `DeleteDockCommand` | `EchoBase.Core/DockAdmin/Commands/` |
| `CreateDockTableCommand` | `EchoBase.Core/DockAdmin/Commands/` |
| `UpdateDockTableCommand` | `EchoBase.Core/DockAdmin/Commands/` — parámetros: `AdminUserId`, `TableId`, `TableKey` (requerido, único en la zona), `Locator?` |
| `DeleteDockTableCommand` | `EchoBase.Core/DockAdmin/Commands/` |
| `ReorderDockZonesCommand` | `EchoBase.Core/DockAdmin/Commands/` — parámetros: `AdminUserId`, `OrderedZoneIds` (lista de IDs en el nuevo orden) |
| `ReorderDockTablesCommand` | `EchoBase.Core/DockAdmin/Commands/` — parámetros: `AdminUserId`, `ZoneId`, `OrderedTableIds` (lista de IDs en el nuevo orden) |
| `GetDockAdminDataQuery` | `EchoBase.Core/DockAdmin/Queries/` |
| `DockAdminErrors` | `EchoBase.Core/DockAdmin/` |
| `IDockAdminRepository` | `EchoBase.Core/Interfaces/` |
| `DockAdminRepository` | `EchoBase.Infrastructure/Repositories/` |

#### Página de administración
Se añadió la pestaña **«Zonas y puestos»** (`Tab.DockConfig`) en `/system-admin` (`SystemAdminDashboard.razor`). La pestaña carga sus datos de forma diferida (lazy) la primera vez que se selecciona.

| Sección | Descripción |
|---|---|
| Lista de zonas | Una tarjeta por zona con badge de orientación, botón editar (formulario inline), botón eliminar (modal de confirmación). Las zonas tienen un icono de agarre (`bi-grip-vertical`) que permite reordenarlas mediante drag-and-drop. |
| Mesas por zona | Tabla con clave (`TableKey`) y localizador; edición inline de ambos campos (`TableKey` + `Locator`); formulario de nueva mesa. Las filas de mesa tienen un icono de agarre para reordenarlas dentro de la zona mediante drag-and-drop. |
| Puestos por zona | Tabla con código, ubicación y equipamiento; edición inline; eliminación con aviso de cancelación de reservas |
| Crear zona | Formulario al pie con campos nombre, descripción, orientación (btn-check Horizontal/Vertical) |
| Modales | Confirmación antes de eliminar zona, mesa o puesto (el de puesto advierte sobre cancelación de reservas futuras) |

## 📊 Funcionalidad 6: Reportes y estadísticas [Pendiente de implementación]
- El Manager puede acceder a un panel de reportes que muestra estadísticas de uso de los puestos de trabajo, como el porcentaje de ocupación por día, semana o mes.
- El Manager puede exportar los datos de reservas en formato CSV para análisis adicionales.
- El sistema genera alertas automáticas para el Manager si se detecta un patrón de reservas inusuales, como un aumento repentino en la demanda de ciertos puestos de trabajo o una alta tasa de cancelaciones.

## 🔧 Funcionalidad 7: Reporte de incidencias en los puestos de trabajo [Pendiente de implementación]
- El usuario puede reportar incidencias relacionadas con los puestos de trabajo (por ejemplo, problemas de equipamiento o limpieza) a través de la aplicación.
- El usuario selecciona el puesto de trabajo afectado y describe la incidencia en un formulario.
- El sistema registra la incidencia y notifica a los usuarios con rol de Manager para que puedan tomar medidas correctivas. El usuario recibe una confirmación de que su reporte ha sido registrado y se le informa sobre el proceso de seguimiento de la incidencia.
- El usuario puede hacer seguimiento del estado de su reporte de incidencia desde su perfil de usuario, recibiendo notificaciones sobre el progreso y la resolución de la incidencia.

## 🚩 Funcionalidad 8: Feature Flags de sistema [Implementada]

El sistema incluye un mecanismo de feature flags basado en configuración para activar o desactivar funcionalidades sin necesidad de redespliegue. Los flags se declaran en la sección `Features` de `appsettings.json` y pueden sobreescribirse en `appsettings.Development.json` o en las variables de entorno del host.

### 🔔 Flag: `Features:TeamsNotificationsEnabled`

| Valor | Comportamiento |
|---|---|
| `true` (por defecto) | Las notificaciones de Teams están activas. Se usa `GraphTeamsNotificationService` en producción o `LogTeamsNotificationService` con stubs de desarrollo. |
| `false` | Las notificaciones de Teams están completamente desactivadas. Se registra `NullTeamsNotificationService` (no-op) independientemente del entorno. El toggle de Teams desaparece de la pantalla de perfil de usuario y no se almacena la preferencia. |

**Alcance del flag:**
- **Infraestructura**: `ServiceCollectionExtensions.AddEchoBaseNotifications` lee el flag durante el arranque y registra la implementación apropiada de `ITeamsNotificationService`.
- **UI**: `UserProfile.razor` lee el flag en tiempo de ejecución para mostrar u ocultar el toggle de preferencias de Teams. Además, aunque el usuario tuviera previamente activada la preferencia, al guardar el perfil con el flag en `false` se persiste `false` para la preferencia de Teams.
- **Lógica de negocio**: Los handlers MediatR (`ReservationCreatedTeamsHandler`, `ReservationCancelledTeamsHandler`, `ReservationReminderTeamsHandler`) no tienen conocimiento del flag; simplemente llaman a `ITeamsNotificationService`, que en caso de flag desactivado es la implementación no-op.

**Configuración de referencia (`appsettings.json`):**
```json
{
  "Features": {
    "TeamsNotificationsEnabled": true
  }
}
```

**Para desactivar Teams en un entorno específico**, añadir a `appsettings.{Environment}.json`:
```json
{
  "Features": {
    "TeamsNotificationsEnabled": false
  }
}
```

**Tests unitarios:** `TeamsFeatureFlagTests` (en `EchoBase.Tests.Unit`) cubre:
- `NullTeamsNotificationService` completa sin efecto ni excepción.
- Con flag `false`, `AddEchoBaseNotifications` registra `NullTeamsNotificationService`.
- Con flag `false` y stubs activos, `NullTeamsNotificationService` sigue teniendo prioridad.
- Con flag `true` y stubs activos, se registra `LogTeamsNotificationService`.
- Con flag `true` y stubs desactivados, se registra `GraphTeamsNotificationService`.
- Sin flag declarado, el valor por defecto (`true`) preserva el comportamiento original.

## 🛠️ Funcionalidad 9: Funciones de administración del sistema [Implementada]
- El Administrador o administradores pueden gestionar los usuarios del sistema, asignar roles y revisar logs de auditoría para acciones críticas como reservas, cancelaciones y bloqueos de puestos de trabajo.
- El sistema registra en un log de auditoría todas las acciones relevantes, incluyendo quién realizó la acción, qué acción se realizó, detalles adicionales y la marca de tiempo. Este log es accesible para los administradores a través de una interfaz dedicada, con opciones de filtrado y exportación para análisis.
- El sistema incluye una función de "modo mantenimiento" que los administradores pueden activar para realizar tareas de mantenimiento sin afectar a los usuarios finales. Cuando el modo mantenimiento está activo, los usuarios reciben una notificación de que el sistema está temporalmente fuera de servicio y no pueden realizar reservas ni acceder a sus perfiles hasta que se desactive el modo mantenimiento.
- El sistema permite a los administradores cancelar reservas concretas y en masa para un día específico o un período de días, lo que es útil en situaciones como cierres de oficina por condiciones climáticas adversas o eventos especiales. Los usuarios afectados por la cancelación masiva reciben notificaciones individuales informándoles de la cancelación y el motivo.
- El sistema incluye una función de "reserva de emergencia" que los administradores pueden usar para reservar puestos de trabajo en nombre de los usuarios en situaciones excepcionales, como problemas técnicos o solicitudes urgentes. Esta función permite a los administradores seleccionar un usuario, un puesto de trabajo y una fecha, y realizar la reserva directamente desde el cuadro de mando de administración.

### Detalles de implementación

#### Rol SystemAdmin
- Semilla de BD: `Id = d0000000-0000-0000-0000-000000000003`, `Name = "SystemAdmin"`.
- Todos los comandos y queries protegidos verifican `UserHasRoleAsync(userId, "SystemAdmin")` via `IBlockedDockRepository`.
- En modo desarrollo, la clave `Authentication:DevUserIsSystemAdmin = true` en `appsettings.Development.json` asigna el rol SystemAdmin al usuario de desarrollo.

#### Modo de mantenimiento (`SystemSetting`)
La entidad `SystemSetting` (clave primaria: `Key: string`) persiste la configuración del sistema como pares clave/valor con auditoría:

| Clave | Descripción |
|---|---|
| `"MaintenanceMode"` | `"true"` / `"false"` |
| `"MaintenanceModeReason"` | Texto libre (vacío si desactivado) |

El handler `GetMaintenanceModeHandler` lee el ajuste completo (incluido `UpdatedAt` y `UpdatedByUserId`) con `GetSettingAsync`.

#### Log de auditoría (`AuditLog`)
La entidad `AuditLog` se genera automáticamente vía `AuditLoggingBehavior<TRequest, TResponse>` (pipeline de MediatR) cuando:
- La solicitud implementa `IAuditableRequest`, Y
- La respuesta es un `Result` o `Result<T>` con `IsSuccess = true`.

Valores del enum `AuditAction`:

| Valor | Descripción |
|---|---|
| `ReservationCreated` | Reserva creada |
| `ReservationCancelled` | Reserva cancelada |
| `DockBlocked` | Puesto(s) bloqueado(s) |
| `DockUnblocked` | Puesto(s) desbloqueado(s) |
| `BulkReservationsCancelled` | Cancelación masiva ejecutada |
| `MaintenanceModeChanged` | Modo mantenimiento activado/desactivado |
| `EmergencyReservationCreated` | Reserva de emergencia creada |
| `UserRoleAssigned` | Rol asignado a un usuario |
| `UserRoleRemoved` | Rol retirado a un usuario |
| `DockZoneCreated` | Zona de trabajo creada |
| `DockZoneUpdated` | Zona de trabajo actualizada |
| `DockZoneDeleted` | Zona de trabajo eliminada |
| `DockCreated` | Puesto de trabajo creado |
| `DockUpdated` | Puesto de trabajo actualizado |
| `DockDeleted` | Puesto de trabajo eliminado |
| `DockTableCreated` | Mesa lógica creada |
| `DockTableUpdated` | Mesa lógica actualizada |
| `DockTableDeleted` | Mesa lógica eliminada |
| `DockZonesReordered` | Orden de visualización de zonas modificado |
| `DockTablesReordered` | Orden de visualización de mesas de una zona modificado |

#### Comandos y queries implementados

| Artefacto | Ubicación |
|---|---|
| `SetMaintenanceModeCommand` | `EchoBase.Core/SystemAdmin/Commands/` |
| `BulkCancelReservationsCommand` | `EchoBase.Core/SystemAdmin/Commands/` |
| `CreateEmergencyReservationCommand` | `EchoBase.Core/SystemAdmin/Commands/` |
| `AssignUserRoleCommand` | `EchoBase.Core/SystemAdmin/Commands/` |
| `RemoveUserRoleCommand` | `EchoBase.Core/SystemAdmin/Commands/` |
| `GetMaintenanceModeQuery` | `EchoBase.Core/SystemAdmin/Queries/` |
| `GetAuditLogsQuery` | `EchoBase.Core/SystemAdmin/Queries/` |
| `AuditLoggingBehavior<,>` | `EchoBase.Core/SystemAdmin/` |

#### Página de administración
`/system-admin` (`SystemAdminDashboard.razor`) — requiere rol `SystemAdmin`. La página se organiza en **5 pestañas Bootstrap (`nav-tabs`)**; solo el contenido de la pestaña activa se renderiza. El log de auditoría carga sus datos de forma diferida (lazy) la primera vez que se selecciona la pestaña.

| Pestaña | Contenido |
|---|---|
| **Mantenimiento** | Activar / desactivar modo mantenimiento con motivo. Badge `ACTIVO` visible en la pestaña cuando está habilitado. |
| **Cancelación masiva** | Rango de fechas + motivo + mapa visual de puestos (mismo layout y estilos que `DockMap.razor`): los puestos actúan como botones toggle (`btn-outline-secondary` ↔ `btn-danger`); sin selección = cancelar todos. Contador de puestos seleccionados con botón "Limpiar selección". Diálogo de confirmación antes de ejecutar. |
| **Reserva de emergencia** | Selector de usuario + selector de fecha con flechas de navegación por días (recarga el mapa al cambiar). Mapa visual interactivo idéntico al de `DockMap.razor` (colores libres/parcial/completo/bloqueado, tooltips). Al hacer clic en un puesto disponible se abre un **modal** (mismo estilo que DockMap) con información del puesto, usuario seleccionado, selector de franja horaria (`btn-check`) y botón "Crear reserva". |
| **Usuarios** | Tabla de usuarios con sus roles actuales (badges de color). Botones de asignar/retirar rol **Manager** (`bi-person-plus` / `bi-person-dash`) y **SystemAdmin** (`bi-shield-plus` / `bi-shield-dash`). El texto del botón muestra el nombre completo del rol (`SystemAdmin`, no `Admin`). |
| **Log de auditoría** | Tabla paginada con filtros de fecha (desde/hasta), tipo de acción y nombre de usuario. Paginación con flechas. |

---

## 🧪 Estrategia de pruebas

### 🔬 Pruebas unitarias (`EchoBase.Tests.Unit`)

- **Framework:** xUnit + NSubstitute
- **Alcance:** Handlers MediatR de forma aislada; cada dependencia externa (repositorios, servicios de notificación) se sustituye por un doble de prueba con NSubstitute.
- **Cobertura actual:** Handlers de comandos/queries de Reservaciones, Usuarios y BlockedDocks; feature flags de Teams; orientación de zona y localizador de mesa en `GetDockMapHandler`.

### 🔗 Pruebas de integración (`EchoBase.Tests.Integration`)

#### 🧰 Herramientas elegidas

| Capa | Decisión | Justificación |
|---|---|---|
| Framework de tests | xUnit (ya en uso) | Consistencia con el resto del proyecto; no se introduce nueva dependencia. |
| Base de datos | EF Core con **SQLite en memoria** (`Microsoft.Data.Sqlite`) | Misma librería ya referenciada en `EchoBase.Infrastructure`. Semántica SQL real (a diferencia del proveedor `InMemory` de EF Core, que no valida tipos ni restricciones). |
| Pipeline de negocio | **MediatR real** — sin mocks | Los tests ejercen el pipeline completo (validación, handler, notificaciones), de modo que cualquier regresión en el ensamblado de dependencias o en el flujo de un comando queda expuesta. |
| Servicios externos | Stubs no-operativos en `Infrastructure/Stubs/` | `NullEmailService` y `NullTeamsNotificationService` implementan las interfaces reales sin efecto secundario; permiten que los handlers de notificación se ejecuten sin SMTP ni Graph API. |
| Tiempo | `FrozenTimeProvider` | Subclase de `TimeProvider` congelada al inicio del día UTC. Hace deterministas las comprobaciones de "hoy" y "máximo 7 días vista". |

#### 🔒 Patrón de aislamiento

Cada clase de tests hereda de `IntegrationTestBase : IAsyncLifetime`.

- En `InitializeAsync`: se abre una `SqliteConnection("Data Source=:memory:")` y se mantiene abierta durante toda la vida del objeto. EF Core recibe esa conexión directamente con `UseSqlite(connection)`, lo que garantiza que el esquema persiste entre operaciones (las bases de datos SQLite en memoria se destruyen en cuanto se cierran todas sus conexiones).
- Se construye un contenedor DI con los repositorios reales, MediatR, stubs y `FrozenTimeProvider`.
- `DbSeeder.SeedAsync` puebla zonas y puestos de la configuración real; a continuación se insertan los tres usuarios de prueba específicos de los tests.
- En `DisposeAsync`: se libera el `DbContext`, el proveedor de servicios y la conexión SQLite.

Cada instancia de clase de tests obtiene su propia base de datos en memoria, por lo que los tests son completamente independientes entre sí.

#### ✅ Cobertura actual — Funcionalidad 5: Configuración de Zonas, Mesas y Puestos

**Pruebas unitarias**

**`DockAdminCommandTests`** (36 casos — `EchoBase.Tests.Unit/DockAdmin/`):

| ID | Caso |
|---|---|
| UT-DA-01 | `CreateDockZone` → success, devuelve Guid no vacío |
| UT-DA-02 | `CreateDockZone` → no SystemAdmin → `NotSystemAdmin` |
| UT-DA-03 | `CreateDockZone` → nombre duplicado → `ZoneNameAlreadyExists` |
| UT-DA-04 | `UpdateDockZone` → success, llama `UpdateZoneAsync` con parámetros correctos |
| UT-DA-05 | `UpdateDockZone` → no SystemAdmin → `NotSystemAdmin` |
| UT-DA-06 | `UpdateDockZone` → zona inexistente → `ZoneNotFound` |
| UT-DA-07 | `UpdateDockZone` → nombre duplicado → `ZoneNameAlreadyExists` |
| UT-DA-08 | `DeleteDockZone` → zona vacía → success, llama `DeleteZoneAsync` |
| UT-DA-09 | `DeleteDockZone` → no SystemAdmin → `NotSystemAdmin` |
| UT-DA-10 | `DeleteDockZone` → zona inexistente → `ZoneNotFound` |
| UT-DA-11 | `DeleteDockZone` → zona con puestos → `ZoneHasDocks` |
| UT-DA-12 | `CreateDock` → success, devuelve Guid no vacío |
| UT-DA-13 | `CreateDock` → no SystemAdmin → `NotSystemAdmin` |
| UT-DA-14 | `CreateDock` → código vacío → `DockCodeRequired` |
| UT-DA-15 | `CreateDock` → código duplicado → `DockCodeAlreadyExists` |
| UT-DA-16 | `CreateDock` → zona inexistente → `ZoneNotFound` |
| UT-DA-17 | `UpdateDock` → success, llama `UpdateDockAsync` con parámetros correctos |
| UT-DA-18 | `UpdateDock` → no SystemAdmin → `NotSystemAdmin` |
| UT-DA-19 | `UpdateDock` → código vacío → `DockCodeRequired` |
| UT-DA-20 | `UpdateDock` → puesto inexistente → `DockNotFound` |
| UT-DA-21 | `UpdateDock` → código duplicado → `DockCodeAlreadyExists` |
| UT-DA-22 | `DeleteDock` → con reservas futuras → cancela (publica notificación × n) + elimina, devuelve count |
| UT-DA-23 | `DeleteDock` → sin reservas futuras → elimina, devuelve 0 |
| UT-DA-24 | `DeleteDock` → no SystemAdmin → `NotSystemAdmin` |
| UT-DA-25 | `DeleteDock` → puesto inexistente → `DockNotFound` |
| UT-DA-26 | `CreateDockTable` → success, devuelve Guid no vacío |
| UT-DA-27 | `CreateDockTable` → no SystemAdmin → `NotSystemAdmin` |
| UT-DA-28 | `CreateDockTable` → clave vacía → `TableKeyRequired` |
| UT-DA-29 | `CreateDockTable` → zona inexistente → `ZoneNotFound` |
| UT-DA-30 | `CreateDockTable` → clave duplicada en zona → `TableKeyAlreadyExists` |
| UT-DA-31 | `UpdateDockTable` → success, llama `UpdateTableLocatorAsync` con params correctos |
| UT-DA-32 | `UpdateDockTable` → no SystemAdmin → `NotSystemAdmin` |
| UT-DA-33 | `UpdateDockTable` → mesa inexistente → `TableNotFound` |
| UT-DA-34 | `DeleteDockTable` → success, llama `DeleteTableAsync` |
| UT-DA-35 | `DeleteDockTable` → no SystemAdmin → `NotSystemAdmin` |
| UT-DA-36 | `DeleteDockTable` → mesa inexistente → `TableNotFound` |

**Pruebas de integración**

**`DockAdminIntegrationTests`** (33 casos — `EchoBase.Tests.Integration/DockAdmin/`):

| ID | Caso |
|---|---|
| IT-DA-01 | `CreateDockZone` → zona persistida en BD con nombre y orientación correctos |
| IT-DA-02 | `CreateDockZone` → entrada de auditoría `DockZoneCreated` escrita |
| IT-DA-03 | `CreateDockZone` → no SystemAdmin → `NotSystemAdmin`, zona no creada |
| IT-DA-04 | `UpdateDockZone` → cambios persistidos en BD (nombre y orientación) |
| IT-DA-05 | `UpdateDockZone` → entrada de auditoría `DockZoneUpdated` escrita |
| IT-DA-06 | `UpdateDockZone` → no SystemAdmin → `NotSystemAdmin` |
| IT-DA-07 | `DeleteDockZone` → zona vacía eliminada de BD |
| IT-DA-08 | `DeleteDockZone` → zona con puestos → `ZoneHasDocks`, zona no eliminada |
| IT-DA-09 | `DeleteDockZone` → no SystemAdmin → `NotSystemAdmin` |
| IT-DA-10 | `CreateDock` → puesto persistido en BD asignado a zona correcta |
| IT-DA-11 | `CreateDock` → entrada de auditoría `DockCreated` escrita |
| IT-DA-12 | `CreateDock` → código duplicado → `DockCodeAlreadyExists` |
| IT-DA-13 | `CreateDock` → no SystemAdmin → `NotSystemAdmin` |
| IT-DA-14 | `UpdateDock` → cambios de código, ubicación y equipamiento persistidos en BD |
| IT-DA-15 | `UpdateDock` → entrada de auditoría `DockUpdated` escrita |
| IT-DA-16 | `UpdateDock` → no SystemAdmin → `NotSystemAdmin` |
| IT-DA-17 | `DeleteDock` → sin reservas → puesto eliminado, devuelve 0 |
| IT-DA-18 | `DeleteDock` → con reserva futura → reserva eliminada de BD, devuelve 1, puesto eliminado |
| IT-DA-19 | `DeleteDock` → entrada de auditoría `DockDeleted` escrita |
| IT-DA-20 | `DeleteDock` → puesto inexistente → `DockNotFound` |
| IT-DA-21 | `DeleteDock` → no SystemAdmin → `NotSystemAdmin` |
| IT-DA-22 | `CreateDockTable` → mesa persistida en BD con clave, localizador y zona correctos |
| IT-DA-23 | `CreateDockTable` → entrada de auditoría `DockTableCreated` escrita |
| IT-DA-24 | `CreateDockTable` → no SystemAdmin → `NotSystemAdmin` |
| IT-DA-25 | `CreateDockTable` → clave duplicada en zona → `TableKeyAlreadyExists` |
| IT-DA-26 | `UpdateDockTable` → localizador actualizado en BD |
| IT-DA-27 | `UpdateDockTable` → entrada de auditoría `DockTableUpdated` escrita |
| IT-DA-28 | `UpdateDockTable` → no SystemAdmin → `NotSystemAdmin` |
| IT-DA-29 | `DeleteDockTable` → mesa eliminada de BD |
| IT-DA-30 | `DeleteDockTable` → entrada de auditoría `DockTableDeleted` escrita |
| IT-DA-31 | `DeleteDockTable` → no SystemAdmin → `NotSystemAdmin` |
| IT-DA-32 | `GetDockAdminData` → devuelve todas las zonas sembradas con puestos y mesas |
| IT-DA-33 | `GetDockAdminData` → zona recién creada aparece en el resultado |

#### ✅ Cobertura actual — Funcionalidad 2: Reserva de puesto de trabajo

**`CreateReservationIntegrationTests`** (9 casos):

| ID | Caso |
|---|---|
| IT-CR-01 | Solicitud válida → reserva persistida, devuelve Guid no vacío |
| IT-CR-02 | Fecha en el pasado → `ReservationErrors.DateInThePast` |
| IT-CR-03 | Fecha demasiado lejana (hoy + 8 días) → `ReservationErrors.DateTooFarAhead` |
| IT-CR-04 | Puesto inexistente → `ReservationErrors.DockNotFound` |
| IT-CR-05 | Puesto bloqueado por administración → `ReservationErrors.DockBlocked` |
| IT-CR-06 | Puesto ya reservado en ambas franjas → `ReservationErrors.DockNotAvailable` |
| IT-CR-07 | Usuario supera máximo de franjas diarias → `ReservationErrors.UserMaxSlotsExceeded` |
| IT-CR-08 | Dos usuarios reservan franjas complementarias (Mañana + Tarde) → ambas se persisten |
| IT-CR-09 | Usuario reserva franja `Both` → éxito, franja correcta almacenada |

**`CancelReservationIntegrationTests`** (4 casos):

| ID | Caso |
|---|---|
| IT-CA-01 | Propietario cancela reserva activa → estado pasa a `Cancelled` |
| IT-CA-02 | No propietario intenta cancelar → `ReservationErrors.NotReservationOwner`, reserva permanece activa |
| IT-CA-03 | Reserva ya cancelada → `ReservationErrors.AlreadyCancelled` |
| IT-CA-04 | Reserva no encontrada → `ReservationErrors.ReservationNotFound` |

#### ✅ Cobertura actual — Funcionalidad 8: Funciones de administración del sistema

**Pruebas unitarias**

**`SetMaintenanceModeTests`** (5 casos):

| ID | Caso |
|---|---|
| UT-SA-01 | AdminUserId con rol SystemAdmin → mode activado, devuelve `Result.Success` |
| UT-SA-02 | AdminUserId con rol SystemAdmin → mode desactivado, devuelve `Result.Success` |
| UT-SA-03 | `SetAsync` llamado con `MaintenanceModeKey` y valor `"true"` |
| UT-SA-04 | Al desactivar, `reason` almacenado como cadena vacía |
| UT-SA-05 | `AdminUserId` sin rol SystemAdmin → `SystemAdminErrors.NotSystemAdmin` |

**`BulkCancelReservationsTests`** (6 casos):

| ID | Caso |
|---|---|
| UT-SA-06 | Cancela todas las reservas activas del rango → devuelve `Result.Success(count)` |
| UT-SA-07 | Sin reservas en el rango → devuelve `Result.Success(0)` |
| UT-SA-08 | Filtro de DockId se pasa al repositorio correctamente |
| UT-SA-09 | No es SystemAdmin → `SystemAdminErrors.NotSystemAdmin` |
| UT-SA-10 | Fecha desde > hasta → `SystemAdminErrors.InvalidDateRange` |
| UT-SA-11 | Fecha desde == hasta → se procesa correctamente |

**`AssignRemoveUserRoleTests`** (9 casos):

| ID | Caso |
|---|---|
| UT-SA-12 | Asignar rol `"Manager"` a usuario existente → éxito |
| UT-SA-13 | Rol `"Manager"` queda en la colección del usuario |
| UT-SA-14 | Asignar siendo no SystemAdmin → `SystemAdminErrors.NotSystemAdmin` |
| UT-SA-15 | Asignar rol inválido → `SystemAdminErrors.InvalidRole` |
| UT-SA-16 | Asignar a usuario no encontrado → `SystemAdminErrors.UserNotFound` |
| UT-SA-17 | Asignar rol ya asignado → `SystemAdminErrors.RoleAlreadyAssigned` |
| UT-SA-18 | Eliminar rol existente → éxito |
| UT-SA-19 | Eliminar rol no asignado → `SystemAdminErrors.RoleNotAssigned` |
| UT-SA-20 | Eliminar siendo no SystemAdmin → `SystemAdminErrors.NotSystemAdmin` |

**`CreateEmergencyReservationTests`** (11 casos):

| ID | Caso |
|---|---|
| UT-SA-21 | Franja `Morning` → reserva creada, devuelve `Result.Success(Guid)` |
| UT-SA-22 | Franja `Afternoon` → reserva creada |
| UT-SA-23 | Franja `Both` → reserva creada |
| UT-SA-24 | `ITeamsNotificationService` notificado al usuario destinatario |
| UT-SA-25 | No es SystemAdmin → `SystemAdminErrors.NotSystemAdmin` |
| UT-SA-26 | Fecha en el pasado → `ReservationErrors.DateInThePast` |
| UT-SA-27 | Fecha a más de 7 días → `ReservationErrors.DateTooFarAhead` |
| UT-SA-28 | Fecha exactamente hoy (límite inferior) → éxito |
| UT-SA-29 | Puesto no encontrado → `ReservationErrors.DockNotFound` |
| UT-SA-30 | Puesto bloqueado → `ReservationErrors.DockBlocked` |
| UT-SA-31 | Puesto ya reservado en ambas franjas → `ReservationErrors.DockNotAvailable` |

**`AuditLoggingBehaviorTests`** (7 casos):

| ID | Caso |
|---|---|
| UT-SA-32 | Solicitud auditable + `Result.Success` → `AuditLogRepository.AddAsync` llamado |
| UT-SA-33 | Solicitud auditable + `Result<Guid>.Success` → entrada de auditoría registrada |
| UT-SA-34 | Solicitud auditable + `Result.Failure` → no se registra auditoría |
| UT-SA-35 | Solicitud auditable + `Result<Guid>.Failure` → no se registra |
| UT-SA-36 | Solicitud NO auditable → `AddAsync` nunca llamado |
| UT-SA-37 | Behavior no altera la respuesta del handler |
| UT-SA-38 | Timestamp de la entrada proviene del `TimeProvider` |

**`AuditDetailsReservationTests`** (4 casos — `EchoBase.Tests.Unit/Reservations/`):

| ID | Caso |
|---|---|
| UT-AD-01 | `CreateReservationCommand` con `ResolvedDockCode` → `Details` contiene código, fecha y franja legible (Mañana/Tarde/Mañana y Tarde); no contiene el GUID |
| UT-AD-02 | `CreateReservationCommand` sin resolución → fallback al GUID del puesto |
| UT-AD-03 | `CancelReservationCommand` con `ResolvedAuditDetails` → `Details` contiene texto enriquecido; GUID de reserva ausente |
| UT-AD-04 | `CancelReservationCommand` sin resolución → fallback al GUID de la reserva |

**`AuditDetailsAdminCommandsTests`** (8 casos — `EchoBase.Tests.Unit/SystemAdmin/`):

| ID | Caso |
|---|---|
| UT-AD-05 | `BlockDocksCommand` con `ResolvedDockCodes` → `Details` contiene códigos, fechas y motivo; no contiene GUIDs |
| UT-AD-06 | `BlockDocksCommand` sin resolución → fallback a conteo de puestos |
| UT-AD-07 | `CreateEmergencyReservationCommand` con resolución → `Details` contiene código puesto, nombre usuario y franja legible |
| UT-AD-08 | `CreateEmergencyReservationCommand` sin resolución → fallback a GUIDs de puesto y usuario |
| UT-AD-09 | `AssignUserRoleCommand` con `ResolvedTargetUserName` → `Details` contiene nombre del usuario; GUID ausente |
| UT-AD-10 | `AssignUserRoleCommand` sin resolución → fallback al GUID del usuario |
| UT-AD-11 | `RemoveUserRoleCommand` con `ResolvedTargetUserName` → `Details` contiene nombre del usuario; GUID ausente |
| UT-AD-12 | `RemoveUserRoleCommand` sin resolución → fallback al GUID del usuario |

**`GetDockMapHandlerTests`** (4 casos sobre orientación de zona y localizador de mesa — `EchoBase.Tests.Unit/Reservations/`):

| ID | Caso |
|---|---|
| UT-DM-01 | Zona con `Orientation` por defecto → `DockZoneMapDto.Orientation` es `Horizontal` |
| UT-DM-02 | Zona con `Orientation = Vertical` → `DockZoneMapDto.Orientation` es `Vertical` |
| UT-DM-03 | `DockTable` con `Locator` no nulo → `DockTableDto.Locator` contiene el valor configurado |
| UT-DM-04 | Zona sin `DockTable` entries → `DockTableDto.Locator` es `null` en todos los DTOs de tabla |

**Pruebas de integración**

**`SetMaintenanceModeIntegrationTests`** (5 casos):

| ID | Caso |
|---|---|
| IT-SA-01 | Activar → `SystemSetting` con `Key = "MaintenanceMode"` y valor `"true"` persistido |
| IT-SA-02 | Activar luego desactivar → valor pasa a `"false"`, reason vacío |
| IT-SA-03 | Activar y consultar `GetMaintenanceModeQuery` → DTO correcto con `IsActive`, `Reason`, `UpdatedAt`, `UpdatedByUserId` |
| IT-SA-04 | No SystemAdmin → `SystemAdminErrors.NotSystemAdmin` |
| IT-SA-05 | Activar → entrada de auditoría con `Action = MaintenanceModeChanged` persistida |

**`BulkCancelReservationsIntegrationTests`** (6 casos):

| ID | Caso |
|---|---|
| IT-SA-06 | Dos reservas en rango → ambas canceladas, devuelve `Result.Success(2)` |
| IT-SA-07 | Sin reservas en rango → devuelve `Result.Success(0)` |
| IT-SA-08 | Cancela solo el rango especificado (reserva fuera de rango intacta) |
| IT-SA-09 | Filtro de puesto → solo cancela la reserva de ese puesto |
| IT-SA-10 | No SystemAdmin → `SystemAdminErrors.NotSystemAdmin` |
| IT-SA-11 | Fecha desde > hasta → `SystemAdminErrors.InvalidDateRange` |

**`CreateEmergencyReservationIntegrationTests`** (5 casos):

| ID | Caso |
|---|---|
| IT-SA-12 | Solicitud válida → reserva persistida con `UserId` y `DockId` correctos |
| IT-SA-13 | No SystemAdmin → `SystemAdminErrors.NotSystemAdmin` |
| IT-SA-14 | Fecha en el pasado → `ReservationErrors.DateInThePast` |
| IT-SA-15 | Puesto no encontrado → `ReservationErrors.DockNotFound` |
| IT-SA-16 | Puesto ya reservado en Both → `ReservationErrors.DockNotAvailable` |

**`AuditLogIntegrationTests`** (6 casos):

| ID | Caso |
|---|---|
| IT-SA-17 | Comando exitoso → entrada de auditoría persistida en BD |
| IT-SA-18 | Comando fallido (no SystemAdmin) → no se escribe entrada |
| IT-SA-19 | Dos comandos → dos entradas, ordenadas por `Timestamp` descendente |
| IT-SA-20 | Filtro por `Action` devuelve solo los registros coincidentes |
| IT-SA-21 | Paginación: solicitar página 2 devuelve los registros correctos |
| IT-SA-22 | Filtro por nombre de usuario devuelve solo entradas de ese usuario |

#### ✅ Cobertura actual — Consulta de mapa de bahías

**`GetDockMapIntegrationTests`** (3 casos):

| ID | Caso |
|---|---|
| IT-DM-01 | `GetDockMapQuery` → zonas retornadas con `Orientation` correcta desde BD (`Nostromo = Horizontal`, `Derelict = Vertical`) |
| IT-DM-02 | `GetDockMapQuery` → tablas retornadas con `Locator` correcto procedente de los registros `DockTable` sembrados |
| IT-DM-03 | `GetDockMapQuery` → `DockMapDto.Date` es la fecha de la query |