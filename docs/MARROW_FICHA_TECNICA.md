# Marrow: ficha técnica del proyecto

Documento de referencia con los requisitos técnicos y la información principal de Marrow. Para el detalle de cada tema, ver
la [guía técnica](GUIA_TECNICA.md), la [guía de despliegue](DESPLIEGUE.md) y el
[seguimiento de requisitos del bootcamp](requisitos/REQUISITOS_BOOTCAMP.md).

> **Estado (24 de noviembre de 2026):** el alcance funcional está implementado. Las siete migraciones están aplicadas en la
> base local y en Neon, Docker se probó en local, y la versión `2d328ce` está publicada en GitHub, con el CI correcto y el
> backend y el frontend en línea en Render. El contraataque por colisión y los últimos cambios del frontend pasaron las pruebas
> del backend y el lint y el build del frontend, y el autor los probó manualmente. Todavía no se ejecutaron Playwright ni las
> pruebas de PostgreSQL sobre ellos.

## 1. Qué es Marrow

Marrow es un RPG web en 2D con vista desde arriba, desarrollado como proyecto final del bootcamp de Devlights. El jugador crea un
guerrero, recorre un mapa, combate contra el enemigo que corresponde a su nivel y gana experiencia hasta llegar al nivel 3. El
progreso se guarda en PostgreSQL.

**Decisión central:** el backend es la autoridad del juego. Calcula daño, vida, resultados, experiencia, niveles y desbloqueos.
El frontend muestra el estado y envía intenciones, como "mover a la derecha" o "atacar".

## 2. Alcance funcional

| Tema | Definición |
|---|---|
| Mapa | Grilla de 11 × 7 casillas, sin paredes ni obstáculos |
| Clase | Una sola: Guerrero |
| Movimiento | Cuatro direcciones, una casilla por pedido |
| Niveles | Mínimo 1, máximo 3 |
| Enemigos | Uno por partida, asignado por el backend según el nivel: Goblin (1), Esqueleto (2) y Orco (3) |
| Combate | Cuerpo a cuerpo y por turnos, con contraataque del enemigo |
| Ataques | Ataque básico desde el nivel 1, y Golpe poderoso desde el nivel 2 |
| Progresión | 100 XP por victoria. Subir al nivel 2 cuesta 100 XP, y al nivel 3, 200 XP |
| Dificultad | Normal por defecto. Ganar en Normal desbloquea Difícil para siempre, que se elige al crear cada partida |
| Fuera del alcance | Inventario, equipamiento, tiendas, misiones, NPC, multijugador, mapas adicionales y árboles de habilidades |

## 3. Tecnologías

| Área | Tecnología | Versión |
|---|---|---|
| Lenguaje del backend | C# | .NET 10 |
| API | ASP.NET Core Web API | .NET 10 |
| ORM | Entity Framework Core | 10.0.13 |
| Proveedor de PostgreSQL | Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.3 |
| Base de datos | PostgreSQL | 13 o superior |
| Usuarios y roles | Microsoft Identity (`Microsoft.AspNetCore.Identity.EntityFrameworkCore`) | 10.0.13 |
| Autenticación | JWT Bearer | 10.0.3 |
| Documentación de la API | Swagger (Swashbuckle) | 10.3.1 |
| Herramienta de migraciones | `dotnet-ef` (en `dotnet-tools.json`) | 10.0.13 |
| Frontend | React y JavaScript | React 19 |
| Herramienta de build | Vite | 8 |
| Estilos | Tailwind CSS | 4 |
| Lint del frontend | oxlint | 1.90 |
| Pruebas del backend | xUnit | 2.9.3 |
| Pruebas del frontend | Playwright | 1.64 |
| Contenedores | Docker y Docker Compose | — |
| Integración continua | GitHub Actions | — |

## 4. Requisitos para ejecutarlo

