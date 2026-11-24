# Requisitos del bootcamp: seguimiento en Marrow

Síntesis de `Contenido para proyecto final bootcamp devlights.docx`, organizada según sus cuatro bloques y unidades.
El estado se revisó el 2026-11-24 sobre el commit `79246da` y los cambios locales posteriores, leyendo el código y los
resultados de las validaciones, incluida la de ese día. No se consultaron las bases de datos ni los servicios de Render.

**Esto no es una rúbrica de aprobación.** Es un seguimiento de los contenidos del programa. El documento no indica cuáles
son obligatorios para aprobar, y "Pautas finales del proyecto integrador" aparece sin detalle.

## Alcance funcional definitivo de Marrow

Es la decisión del proyecto, no un requisito del programa. Todos sus puntos están implementados en código. El flujo principal
se recorrió en ejecución el 2026-11-09, antes de los cambios locales posteriores a `79246da`.

- Mapa de 11 × 7 casillas y movimiento en cuatro direcciones.
- Una clase: Guerrero.
- Nivel mínimo 1 y máximo 3. Cada victoria otorga 100 XP; subir cuesta 100 XP al nivel 2 y 200 XP al nivel 3.
- Un enemigo por partida, asignado por el backend según el nivel: Goblin (1), Esqueleto (2) y Orco (3).
- Combate cuerpo a cuerpo, con ataque básico desde el nivel 1.
- Una habilidad especial: Golpe poderoso, desde el nivel 2.
- Dificultades Normal, predeterminada, y Difícil, que se desbloquea al ganar en Normal y se elige al crear la partida.
- Sin inventario, equipamiento, tiendas, misiones, NPC, multijugador, mapas adicionales ni árboles de habilidades.

## Referencias

- `[x]` **Implementado en código.** Hay evidencia en el repositorio; si además se comprobó en ejecución, la observación lo dice.
- `[P]` **Preparado, pero pendiente de ejecutar o configurar.**
- `[~]` **Parcial o por verificar.** Cuando falta información, la observación dice "Por verificar".
- `[ ]` **Pendiente de desarrollo.**

## Bloque 1: Fundamentos y dominio

### Unidad 1: Introducción y sintaxis

- [x] Configuración del entorno y proyecto. Proyectos .NET 10 en `backend/`.
- [x] Tipos de datos, variables y operadores.
- [~] Control de flujo. `if`, `switch` y `while` en la API; `foreach` en la API y las pruebas. La API no usa `for`.

### Unidad 2: Colecciones y lógica

- [x] Métodos y funciones.
- [x] Listas con `List`.
- [x] Consultas con LINQ. `Where`, `Select` y `OrderBy` en repositorios y servicios.
- [x] Control de errores con `try-catch`. Se usa en `AuthController` y `Program.cs`; el manejo global está en la unidad 11.

### Unidad 3: POO y arquitectura

- [x] Clases, objetos y propiedades. Ver `Models/`.
- [x] Interfaces para desacoplar. Ver `Interfaces/`.
- [~] Modelo de dominio en consola. Por verificar: no está en este repositorio. Se informó como entregado en un trabajo
  práctico independiente, sin evidencia aquí.

## Bloque 2: Web API y arquitectura

Objetivo del bloque: API RESTful en memoria.

- [~] API en memoria. La API actual persiste en PostgreSQL, como pide la unidad 8. Por verificar: si existió la etapa en
  memoria.

### Unidad 4: Web API, HTTP y controllers

- [x] Arquitectura cliente-servidor y estándar REST.
- [x] Verbos HTTP. GET, POST, PUT, PATCH y DELETE.
- [x] Códigos de estado. Se usan 200, 201, 204, 400, 401, 403, 404, 409 y 500.
- [x] Controladores y ruteo.
- [x] Documentación con Swagger/OpenAPI. Comentarios XML, códigos HTTP y JWT, solo en desarrollo. Se abrió en
  ejecución el 2026-11-09.

### Unidad 5: DTOs, validaciones y respuestas HTTP

- [~] Operaciones CRUD completas. Personajes tiene las cuatro, con restricciones: solo se edita el nombre y se elimina si no
  tiene partidas. Partidas no tiene DELETE porque son historial, y Enemigos es de solo lectura por la API.
- [~] Respuestas con `IActionResult`. `Ok`, `CreatedAtAction`, `NoContent`, `NotFound`, `Conflict` y `Unauthorized` se usan.
  El 400 lo genera `[ApiController]`, sin `BadRequest` explícito.
