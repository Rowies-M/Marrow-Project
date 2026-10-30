# Marrow

Marrow es un RPG web 2D top-down desarrollado como proyecto final de bootcamp.
La primera versión será un MVP pequeño con una mazmorra, un guerrero, combate por turnos y progresión básica.

## Stack

- Backend: C#, .NET 10, ASP.NET Core Web API y Swagger/OpenAPI.
- Frontend: React, Vite, JavaScript y Tailwind CSS.
- Persistencia prevista: Entity Framework Core y PostgreSQL.

La autenticación, la base de datos y la lógica del juego todavía no están implementadas.

## Requisitos

- .NET SDK 10.
- Node.js y npm.
- PostgreSQL será necesario en una etapa posterior.

## Ejecutar el backend

Desde la raíz del repositorio:

```powershell
dotnet run --project backend/Marrow.API/Marrow.API.csproj
```

En Development, Swagger UI estará disponible en la ruta `/swagger` del puerto indicado por la consola.

## Ejecutar el frontend

Desde la raíz del repositorio:

```powershell
cd frontend/marrow-web
npm.cmd install
npm.cmd run dev
```

Vite mostrará en la consola la URL local del frontend.

## Estructura

```text
backend/Marrow.API/  API y capas iniciales del backend
frontend/marrow-web/ Aplicación React
```

## Estado

La estructura inicial está preparada. No se incluyen todavía endpoints de juego, autenticación, conexión a PostgreSQL ni funcionalidades RPG.
