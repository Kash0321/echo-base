## ADDED Requirements

### Requirement: Autenticación con Azure AD en la app MAUI
La app MAUI SHALL autenticar a los usuarios con Azure AD mediante MSAL (`Microsoft.Identity.Client`), obteniendo un Bearer token para acceder a `EchoBase.Api`.

#### Scenario: Inicio de sesión exitoso
- **WHEN** el usuario abre la app por primera vez o su sesión ha expirado
- **THEN** la app muestra el flujo de autenticación de Azure AD (browser embebido o sistema) y, tras autenticarse, navega a la pantalla principal

#### Scenario: Token en caché válido
- **WHEN** el usuario abre la app y MSAL tiene un token válido en caché
- **THEN** la app navega directamente a la pantalla principal sin mostrar el flujo de autenticación

#### Scenario: Cierre de sesión
- **WHEN** el usuario cierra sesión desde el perfil
- **THEN** MSAL elimina el token del caché y la app vuelve a la pantalla de inicio de sesión

---

### Requirement: Pantalla de mapa de puestos en MAUI
La app MAUI SHALL mostrar el mapa de puestos de trabajo con su estado para una fecha seleccionable, y permitir reservar un puesto disponible.

#### Scenario: Ver el mapa del día actual
- **WHEN** el usuario navega a la pantalla de mapa
- **THEN** la app carga y muestra el mapa de puestos para la fecha actual, agrupados por zonas, con su estado (libre, parcialmente reservado, completamente reservado, bloqueado)

#### Scenario: Cambiar la fecha del mapa
- **WHEN** el usuario selecciona una fecha diferente dentro del rango permitido (hoy + 7 días)
- **THEN** la app recarga el mapa para la fecha seleccionada

#### Scenario: Reservar un puesto disponible
- **WHEN** el usuario pulsa un puesto libre o parcialmente reservado y selecciona la franja horaria disponible
- **THEN** la app envía la reserva a la API y, si tiene éxito, actualiza el estado del puesto en pantalla y muestra confirmación

#### Scenario: Puesto no disponible
- **WHEN** el usuario intenta reservar un puesto completamente reservado o bloqueado
- **THEN** la app deshabilita la acción de reserva para ese puesto

---

### Requirement: Pantalla de mis reservas en MAUI
La app MAUI SHALL mostrar las reservas del usuario autenticado y permitir cancelarlas.

#### Scenario: Ver reservas propias
- **WHEN** el usuario navega a la pantalla "Mis reservas"
- **THEN** la app muestra la lista de reservas activas y pasadas del usuario, ordenadas por fecha

#### Scenario: Cancelar una reserva futura
- **WHEN** el usuario selecciona una reserva futura y confirma la cancelación
- **THEN** la app envía la cancelación a la API y elimina la reserva de la lista

#### Scenario: No se puede cancelar una reserva pasada
- **WHEN** el usuario ve una reserva con fecha pasada
- **THEN** la opción de cancelar no está disponible para esa reserva

---

### Requirement: Pantalla de perfil de usuario en MAUI
La app MAUI SHALL mostrar y permitir editar los datos de perfil del usuario (línea de negocio, teléfono, preferencias de notificación). Los datos de solo lectura (nombre, email) se muestran pero no son editables.

#### Scenario: Ver perfil
- **WHEN** el usuario navega a la pantalla de perfil
- **THEN** la app muestra los datos del perfil obtenidos de `GET /api/v1/users/me`

#### Scenario: Editar perfil
- **WHEN** el usuario modifica un campo editable y guarda
- **THEN** la app envía los cambios a `PUT /api/v1/users/me` y muestra confirmación de éxito

---

### Requirement: Pantalla de reporte de incidencias en MAUI
La app MAUI SHALL permitir al usuario reportar una incidencia en un puesto de trabajo y ver sus incidencias anteriores.

#### Scenario: Reportar una incidencia
- **WHEN** el usuario selecciona un puesto en el mapa y elige "Reportar incidencia", escribe la descripción y confirma
- **THEN** la app envía la incidencia a `POST /api/v1/incidences` y muestra confirmación

#### Scenario: Ver incidencias propias
- **WHEN** el usuario navega a la sección de incidencias
- **THEN** la app muestra la lista de incidencias reportadas por el usuario con su estado actual

---

### Requirement: Soporte multi-plataforma de la app MAUI
La app MAUI SHALL compilar y ejecutarse correctamente en Windows, Android (teléfono y tablet) e iOS (teléfono y tablet), adaptando la navegación y el layout al tamaño de pantalla disponible.

#### Scenario: Ejecución en Windows
- **WHEN** la app se ejecuta en Windows 10/11
- **THEN** todas las pantallas son funcionales y el layout aprovecha el espacio de pantalla de escritorio

#### Scenario: Ejecución en Android
- **WHEN** la app se ejecuta en un dispositivo Android (phone o tablet)
- **THEN** todas las pantallas son funcionales, la navegación es táctil-friendly y el layout se adapta al tamaño de pantalla

#### Scenario: Ejecución en iOS
- **WHEN** la app se ejecuta en un dispositivo iOS (iPhone o iPad)
- **THEN** todas las pantallas son funcionales, la navegación sigue las convenciones de iOS y el layout se adapta al tamaño de pantalla

---

### Requirement: Manejo de errores de red en MAUI
La app MAUI SHALL informar al usuario cuando no hay conexión a la API o se produce un error de red, sin crashear.

#### Scenario: Sin conexión de red
- **WHEN** el usuario intenta cargar datos sin conexión
- **THEN** la app muestra un mensaje de error claro indicando que no hay conexión y ofrece la opción de reintentar

#### Scenario: Error del servidor (5xx)
- **WHEN** la API devuelve un error 5xx
- **THEN** la app muestra un mensaje de error genérico sin exponer detalles técnicos
