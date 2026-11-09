# Requisitos del bootcamp: seguimiento en Marrow

Síntesis de `Contenido para proyecto final bootcamp devlights.docx`, organizada según sus cuatro bloques y unidades.
El estado se revisó el 2026-11-06 sobre el commit `ad15a3d` y los cambios locales posteriores, leyendo el código. Para esta
revisión no se compiló, no se ejecutaron pruebas y no se consultó ninguna base de datos.

**Esto no es una rúbrica de aprobación.** Es un seguimiento de los contenidos del programa. El documento no indica cuáles
son obligatorios para aprobar, y "Pautas finales del proyecto integrador" aparece sin detalle.

## Alcance funcional definitivo de Marrow

Es la decisión del proyecto, no un requisito del programa. Todos sus puntos están implementados en código, sin validar todavía en ejecución.

- Mapa de 11 × 7 casillas y movimiento en cuatro direcciones.
- Una clase: Guerrero.
- Nivel mínimo 1 y máximo 3. Cada victoria otorga 100 XP; subir cuesta 100 XP al nivel 2 y 200 XP al nivel 3.
- Un enemigo por partida, asignado por el backend según el nivel: Goblin (1), Esqueleto (2) y Orco (3).
- Combate cuerpo a cuerpo, con ataque básico desde el nivel 1.
- Una habilidad especial: Golpe poderoso, desde el nivel 2.
- Dificultades Normal, predeterminada, y Difícil, que se desbloquea al ganar en Normal y se elige al crear la partida.
- Sin inventario, equipamiento, tiendas, misiones, NPC, multijugador, mapas adicionales ni árboles de habilidades.

## Referencias

- `[x]` **Implementado en código.** Hay evidencia en el repositorio; no significa que ya se haya validado en esta etapa.
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
- [x] Documentación con Swagger/OpenAPI. Comentarios XML, códigos HTTP y JWT, solo en desarrollo. Falta abrirlo en ejecución.

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
- [P] Ciclo de vida de migraciones. Hay 7 migraciones; `AgregarHabilidadesPersonaje`, `IncorporarIdentityYRoles` y
  `ActualizarExperienciaEnemigos` no se aplicaron.

### Unidad 8: Relaciones entre entidades y CRUD persistido

- [x] Relaciones 1:N con Fluent API.
- [x] Relaciones N:M con Fluent API. Personaje y Habilidad, por `PersonajeHabilidades`. Migración sin aplicar y sin datos:
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

- [~] React. Figura en el título del bloque, sin unidad propia. El frontend tiene conectado el flujo principal. Por
  verificar: recorrido contra la API y PostgreSQL reales.

### Unidad 10: JWT y CORS

- [x] Autenticación y autorización con JWT. Generación y validación de firma, emisor, audiencia y expiración.
- [x] Diferencia entre 401 y 403. `401` sin token o con token inválido; `403` con token válido pero sin el rol `Jugador`. Los
  recursos ajenos responden `404`, para no revelar que existen.
- [x] Microsoft Identity: `UserManager`, `RoleManager` e `IdentityDbContext`. Migración sin aplicar y sin prueba real de
  inicio de sesión con usuarios existentes.
- [x] `IJwtService`. Implementado como `IJwtTokenService`.
- [~] `IAuthService`. Su función la cumple `IUsuarioService`, con otro nombre.
- [x] `AuthController`.
- [x] `[Authorize]`.
- [x] `[AllowAnonymous]`. En `AuthController` y `EnemigosController`.
- [x] Protección por roles. `PersonajesController` y `PartidasController` exigen `[Authorize(Roles = RolesUsuario.Jugador)]`.
  El rol `Administrador` existe y se asigna, pero no protege endpoints porque el alcance no incluye operaciones
  administrativas. Las pruebas de autorización se modificaron y todavía no se ejecutaron.
- [x] Candado Bearer en Swagger. Falta probarlo en ejecución.

### Unidad 11: Middleware

- [x] Pipeline HTTP. Configurado en `Program.cs`.
- [x] Manejo global de excepciones. `UseExceptionHandler` y `ManejadorGlobalExcepciones` responden con `ProblemDetails`.
- [x] Data seeding. Enemigos, roles, administrador por configuración segura, rol `Jugador` para usuarios existentes y corrección de
  valores inválidos en personajes, de forma idempotente. Todavía no se ejecutó contra una base.
- [x] CORS para frontends. Los orígenes se configuran por entorno; Production exige indicarlos y no admite `*`.

### Unidad 12: Seed data, despliegue y cierre

Objetivo de la unidad: Web API desplegada en la nube con base productiva y Docker.

- [P] Dockerfile multietapa. API y frontend configurados, con Docker Compose para prueba local. No se construyeron imágenes
  ni se levantaron contenedores.
- [P] PostgreSQL en la nube (Render, Supabase o Neon). Neon está documentado en `docs/DESPLIEGUE.md`. No existe el proyecto ni
  una conexión real.
- [ ] Despliegue continuo desde GitHub. No está configurado. Existe CI sin despliegue, y el remoto todavía no está conectado.
- [P] Migraciones en producción. El procedimiento está documentado; no se ejecutó.
- [P] Variables de entorno seguras. La configuración por entorno y su validación están listas; falta cargarlas en un despliegue
  real. `appsettings.json` solo tiene marcadores en su cadena local.
- [~] Pautas finales del proyecto integrador. Por verificar: el documento no las detalla.

## Resumen de pendientes

**Preparado, falta ejecutar o configurar**

- Aplicar las migraciones `AgregarHabilidadesPersonaje`, `IncorporarIdentityYRoles` y `ActualizarExperienciaEnemigos`.
- Probar Identity, roles y datos iniciales con una base real.
- Recorrer en ejecución los tres enemigos, Golpe poderoso y el nivel máximo.
- Crear el proyecto en Neon y comprobar la conexión.
- Construir las imágenes Docker y levantar los contenedores.
- Conectar el remoto de GitHub y ejecutar el CI por primera vez.
- Realizar el despliegue real.
- Preparar y ejecutar las pruebas de PostgreSQL real (`Marrow.API.PostgresTests`).

**Pendiente de desarrollo**

- Despliegue continuo desde GitHub.

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
- [P] Integración continua con GitHub Actions. Configurada; todavía no se ejecutó en GitHub.
- [x] Documentación del proyecto. `README.md`, `docs/GUIA_TECNICA.md` y `docs/DESPLIEGUE.md`.

**Validación:** esta checklist no registra resultados de pruebas. La validación general del estado actual todavía está pendiente.
