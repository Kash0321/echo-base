# 🛰️ Proyecto Echo-Base: Strategic Docking Coordinator

[![Platform: .NET 10](https://img.shields.io/badge/.NET-10.0-blueviolet)](https://dotnet.microsoft.com/)
[![Framework: Blazor](https://img.shields.io/badge/UI-Blazor-blue)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![AI: GitHub Copilot Pro](https://img.shields.io/badge/AI-Copilot_Pro-brightgreen)](https://github.com/features/copilot)

**Status:** Orbital (Satellite App)

**Misión:** Coordinar múltiples unidades rebeldes a través de un conjunto limitado de bahías de atraque.

**Tech ground**
- **Command Center:** .NET 10 / Blazor Web App
- **Logic:** Spec-Driven Development (SDD)
- **Data Core:** Entity Framework Core (Current: SQLite / Target: Azure SQL)
- **AI Navigator:** GitHub Copilot Pro

**Echo-Base** es una aplicación satelital diseñada para gestionar la logística de puestos de trabajo en un entorno de modelo híbrido. Su misión principal es coordinar el acceso a recursos físicos limitados mediante un flujo de trabajo de **Spec-Driven Development (SDD)** asistido por Inteligencia Artificial.

## 🌌 ¿Por qué "Echo-Base"?

El nombre no es solo una referencia a Star Wars; es una metáfora de nuestra realidad operativa:

1.  **El Concepto de Base:** Al igual que la base rebelde en el planeta Hoth, nuestra oficina ya no es un asentamiento permanente para todos, sino un punto de encuentro táctico. Los empleados operan de forma remota ("en la galaxia") y acuden a la base para misiones específicas de colaboración.
2.  **Las Docking Bays:** Cada uno de los **puestos disponibles** se trata como una "bahía de atraque" (*docking station*). La aplicación garantiza que ninguna "nave" (empleado) intente aterrizar sin una bahía asignada.
3.  **El Desafío Logístico:** Aplicamos el **Principio del Palomar**: gestionar múltiples unidades para un conjunto limitado de espacios requiere una coordinación de precisión quirúrgica para evitar colisiones.

## 🛠️ Stack Tecnológico

| Componente | Tecnología |
| :--- | :--- |
| **Runtime** | .NET 10 (Standard Support) |
| **Frontend** | Blazor Web App (Interactive Mode) |
| **Persistencia** | EF Core + SQLite (Dev) / Azure SQL (Prod) |
| **Arquitectura** | Layered Clean Architecture (Core, Infra, Web) |
| **AI Navigator** | GitHub Copilot Pro (GPT-4o / Claude 3.5 Sonnet) |

## 🏗️ Estructura del Proyecto

El repositorio sigue una separación de intereses estricta para facilitar la mantenibilidad y la escalabilidad:

*   `src/EchoBase.Core`: Entidades de dominio y reglas de negocio puras.
*   `src/EchoBase.Infrastructure`: Implementación de datos y acceso a servicios externos.
*   `src/EchoBase.Web`: Interfaz de usuario y endpoints.
*   `tests/EchoBase.Tests.Unit`: Tests unitarios para lógica de negocio y componentes individuales.
*   `tests/EchoBase.Tests.Integration`: Tests de integración para verificar la interacción entre componentes.
*   `docs/`: Documentación técnica y archivos `spec.md` para el contexto de la IA.

## 🚀 Guía de Inicio Rápido

### Requisitos previos
*   SDK de .NET 10 instalado (`winget install Microsoft.DotNet.SDK.10`).
*   VS Code con la extensión de GitHub Copilot Pro.

### Instalación
1. Clonar el repositorio:
   ```bash
   git clone [https://github.com/tu-usuario/echo-base.git](https://github.com/tu-usuario/echo-base.git)
   cd echo-base
   ```

2. Restaurar dependencias:
   ```bash
   dotnet restore
   ```

3. Ejecutar la aplicación en modo desarrollo (autenticación simulada, SQLite local):
   ```bash
   dotnet run --project src/EchoBase.Web
   ```

La aplicación arranca automáticamente en modo desarrollo con:
- **Autenticación simulada** (`DevAuth`): no requiere Azure AD. El usuario de desarrollo tiene rol **Manager** por defecto (configurable).
- **Base de datos SQLite local** (`echobase-dev.db`): se crea y migra automáticamente al arrancar.
- **Notificaciones stub**: los envíos de email y Teams se simulan con logs en consola.

---

## 🔐 Configuración y Gestión de Secretos

### Principio general

Los ficheros `appsettings.json` y `appsettings.Development.json` del repositorio **no deben contener nunca valores reales** de contraseñas, clientes de Azure AD ni secretos de API. Solo contienen placeholders (`YOUR_*`) o stubs para desarrollo.

Los valores sensibles deben gestionarse según el entorno:

| Entorno | Mecanismo recomendado |
| :--- | :--- |
| **Desarrollo local** | [.NET User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) |
| **Staging / Producción** | Variables de entorno o [Azure Key Vault](https://learn.microsoft.com/azure/key-vault/general/overview) |

---

### Entorno de desarrollo — .NET User Secrets

Los User Secrets se almacenan fuera del directorio del repositorio, en:
```
%APPDATA%\Microsoft\UserSecrets\echobase-web-devenv-001\secrets.json
```
Git nunca los ve. El `UserSecretsId` está definido en `src/EchoBase.Web/EchoBase.Web.csproj`.

#### Configuración automática con el script incluido

El repositorio incluye un script PowerShell para establecer todos los secretos en un solo paso:

```powershell
# 1. Abre el script y rellena los valores YOUR_* con los reales
notepad scripts\setup-dev-secrets.ps1

# 2. Ejecuta desde la raíz del repositorio (requiere PowerShell 7+)
.\scripts\setup-dev-secrets.ps1

# 3. Verifica que se han guardado correctamente
dotnet user-secrets list --project src\EchoBase.Web
```

#### Configuración manual clave a clave

Si prefieres hacerlo a mano, estos son todos los secretos necesarios:

```powershell
# ── Azure AD ──────────────────────────────────────────────────────
# Obtener desde: portal.azure.com → Azure Active Directory → App Registrations
dotnet user-secrets set "AzureAd:Domain"   "contoso.onmicrosoft.com"    --project src\EchoBase.Web
dotnet user-secrets set "AzureAd:TenantId" "<GUID-del-tenant>"           --project src\EchoBase.Web
dotnet user-secrets set "AzureAd:ClientId" "<GUID-del-app-registration>" --project src\EchoBase.Web

# ── SMTP (Office 365) ─────────────────────────────────────────────
# Usar una App Password de M365; nunca la contraseña personal
dotnet user-secrets set "Smtp:UserName"    "notificaciones@contoso.com" --project src\EchoBase.Web
dotnet user-secrets set "Smtp:Password"    "<app-password>"             --project src\EchoBase.Web
dotnet user-secrets set "Smtp:FromAddress" "notificaciones@contoso.com" --project src\EchoBase.Web

# ── Microsoft Graph (notificaciones Teams) ────────────────────────
# Requiere permisos: Chat.Create, ChatMessage.Send (Application permissions)
dotnet user-secrets set "MicrosoftGraph:TenantId"     "<GUID-del-tenant>"       --project src\EchoBase.Web
dotnet user-secrets set "MicrosoftGraph:ClientId"     "<GUID-del-app-graph>"    --project src\EchoBase.Web
dotnet user-secrets set "MicrosoftGraph:ClientSecret" "<valor-del-secreto>"     --project src\EchoBase.Web
```

#### Limpiar todos los secretos

```powershell
dotnet user-secrets clear --project src\EchoBase.Web
```

---

### Modos de autenticación

El modo de autenticación se controla mediante `appsettings.Development.json`:

```json
"Authentication": {
  "UseDevelopmentStub": true,    // true → DevAuth sin Azure AD; false → Azure AD real
  "DevUserIsManager": true,      // true → el usuario dev tiene rol Manager,
  "DevUserIsSystemAdmin": true   // true → el usuario dev también tiene rol SystemAdmin
}
```

| `UseDevelopmentStub` | Comportamiento |
| :--- | :--- |
| `true` | Autenticación simulada (`DevAuth`). No requiere Azure AD. Válido para desarrollo local sin acceso al tenant. |
| `false` | Autenticación real con Azure AD (OpenID Connect). Requiere `AzureAd:*` configurados vía User Secrets. |

> **Nota:** Mientras no esté disponible la configuración del tenant de Azure AD, mantén `UseDevelopmentStub: true`. El usuario simulado tiene un ID determinístico (`00000000-0000-0000-0000-000000000001`) y se crea automáticamente en la base de datos con los roles configurados (`DevUserIsManager`, `DevUserIsSystemAdmin`).

---

### Entorno de producción — Variables de entorno / Azure Key Vault

En producción (Azure App Service, Container Apps, etc.), las mismas claves se pueden pasar como **variables de entorno** usando el separador `__` (doble guion bajo) en lugar de `:`:

```
AzureAd__TenantId=<valor>
AzureAd__ClientId=<valor>
Smtp__Password=<valor>
MicrosoftGraph__ClientSecret=<valor>
```

Para un enfoque más seguro, se recomienda integrar **Azure Key Vault**:
1. Crear un Key Vault en el mismo tenant.
2. Añadir el paquete `Azure.Extensions.AspNetCore.Configuration.Secrets` al proyecto Web.
3. Registrar el Key Vault en `Program.cs` usando la identidad gestionada del App Service (sin secretos en código).

Consulta la [documentación oficial de Azure Key Vault con ASP.NET Core](https://learn.microsoft.com/azure/key-vault/general/tutorial-net-create-vault-azure-web-app) para la integración completa.

---

### Resumen de claves de configuración sensibles

| Clave | Descripción | Obligatoria en prod |
| :--- | :--- | :---: |
| `AzureAd:Domain` | Dominio del tenant (`contoso.onmicrosoft.com`) | ✅ |
| `AzureAd:TenantId` | GUID del tenant de Azure AD | ✅ |
| `AzureAd:ClientId` | GUID del App Registration de la aplicación | ✅ |
| `Smtp:UserName` | Cuenta de correo para envío de notificaciones | ✅ |
| `Smtp:Password` | App Password de la cuenta de correo | ✅ |
| `Smtp:FromAddress` | Dirección de remitente del correo | ✅ |
| `MicrosoftGraph:TenantId` | GUID del tenant (coincide con `AzureAd:TenantId`) | Solo si Teams activo |
| `MicrosoftGraph:ClientId` | GUID del App Registration con permisos Graph | Solo si Teams activo |
| `MicrosoftGraph:ClientSecret` | Secreto del App Registration con permisos Graph | Solo si Teams activo |
| `ConnectionStrings:EchoBase` | Cadena de conexión a la base de datos | ✅ |

---

## 📏 Reporte LoC por commit

Esta sección mantiene un historico del LoC con el formato acordado, separando:
- Codigo de aplicacion
- Codigo de pruebas

Definicion de conteo usada por script:
- Lineas no vacias de archivos fuente
- Excluye `bin`, `obj`, `Migrations`, `wwwroot/lib`, `*.min.*`, `*.Designer.cs`, `*.g.cs`, `*.g.i.cs`

### Generar y registrar el reporte (manual)

Desde la raiz del repositorio:

```powershell
pwsh -File .\scripts\update-loc-report.ps1 -CommitRef HEAD
```

### Automatizar en cada commit

Instala el hook de Git una sola vez:

```powershell
pwsh -File .\scripts\install-loc-post-commit-hook.ps1
```

El hook ejecuta el calculo tras cada commit y enmienda el mismo commit para incluir la actualizacion de `README.md` con el nuevo bloque LoC.

### Historico

<!-- LOC_REPORT_HISTORY_START -->

### 2026-04-04 | commit a037e85

Resumen

| Categoria | Ficheros | LoC | % sobre total |
|---|---:|---:|---:|
| Codigo de aplicacion | 140 | 11.551 | 70,0% |
| Codigo de pruebas | 35 | 4.939 | 30,0% |
| Total | 175 | 16.490 | 100% |

Desglose de codigo de aplicacion

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Core | 74 | 3.470 |
| EchoBase.Infrastructure | 33 | 2.095 |
| EchoBase.Web | 33 | 5.986 |

Desglose de pruebas

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Tests.Integration | 13 | 1.364 |
| EchoBase.Tests.Unit | 22 | 3.575 |

### 2026-04-03 | commit 89072f2

Resumen

| Categoria | Ficheros | LoC | % sobre total |
|---|---:|---:|---:|
| Codigo de aplicacion | 140 | 11.549 | 70,0% |
| Codigo de pruebas | 35 | 4.939 | 30,0% |
| Total | 175 | 16.488 | 100% |

Desglose de codigo de aplicacion

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Core | 74 | 3.459 |
| EchoBase.Infrastructure | 33 | 2.095 |
| EchoBase.Web | 33 | 5.995 |

Desglose de pruebas

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Tests.Integration | 13 | 1.364 |
| EchoBase.Tests.Unit | 22 | 3.575 |

### 2026-04-03 | commit 7c48247

Resumen

| Categoria | Ficheros | LoC | % sobre total |
|---|---:|---:|---:|
| Codigo de aplicacion | 140 | 11.496 | 69,9% |
| Codigo de pruebas | 35 | 4.939 | 30,1% |
| Total | 175 | 16.435 | 100% |

Desglose de codigo de aplicacion

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Core | 74 | 3.459 |
| EchoBase.Infrastructure | 33 | 2.095 |
| EchoBase.Web | 33 | 5.942 |

Desglose de pruebas

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Tests.Integration | 13 | 1.364 |
| EchoBase.Tests.Unit | 22 | 3.575 |

### 2026-04-03 | commit 933e462

Resumen

| Categoria | Ficheros | LoC | % sobre total |
|---|---:|---:|---:|
| Codigo de aplicacion | 127 | 10.226 | 68,9% |
| Codigo de pruebas | 33 | 4.616 | 31,1% |
| Total | 160 | 14.842 | 100% |

Desglose de codigo de aplicacion

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Core | 65 | 3.064 |
| EchoBase.Infrastructure | 31 | 1.908 |
| EchoBase.Web | 31 | 5.254 |

Desglose de pruebas

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Tests.Integration | 12 | 1.223 |
| EchoBase.Tests.Unit | 21 | 3.393 |

### 2026-04-03 | commit ef959a5

Resumen

| Categoria | Ficheros | LoC | % sobre total |
|---|---:|---:|---:|
| Codigo de aplicacion | 127 | 10.203 | 68,9% |
| Codigo de pruebas | 33 | 4.616 | 31,1% |
| Total | 160 | 14.819 | 100% |

Desglose de codigo de aplicacion

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Core | 65 | 3.064 |
| EchoBase.Infrastructure | 31 | 1.884 |
| EchoBase.Web | 31 | 5.255 |

Desglose de pruebas

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Tests.Integration | 12 | 1.223 |
| EchoBase.Tests.Unit | 21 | 3.393 |

### 2026-04-03 | commit c8d0ff9

Resumen

| Categoria | Ficheros | LoC | % sobre total |
|---|---:|---:|---:|
| Codigo de aplicacion | 127 | 10.203 | 68,9% |
| Codigo de pruebas | 33 | 4.616 | 31,1% |
| Total | 160 | 14.819 | 100% |

Desglose de codigo de aplicacion

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Core | 65 | 3.064 |
| EchoBase.Infrastructure | 31 | 1.884 |
| EchoBase.Web | 31 | 5.255 |

Desglose de pruebas

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Tests.Integration | 12 | 1.223 |
| EchoBase.Tests.Unit | 21 | 3.393 |

### 2026-04-03 | commit 0095fd2

Resumen

| Categoria | Ficheros | LoC | % sobre total |
|---|---:|---:|---:|
| Codigo de aplicacion | 127 | 10.208 | 68,9% |
| Codigo de pruebas | 33 | 4.616 | 31,1% |
| Total | 160 | 14.824 | 100% |

Desglose de codigo de aplicacion

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Core | 65 | 3.062 |
| EchoBase.Infrastructure | 31 | 1.883 |
| EchoBase.Web | 31 | 5.263 |

Desglose de pruebas

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Tests.Integration | 12 | 1.223 |
| EchoBase.Tests.Unit | 21 | 3.393 |

### 2026-04-03 | commit dcdb2fa

Resumen

| Categoria | Ficheros | LoC | % sobre total |
|---|---:|---:|---:|
| Codigo de aplicacion | 127 | 10.208 | 68,9% |
| Codigo de pruebas | 33 | 4.616 | 31,1% |
| Total | 160 | 14.824 | 100% |

Desglose de codigo de aplicacion

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Core | 65 | 3.062 |
| EchoBase.Infrastructure | 31 | 1.883 |
| EchoBase.Web | 31 | 5.263 |

Desglose de pruebas

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Tests.Integration | 12 | 1.223 |
| EchoBase.Tests.Unit | 21 | 3.393 |

### 2026-04-03 | commit 8f0b837

Resumen

| Categoria | Ficheros | LoC | % sobre total |
|---|---:|---:|---:|
| Codigo de aplicacion | 127 | 10.208 | 68,9% |
| Codigo de pruebas | 33 | 4.616 | 31,1% |
| Total | 160 | 14.824 | 100% |

Desglose de codigo de aplicacion

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Core | 65 | 3.062 |
| EchoBase.Infrastructure | 31 | 1.883 |
| EchoBase.Web | 31 | 5.263 |

Desglose de pruebas

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Tests.Integration | 12 | 1.223 |
| EchoBase.Tests.Unit | 21 | 3.393 |

### 2026-04-03 | commit 6bbaa2f

Resumen

| Categoria | Ficheros | LoC | % sobre total |
|---|---:|---:|---:|
| Codigo de aplicacion | 127 | 10.208 | 68,9% |
| Codigo de pruebas | 33 | 4.616 | 31,1% |
| Total | 160 | 14.824 | 100% |

Desglose de codigo de aplicacion

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Core | 65 | 3.062 |
| EchoBase.Infrastructure | 31 | 1.883 |
| EchoBase.Web | 31 | 5.263 |

Desglose de pruebas

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Tests.Integration | 12 | 1.223 |
| EchoBase.Tests.Unit | 21 | 3.393 |

### 2026-04-03 | commit 5743c7a

Resumen

| Categoria | Ficheros | LoC | % sobre total |
|---|---:|---:|---:|
| Codigo de aplicacion | 124 | 9.958 | 69,1% |
| Codigo de pruebas | 33 | 4.457 | 30,9% |
| Total | 157 | 14.415 | 100% |

Desglose de codigo de aplicacion

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Core | 62 | 2.959 |
| EchoBase.Infrastructure | 31 | 1.841 |
| EchoBase.Web | 31 | 5.158 |

Desglose de pruebas

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Tests.Integration | 12 | 1.153 |
| EchoBase.Tests.Unit | 21 | 3.304 |

### 2026-04-03 | commit 61a0b34

Resumen

| Categoria | Ficheros | LoC | % sobre total |
|---|---:|---:|---:|
| Codigo de aplicacion | 124 | 9.911 | 69,0% |
| Codigo de pruebas | 33 | 4.457 | 31,0% |
| Total | 157 | 14.368 | 100% |

Desglose de codigo de aplicacion

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Core | 62 | 2.959 |
| EchoBase.Infrastructure | 31 | 1.841 |
| EchoBase.Web | 31 | 5.111 |

Desglose de pruebas

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Tests.Integration | 12 | 1.153 |
| EchoBase.Tests.Unit | 21 | 3.304 |

### 2026-04-02 | commit 8f437b4

Resumen

| Categoria | Ficheros | LoC | % sobre total |
|---|---:|---:|---:|
| Codigo de aplicacion | 124 | 9.889 | 68,9% |
| Codigo de pruebas | 33 | 4.457 | 31,1% |
| Total | 157 | 14.346 | 100% |

Desglose de codigo de aplicacion

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Core | 62 | 2.959 |
| EchoBase.Infrastructure | 31 | 1.841 |
| EchoBase.Web | 31 | 5.089 |

Desglose de pruebas

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Tests.Integration | 12 | 1.153 |
| EchoBase.Tests.Unit | 21 | 3.304 |

### 2026-04-02 | commit 1ae8359

Resumen

| Categoria | Ficheros | LoC | % sobre total |
|---|---:|---:|---:|
| Codigo de aplicacion | 111 | 8.231 | 69,4% |
| Codigo de pruebas | 31 | 3.636 | 30,6% |
| Total | 142 | 11.867 | 100% |

Desglose de codigo de aplicacion

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Core | 50 | 2.256 |
| EchoBase.Infrastructure | 30 | 1.691 |
| EchoBase.Web | 31 | 4.284 |

Desglose de pruebas

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Tests.Integration | 11 | 785 |
| EchoBase.Tests.Unit | 20 | 2.851 |

### 2026-04-02 | commit 507a798

Resumen

| Categoria | Ficheros | LoC | % sobre total |
|---|---:|---:|---:|
| Codigo de aplicacion | 111 | 8.231 | 69,4% |
| Codigo de pruebas | 31 | 3.636 | 30,6% |
| Total | 142 | 11.867 | 100% |

Desglose de codigo de aplicacion

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Core | 50 | 2.256 |
| EchoBase.Infrastructure | 30 | 1.691 |
| EchoBase.Web | 31 | 4.284 |

Desglose de pruebas

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Tests.Integration | 11 | 785 |
| EchoBase.Tests.Unit | 20 | 2.851 |

### 2026-04-02 | commit 248ec9d

Resumen

| Categoria | Ficheros | LoC | % sobre total |
|---|---:|---:|---:|
| Codigo de aplicacion | 111 | 8.217 | 69,3% |
| Codigo de pruebas | 31 | 3.636 | 30,7% |
| Total | 142 | 11.853 | 100% |

Desglose de codigo de aplicacion

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Core | 50 | 2.256 |
| EchoBase.Infrastructure | 30 | 1.691 |
| EchoBase.Web | 31 | 4.270 |

Desglose de pruebas

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Tests.Integration | 11 | 785 |
| EchoBase.Tests.Unit | 20 | 2.851 |

### 2026-04-02 | commit b4581b5

Resumen

| Categoria | Ficheros | LoC | % sobre total |
|---|---:|---:|---:|
| Codigo de aplicacion | 108 | 8.029 | 69,4% |
| Codigo de pruebas | 30 | 3.536 | 30,6% |
| Total | 138 | 11.565 | 100% |

Desglose de codigo de aplicacion

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Core | 48 | 2.187 |
| EchoBase.Infrastructure | 29 | 1.607 |
| EchoBase.Web | 31 | 4.235 |

Desglose de pruebas

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Tests.Integration | 10 | 738 |
| EchoBase.Tests.Unit | 20 | 2.798 |

### 2026-04-02 | commit f800edc

Resumen

| Categoria | Ficheros | LoC | % sobre total |
|---|---:|---:|---:|
| Codigo de aplicacion | 108 | 7.958 | 69,2% |
| Codigo de pruebas | 30 | 3.536 | 30,8% |
| Total | 138 | 11.494 | 100% |

Desglose de codigo de aplicacion

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Core | 48 | 2.187 |
| EchoBase.Infrastructure | 29 | 1.607 |
| EchoBase.Web | 31 | 4.164 |

Desglose de pruebas

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Tests.Integration | 10 | 738 |
| EchoBase.Tests.Unit | 20 | 2.798 |

### 2026-04-01 | commit 2444a83

Resumen

| Categoria | Ficheros | LoC | % sobre total |
|---|---:|---:|---:|
| Codigo de aplicacion | 108 | 7.958 | 69,2% |
| Codigo de pruebas | 30 | 3.536 | 30,8% |
| Total | 138 | 11.494 | 100% |

Desglose de codigo de aplicacion

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Core | 48 | 2.187 |
| EchoBase.Infrastructure | 29 | 1.607 |
| EchoBase.Web | 31 | 4.164 |

Desglose de pruebas

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Tests.Integration | 10 | 738 |
| EchoBase.Tests.Unit | 20 | 2.798 |

### 2026-04-01 | commit 600fd84

Resumen

| Categoria | Ficheros | LoC | % sobre total |
|---|---:|---:|---:|
| Codigo de aplicacion | 107 | 7.903 | 69,3% |
| Codigo de pruebas | 30 | 3.499 | 30,7% |
| Total | 137 | 11.402 | 100% |

Desglose de codigo de aplicacion

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Core | 48 | 2.187 |
| EchoBase.Infrastructure | 28 | 1.552 |
| EchoBase.Web | 31 | 4.164 |

Desglose de pruebas

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Tests.Integration | 10 | 725 |
| EchoBase.Tests.Unit | 20 | 2.774 |

### 2026-03-31 | commit 8e19fc7

Resumen

| Categoria | Ficheros | LoC | % sobre total |
|---|---:|---:|---:|
| Codigo de aplicacion | 107 | 7.903 | 69,3% |
| Codigo de pruebas | 30 | 3.499 | 30,7% |
| Total | 137 | 11.402 | 100% |

Desglose de codigo de aplicacion

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Core | 48 | 2.187 |
| EchoBase.Infrastructure | 28 | 1.552 |
| EchoBase.Web | 31 | 4.164 |

Desglose de pruebas

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Tests.Integration | 10 | 725 |
| EchoBase.Tests.Unit | 20 | 2.774 |

### 2026-03-31 | commit 0e61f87

Resumen

| Categoria | Ficheros | LoC | % sobre total |
|---|---:|---:|---:|
| Codigo de aplicacion | 107 | 7.903 | 69,3% |
| Codigo de pruebas | 30 | 3.499 | 30,7% |
| Total | 137 | 11.402 | 100% |

Desglose de codigo de aplicacion

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Core | 48 | 2.187 |
| EchoBase.Infrastructure | 28 | 1.552 |
| EchoBase.Web | 31 | 4.164 |

Desglose de pruebas

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Tests.Integration | 10 | 725 |
| EchoBase.Tests.Unit | 20 | 2.774 |

### 2026-03-31 | commit 97abd4d

Resumen

| Categoria | Ficheros | LoC | % sobre total |
|---|---:|---:|---:|
| Codigo de aplicacion | 107 | 7.903 | 69,3% |
| Codigo de pruebas | 30 | 3.499 | 30,7% |
| Total | 137 | 11.402 | 100% |

Desglose de codigo de aplicacion

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Core | 48 | 2.187 |
| EchoBase.Infrastructure | 28 | 1.552 |
| EchoBase.Web | 31 | 4.164 |

Desglose de pruebas

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Tests.Integration | 10 | 725 |
| EchoBase.Tests.Unit | 20 | 2.774 |

### 2026-03-31 | commit 62e3dfa

Resumen

| Categoria | Ficheros | LoC | % sobre total |
|---|---:|---:|---:|
| Codigo de aplicacion | 107 | 7.767 | 70,0% |
| Codigo de pruebas | 28 | 3.321 | 30,0% |
| Total | 135 | 11.088 | 100% |

Desglose de codigo de aplicacion

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Core | 48 | 2.134 |
| EchoBase.Infrastructure | 28 | 1.552 |
| EchoBase.Web | 31 | 4.081 |

Desglose de pruebas

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Tests.Integration | 10 | 725 |
| EchoBase.Tests.Unit | 18 | 2.596 |

### 2026-03-31 | commit 41d47bf

Resumen

| Categoria | Ficheros | LoC | % sobre total |
|---|---:|---:|---:|
| Codigo de aplicacion | 107 | 7.375 | 69,0% |
| Codigo de pruebas | 28 | 3.321 | 31,0% |
| Total | 135 | 10.696 | 100% |

Desglose de codigo de aplicacion

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Core | 48 | 2.134 |
| EchoBase.Infrastructure | 28 | 1.552 |
| EchoBase.Web | 31 | 3.689 |

Desglose de pruebas

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Tests.Integration | 10 | 725 |
| EchoBase.Tests.Unit | 18 | 2.596 |

### 2026-03-31 | commit 24d0784

Resumen

| Categoria | Ficheros | LoC | % sobre total |
|---|---:|---:|---:|
| Codigo de aplicacion | 85 | 5.408 | 69,5% |
| Codigo de pruebas | 19 | 2.378 | 30,5% |
| Total | 104 | 7.786 | 100% |

Desglose de codigo de aplicacion

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Core | 32 | 1.356 |
| EchoBase.Infrastructure | 24 | 1.284 |
| EchoBase.Web | 29 | 2.768 |

Desglose de pruebas

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Tests.Integration | 6 | 374 |
| EchoBase.Tests.Unit | 13 | 2.004 |

### 2026-03-31 | commit 58cf85b

Resumen

| Categoria | Ficheros | LoC | % sobre total |
|---|---:|---:|---:|
| Codigo de aplicacion | 85 | 5.408 | 73,0% |
| Codigo de pruebas | 13 | 2.004 | 27,0% |
| Total | 98 | 7.412 | 100% |

Desglose de codigo de aplicacion

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Core | 32 | 1.356 |
| EchoBase.Infrastructure | 24 | 1.284 |
| EchoBase.Web | 29 | 2.768 |

Desglose de pruebas

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Tests.Integration | 0 | 0 |
| EchoBase.Tests.Unit | 13 | 2.004 |

### 2026-03-31 | commit a6804ca

Resumen

| Categoria | Ficheros | LoC | % sobre total |
|---|---:|---:|---:|
| Codigo de aplicacion | 84 | 5.374 | 73,9% |
| Codigo de pruebas | 12 | 1.895 | 26,1% |
| Total | 96 | 7.269 | 100% |

Desglose de codigo de aplicacion

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Core | 32 | 1.356 |
| EchoBase.Infrastructure | 23 | 1.257 |
| EchoBase.Web | 29 | 2.761 |

Desglose de pruebas

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Tests.Integration | 0 | 0 |
| EchoBase.Tests.Unit | 12 | 1.895 |

### 2026-03-30 | commit 8efad17

Resumen

| Categoria | Ficheros | LoC | % sobre total |
|---|---:|---:|---:|
| Codigo de aplicacion | 84 | 5.370 | 73,9% |
| Codigo de pruebas | 12 | 1.895 | 26,1% |
| Total | 96 | 7.265 | 100% |

Desglose de codigo de aplicacion

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Core | 32 | 1.356 |
| EchoBase.Infrastructure | 23 | 1.257 |
| EchoBase.Web | 29 | 2.757 |

Desglose de pruebas

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Tests.Integration | 0 | 0 |
| EchoBase.Tests.Unit | 12 | 1.895 |

### 2026-03-30 | commit ca7ff95

Resumen

| Categoria | Ficheros | LoC | % sobre total |
|---|---:|---:|---:|
| Codigo de aplicacion | 84 | 5.282 | 74,6% |
| Codigo de pruebas | 12 | 1.795 | 25,4% |
| Total | 96 | 7.077 | 100% |

Desglose de codigo de aplicacion

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Core | 32 | 1.326 |
| EchoBase.Infrastructure | 23 | 1.256 |
| EchoBase.Web | 29 | 2.700 |

Desglose de pruebas

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Tests.Integration | 0 | 0 |
| EchoBase.Tests.Unit | 12 | 1.795 |

### 2026-03-29 | commit 29eda05

Resumen

| Categoria | Ficheros | LoC | % sobre total |
|---|---:|---:|---:|
| Codigo de aplicacion | 84 | 5.282 | 74,6% |
| Codigo de pruebas | 12 | 1.795 | 25,4% |
| Total | 96 | 7.077 | 100% |

Desglose de codigo de aplicacion

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Core | 32 | 1.326 |
| EchoBase.Infrastructure | 23 | 1.256 |
| EchoBase.Web | 29 | 2.700 |

Desglose de pruebas

| Proyecto | Ficheros | LoC |
|---|---:|---:|
| EchoBase.Tests.Integration | 0 | 0 |
| EchoBase.Tests.Unit | 12 | 1.795 |

<!-- LOC_REPORT_HISTORY_END -->


































