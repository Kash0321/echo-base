## ADDED Requirements

### Requirement: Exposición de endpoints REST para reservas
La API SHALL exponer endpoints HTTP REST para todas las operaciones de reservas: consultar el mapa de puestos, obtener las reservas del usuario, crear una reserva y cancelar una reserva.

#### Scenario: Consultar el mapa de puestos
- **WHEN** un usuario autenticado hace GET `/api/v1/docks/map?date={fecha}`
- **THEN** la API devuelve HTTP 200 con la estructura completa de zonas, mesas y puestos con su estado para la fecha indicada

#### Scenario: Obtener reservas del usuario autenticado
- **WHEN** un usuario autenticado hace GET `/api/v1/reservations`
- **THEN** la API devuelve HTTP 200 con la lista de reservas del usuario en sesión

#### Scenario: Crear una reserva
- **WHEN** un usuario autenticado hace POST `/api/v1/reservations` con `dockId`, `date` y `timeSlot` válidos
- **THEN** la API devuelve HTTP 201 con el `id` de la reserva creada

#### Scenario: Crear una reserva con datos inválidos
- **WHEN** un usuario autenticado hace POST `/api/v1/reservations` con una fecha pasada, puesto inexistente o franja ya ocupada
- **THEN** la API devuelve HTTP 422 con el código de error correspondiente del dominio

#### Scenario: Cancelar una reserva propia
- **WHEN** un usuario autenticado hace DELETE `/api/v1/reservations/{id}` siendo propietario de la reserva
- **THEN** la API devuelve HTTP 204

#### Scenario: Cancelar una reserva ajena
- **WHEN** un usuario autenticado hace DELETE `/api/v1/reservations/{id}` sin ser propietario
- **THEN** la API devuelve HTTP 403

---

### Requirement: Exposición de endpoints REST para incidencias
La API SHALL exponer endpoints para reportar incidencias en puestos y consultar las incidencias propias del usuario.

#### Scenario: Reportar una incidencia
- **WHEN** un usuario autenticado hace POST `/api/v1/incidences` con `dockId` y `description` válidos
- **THEN** la API devuelve HTTP 201 con el `id` de la incidencia creada

#### Scenario: Consultar incidencias propias
- **WHEN** un usuario autenticado hace GET `/api/v1/incidences/mine`
- **THEN** la API devuelve HTTP 200 con la lista de incidencias reportadas por el usuario en sesión

---

### Requirement: Exposición de endpoints REST para perfil de usuario
La API SHALL exponer endpoints para consultar y actualizar el perfil del usuario autenticado.

#### Scenario: Consultar perfil propio
- **WHEN** un usuario autenticado hace GET `/api/v1/users/me`
- **THEN** la API devuelve HTTP 200 con los datos del perfil del usuario (nombre, email, línea de negocio, teléfono, preferencias de notificación)

#### Scenario: Actualizar perfil propio
- **WHEN** un usuario autenticado hace PUT `/api/v1/users/me` con campos editables válidos
- **THEN** la API devuelve HTTP 204

---

### Requirement: Autenticación Bearer JWT en la API
Todos los endpoints de la API SHALL requerir un Bearer token JWT válido emitido por Azure AD.

#### Scenario: Petición sin token
- **WHEN** cualquier cliente hace una petición a cualquier endpoint sin cabecera `Authorization`
- **THEN** la API devuelve HTTP 401

#### Scenario: Petición con token válido
- **WHEN** un cliente presenta un Bearer token JWT válido del tenant de Azure AD
- **THEN** la API procesa la petición y devuelve la respuesta correspondiente

#### Scenario: Petición con token expirado o inválido
- **WHEN** un cliente presenta un Bearer token JWT inválido o expirado
- **THEN** la API devuelve HTTP 401

---

### Requirement: Autorización por roles en endpoints de la API
La API SHALL aplicar autorización basada en roles para los endpoints que requieren privilegios elevados, usando los mismos roles definidos en el dominio (`Manager`, `SystemAdmin`).

#### Scenario: Acceso a endpoint de Manager sin rol
- **WHEN** un usuario autenticado sin rol `Manager` intenta acceder a un endpoint restringido a Manager (por ejemplo, bloquear puestos)
- **THEN** la API devuelve HTTP 403

#### Scenario: Acceso a endpoint de Manager con rol correcto
- **WHEN** un usuario autenticado con rol `Manager` accede a un endpoint de Manager
- **THEN** la API procesa la petición normalmente

---

### Requirement: Stub de autenticación para desarrollo local de la API
La API SHALL soportar un modo de desarrollo que auto-autentica un usuario simulado sin Azure AD, equivalente al `DevAuthHandler` de `EchoBase.Web`.

#### Scenario: Activar stub de desarrollo
- **WHEN** `Authentication:UseDevelopmentStub` es `true` en la configuración y se hace cualquier petición
- **THEN** la API autentica automáticamente el usuario de desarrollo con sus roles configurados, sin validar el token Bearer

#### Scenario: Stub desactivado en producción
- **WHEN** `Authentication:UseDevelopmentStub` es `false`
- **THEN** la API exige Bearer JWT válido de Azure AD para todas las peticiones

---

### Requirement: Documentación OpenAPI de la API
La API SHALL exponer documentación OpenAPI (Swagger) en entornos de desarrollo.

#### Scenario: Acceso a Swagger UI en desarrollo
- **WHEN** la API está ejecutándose en entorno Development y se accede a `/swagger`
- **THEN** se muestra la interfaz Swagger UI con todos los endpoints documentados

#### Scenario: Swagger deshabilitado en producción
- **WHEN** la API está ejecutándose en entorno Production
- **THEN** `/swagger` no está disponible (404)
