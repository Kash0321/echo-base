# Spec: Sistema de Reserva de Puestos (Hot Desking)

## Objetivo
App interna para que 70 empleados reserven 24 puestos físicos, permitiendo cada puesto dos franjas horarias, mañana o tarde.

## Stack técnico
- Framework: .NET 10
- Frontend: Blazor Web App (Interactive Server/WebAssembly)
- Estilo: Tailwind CSS o MudBlazor (UI Components)
- Persistencia: Entity Framework Core con SQLite (Local) / Azure SQL (Prod)

## Convenciones de implementación
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

## Diseño y UX (UI Guidelines)
Para mantener coherencia en el rediseño y ampliaciones futuras de Echo Base, se establecieron las siguientes bases de UX/UI en el desarrollo interactivo de `Home.razor`, `DockMap.razor` y `MyReservations.razor`:

1. **Cabeceras de página (`eb-page-header`, `eb-hero`)**
   - Dominadas por un gradiente espacial/táctico oscuro: `linear-gradient(135deg, #0d1b2a 0%, #1a3a5c 60%, #0d1b2a 100%)`.
   - Textos en blanco para máximo contraste, integrando subtítulos ligeros tipo "eyebrow" (letras en mayúscula, pequeñas, con tracking/letter-spacing aumentado).
   - En vistas operativas (DockMap), la cabecera acomoda controles compactos (selector de fecha transparente y leyenda simplificada con `span` circulares) para maximizar el espacio vertical útil (above the fold).

2. **Densidad y Espaciado (Desktop/Mobile)**
   - Elementos *compactos* (`py-2`, `mb-2`, `p-3`) en lugar del espaciado excesivo de Bootstrap por defecto. El objetivo de la interfaz es evitar el scroll vertical o minimizarlo, para que los usuarios vean el contexto visual (ej. el mapa de bahías interactivo) sin tener que desplazarse demasiado.
   - Botones sutiles e inputs con clases como `form-control-sm` o `btn-sm` para interacciones recurrentes.

3. **Uso de Iconografía (Bootstrap Icons)**
   - Iconos integrados sistemáticamente en los títulos de secciones/vistas y en acciones principales (`bi-*` junto a textos como reservas confirmadas, botones de cancelación con *spinners* o símbolos).
   - Modalidades de alerta: uso de alertas (`.alert`) acompañadas de íconos in-line y flexbox (`d-flex align-items-center gap-2`) para feedback de éxito o advertencia en vez de simples bloques de texto monótonos.

4. **Componentización Visual**
   - **Tarjetas sin borde y elevación sutil**: Uso de `.card.border-0.shadow-sm`.
   - **Hover effects táctiles**: Aplicados en tarjetas informativas (reservas futuras) y los puestos del mapa (`.eb-reservation-card:hover`, `.eb-dock-seat:not(:disabled):hover`) usando `transform: translateY(-2px); box-shadow: ...`.
   - **Interacciones enriquecidas (Formularios/Modales)**: Uso moderno de entradas, por ejemplo, convirtiendo opciones en botones (`.btn-check` + `.btn-outline-primary`) en lugar de radio buttons clásicos para mejorar las áreas táctiles en móviles y darle un look moderno.
   - **Datos históricos**: Manejo diferenciado visualmente. Los elementos cancelados o pasados bajan su opacidad y tienen efecto tachado sobre los listados.

