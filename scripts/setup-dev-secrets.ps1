#Requires -Version 7
<#
.SYNOPSIS
    Configura los secretos de desarrollo local para EchoBase.Web mediante .NET User Secrets.

.DESCRIPTION
    Este script establece todos los valores de configuración sensibles como User Secrets,
    de modo que no se almacenen en ficheros de configuración que se sincronizan con el
    control de código fuente (git).

    Los User Secrets se almacenan en:
        %APPDATA%\Microsoft\UserSecrets\echobase-web-devenv-001\secrets.json

    Para producción, estos valores deben configurarse como variables de entorno
    o mediante Azure Key Vault / Azure App Configuration.

.NOTES
    Requisito: .NET SDK instalado y proyecto con <UserSecretsId> configurado.

    Uso:
        1. Edita este script y sustituye los valores YOUR_* por los reales.
        2. Ejecuta desde la raíz del repositorio:
               .\scripts\setup-dev-secrets.ps1
        3. Verifica con:
               dotnet user-secrets list --project src\EchoBase.Web

    Para limpiar todos los secretos:
               dotnet user-secrets clear --project src\EchoBase.Web
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$project = "src\EchoBase.Web"

function Set-Secret {
    param([string]$Key, [string]$Value)
    Write-Host "  Setting $Key..." -ForegroundColor Cyan
    dotnet user-secrets set $Key $Value --project $project
}

Write-Host ""
Write-Host "=== EchoBase — Configuración de User Secrets para desarrollo ===" -ForegroundColor Yellow
Write-Host ""

# ──────────────────────────────────────────────────────────────────────────────
# Azure AD (Microsoft Identity Platform)
# Obtener desde: portal.azure.com → Azure Active Directory → App Registrations
# ──────────────────────────────────────────────────────────────────────────────
Write-Host "[ Azure AD ]" -ForegroundColor Magenta
Set-Secret "AzureAd:Domain"       "YOUR_TENANT_DOMAIN.onmicrosoft.com"   # Ej: contoso.onmicrosoft.com
Set-Secret "AzureAd:TenantId"     "YOUR_TENANT_GUID"                      # GUID del tenant de Azure AD
Set-Secret "AzureAd:ClientId"     "YOUR_APP_CLIENT_GUID"                  # GUID del registro de aplicación

# ──────────────────────────────────────────────────────────────────────────────
# SMTP (Microsoft Exchange / Office 365)
# Usar una cuenta de servicio o App Password de M365
# ──────────────────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "[ SMTP — Office 365 ]" -ForegroundColor Magenta
Set-Secret "Smtp:UserName"    "notificaciones@yourdomain.com"              # Cuenta de envío
Set-Secret "Smtp:Password"    "YOUR_SMTP_APP_PASSWORD"                     # App password (no la contraseña personal)
Set-Secret "Smtp:FromAddress" "notificaciones@yourdomain.com"

# ──────────────────────────────────────────────────────────────────────────────
# Microsoft Graph API (para notificaciones de Teams)
# Obtener desde: portal.azure.com → App Registrations → tu app → Certificates & secrets
# Permisos necesarios: Chat.Create, ChatMessage.Send (Application permissions)
# ──────────────────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "[ Microsoft Graph ]" -ForegroundColor Magenta
Set-Secret "MicrosoftGraph:TenantId"     "YOUR_TENANT_GUID"               # Mismo GUID que AzureAd:TenantId
Set-Secret "MicrosoftGraph:ClientId"     "YOUR_GRAPH_APP_CLIENT_GUID"     # Puede ser el mismo App Registration o uno dedicado
Set-Secret "MicrosoftGraph:ClientSecret" "YOUR_GRAPH_CLIENT_SECRET_VALUE" # El valor del secreto generado en el portal

Write-Host ""
Write-Host "=== Secretos configurados correctamente ===" -ForegroundColor Green
Write-Host ""
Write-Host "Verifica con: dotnet user-secrets list --project $project" -ForegroundColor DarkCyan
Write-Host ""
Write-Host "NOTA: Para producción (Azure App Service / Container Apps)," -ForegroundColor DarkYellow
Write-Host "      usa Azure Key Vault o variables de entorno con el mismo" -ForegroundColor DarkYellow
Write-Host "      esquema de claves (AzureAd__TenantId, Smtp__Password, etc.)." -ForegroundColor DarkYellow
Write-Host "      El doble guion bajo (__) equivale al separador ':' de configuración." -ForegroundColor DarkYellow
Write-Host ""