| Requisito | Versión |
|---|---|
| .NET SDK | 10 |
| Node.js | 20.19 o superior, o 22.12 o superior, con npm (lo exigen Vite, Rolldown y oxlint). Docker y CI usan Node 22 |
| PostgreSQL | 13 o superior, local o en Neon |
| Sistema para las pruebas E2E | Windows: `playwright.config.js` usa `npm.cmd` |

Los pasos de instalación están en el [README](../README.md#puesta-en-marcha).

## 5. Arquitectura

```text
React (frontend)  →  HTTP/JSON con JWT  →  Controllers  →  Services  →  Repositories  →  PostgreSQL
```

| Capa | Responsabilidad |
|---|---|
| Controllers | Reciben pedidos HTTP, leen el usuario del token y traducen resultados a códigos HTTP |
| Services | Reglas de negocio y validaciones |
| Reglas | `ReglasCombate` y `ReglasProgresion`: cálculos puros, sin acceso a datos |
| Repositories | Acceso a datos con Entity Framework Core |
| DTOs | Contratos de entrada y salida, validados con Data Annotations |
| Models | Entidades del dominio y de la base |
| Middleware | Manejo global de excepciones con `ProblemDetails` |

**Estructura del repositorio:**

```text
backend/Marrow.API/                            API: Controllers, Services, Repositories, DTOs, Models, Data, Middleware
backend/Marrow.API.Tests/                      Pruebas unitarias y de repositorios
backend/Marrow.API.IntegrationTests/           Pruebas de integración aisladas
backend/Marrow.API.IntegrationTests.Infraestructura/   Fábrica y protecciones de las pruebas
backend/Marrow.API.PostgresTests/              Pruebas contra PostgreSQL real (fuera de la solución)
frontend/marrow-web/                           Aplicación React
docs/                                          Documentación
.github/workflows/ci.yml                       Integración continua
docker-compose.yml                             Prueba local de las imágenes
```

## 6. Modelo de datos

| Entidad | Datos principales |
|---|---|
| `Usuario` | Hereda de `IdentityUser<int>`; suma la fecha de creación. Tabla `Usuarios` |
| `Personaje` | Nombre, nivel, experiencia, vida, ataque, defensa, clase, dificultad desbloqueada, usuario y habilidades |
| `Enemigo` | Nombre, nivel, vida, ataque, defensa y experiencia otorgada |
| `Partida` | Estado, dificultad, resultado, vidas actuales, posiciones, personaje y enemigo |
| `Habilidad` | Nombre y personajes |

**Relaciones:**

- **1:N:** un usuario tiene muchos personajes; un personaje, muchas partidas; y un enemigo aparece en muchas partidas.
- **N:M:** Personaje y Habilidad, mediante `PersonajeHabilidades`, con clave compuesta y configurada con Fluent API. La tabla
  existe pero no tiene datos: Golpe poderoso se decide por nivel y no la usa.

**Enums:** `Dificultad` (Normal, Dificil), `EstadoPartida` (Activa, Finalizada) y `ResultadoCombate` (Victoria, Derrota). Se
guardan como enteros en la base y viajan como texto en JSON.

## 7. Endpoints

| Método y ruta | Uso | Token |
|---|---|---|
| `POST /api/auth/register` | Registrar un usuario | No |
| `POST /api/auth/login` | Iniciar sesión por nombre de usuario o email | No |
| `GET /api/personajes` | Listar los personajes propios | Sí |
| `POST /api/personajes` | Crear un personaje; solo se envía el nombre | Sí |
| `GET /api/personajes/{id}` | Ver un personaje propio | Sí |
| `PUT /api/personajes/{id}` | Cambiar el nombre | Sí |
| `DELETE /api/personajes/{id}` | Eliminar un personaje sin partidas | Sí |
| `GET /api/enemigos` | Listar el catálogo de enemigos | No |
| `GET /api/enemigos/{id}` | Ver un enemigo | No |
| `GET /api/partidas` | Listar partidas con filtros, orden y paginación | Sí |
| `POST /api/partidas` | Crear una partida, con dificultad opcional | Sí |
| `GET /api/partidas/{id}` | Ver una partida propia | Sí |
| `GET /api/partidas/{id}/estado` | Ver el estado de juego | Sí |
| `PATCH /api/partidas/{id}/movimiento` | Mover una casilla | Sí |
| `POST /api/partidas/{id}/ataque` | Ataque básico | Sí |
| `POST /api/partidas/{id}/golpe-poderoso` | Golpe poderoso; requiere nivel 2 | Sí |
| `PATCH /api/partidas/{id}/finalizar` | Abandonar | Sí |

Los endpoints que piden token exigen además el rol `Jugador`.

**Códigos HTTP usados:** 200, 201, 204, 400, 401, 403, 404, 409 y 500. Un recurso ajeno responde 404, como si no existiera.
Los ataques no llevan cuerpo: el cliente no puede enviar daño ni resultados.

**Listado de partidas:** acepta `estado`, `dificultad`, `orden` (`asc` o `desc`, por defecto `desc`), `page` (de 1 a 1.000.000)
y `pageSize` (de 1 a 50, por defecto 10). Responde `items`, `page`, `pageSize`, `totalItems` y `totalPages`.

**Restricciones del CRUD:** de un personaje solo se edita el nombre, y solo se elimina si no tiene partidas (si tiene, responde
409). Partidas y enemigos no tienen `DELETE`: las partidas son el historial, y los enemigos, datos de referencia.

## 8. Reglas del juego

### Partidas y movimiento

- El jugador empieza en la casilla (1, 1) y el enemigo, en (5, 3). El enemigo no se mueve.
- Cada movimiento es un pedido al backend, nunca uno por fotograma. En el borde, el personaje se queda en su lugar.
- Si el personaje intenta entrar en la casilla del enemigo, no se mueve y el enemigo contraataca. El backend calcula y guarda
  el daño, que puede terminar la partida en Derrota. Ese contraataque no otorga experiencia ni afecta el desbloqueo de Difícil.
- `PATCH /api/partidas/{id}/movimiento` responde 200 con `{ estado, danioRecibido }`; `danioRecibido` es 0 en un movimiento normal.
- Mover en una partida finalizada, o en una que cambió durante el contraataque, responde 409.

### Enemigos

El backend elige al crear la partida el enemigo cuyo nivel coincide con el del personaje. El cliente no puede elegirlo.

| Enemigo | Nivel | Vida | Ataque | Defensa | XP | Vida en Difícil | Ataque en Difícil |
|---|---|---|---|---|---|---|---|
| Goblin | 1 | 30 | 5 | 2 | 100 | 60 | 7 |
| Esqueleto | 2 | 45 | 8 | 3 | 100 | 90 | 12 |
| Orco | 3 | 80 | 12 | 5 | 100 | 160 | 18 |

### Combate

- Para atacar, el personaje debe estar en una casilla adyacente, sin diagonales.
- **Daño:** `ataque - defensa`, con un mínimo de 1.
- **Turno:** el jugador golpea primero. Si el enemigo llega a 0, es Victoria, sin contraataque. Si sobrevive, contraataca, y si
  el jugador llega a 0, es Derrota. Intentar entrar en la casilla del enemigo provoca el mismo contraataque, sin golpe previo.
- **Personaje:** usa su vida, ataque y defensa. Uno nuevo tiene 80, 15 y 5. Subir de nivel no cambia sus estadísticas.
- **Golpe poderoso:** desde el nivel 2. Duplica el ataque del personaje antes de restar la defensa. No tiene coste ni espera, y
  el backend valida el nivel en cada uso.

### Dificultad

- Normal usa el catálogo tal cual. Difícil duplica la vida del enemigo y aumenta su ataque un 50 %, redondeando hacia abajo.
- Normal está disponible desde el principio y es la opción predeterminada.
- Ganar una partida en Normal desbloquea Difícil para siempre, sin seleccionarla.
- Ganar en Difícil, perder o abandonar no cambia el desbloqueo.
- Pedir Difícil sin haberla desbloqueado responde 409.

### Experiencia y niveles

- Solo las victorias otorgan experiencia: 100 XP. Derrotas y abandonos no otorgan nada.
- Subir cuesta `100 × nivel actual`: 100 XP del nivel 1 al 2, y 200 XP del nivel 2 al 3. El sobrante se conserva.
- En el nivel 3 la experiencia sigue acumulándose, pero el nivel no cambia.
- Un personaje nuevo llega al nivel 2 con 1 victoria, y al nivel 3 con 2 victorias más.

### Resultados con un personaje nuevo

Atacando siempre de la misma forma y sin intentar entrar en la casilla del enemigo: cada intento suma un contraataque que la
tabla no contempla. Las colisiones repetidas pueden provocar la derrota contra cualquier enemigo.

| Enemigo y dificultad | Solo ataque básico | Solo Golpe poderoso |
|---|---|---|
| Goblin, Normal | Gana con 78 de vida | No disponible en nivel 1 |
| Esqueleto, Normal | Gana con 71 de vida | Gana con 77 de vida |
| Orco, Normal | Gana con 31 de vida | Gana con 59 de vida |
| Goblin, Difícil | Gana con 72 de vida | No disponible en nivel 1 |
| Esqueleto, Difícil | Gana con 31 de vida | Gana con 59 de vida |
| Orco, Difícil | Pierde | Gana con 2 de vida |

## 9. Seguridad

- **Microsoft Identity:** `UserManager` y `RoleManager`, sin cookies. Contraseñas de al menos 8 caracteres, guardadas con hash
  PBKDF2 y sal. Nombre de usuario y email únicos, sin distinguir mayúsculas.
- **JWT:** firmado con HMAC-SHA256 y válido por 60 minutos. Incluye id, nombre, email y roles. El id del usuario siempre sale
  del token, nunca del pedido.
- **Roles:** `Jugador`, que se asigna a cada usuario, y `Administrador`, solo para el administrador inicial configurado. El
  rol `Administrador` no protege ningún endpoint, porque el alcance no incluye operaciones administrativas.
- **Autorización:** `PersonajesController` y `PartidasController` exigen `[Authorize(Roles = RolesUsuario.Jugador)]`.
  `AuthController` y `EnemigosController` son públicos, con `[AllowAnonymous]`.
- **401 y 403:** `401` cuando falta el token o es inválido; `403` cuando el token es válido pero no tiene el rol `Jugador`.
- **CORS:** orígenes configurados por entorno. En Production es obligatorio indicarlos y no se admite `*`.
- **Errores:** respuestas `ProblemDetails`. El detalle técnico solo se muestra en Development.
- **Secretos:** fuera del repositorio. En Development, con User Secrets; en Production, con variables de entorno.

## 10. Configuración

### Development

| Clave de User Secrets | Uso |
|---|---|
| `ConnectionStrings:MarrowDatabase` | Conexión a PostgreSQL, local o Neon |
| `Jwt:SigningKey` | Firma de los JWT, de al menos 32 bytes |
| `AdministradorInicial:NombreUsuario`, `Email`, `Password` | Opcionales: crean el administrador inicial |

Swagger está disponible en `/swagger`. El frontend local se permite desde `http://localhost:5173`.

### Production

| Variable de entorno | Obligatoria | Uso |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | Sí | `Production` |
| `ConnectionStrings__MarrowDatabase` | Sí | Conexión a PostgreSQL. En Neon: formato clave=valor, host directo y `SSL Mode=Require` |
| `Jwt__SigningKey` | Sí | Clave aleatoria de al menos 32 bytes |
| `Cors__AllowedOrigins__0` | Sí | Dominio exacto del frontend, con `https://` y sin barra final |
| `Jwt__Issuer`, `Jwt__Audience`, `Jwt__ExpirationMinutes` | No | Reemplazan los valores de `appsettings.Production.json` |
| `AdministradorInicial__NombreUsuario`, `__Email`, `__Password` | No | Crean el administrador inicial |
| `ASPNETCORE_HTTP_PORTS` | Según el proveedor | Puerto de la API |
| `VITE_API_URL` (frontend) | Sí | URL pública de la API; se fija al construir el frontend |

En Production, Swagger está desactivado. Si falta una variable obligatoria, la API no arranca y el log indica cuál, sin mostrar
valores. Hay ejemplos en `backend/.env.example` y `frontend/marrow-web/.env.example`.

## 11. Base de datos y migraciones

La API no aplica migraciones sola: si falta alguna, no arranca.

| Orden | Migración | Qué hace |
|---|---|---|
| 1 | `InitialCreate` | Crea las tablas iniciales |
| 2 | `SeedEnemigosIniciales` | Inserta los enemigos |
| 3 | `PersistirEstadoInicialPartida` | Guarda el estado inicial de cada partida |
| 4 | `AgregarDificultadYResultadoCombate` | Agrega dificultad y resultado |
| 5 | `AgregarHabilidadesPersonaje` | Crea habilidades y la relación N:M |
| 6 | `IncorporarIdentityYRoles` | Agrega Identity y conserva a los usuarios existentes |
| 7 | `ActualizarExperienciaEnemigos` | Cambia a 100 la XP de los tres enemigos |

Las siete están aplicadas en la base local y en Neon. Antes de aplicar la 6 sobre otra base con datos, hay que hacer un
respaldo y comprobar que no haya nombres de usuario ni emails repetidos.

**Al arrancar**, salvo en el entorno `Testing`, la API:

1. Agrega los enemigos que falten.
2. Corrige solo los valores inválidos de personajes existentes: nivel fuera de 1 a 3, experiencia negativa, vida máxima de 0 o
   menos, ataque de 0 o menos y defensa negativa.
3. Crea los roles que falten.
4. Crea el administrador inicial, si está configurado.
5. Asigna el rol `Jugador` a los usuarios que no lo tengan.

No elimina usuarios, personajes ni partidas.

## 12. Frontend

- Pantallas: inicio de sesión, registro, selección de personaje, inicio del personaje con sus partidas, y juego. La aplicación
  usa una sola URL, sin biblioteca de rutas.
- Escena de 11 × 7 con controles de teclado (WASD o flechas, Espacio para atacar) y controles táctiles en celulares.
- HUD con vida, nivel actual y máximo, y experiencia. Panel de combate con ataque básico y Golpe poderoso, que aparece bloqueado
  antes del nivel 2.
- Al ganar o perder, `BattleEndPanel` avisa el final sobre la escena y, al pulsar «Continuar», se abre el panel de resultado.
- Listado de partidas con filtros, orden, paginación y selector de dificultad para la partida nueva.
- Estados de carga, listas vacías, errores comprensibles y botón `Reintentar`. Ante un 401, se cierra la sesión.
- La sesión se guarda en `sessionStorage`.

### Recursos gráficos

Los sprites están en `frontend/marrow-web/src/assets/sprites/`. Son recursos de terceros:

| Personaje | Recurso y autor | Licencia declarada |
|---|---|---|
| Guerrero | [Free Knight](https://vibeforge.itch.io/free-knight-top-down-pixel-art-character), de VibeForge | Propia. Permite el uso y la modificación. Restringe la redistribución como recurso independiente |
| Goblin | Free Goblin Scout, de [Sova Digital](https://sovdigital.itch.io) | Propia. Permite el uso comercial, la modificación y la inclusión en juegos compilados o empaquetados. Restringe la redistribución como recurso independiente |
| Orco | [Dark Troll Boss](https://iguiw.itch.io/free-dark-troll-boss-pixel-art-character-effects-2d), de iguiw | CC0 1.0. Permite el uso, la modificación y la redistribución |
| Esqueleto | [Skeleton](https://opengameart.org/content/skeleton-4), de rehcub, en OpenGameArt | CC0 1.0, según la página consultada. Permite el uso, la modificación y la redistribución |

- **Repositorio público:** los cuatro conjuntos se conservan en el repositorio para la evaluación académica. Es una decisión del
  proyecto y no modifica las condiciones de las licencias.
- **Salvedad de Guerrero y Goblin:** sus licencias no aclaran expresamente si los PNG, originales o derivados, pueden publicarse
  en un repositorio público. No se cuenta con una autorización específica de sus autores para hacerlo.
- **Atribución:** en las licencias consultadas no figura como obligatoria. Esta tabla documenta la procedencia.

## 13. Pruebas

| Proyecto | Qué cubre | ¿Requiere PostgreSQL? |
|---|---|---|
| `Marrow.API.Tests` | Reglas, servicios, repositorios e Identity | No |
| `Marrow.API.IntegrationTests` | API, fábrica de pruebas y protecciones | No |
| `Marrow.API.PostgresTests` | Conexión a una base real | Sí: `MARROW_TEST_CONNECTION` y una base `marrow_test` |
| `frontend/marrow-web/tests` | Flujos con Playwright y API simulada | No |

- `dotnet test backend/Marrow.sln` ejecuta las dos primeras suites.
- `npm run test:e2e --prefix frontend/marrow-web` ejecuta Playwright, solo en Windows.
- **Pruebas automatizadas locales (24 de noviembre de 2026, antes de publicar `2d328ce`):** `Marrow.sln` compiló en Release
  con 0 errores y 2 advertencias `CS1573` en `ResultadoPartida.cs`. Pasaron 353 de 353 en `Marrow.API.Tests` y 216 de 216 en
  `Marrow.API.IntegrationTests`: 569 en total.
- **Frontend:** `npm run lint` terminó sin observaciones y `npm run build` fue exitoso, con 52 módulos transformados. Las 9
  importaciones de sprites apuntan a archivos existentes.
- **Comprobación manual:** el autor probó y aprobó el contraataque, el panel final, el ingreso directo a la partida y las
  animaciones.
- **No ejecutado sobre `2d328ce`:** `Marrow.API.PostgresTests` y Playwright.
- Quedan por actualizar una aserción de `Marrow.API.PostgresTests` y una de Playwright, y por revisar los flujos E2E que
  cambiaron.

## 14. Infraestructura

| Tema | Estado |
|---|---|
| Docker | Dockerfile multietapa para la API (runtime de .NET, puerto 8080) y para el frontend (nginx). `docker-compose.yml` sirve para prueba local y no incluye PostgreSQL. Las imágenes se construyeron y se probaron en local |
| Neon | Base de producción configurada, con las siete migraciones aplicadas. La conexión local se configura con User Secrets |
| CI | `.github/workflows/ci.yml`: compila y prueba el backend, y ejecuta lint y build del frontend. No incluye Playwright ni PostgreSQL. Su ejecución sobre `2d328ce` fue exitosa |
| Despliegue | `marrow-api` y `marrow-frontend` en línea en Render desde `2d328ce`, con Auto-Deploy en On Commit. Cómo se configura: [DESPLIEGUE.md](DESPLIEGUE.md). El repositorio no incluye la configuración de Render ni un workflow de despliegue |

## 15. Pendiente

- Actualizar las aserciones pendientes de `Marrow.API.PostgresTests` y Playwright, revisar los flujos E2E que cambiaron y
  ejecutar ambas suites.

**Ya completado:** validación general de `79246da`; compilación y pruebas del backend, lint y build del frontend y prueba manual
de los cambios de `2d328ce`; migraciones en la base local y en Neon, imágenes Docker, CI y despliegue en Render.

**Límites conocidos:**

- El enemigo no se mueve, el mapa no tiene obstáculos, y Golpe poderoso siempre conviene más que el ataque básico.
- El listado obtiene el resultado de cada partida finalizada con un pedido aparte.
- Los filtros del listado se pierden al recargar la página.
- El frontend no ofrece la eliminación de personajes: se usa desde Swagger.
