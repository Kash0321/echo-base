# 🗃️ Modelo de datos

## Entidades de negocio

### `User` — Empleado
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

### `Dock` — Puesto de trabajo
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

### `DockZone` — Zona de trabajo
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

### `DockTable` — Mesa física
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

### `Reservation` — Reserva de puesto
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

### `BlockedDock` — Bloqueo de puesto
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

### `Role` — Rol de autorización

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

### `AuditLog` — Registro de auditoría
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

### `SystemSetting` — Configuración del sistema
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

## Enums del dominio

| Enum | Valores |
|---|---|
| `BusinessLine` | `Core = 1`, `Energia = 2`, `ScrapWaste = 3`, `Transversal = 4` |
| `TimeSlot` | `Morning = 1` (hasta 14:00 h), `Afternoon = 2` (14:00 h – fin jornada), `Both = 3` (jornada completa) |
| `ReservationStatus` | `Active = 1`, `Cancelled = 2` |
| `ZoneOrientation` | `Horizontal = 0` (mesas en fila, por defecto), `Vertical = 1` (mesas apiladas en columna) |
| `AuditAction` | (ver tabla de `AuditLog` arriba) |

---