- [x] Separación entre modelos de dominio y DTOs.
- [x] Validación con Data Annotations. `[Required]`, `[StringLength]`, `[Range]`, `[EnumDataType]` y `[RegularExpression]`.

### Unidad 6: Arquitectura en capas y servicios

- [x] Organización Controller, Service y Repository.
- [~] Contenedor de dependencias. `AddScoped` y `AddSingleton` se usan en `Program.cs`; `AddTransient` no se usa.

## Bloque 3: Persistencia y datos

Objetivo del bloque: base de datos relacional integrada con EF Core, PostgreSQL, migraciones y CRUD.

### Unidad 7: Fundamentos de EF Core y migraciones

- [x] ORM, `DbContext` y `DbSet`. Ver `MarrowDbContext`.
- [x] Connection string y provider en `Program.cs`.
- [x] Ciclo de vida de migraciones. Hay 7 migraciones, aplicadas en la base local y en Neon.

### Unidad 8: Relaciones entre entidades y CRUD persistido

- [x] Relaciones 1:N con Fluent API.
- [x] Relaciones N:M con Fluent API. Personaje y Habilidad, por `PersonajeHabilidades`. Migración aplicada y sin datos:
  Golpe poderoso se decide por nivel y no usa estas tablas.
- [x] Reemplazo del repositorio en memoria por EF Core. Los repositorios actuales usan `MarrowDbContext`.
- [~] CRUD asincrónico persistido. Es asincrónico en todas las operaciones existentes, pero completo solo en Personajes.

### Unidad 9: Consultas avanzadas, paginado y filtros

- [x] Eager loading con `Include`. Se usa en el estado de la partida; el listado no lo necesita.
- [x] DTOs planos. Por ejemplo, `EstadoPartidaResponse`.
- [x] Paginación con `Skip` y `Take`. `GET /api/partidas` acepta `page` y `pageSize`, y devuelve los totales.
- [x] Filtros dinámicos. `GET /api/partidas` filtra por `estado` y `dificultad`.
- [x] Ordenamiento con LINQ. Por fecha de creación, ascendente o descendente, con el parámetro `orden`.

## Bloque 4: Seguridad y React

Objetivo del bloque: API robusta lista para consumir desde el frontend.

- [x] React. Figura en el título del bloque, sin unidad propia. El frontend tiene conectado el flujo principal, recorrido
  contra la API y PostgreSQL locales el 2026-11-09. Los cambios posteriores del frontend pasaron lint y build, y el
  autor los probó manualmente; no se ejecutó Playwright.

### Unidad 10: JWT y CORS

- [x] Autenticación y autorización con JWT. Generación y validación de firma, emisor, audiencia y expiración.
- [x] Diferencia entre 401 y 403. `401` sin token o con token inválido; `403` con token válido pero sin el rol `Jugador`. Los
  recursos ajenos responden `404`, para no revelar que existen.
- [x] Microsoft Identity: `UserManager`, `RoleManager` e `IdentityDbContext`. Migración aplicada, con registro e
  inicio de sesión probados con usuarios nuevos. No consta una prueba con una cuenta anterior a la migración.
- [x] `IJwtService`. Implementado como `IJwtTokenService`.
- [~] `IAuthService`. Su función la cumple `IUsuarioService`, con otro nombre.
- [x] `AuthController`.
- [x] `[Authorize]`.
- [x] `[AllowAnonymous]`. En `AuthController` y `EnemigosController`.
- [x] Protección por roles. `PersonajesController` y `PartidasController` exigen `[Authorize(Roles = RolesUsuario.Jugador)]`.
  El rol `Administrador` existe y se asigna, pero no protege endpoints porque el alcance no incluye operaciones
  administrativas. Las pruebas de autorización pasaron en la última validación.
- [x] Candado Bearer en Swagger. Aparece en ejecución; no consta que se haya usado con un token.

### Unidad 11: Middleware

- [x] Pipeline HTTP. Configurado en `Program.cs`.
- [x] Manejo global de excepciones. `UseExceptionHandler` y `ManejadorGlobalExcepciones` responden con `ProblemDetails`.
- [x] Data seeding. Enemigos, roles, administrador por configuración segura, rol `Jugador` para usuarios existentes y corrección de
  valores inválidos en personajes, de forma idempotente. Se ejecutó en la base local y en Neon; en esos arranques no se
  creó el administrador ni hubo personajes que corregir.