## Modelo de datos
1. **User** (Empleado que reserva espacio): Id, Nombre, Email, Línea de negocio (Core, Energía, Scrap/Waste, Transversal).
2. **Dock** (Puesto de trabajo): Id, Código (ej: A-01), Ubicación, Equipamiento (Monitor doble, etc.).
3. **Reservation** (Reserva de espacio de trabajo): Id, UserId, DockId, Fecha (Solo fecha, sin hora), Estado (Activa, Cancelada).
4. **BlockedDock** (Puestos bloqueados por administración): Id, DockId, FechaInicio, FechaFin, Motivo.
5. **UserPreferences** (Preferencias de notificación): Id, UserId, NotificacionEmail (bool), NotificacionTeams (bool).
6. **IncidenceReport** (Reporte de incidencias en los puestos de trabajo): Id, UserId, DockId, Fecha, Descripción, Estado (Abierta, En Proceso, Resuelta).
7. **AuditLog** (Registro de auditoría para acciones críticas): Id, UserId, Acción (Reserva, Cancelación, Bloqueo), Detalles, Timestamp.
8. **Report** (Reportes de uso y estadísticas): Id, Tipo (Ocupación, Cancelaciones, Incidencias), Periodo (Diario, Semanal, Mensual), Datos (JSON).
9. **Notification** (Notificaciones para usuarios): Id, UserId, Tipo (ReservaConfirmada, ReservaCancelada, Recordatorio), Contenido, Leída (bool), Timestamp.
10. **Role** (Roles de usuario para autorización): Id, Nombre (BasicUser, Manager).
11. **UserRole** (Relación entre usuarios y roles): Id, UserId, RoleId.
12. **DockEquipment** (Equipamiento específico de cada puesto): Id, DockId, Tipo (MonitorDoble, SillaErgonómica, etc.), Descripción.
13. **DockZone** (Zona de los puestos de trabajo): Id, Nombre (Nostromo, Derelict), Descripción.
14. **DockZoneAssignment** (Asignación de puestos a zonas): Id, DockId, DockZoneId.
15. **ReservationHistory** (Historial de reservas para auditoría y análisis): Id, ReservationId, UserId, DockId, Fecha, Acción (Creada, Modificada, Cancelada), Timestamp.
16. **IncidenceHistory** (Historial de incidencias para seguimiento): Id, IncidenceReportId, UserId, DockId, Fecha, Acción (Reportada, En Proceso, Resuelta), Timestamp.
17. **ReportData** (Datos específicos para reportes personalizados): Id, ReportId, Clave, Valor.
18. **BlockedDockHistory** (Historial de bloqueos de puestos de trabajo): Id, BlockedDockId, UserId, Acción (Bloqueado, Desbloqueado), Timestamp.


## Modelo de autenticación y autorización
- **Autenticación**: Azure AD (Single Sign-On)
- **Roles/Claims**: BasicUser (puede reservar), Manager (puede reservar de modo normal y bloquear puestos)

Integración con Azure AD (tennant de nuestra compañía) para autenticación y autorización basada en roles. Los usuarios con rol "Manager" tendrán acceso a funcionalidades adicionales para bloquear puestos de trabajo.

## Reglas de negocio generales
- Un usuario solo puede reservar 1 puesto por día, y puede indicar la franja o franjas horarias en la que va a usar el puesto (mañanas hasta las 14 y tardes de 14 fin de jornada), de modo que otro empleado pueda reservar el puesto en una franja horaria diferente el mismo día si está disponible, pero un empleado puede reservar como máximo dos franjas horarias en el mismo puesto de trabajo o en dos puestos de trabajo distintos.
- Las reservas se abren con 7 días de antelación.
- Capacidad máxima: 24 puestos de trabajo
- Interfaz visual: Mapa de puestos de trabajo, agrupados en dos zonas: Nostromo tiene 12 puestos de trabajo en una mesa corrida con 6 puestos a cada lado, Derelict tiene 12 puestos de trabajo, en dos mesas corridas con 3 puestos a cada lado en cada una de las mesas
- Existirá un cuadro de mando para usuarios con privilegios de administración, que permitirá bloquear varios puestos de trabajo en un día o un período de días más largo, lo que bloqueará (impedirá reservar) esos puestos de trabajo para su reserva al resto de usuarios con privilegios normales.

## Funcionalidad 0: Gestión cuentas de usuario
- El sistema se integra con Azure AD para la autenticación de usuarios.
- Los usuarios se asignan automáticamente al rol "BasicUser" al iniciar sesión por primera vez.
- Un administrador puede asignar el rol "Manager" a usuarios específicos desde el portal de Azure AD, lo que les otorga privilegios adicionales para gestionar reservas y bloquear puestos de trabajo.

