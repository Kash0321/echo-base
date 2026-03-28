# 🛰️ Proyecto Echo-Base: Strategic Docking Coordinator

[![Platform: .NET 10](https://img.shields.io/badge/.NET-10.0-blueviolet)](https://dotnet.microsoft.com/)
[![Framework: Blazor](https://img.shields.io/badge/UI-Blazor-blue)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![AI: GitHub Copilot Pro](https://img.shields.io/badge/AI-Copilot_Pro-brightgreen)](https://github.com/features/copilot)

**Status:** Orbital (Satellite App)

**Mission:** Coordinate 70 Rebel Units across 24 Docking Bays.

**Tech ground**
- **Command Center:** .NET 10 / Blazor Web App
- **Logic:** Spec-Driven Development (SDD)
- **Data Core:** Entity Framework Core (Current: SQLite / Target: Azure SQL)
- **AI Navigator:** GitHub Copilot Pro

**Echo-Base** es una aplicación satelital diseñada para gestionar la logística de puestos de trabajo en un entorno de modelo híbrido. Su misión principal es coordinar el acceso a recursos físicos limitados mediante un flujo de trabajo de **Spec-Driven Development (SDD)** asistido por Inteligencia Artificial.


## 🌌 ¿Por qué "Echo-Base"?

El nombre no es solo una referencia a Star Wars; es una metáfora de nuestra realidad operativa:

1.  **El Concepto de Base:** Al igual que la base rebelde en el planeta Hoth, nuestra oficina ya no es un asentamiento permanente para todos, sino un punto de encuentro táctico. Los empleados operan de forma remota ("en la galaxia") y acuden a la base para misiones específicas de colaboración.
2.  **Las Docking Bays:** Cada uno de los **24 puestos disponibles** se trata como una "bahía de atraque" (*docking station*). La aplicación garantiza que ninguna "nave" (empleado) intente aterrizar sin una bahía asignada.
3.  **El Desafío Logístico:** Aplicamos el **Principio del Palomar**: gestionar 70 unidades para 24 espacios requiere una coordinación de precisión quirúrgica para evitar colisiones.

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
