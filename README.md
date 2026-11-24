# Marrow

Marrow es un RPG web en 2D con vista desde arriba, desarrollado como proyecto final del bootcamp de Devlights. El jugador crea un guerrero, recorre un mapa, combate contra el enemigo que corresponde a su nivel y gana experiencia para llegar hasta el nivel 3. El progreso queda guardado en PostgreSQL.

El backend es la autoridad del juego: calcula daño, vida, resultados y progreso. El frontend muestra el estado y envía las acciones del jugador.

> **Estado:** el alcance funcional está implementado. Las migraciones están aplicadas en la base local y en Neon, Docker se probó en local, y la versión `2d328ce` está publicada en GitHub, con el CI correcto y el backend y el frontend en línea en Render. El contraataque por colisión y los últimos cambios del frontend pasaron las pruebas del backend y el lint y el build del frontend, y fueron probados manualmente por el autor; todavía no se ejecutaron Playwright ni las pruebas de PostgreSQL sobre ellos. El detalle está en la [ficha técnica](docs/MARROW_FICHA_TECNICA.md).

## Tecnologías

| Área | Tecnologías |
|---|---|
| Backend | C#, .NET 10, ASP.NET Core Web API, Swagger |
| Datos | Entity Framework Core, PostgreSQL |
| Seguridad | Microsoft Identity, JWT Bearer, roles |
| Frontend | React, Vite, JavaScript, Tailwind CSS |
| Pruebas | xUnit, Playwright |
| Infraestructura | Docker, Docker Compose, GitHub Actions |

## Arquitectura

```text
React (frontend)  →  HTTP/JSON con JWT  →  Controllers  →  Services  →  Repositories  →  PostgreSQL
```

```text
backend/Marrow.API/    API: Controllers, Services, Repositories, DTOs y Models
backend/*Tests*/       Proyectos de pruebas
frontend/marrow-web/   Aplicación React
docs/                  Guía técnica, despliegue y seguimiento de requisitos
.github/workflows/     Integración continua
```

## Funcionalidades implementadas

- **Cuentas:** registro e inicio de sesión con Microsoft Identity y JWT, y datos aislados por usuario. Personajes y partidas exigen el rol `Jugador`; el rol `Administrador` existe, pero no protege endpoints porque no hay operaciones administrativas.
- **Personajes:** crear, consultar, cambiar el nombre y eliminar, solo si no tienen partidas. Las estadísticas y el progreso los asigna el backend.
- **Partidas:** creación, consulta, abandono, filtros por estado y dificultad, orden por fecha y paginación.
- **Juego:** mapa de 11 × 7 casillas, movimiento en cuatro direcciones con teclado o controles táctiles, combate cuerpo a cuerpo con contraataque y panel de resultado. Intentar entrar en la casilla del enemigo no mueve al Guerrero: el enemigo contraataca y el backend calcula y guarda el daño. Las colisiones pueden terminar en derrota, que no otorga experiencia ni desbloquea la dificultad Difícil.
- **Enemigos:** el backend asigna uno por partida según el nivel del personaje: Goblin en nivel 1, Esqueleto en nivel 2 y Orco en nivel 3. Sus estadísticas salen del catálogo.
- **Combate:** ataque básico desde el nivel 1 y Golpe poderoso desde el nivel 2.
- **Progresión:** 100 XP por victoria, 100 XP para pasar al nivel 2 y 200 XP para pasar al nivel 3, que es el máximo.
- **Dificultad:** Normal es la predeterminada. Ganar una partida en Normal desbloquea Difícil para siempre, sin seleccionarla: se elige al crear cada partida.
- **Modelo:** relación N:M entre Personaje y Habilidad, sin datos cargados. Golpe poderoso no la usa: se desbloquea por nivel.
- **API:** Swagger, manejo global de errores con `ProblemDetails`, CORS por entorno, datos iniciales y logging.
- **Interfaz:** estados de carga, listas vacías, errores comprensibles y reintentos.

Los límites actuales están en la [guía técnica](docs/GUIA_TECNICA.md#pendientes-y-límites-conocidos).

## Requisitos

- .NET SDK 10
- Node.js 20.19 o superior, o 22.12 o superior, con npm
- PostgreSQL 13 o superior

## Puesta en marcha

Todos los comandos se ejecutan desde la raíz del repositorio.

### 1. Configurar PostgreSQL y User Secrets

Creá una base de datos y guardá la conexión y la clave JWT fuera del repositorio:

```bash
dotnet user-secrets set "ConnectionStrings:MarrowDatabase" "Host=localhost;Port=5432;Database=TU_BASE;Username=TU_USUARIO;Password=TU_CONTRASEÑA" --project backend/Marrow.API
```

```bash
dotnet user-secrets set "Jwt:SigningKey" "UNA_CLAVE_ALEATORIA_DE_AL_MENOS_32_BYTES" --project backend/Marrow.API
```

Opcionalmente, para crear un administrador, definí también `AdministradorInicial:NombreUsuario`, `AdministradorInicial:Email` y `AdministradorInicial:Password`.

### 2. Aplicar las migraciones

Si la base ya tiene datos, leé antes las [precauciones de la guía técnica](docs/GUIA_TECNICA.md#postgresql-y-migraciones).

```bash
dotnet tool restore
```

```bash
dotnet ef database update --project backend/Marrow.API
```

La API no aplica migraciones sola: si hay pendientes, no arranca. Al iniciar, carga los enemigos y los roles que falten, y corrige valores inválidos de personajes existentes.

### 3. Iniciar el backend

```bash
dotnet run --project backend/Marrow.API --launch-profile http
```

La API queda en `http://localhost:5137`, con Swagger en `http://localhost:5137/swagger`.

### 4. Iniciar el frontend

En otra terminal:

```bash
npm install --prefix frontend/marrow-web
```

```bash
npm run dev --prefix frontend/marrow-web
```

La aplicación queda en `http://localhost:5173`.

## Pruebas

```bash
dotnet test backend/Marrow.sln
```

```bash
npm run test:e2e --prefix frontend/marrow-web
```

Estas pruebas no usan una base de datos real: las de E2E usan una API simulada. Las pruebas contra PostgreSQL están en `backend/Marrow.API.PostgresTests`, fuera de la solución, y requieren preparación. Ver la [guía técnica](docs/GUIA_TECNICA.md#pruebas-automatizadas).

## Docker e integración continua

- **Docker:** hay un Dockerfile para la API, otro para el frontend y un `docker-compose.yml` para probarlos en local. No incluyen PostgreSQL.
- **CI:** `.github/workflows/ci.yml` compila y prueba el backend, y ejecuta lint y build del frontend.

Los pasos están en la [guía de despliegue](docs/DESPLIEGUE.md).

## Documentación

- [Guía técnica](docs/GUIA_TECNICA.md): arquitectura, funcionamiento y conceptos clave.
- [Despliegue](docs/DESPLIEGUE.md): producción, Neon, Docker y CI.
- [Requisitos del bootcamp](docs/requisitos/REQUISITOS_BOOTCAMP.md): seguimiento de los contenidos del programa.
