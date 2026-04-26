# 🎨 Diseño y UX (UI Guidelines)
Para mantener coherencia en el rediseño y ampliaciones futuras de Echo Base, se establecen las siguientes bases de UX/UI para el desarrollo de la aplicación Blazor Server. Estas pautas se aplican a todos los componentes y vistas, asegurando una experiencia de usuario consistente, moderna y alineada con la identidad visual de la marca.

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