## Funcionalidad 1: Configuración de cuenta de usuario
- El usuario inicia sesión en la aplicación utilizando su cuenta de Azure AD.
- El usuario accede a su perfil de usuario, donde puede configurar sus preferencias de notificación (correo electrónico o chat de Microsoft Teams) para recibir confirmaciones de reservas, cancelaciones y recordatorios.
- El usuario puede actualizar su información de contacto, como número de teléfono o dirección de correo electrónico, desde su perfil de usuario.

## Funcionalidad 2: Reserva de puesto de trabajo
- El usuario inicia sesión en la aplicación utilizando su cuenta de Azure AD.
- El usuario ve un mapa visual de los 24 puestos de trabajo, agrupados en dos zonas: Nostromo (12 puestos) y Derelict (12 puestos).
- El usuario selecciona un puesto de trabajo disponible para la fecha deseada.
- El usuario indica la franja horaria (mañana, tarde o ambas) para su reserva.
- El sistema valida que el usuario no tenga otra reserva para ese día y que el puesto esté disponible en la franja horaria seleccionada.
- El sistema confirma la reserva y muestra un resumen de la misma. El usuario puede cancelar la reserva desde el mismo resumen. El usuario recibe una notificación por correo electrónico o chat de Microsoft Teams (según configuración) confirmando la reserva.

## Funcionalidad 3: Cuadro de mando para administración
- Un usuario con rol de Manager inicia sesión en la aplicación.
- El Manager accede a un cuadro de mando que muestra el mapa de puestos de trabajo con la capacidad de seleccionar uno o varios puestos de trabajo.
- El Manager selecciona los puestos de trabajo que desea bloquear para un día específico o un período de días.
- El sistema bloquea los puestos de trabajo seleccionados, impidiendo que los usuarios con rol BasicUser puedan reservar esos puestos para las fechas bloqueadas.
- El Manager puede desbloquear los puestos de trabajo bloqueados desde el mismo cuadro de mando. 

## Funcionalidad 4: Gestión de reservas
- El usuario puede ver un historial de sus reservas pasadas y futuras.
- El usuario puede cancelar una reserva activa desde su historial en cualquier momento, sin restricción de antelación. Las cancelaciones de última hora son bienvenidas, ya que liberan el puesto para otros compañeros.
- El sistema envía notificaciones por correo electrónico al usuario para confirmar la creación, modificación o cancelación de una reserva.
- El sistema envía recordatorios automáticos a los usuarios sobre sus reservas próximas, con opciones para modificar o cancelar la reserva directamente desde la notificación.

## Funcionalidad 5: Reportes y estadísticas
- El Manager puede acceder a un panel de reportes que muestra estadísticas de uso de los puestos de trabajo, como el porcentaje de ocupación por día, semana o mes.
- El Manager puede exportar los datos de reservas en formato CSV para análisis adicionales.
- El sistema genera alertas automáticas para el Manager si se detecta un patrón de reservas inusuales, como un aumento repentino en la demanda de ciertos puestos de trabajo o una alta tasa de cancelaciones.

## Funcionalidad 6: Reporte de incidencias en los puestos de trabajo
- El usuario puede reportar incidencias relacionadas con los puestos de trabajo (por ejemplo, problemas de equipamiento o limpieza) a través de la aplicación.
- El usuario selecciona el puesto de trabajo afectado y describe la incidencia en un formulario.
- El sistema registra la incidencia y notifica a los usuarios con rol de Manager para que puedan tomar medidas correctivas. El usuario recibe una confirmación de que su reporte ha sido registrado y se le informa sobre el proceso de seguimiento de la incidencia.
- El usuario puede hacer seguimiento del estado de su reporte de incidencia desde su perfil de usuario, recibiendo notificaciones sobre el progreso y la resolución de la incidencia.
