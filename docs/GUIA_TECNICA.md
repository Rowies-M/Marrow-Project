# Guía técnica de Marrow

Material de estudio y apoyo para la defensa del proyecto final del bootcamp. Describe la implementación real del repositorio. Lo que todavía no existe está indicado en [Pendientes y límites conocidos](#pendientes-y-límites-conocidos).

Para publicar el proyecto, ver [DESPLIEGUE.md](DESPLIEGUE.md).

## Introducción

### Qué es Marrow

Marrow es un RPG web en 2D, con vista desde arriba. El jugador crea un guerrero, inicia una partida, se mueve por un mapa de 11 × 7 casillas y combate contra el enemigo que corresponde a su nivel. Al ganar obtiene experiencia y puede subir hasta el nivel 3. El progreso queda guardado en una base de datos.

### Objetivo general

Demostrar en una aplicación real lo aprendido en el bootcamp: una API REST en capas, persistencia relacional, autenticación con JWT y un frontend en React que consume esa API.

La decisión técnica central es que **el backend es la autoridad del juego**. El frontend muestra el estado y envía intenciones, como "moverme a la derecha" o "atacar". El daño, la vida y los resultados siempre los calcula el servidor.

### Tecnologías

| Área | Tecnología | Para qué se usa |
|---|---|---|
| Backend | C# y .NET 10 | Lenguaje y plataforma |
| Backend | ASP.NET Core Web API | Endpoints HTTP |
| Datos | Entity Framework Core 10 y Npgsql | Acceso a datos |
| Datos | PostgreSQL | Base de datos |
| Seguridad | Microsoft Identity y JWT Bearer | Usuarios, contraseñas, roles y autenticación |
| Documentación | Swagger (Swashbuckle) | Probar y documentar la API |
| Frontend | React 19, Vite y JavaScript | Interfaz |
| Frontend | Tailwind CSS | Estilos |
| Pruebas | xUnit y Playwright | Backend y flujos del frontend |
| Infraestructura | Docker, Docker Compose y GitHub Actions | Empaquetado e integración continua |

## Arquitectura

### Organización del repositorio

```text
backend/
  Marrow.API/                                  API
  Marrow.API.Tests/                            Pruebas de reglas, servicios y repositorios
  Marrow.API.IntegrationTests/                 Pruebas de la API sin base de datos real
  Marrow.API.IntegrationTests.Infraestructura/ Fábrica y protecciones para esas pruebas
  Marrow.API.PostgresTests/                    Pruebas contra PostgreSQL (fuera de Marrow.sln)
frontend/marrow-web/                           Aplicación React
docs/                                          Documentación
.github/workflows/ci.yml                       Integración continua
docker-compose.yml                             Prueba local de las imágenes
```

### Backend

`Marrow.API` es una Web API organizada por responsabilidad:

```text
Controllers/   Reciben el pedido HTTP y eligen la respuesta
Services/      Reglas de negocio y validaciones
Repositories/  Acceso a datos con EF Core
Interfaces/    Contratos entre capas
Models/        Entidades y enums
DTOs/          Datos que entran y salen por la API
Data/          DbContext y datos iniciales
Middleware/    Manejador global de errores
Migrations/    Historial del esquema de la base
```

### Frontend

`marrow-web` es una aplicación de una sola página. No usa una biblioteca de rutas: `App.jsx` decide qué pantalla mostrar según el estado de la sesión, el personaje activo y la partida elegida.

```text
Login o Registro → Selección de personaje → Listado de partidas → Juego
```

### Comunicación entre frontend y backend

Se comunican por HTTP con JSON, y los errores llegan como `ProblemDetails`. Los endpoints protegidos reciben el token en un encabezado:

```text
Authorization: Bearer <token>
```

Por ejemplo, al atacar:

1. El frontend envía `POST /api/partidas/17/ataque`, sin cuerpo.
2. `PartidasController` toma el usuario del token y llama a `PartidaService`.
3. El servicio lee la partida, aplica las reglas y le pide guardar a `PartidaRepository`.
4. La API responde con el estado actualizado, el daño y la experiencia ganada.
5. React actualiza el mapa y el HUD con esa respuesta, sin calcular nada.

### Endpoints

| Método y ruta | Uso | Token |
|---|---|---|
| `POST /api/auth/register` | Registrar un usuario | No |
| `POST /api/auth/login` | Iniciar sesión | No |
| `GET /api/personajes` | Listar personajes propios | Sí |
| `POST /api/personajes` | Crear un personaje | Sí |
| `GET /api/personajes/{id}` | Ver un personaje propio | Sí |
| `PUT /api/personajes/{id}` | Cambiar el nombre | Sí |
| `DELETE /api/personajes/{id}` | Eliminar un personaje sin partidas | Sí |
| `GET /api/enemigos` | Listar enemigos | No |
| `GET /api/enemigos/{id}` | Ver un enemigo | No |
| `GET /api/partidas` | Listar partidas con filtros y paginación | Sí |
| `POST /api/partidas` | Crear una partida | Sí |
| `GET /api/partidas/{id}` | Ver una partida propia | Sí |
| `GET /api/partidas/{id}/estado` | Ver el estado de juego | Sí |
| `PATCH /api/partidas/{id}/movimiento` | Mover una casilla | Sí |
| `POST /api/partidas/{id}/ataque` | Usar el ataque básico | Sí |
| `POST /api/partidas/{id}/golpe-poderoso` | Usar Golpe poderoso; requiere nivel 2 | Sí |
| `PATCH /api/partidas/{id}/finalizar` | Abandonar | Sí |

Los endpoints que piden token exigen además el rol `Jugador`. Ver [`[Authorize]` y `[AllowAnonymous]`](#authorize-y-allowanonymous).

## Backend

### Responsabilidad de los Controllers

Los controladores no contienen reglas del juego. Solo:

- reciben los datos del pedido;
- obtienen el usuario autenticado;
- llaman al servicio;
- traducen el resultado a un código HTTP.

En `PartidasController.Atacar`, por ejemplo, un resultado exitoso se convierte en `200 OK` y un ataque fuera de alcance en `409 Conflict`. La decisión de si está fuera de alcance la toma el servicio.

### Separación Controller → Service → Repository

Cada capa tiene un solo motivo para cambiar. El mismo ataque, capa por capa:

| Capa | Clase | Qué hace |
|---|---|---|
| Controller | `PartidasController` | Recibe el `POST` y responde 200 o 409 |
| Service | `PartidaService` | Comprueba adyacencia, resuelve el ataque y calcula la experiencia |
| Reglas | `ReglasCombate`, `ReglasProgresion` | Fórmulas sin acceso a datos |
| Repository | `PartidaRepository` | Guarda los cambios en una transacción |

`ReglasCombate` y `ReglasProgresion` son clases estáticas sin dependencias, por lo que se pueden probar directamente con distintos valores.

### Inyección de dependencias

Las clases reciben lo que necesitan por constructor, y el contenedor de ASP.NET Core las crea. El registro está en `Program.cs`:

```csharp
builder.Services.AddScoped<IPartidaRepository, PartidaRepository>();
builder.Services.AddScoped<IPartidaService, PartidaService>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
```

- **`AddScoped`:** una instancia por pedido HTTP. Se usa en servicios y repositorios, porque trabajan con el `DbContext`.
- **`AddSingleton`:** una sola instancia para toda la aplicación. Se usa en `JwtTokenService`, que no guarda estado por pedido.

Como `PartidaService` depende de la interfaz `IPartidaRepository`, las pruebas pueden pasarle un repositorio falso en memoria.

### DTOs y validaciones

Un DTO define qué datos entran y salen por la API, sin exponer las entidades. `CrearPersonajeRequest` solo acepta el nombre, así que el cliente no puede asignarse estadísticas:

```csharp
public sealed record CrearPersonajeRequest
{
    [Required, StringLength(100, MinimumLength = 1)]
    public required string Nombre { get; init; }
}
```

Hay dos niveles de validación:

- **Data Annotations en los DTOs:** como `[Required]`, `[Range]` y `[RegularExpression]`. Por `[ApiController]`, si fallan, la API responde `400` sin ejecutar el controlador.
- **Validaciones en los servicios:** reglas que dependen del negocio o de los datos, como que la partida esté activa.

### Entity Framework Core

EF Core es el ORM: permite consultar y guardar con C# en lugar de escribir SQL. `MarrowDbContext` declara las tablas con `DbSet` y las relaciones con Fluent API.

El listado de partidas muestra varias técnicas juntas:

```csharp
var consulta = context.Partidas.AsNoTracking()
    .Where(partida => partida.Personaje.UsuarioId == usuarioId);
```

- **`Where`:** filtra por usuario, estado y dificultad.
- **`OrderBy` y `ThenBy`:** ordenan por fecha de creación, con el `Id` como desempate.
- **`Skip` y `Take`:** implementan la paginación.
- **`AsNoTracking`:** evita el seguimiento de cambios en lecturas.
- **`Include`:** carga relaciones cuando se usan. `ObtenerEstadoAsync` incluye personaje y enemigo porque la respuesta necesita sus datos. El listado no lo usa, porque no devuelve esos datos.

**Actualizaciones atómicas:** movimiento, ataque y abandono usan `ExecuteUpdateAsync`, con condiciones en el `WHERE`. Si otra petición cambió la partida primero, la actualización no modifica filas y la API responde 409.

### PostgreSQL y migraciones

Las migraciones son el historial versionado del esquema. El repositorio tiene siete:

| Migración | Qué hace |
|---|---|
| `InitialCreate` | Crea las tablas iniciales |
| `SeedEnemigosIniciales` | Inserta los enemigos |
| `PersistirEstadoInicialPartida` | Guarda el estado inicial de cada partida |
| `AgregarDificultadYResultadoCombate` | Agrega dificultad y resultado |
| `AgregarHabilidadesPersonaje` | Crea habilidades y su relación N:M con personajes. **Sin aplicar** |
| `IncorporarIdentityYRoles` | Agrega Identity y conserva a los usuarios existentes. **Sin aplicar** |
| `ActualizarExperienciaEnemigos` | Cambia a 100 la XP de los tres enemigos, sin tocar el esquema. **Sin aplicar** |

La API **no aplica migraciones sola**. Al arrancar comprueba si hay pendientes: si las hay, se detiene y pide aplicarlas. Por eso, con las tres últimas sin aplicar, no arranca contra una base anterior.

Los enemigos se cargan con `HasData` desde `DatosIniciales.cs`. Por eso cambiar un valor del catálogo modifica el modelo de EF Core y requiere una migración: sin ella, las bases existentes conservarían los valores anteriores.

**Precauciones antes de aplicarlas sobre una base con datos:**

1. Hacer un respaldo.
2. Comprobar que no haya nombres de usuario ni emails repetidos sin distinguir mayúsculas. `IncorporarIdentityYRoles` crea un índice único para los nombres: si chocan, falla sin perder datos.

```sql
SELECT UPPER("NombreUsuario"), COUNT(*) FROM "Usuarios" GROUP BY 1 HAVING COUNT(*) > 1;
```

```sql
SELECT UPPER("Email"), COUNT(*) FROM "Usuarios" GROUP BY 1 HAVING COUNT(*) > 1;
```

**Inicialización de datos:** al arrancar, salvo en el entorno `Testing`, `InicializadorDatos`:

1. Agrega los enemigos que falten, definidos en `DatosIniciales.cs` y comparados por `Id`.
2. Corrige los valores inválidos de personajes existentes.
3. Crea los roles `Administrador` y `Jugador` si no existen.
4. Crea el administrador inicial, solo si sus credenciales están configuradas.
5. Asigna `Jugador` a los usuarios que no lo tengan.

Nunca borra datos ni quita roles, y no crea personajes ni partidas.

**Corrección de personajes:** cada actualización afecta solo a las filas con un valor inválido, y los personajes correctos no cambian.

| Valor inválido | Se corrige a |
|---|---|
| Nivel mayor que 3 | 3 |
| Nivel menor que 1 | 1 |
| Experiencia negativa | 0 |
| Vida máxima de 0 o menos | Vida máxima y actual en 80 |
| Ataque de 0 o menos | 15 |
| Defensa negativa | 5 |

La experiencia válida se conserva, también al limitar el nivel.

### Enums y modelos principales

| Modelo | Datos principales |
|---|---|
| `Usuario` | Hereda de `IdentityUser<int>`; suma la fecha de creación |
| `Personaje` | Nombre, nivel, experiencia, estadísticas, clase, dificultad desbloqueada, usuario y habilidades |
| `Enemigo` | Nombre, nivel, vida, ataque, defensa y experiencia otorgada |
| `Partida` | Estado, dificultad, resultado, vidas, posiciones, personaje y enemigo |
| `Habilidad` | Nombre y personajes |

**Relaciones 1:N:** un usuario tiene muchos personajes, un personaje tiene muchas partidas y un enemigo aparece en muchas partidas.

**Relación N:M:** un personaje puede tener varias habilidades, y una habilidad, varios personajes. Se configura con Fluent API en `MarrowDbContext`:

```csharp
modelBuilder.Entity<Personaje>()
    .HasMany(personaje => personaje.Habilidades)
    .WithMany(habilidad => habilidad.Personajes)
    .UsingEntity<Dictionary<string, object>>(
        "PersonajeHabilidades",
        union => union.HasOne<Habilidad>().WithMany().HasForeignKey("HabilidadId"),
        union => union.HasOne<Personaje>().WithMany().HasForeignKey("PersonajeId"),
        union => union.HasKey("PersonajeId", "HabilidadId"));
```

La clave compuesta impide repetir una habilidad en un personaje. Hoy es solo la base del modelo: no hay habilidades cargadas, endpoints ni uso en el combate.

**Eliminación de personajes:** la base borra en cascada las partidas de un personaje, y eso destruiría su historial. Por eso `DELETE /api/personajes/{id}` solo elimina si no tiene partidas, con la comprobación en la misma sentencia SQL. Responde `204`, `404` si no existe o es ajeno, y `409` si tiene partidas.

Partidas y enemigos no tienen `DELETE`: las primeras son el historial del juego, y su fin es el abandono. Los segundos son datos de referencia.

| Enum | Valores |
|---|---|
| `ClasePersonaje` | `Guerrero` |
| `EstadoPartida` | `Activa`, `Finalizada` |
| `Dificultad` | `Normal`, `Dificil` |
| `ResultadoCombate` | `Victoria`, `Derrota` |

En la base se guardan como enteros, y en la API viajan como texto gracias a `JsonStringEnumConverter`.

## Seguridad

### Registro e inicio de sesión

Marrow usa Microsoft Identity para gestionar usuarios y roles, y JWT para autenticar cada pedido. `AddIdentityCore` no agrega cookies, y la arquitectura en capas se mantiene.

1. **Registro:** `UsuarioService` comprueba que el nombre y el email no existan, crea el usuario con `UserManager` y le asigna el rol `Jugador`. Si el rol falla, elimina la cuenta para no dejarla a medio crear.
2. **Inicio de sesión:** busca por nombre de usuario o email, verifica la contraseña con `UserManager` y devuelve un JWT.

Ante credenciales incorrectas, la API responde `401` con el mismo mensaje exista o no el usuario. Los nombres y los emails se comparan sin distinguir mayúsculas.

**Compatibilidad:** `Usuario` hereda de `IdentityUser<int>`, así que los `Id` y las relaciones siguen siendo enteros. La tabla `Usuarios` se conserva, y `NombreUsuario` es un alias de `UserName`, guardado en la misma columna.

**Política de contraseñas:** mínimo 8 caracteres, la misma regla pública que ya tenía el registro.

### Hash de contraseñas

Las contraseñas nunca se guardan en texto plano. `UserManager` usa el hasher de Identity, con PBKDF2 y una sal aleatoria.

Un hash no se puede revertir. Para comprobar una contraseña, `CheckPasswordAsync` la procesa de la misma forma y compara el resultado.

### Roles

| Rol | Quién lo recibe |
|---|---|
| `Jugador` | Cada usuario nuevo y, al arrancar, los que todavía no lo tengan |
| `Administrador` | Solo el administrador inicial, si se configuran `AdministradorInicial:NombreUsuario`, `Email` y `Password` |

Una cuenta existente nunca se promueve a administrador de forma automática. Si faltan esas credenciales, no se crea el administrador y queda un aviso en el log.

**Uso de los roles:** `Jugador` protege todos los endpoints de personajes y partidas. `Administrador` existe y se asigna, pero hoy no protege ningún endpoint: el alcance funcional no incluye operaciones administrativas. El administrador inicial también recibe `Jugador`, así que puede jugar.

### JWT

Un JWT es un texto firmado que el cliente presenta en cada pedido. `JwtTokenService` lo genera con:

- **Datos (claims):** id de usuario, nombre, email, roles y un identificador único del token.
- **Firma:** HMAC-SHA256, con la clave `Jwt:SigningKey`.
- **Vencimiento:** configurable; hoy, 60 minutos.

La API valida firma, emisor, audiencia y vencimiento en cada pedido. El contenido del token es legible, pero alterarlo invalida la firma: por eso no incluye contraseñas.

### `[Authorize]` y `[AllowAnonymous]`

| Controller | Atributo | Acceso |
|---|---|---|
| `PersonajesController` | `[Authorize(Roles = RolesUsuario.Jugador)]` | Token válido con el rol `Jugador` |
| `PartidasController` | `[Authorize(Roles = RolesUsuario.Jugador)]` | Token válido con el rol `Jugador` |
| `AuthController` | `[AllowAnonymous]` | Público: emite el token |
| `EnemigosController` | `[AllowAnonymous]` | Público: catálogo de solo lectura |

**401 y 403:**

- **`401 Unauthorized`:** la API no sabe quién sos, porque falta el token o es inválido.
- **`403 Forbidden`:** la API sabe quién sos, pero tu token no tiene el rol que exige el endpoint.

Un token emitido antes de que el usuario tuviera roles recibe `403` hasta volver a iniciar sesión.

### Identificación del usuario y aislamiento de datos

El id de usuario **nunca viene del cuerpo ni de la URL**. Se toma del token, que no se puede falsificar sin la clave:

```csharp
var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
```

Después, cada consulta filtra por ese usuario:

```csharp
partida => partida.Id == id && partida.Personaje.UsuarioId == usuarioId
```

Si alguien pide una partida ajena, la API responde `404`, igual que si no existiera. Así tampoco revela qué identificadores existen.

### User Secrets y variables de entorno

La configuración sensible real va fuera del repositorio:

- **Development:** User Secrets guarda la conexión y la clave JWT fuera del repositorio.
- **Production:** se usan variables de entorno, como `Jwt__SigningKey`. El `__` equivale a `:` en la configuración de .NET.

`appsettings.json` trae una cadena de conexión local que Production no utiliza. Ver [Pendientes y límites conocidos](#pendientes-y-límites-conocidos).

## Funcionamiento del juego

### Personajes y partidas

- **Personaje:** el cliente solo envía el nombre. El backend asigna nivel 1, 0 de experiencia, clase Guerrero y el resto de los valores iniciales. Solo se puede cambiar el nombre, y eliminarlo si no tiene partidas.
- **Partida:** pertenece a un personaje del usuario. Empieza con el jugador en la casilla (1, 1), el enemigo en (5, 3) y la dificultad elegida al crearla, Normal por defecto.
- **Enemigo:** cada partida tiene uno solo, y lo elige el backend al crearla. `POST /api/partidas` solo recibe el personaje, así que el cliente no puede elegirlo ni forzarlo.

| Nivel del personaje | Enemigo |
|---|---|
| 1 | Goblin |
| 2 | Esqueleto |
| 3 | Orco |

`PartidaService` busca en el catálogo el enemigo cuyo `Nivel` coincide con el del personaje, sin repetir sus valores en otras clases. El enemigo queda fijo en la partida, aunque después el personaje suba de nivel.

### Movimiento

El mapa va de 0 a 10 en X y de 0 a 6 en Y.

1. El frontend envía `PATCH /api/partidas/{id}/movimiento` con la dirección.
2. El repositorio actualiza la posición en un solo `UPDATE`, limitándola a los bordes y excluyendo la casilla del enemigo.
3. Si intenta entrar en la casilla del enemigo o la partida terminó, la API responde `409`.

Se envía una petición por casilla, nunca una por fotograma. Mantener una tecla apretada no repite el movimiento.

### Combate

Para atacar, el jugador debe estar en una casilla adyacente, sin diagonales.

```text
1. El jugador golpea.
2. Si el enemigo llega a 0: Victoria, sin contraataque.
3. Si sobrevive: el enemigo contraataca.
4. Si el jugador llega a 0: Derrota.
5. Si ninguno muere: la partida sigue activa.
```

**De dónde salen los valores**

- **Personaje:** usa su `VidaMaxima`, `Ataque` y `Defensa`. Un personaje nuevo tiene 80, 15 y 5. Subir de nivel no los cambia.
- **Enemigo:** usa su vida, ataque, defensa y experiencia del catálogo.
- **Dificultad:** Normal usa el catálogo tal cual. Difícil duplica la vida del enemigo y aumenta su ataque un 50 %, redondeando hacia abajo. No cambia su defensa ni su experiencia.
- **Daño:** `ataque - defensa`, con un mínimo de 1.

| Enemigo | Vida | Ataque | Defensa | XP | Vida en Difícil | Ataque en Difícil |
|---|---|---|---|---|---|---|
| Goblin | 30 | 5 | 2 | 100 | 60 | 7 |
| Esqueleto | 45 | 8 | 3 | 100 | 90 | 12 |
| Orco | 80 | 12 | 5 | 100 | 160 | 18 |

**Desbloqueo de Difícil**

- Normal está disponible desde el principio y es la opción predeterminada.
- Ganar una partida en Normal guarda `Dificil` en `Personaje.DificultadDesbloqueada`, en la misma transacción que el resultado. Esto no selecciona Difícil: cada partida usa la dificultad elegida al crearla.
- Una vez desbloqueada, permanece disponible. Ganar en Difícil no modifica el desbloqueo, y las derrotas y los abandonos no desbloquean nada.
- `POST /api/partidas` acepta `dificultad`, opcional. Si se pide Difícil sin haberla desbloqueado, responde `409` y no crea la partida. El selector del frontend solo es una ayuda visual: la decisión es del backend.
- `GET /api/personajes` informa `dificultadDesbloqueada`, para que el frontend sepa si habilitar la opción.
- La dificultad es independiente del nivel y del enemigo: define sus modificadores, no cuál enemigo toca.

### Golpe poderoso

Es la única habilidad especial, y está disponible desde el nivel 2.

- **Efecto:** duplica el ataque del personaje en ese golpe, antes de restar la defensa. El enemigo contraataca igual que con el ataque básico.
- **Validación:** `PartidaService` comprueba el nivel en cada uso. Si es menor que 2, responde `409` sin modificar la partida.
- **Sin coste ni espera:** se puede usar en todos los turnos. Por eso, desde el nivel 2 siempre conviene más que el ataque básico.

**Implementación:** una regla por nivel en `ReglasCombate`. No usa la tabla N:M de habilidades ni un sistema genérico, y no agregó columnas.

Con un personaje nuevo, atacando siempre de la misma forma:

| Enemigo y dificultad | Ataque básico | Golpe poderoso |
|---|---|---|
| Goblin, Normal | Gana con 78 de vida | No disponible en nivel 1 |
| Esqueleto, Normal | Gana con 71 de vida | Gana con 77 de vida |
| Orco, Normal | Gana con 31 de vida | Gana con 59 de vida |
| Goblin, Difícil | Gana con 72 de vida | No disponible en nivel 1 |
| Esqueleto, Difícil | Gana con 31 de vida | Gana con 59 de vida |
| Orco, Difícil | **Pierde** | Gana con 2 de vida |

### Responsabilidad del backend

Los pedidos de ataque **no llevan cuerpo**: el cliente no puede enviar daño, vida, resultado ni desbloqueos. `ReglasCombate` calcula todo. El frontend solo usa la adyacencia y el dato `golpePoderosoDesbloqueado` para habilitar los botones. Es una ayuda visual: si no coincide, el backend igual rechaza la acción.

### Finalización de partidas

| Cómo termina | Resultado | Experiencia |
|---|---|---|
| El enemigo llega a 0 | `Victoria` | 100, la que otorga el enemigo |
| El jugador llega a 0 | `Derrota` | 0 |
| El jugador abandona | Sin resultado (`null`) | 0 |

Una partida finalizada no se puede mover, atacar ni reanudar. El listado muestra "Abandonada" cuando no hay resultado, porque el backend solo deja una partida finalizada sin resultado al abandonarla.

### Experiencia y subida de nivel

Cada victoria otorga los 100 XP del enemigo derrotado; derrotas y abandonos no otorgan experiencia. Subir de nivel cuesta `100 * nivel actual`: 100 XP del nivel 1 al 2, y 200 XP del nivel 2 al 3. El excedente se conserva. El nivel mínimo es 1 y el máximo es 3:

| Antes | Después de ganar |
|---|---|
| Nivel 1, 0 XP | Nivel 2, 0 XP |
| Nivel 1, 50 XP | Nivel 2, 50 XP |
| Nivel 2, 100 XP | Nivel 3, 0 XP |
| Nivel 2, 150 XP | Nivel 3, 50 XP |
| Nivel 3, 290 XP | Nivel 3, 390 XP |

**Ritmo:** un personaje nuevo llega al nivel 2 con 1 victoria, y al nivel 3 con 2 victorias más.

**Nivel máximo:** en el nivel 3 la experiencia sigue acumulándose, pero el nivel no cambia. La API informa `personajeExperienciaSiguienteNivel` como `null`, y el frontend muestra "Nivel máximo".

Subir de nivel no cambia vida, ataque ni defensa.

**Cómo se evita pagar dos veces una victoria:** el repositorio guarda partida y progreso en la misma transacción. La recompensa solo se aplica si el `UPDATE` que exige `Estado = Activa` modificó la fila, y esa transición ocurre una sola vez.

**Dos victorias simultáneas del mismo personaje:** el progreso solo se guarda si el nivel y la experiencia siguen iguales a los leídos. Si cambiaron, se revierte la transacción y la API responde `409` para reintentar.

## Frontend

### Páginas y componentes principales

| Archivo | Responsabilidad |
|---|---|
| `App.jsx` | Decide qué pantalla mostrar y conserva los filtros de partidas |
| `LoginPage`, `RegisterPage` | Autenticación |
| `CharacterSelectionPage` | Listar, elegir y crear personajes |
| `PartidasPanel` | Listado, filtros y paginación |
| `GamePage` | Coordina la partida y las llamadas a la API |
| `GameScene` | Dibuja el mapa, el guerrero y el enemigo |
| `GameHud` | Vida, nivel y experiencia |
| `CombatPanel` | Botones de ataque y Golpe poderoso, y daño del último golpe |
| `GameInfoPanel` | Datos de la partida y botón de abandono |
| `GameResultPanel` | Victoria, derrota o partida finalizada |
| `TouchControls` | Flechas para pantallas táctiles |

Dos hooks propios separan la entrada del jugador: `useGameKeyboard` para W, A, S, D, flechas y Espacio, y `useIsTouchDevice` para detectar pantallas táctiles. El teclado y las flechas táctiles llaman a la misma función de movimiento.

### Contexto de autenticación

`AuthContext.jsx` guarda la sesión y la comparte con toda la aplicación, sin pasarla por cada componente. `useAuth()` da acceso al token, al usuario y a las funciones de login y logout.

### Servicios para consumir la API

Los componentes no llaman a `fetch` directamente:

- **`apiClient.js`:** único lugar con la URL de la API. Agrega el token, aplica un límite de 15 segundos y traduce los errores.
- **`authService.js`, `personajeService.js`, `partidaService.js`:** una función por operación, como `attackPartida(token, partidaId)`.

### Sesión, errores y estados de carga

- **Sesión:** se guarda en `sessionStorage`. Sobrevive a una recarga y se pierde al cerrar la pestaña.
- **Sesión vencida:** un `401` con token limpia la sesión y vuelve al login con un aviso.
- **Errores:** `apiClient` convierte los `ProblemDetails` y los cortes de red en mensajes comprensibles.
- **Carga:** las pantallas muestran un mensaje mientras esperan, y los botones se deshabilitan para evitar envíos repetidos.
- **Vacío o error:** una lista vacía y un error de API son estados distintos. El error ofrece `Reintentar`.

## Pruebas automatizadas

| Proyecto | Qué cubre | ¿Requiere PostgreSQL? |
|---|---|---|
| `Marrow.API.Tests` | Reglas, servicios, repositorios e Identity | No: usa dobles y conexiones interceptadas |
| `Marrow.API.IntegrationTests` | API, fábrica de pruebas, protecciones y separación de proyectos | No |
| `Marrow.API.IntegrationTests.Infraestructura` | No contiene pruebas: aporta la fábrica y las protecciones | No |
| `Marrow.API.PostgresTests` | Conexión real | **Sí** |
| `frontend/marrow-web/tests` | Flujos con Playwright y API simulada | No |

- `dotnet test backend/Marrow.sln` ejecuta las dos primeras suites.
- **Autorización:** las pruebas comprueban que partidas exija el rol `Jugador` y que un token sin ese rol reciba `403`. Se modificaron después de la última ejecución y todavía no se ejecutaron.
- **`Marrow.API.PostgresTests`** está fuera de la solución a propósito. Requiere `MARROW_TEST_CONNECTION`, una base `marrow_test` preparada y autorización expresa, porque se conecta a una base real. Todavía falta prepararla.
- **Playwright** se ejecuta en local con `npm run test:e2e`. No está en GitHub Actions, porque `playwright.config.js` usa `npm.cmd`, que solo existe en Windows.

## Configuración y ejecución

### Requisitos

- .NET SDK 10
- Node.js y npm
- PostgreSQL

### Configurar PostgreSQL y User Secrets

Con una base de datos creada, y desde la raíz del repositorio:

```bash
dotnet user-secrets set "ConnectionStrings:MarrowDatabase" "Host=localhost;Port=5432;Database=TU_BASE;Username=TU_USUARIO;Password=TU_CONTRASEÑA" --project backend/Marrow.API
```

```bash
dotnet user-secrets set "Jwt:SigningKey" "UNA_CLAVE_ALEATORIA_DE_AL_MENOS_32_BYTES" --project backend/Marrow.API
```

Después, aplicá las migraciones:

```bash
dotnet tool restore
```

```bash
dotnet ef database update --project backend/Marrow.API
```

### Iniciar backend y frontend

En una terminal:

```bash
dotnet run --project backend/Marrow.API --launch-profile http
```

En otra:

```bash
npm install --prefix frontend/marrow-web
```

```bash
npm run dev --prefix frontend/marrow-web
```

| Servicio | URL local |
|---|---|
| Frontend | `http://localhost:5173` |
| API | `http://localhost:5137` |
| Swagger | `http://localhost:5137/swagger` |

Para probar un endpoint protegido en Swagger, iniciá sesión, copiá el token y pegalo en **Authorize**, sin la palabra `Bearer`.

### Development y Production

| Aspecto | Development | Production |
|---|---|---|
| Secretos | User Secrets | Variables de entorno |
| Swagger | Activo | Desactivado |
| Errores | Incluyen detalle técnico | Sin detalle técnico |
| CORS | `http://localhost:5173` | Debe configurarse; sin él, la API no arranca |
| Configuración faltante | Sin validación estricta al inicio | Detiene el arranque e indica la variable |
| URL de la API en el frontend | `localhost:5137` por defecto | `VITE_API_URL` obligatoria al construir |
| Base de datos | PostgreSQL local | PostgreSQL externo; la opción prevista es Neon |

En Production, la conexión se configura con `ConnectionStrings__MarrowDatabase`, en formato clave=valor. Los pasos para Neon y Docker están en [DESPLIEGUE.md](DESPLIEGUE.md).

### Docker e integración continua

- **Docker:** la API y el frontend tienen un Dockerfile multietapa cada uno, y sus imágenes finales no incluyen SDK, Node ni código fuente. Los secretos llegan por variables de entorno, y `appsettings.json` no entra en la imagen. `docker-compose.yml` sirve para probarlas en local, sin PostgreSQL.
- **CI:** `.github/workflows/ci.yml` corre en cada push y Pull Request a `master` o `main`. Compila y prueba el backend, y ejecuta lint y build del frontend. No despliega nada.

Ambos están configurados, pero todavía no se ejecutaron.

## Conceptos importantes para defender

### REST

Una forma de diseñar APIs sobre HTTP: las URLs representan recursos, y los métodos, lo que se hace con ellos.

**En Marrow:** `GET /api/personajes` lista y `POST /api/personajes` crea. Los códigos de estado comunican el resultado: `201` creado, `401` sin autenticar, `403` sin el rol necesario, `404` no encontrado, `409` conflicto con el estado actual.

### DTO

Un objeto que define los datos que cruzan la API, separado de la entidad de la base.

**En Marrow:** `PersonajeResponse` no expone `UsuarioId`, y `CrearPersonajeRequest` solo acepta el nombre.

### Inyección de dependencias

Una clase recibe sus dependencias desde afuera, en lugar de crearlas.

**En Marrow:** `PartidaService` recibe `IPartidaRepository` por constructor. En producción es el repositorio real, y en las pruebas, uno falso en memoria.

### Repository Pattern

Concentra el acceso a datos detrás de una interfaz, para que el resto de la aplicación no dependa de cómo se guardan.

**En Marrow:** `PartidaService` llama a `AplicarAtaqueAsync`, sin conocer el SQL ni la transacción que usa `PartidaRepository`.

### Service Layer

Una capa que concentra las reglas de negocio, separadas de HTTP y de la base.

**En Marrow:** `PartidaService.AtacarAsync` comprueba el estado, resuelve el ataque y calcula el progreso. No sabe qué es un `409`: eso lo decide el controlador.

### ORM

Traduce entre objetos del código y tablas de la base.

**En Marrow:** `context.Partidas.Where(...)` genera un `SELECT` en PostgreSQL mediante EF Core.

### Migraciones

Archivos versionados con los cambios del esquema, para que cualquier base llegue al mismo estado.

**En Marrow:** `AgregarDificultadYResultadoCombate` agregó esas columnas, y `dotnet ef database update` aplica las pendientes.

### Middleware

Componentes por los que pasa cada pedido, en orden. Marrow los configura así:

```text
CORS → Manejo de errores → Swagger (solo en Development) → Redirección HTTPS
     → Autenticación → Autorización → Controllers
```

**En Marrow:** `UseExceptionHandler` captura lo que nadie manejó y `ManejadorGlobalExcepciones` lo convierte en `ProblemDetails`. CORS va primero para que el navegador también pueda leer las respuestas de error.

### JWT

Un token firmado que prueba quién es el usuario, sin guardar sesiones en el servidor.

**En Marrow:** se genera al iniciar sesión, el frontend lo envía en `Authorization`, y el id que contiene decide qué partidas se pueden ver.

### CORS

Una protección del navegador: un sitio solo puede leer respuestas de otro origen si ese origen lo permite.

**En Marrow:** el frontend (`:5173`) y la API (`:5137`) son orígenes distintos. `Cors:AllowedOrigins` define los permitidos, junto con los métodos y encabezados necesarios. CORS no reemplaza a la autenticación: solo la complementa en el navegador.

### Logging

El registro de lo que ocurre en la aplicación, para diagnosticar problemas.

**En Marrow:** `ILogger<PartidaService>` registra `Partida {PartidaId} creada...` en nivel `Information`, y un inicio de sesión fallido en `Warning`. Se registran identificadores, nunca contraseñas, tokens ni cadenas de conexión.

### Microsoft Identity y roles

Identity resuelve lo común de las cuentas: usuarios, contraseñas y roles. Un rol agrupa permisos para autorizar con `[Authorize(Roles = ...)]`.

**En Marrow:** `UserManager` crea y valida usuarios, `RoleManager` crea `Administrador` y `Jugador`, y JWT sigue siendo la autenticación de la API. Personajes y partidas exigen el rol `Jugador`.

### Relación N:M

Dos entidades donde cada una puede relacionarse con muchas de la otra, mediante una tabla intermedia.

**En Marrow:** `PersonajeHabilidades` une personajes y habilidades, y su clave compuesta evita duplicados.

### Docker

Empaqueta una aplicación con lo necesario para ejecutarla. Un Dockerfile multietapa compila en una imagen y deja solo el resultado en la final, más liviana.

**En Marrow:** la API se publica con el SDK de .NET y corre solo con el runtime. El frontend se construye con Node y lo sirve nginx.

### Integración continua

Comprueba automáticamente cada cambio al subirlo.

**En Marrow:** GitHub Actions compila y prueba el backend, y ejecuta lint y build del frontend.

## Pendientes y límites conocidos

Conviene conocerlos antes de la defensa, para no presentarlos como terminados.

**Preparado, pero sin ejecutar**

| Tema | Estado actual |
|---|---|
| Identity, roles y N:M | Implementados en código. Sus dos migraciones no se aplicaron, y la API no arranca contra una base sin ellas |
| XP de 100 en el catálogo | En código. La base conserva 10, 20 y 35 hasta aplicar `ActualizarExperienciaEnemigos` |
| Corrección de personajes | En código. Todavía no se ejecutó contra una base |
| Neon | Documentado. No existe el proyecto ni una conexión real |
| Docker | Configurado. No se construyeron imágenes ni se levantaron contenedores |
| CI | Configurado. No se ejecutó en GitHub, y el remoto todavía no está conectado |
| Pruebas de PostgreSQL real | Existen, pero requieren preparación |
| Despliegue real | Solo está preparada la configuración. No se publicó |

**No implementado**

| Tema | Estado actual |
|---|---|
| Datos de la relación N:M | Las tablas de habilidades existen sin datos. Golpe poderoso se decide por nivel y no las usa |
| Funciones de administración | Fuera del alcance funcional: el rol `Administrador` existe, pero no protege ningún endpoint |
| `DELETE` de partidas y enemigos | No existe, a propósito. El CRUD completo solo está en personajes |
| Interfaz para eliminar personajes | El endpoint existe, pero el frontend no lo usa |
| Playwright en CI | No está integrado |
| Despliegue continuo | No está configurado |

**Límites del juego**

- **Derrota poco frecuente:** con las estadísticas iniciales, solo se puede perder contra el Orco en Difícil. En Normal se gana contra los tres enemigos incluso solo con el ataque básico.
- **Golpe poderoso domina desde el nivel 2:** no tiene coste ni espera, así que el ataque básico deja de convenir. Contra el Orco en Difícil, solo usándolo casi siempre se puede ganar.
- **Partidas anteriores:** las vidas máximas se calculan con las reglas actuales. Una partida creada antes de este cambio puede mostrar una vida actual distinta de esas referencias.
- **El enemigo no se mueve:** solo responde cuando lo atacan.
- **Movimiento sin mazmorra real:** el mapa no tiene paredes ni obstáculos.

**Límites técnicos**

- **Listado:** muestra las partidas de todos los personajes del usuario y marca las ajenas al personaje activo, sin filtrarlas.
- **Resultado en el listado:** se obtiene con una petición por cada partida finalizada de la página, porque `PartidaResponse` no lo incluye.
- **Registro simultáneo:** el nombre tiene índice único, y dos registros simultáneos con el mismo nombre podrían terminar en un `500`. El email se comprueba en el servicio, sin índice único.
- **Filtros:** se pierden al recargar la página.
- **Validación:** esta guía se escribió antes de la validación general final y sin recorrer el flujo completo contra la API y PostgreSQL reales.