- [x] CORS para frontends. Los orígenes se configuran por entorno; Production exige indicarlos y no admite `*`.

### Unidad 12: Seed data, despliegue y cierre

Objetivo de la unidad: Web API desplegada en la nube con base productiva y Docker.

- [x] Dockerfile multietapa. API y frontend, con Docker Compose para prueba local. Las imágenes se construyeron y se probaron
  en local el 2026-11-09.
- [x] PostgreSQL en la nube (Render, Supabase o Neon). Se usa Neon, con la conexión comprobada desde la API en modo Production.
- [~] Despliegue continuo desde GitHub. El backend y el frontend están desplegados en Render y el código está en GitHub. El
  repositorio solo incluye CI, sin despliegue. Por verificar en Render: el vínculo con el repositorio y la publicación
  automática.
- [x] Migraciones en producción. Las 7 se aplicaron en la rama `production` de Neon el 2026-11-09.
- [~] Variables de entorno seguras. La configuración por entorno y su validación están implementadas, y `appsettings.json` solo
  tiene marcadores en su cadena local. Por verificar: los valores cargados en Render.
- [~] Pautas finales del proyecto integrador. Por verificar: el documento no las detalla.

## Resumen de pendientes

**Preparado, falta ejecutar o configurar**

- Subir a GitHub y desplegar los cambios locales posteriores a `79246da`.
- Corregir una prueba de `Marrow.API.PostgresTests` y una de Playwright, revisar los flujos E2E que cambiaron y ejecutar ambas
  suites con esos cambios.

**Por verificar**

- El vínculo de Render con GitHub, la publicación automática y las variables cargadas.
- El inicio de sesión con una cuenta anterior a la migración de Identity.
- El uso del candado de Swagger con un token.
- Los puntos marcados "Por verificar" sin evidencia en el repositorio: el modelo de dominio en consola, la etapa de API en
  memoria y las pautas finales.

**Decisiones que conviene consultar con el docente**

- Si CRUD completo en un recurso cubre "operaciones CRUD completas".

## Implementado fuera del programa

Estos puntos no figuran como requisitos en el documento original, pero forman parte del proyecto:

- [x] Experiencia y progresión. Cada victoria otorga los 100 XP del enemigo, subir de nivel cuesta `100 * nivel actual` y el
  nivel máximo es 3, solo en el backend.
- [x] Selección automática del enemigo según el nivel, con estadísticas del catálogo ajustadas por la dificultad.
- [x] Golpe poderoso, desbloqueado desde el nivel 2 y validado en el backend.
- [x] Logging con `ILogger<T>`. Inicio, detención, datos iniciales, autenticación, partidas y errores, sin datos sensibles.
- [x] Estados de carga, vacíos y errores en el frontend.
- [x] Pruebas automatizadas con xUnit y Playwright. Las de Playwright no están integradas en GitHub Actions.
- [x] Integración continua con GitHub Actions. Su ejecución sobre `79246da` fue exitosa.
- [x] Contraataque por colisión y últimos cambios del frontend: sprites, `BattleEndPanel` e ingreso directo a la partida nueva.
  Compilados y probados solo en local, con las pruebas del backend y una comprobación manual del autor; sin Playwright y sin
  desplegar.
- [x] Documentación del proyecto. `README.md`, `docs/GUIA_TECNICA.md` y `docs/DESPLIEGUE.md`.

**Validación de los cambios locales (2026-11-24):**

- **Automatizada:** `Marrow.sln` compiló en Release con 0 errores y 2 advertencias `CS1573` en `ResultadoPartida.cs`. Pasaron
  353 de 353 pruebas de `Marrow.API.Tests` y 216 de 216 de `Marrow.API.IntegrationTests`: 569 en total. El lint y el build del
  frontend terminaron sin errores.
- **Manual:** el autor probó y aprobó el contraataque, el panel final, el ingreso directo a la partida y las animaciones. También
  se comprobó que las 9 importaciones de sprites apuntan a archivos existentes.
- **No ejecutada:** `Marrow.API.PostgresTests` y Playwright.

**Validación anterior (2026-11-09 al 2026-11-11, sobre el código de `79246da`):** 548 de 548 pruebas del backend, 13 de 14 de
PostgreSQL real, 64 de 65 E2E, y lint y build del frontend sin errores. Las dos fallas son de pruebas que hay que corregir.
