# Despliegue de Marrow

Marrow usa tres servicios. El despliegue actual está en Render y Neon, pero el código no depende de esos proveedores:

| Servicio | Requisito | Despliegue actual |
|---|---|---|
| Base de datos | PostgreSQL 13 o superior | Neon |
| Backend | Runtime de .NET 10, con HTTPS provisto por la plataforma | Render |
| Frontend | Hosting de archivos estáticos, con HTTPS | Render |

```text
Navegador  →  Frontend (Render)
Navegador  →  HTTPS/JSON con JWT  →  API (Render)  →  SSL  →  PostgreSQL (Neon)
```

El navegador descarga el frontend y desde ahí llama directamente a la API. Solo la API se conecta a la base.

**Estado al 24 de noviembre de 2026**

- **Despliegue:** en Render, `marrow-api` y `marrow-frontend` están en estado Live, desplegados desde el commit `2d328ce`, con la base en Neon. Ambos tienen **Auto-Deploy** en **On Commit**.
- **Configuración:** está en los paneles de Render y Neon, no en el repositorio. Las variables cargadas en Render no se revisaron para esta guía.
- **Versión publicada:** `2d328ce` incluye el contraataque por colisión y los últimos cambios del frontend. Antes de publicarla, en local, el backend compiló en Release y pasaron sus 569 pruebas, el lint y el build del frontend terminaron sin errores, y el autor probó los cambios manualmente. No se ejecutaron `Marrow.API.PostgresTests` ni Playwright.

Las secciones siguientes explican cómo se configura cada parte.

## 1. Base de datos

1. Creá una base de datos PostgreSQL y guardá su cadena de conexión. Para Neon, seguí los pasos de la sección siguiente.
2. Aplicá las migraciones desde una máquina con el SDK de .NET y acceso a esa base. La API no las aplica sola: si faltan, no arranca.

Desde la raíz del repositorio, restaurá la herramienta `dotnet-ef`, que ya está definida en `dotnet-tools.json`:

```bash
dotnet tool restore
```

Con `ASPNETCORE_ENVIRONMENT`, `ConnectionStrings__MarrowDatabase` y `Jwt__SigningKey` definidas en esa terminal, aplicá las migraciones:

```bash
dotnet ef database update --project backend/Marrow.API
```

### Migraciones

El repositorio tiene siete. `dotnet ef database update` aplica las que falten, en este orden:

| Orden | Migración | Qué hace |
|---|---|---|
| 1 | `InitialCreate` | Crea las tablas iniciales |
| 2 | `SeedEnemigosIniciales` | Inserta los enemigos |
| 3 | `PersistirEstadoInicialPartida` | Guarda el estado inicial de cada partida |
| 4 | `AgregarDificultadYResultadoCombate` | Agrega dificultad y resultado |
| 5 | `AgregarHabilidadesPersonaje` | Crea las habilidades y su relación con personajes |
| 6 | `IncorporarIdentityYRoles` | Agrega Identity y conserva a los usuarios existentes |
| 7 | `ActualizarExperienciaEnemigos` | Cambia a 100 la XP de los tres enemigos |

**Estado:** las siete están aplicadas en la base local de desarrollo y en la rama `production` de Neon. El commit `2d328ce` no agrega migraciones.

- **Base nueva, como Neon:** se aplican las siete. La migración 7 corrige los valores que inserta la 2, así que no hay que saltear ninguna ni aplicarlas a mano.
- **Base con datos:** antes de aplicar la migración 6, hacé un respaldo y seguí las [precauciones de la guía técnica](GUIA_TECNICA.md#postgresql-y-migraciones). Si hay nombres de usuario repetidos sin distinguir mayúsculas, la migración falla.

Para ver cuáles faltan en la base configurada, este comando se conecta a ella, pero no la modifica:

```bash
dotnet ef migrations list --project backend/Marrow.API
```

### Qué hace la API al arrancar

Primero comprueba las migraciones: si falta alguna, se detiene y no modifica datos. Después, en cada arranque y en este orden:

1. **Enemigos:** agrega los que falten, sin modificar los existentes.
2. **Personajes:** corrige solo los valores inválidos, y registra en el log cuántas correcciones hizo:

| Valor inválido | Se corrige a |
|---|---|
| Nivel mayor que 3 | 3 |
| Nivel menor que 1 | 1 |
| Experiencia negativa | 0 |
| Vida máxima de 0 o menos | Vida máxima y actual en 80 |
| Ataque de 0 o menos | 15 |
| Defensa negativa | 5 |

3. **Roles:** crea `Administrador` y `Jugador` si no existen.
4. **Administrador inicial:** lo crea solo si están configuradas sus tres variables. Sin ellas, deja un aviso en el log. Nunca convierte en administrador a una cuenta existente. Si no puede crearlo, por ejemplo por una contraseña de menos de 8 caracteres, lo registra en el log y la API sigue arrancando.
5. **Rol `Jugador`:** lo asigna a los usuarios que no lo tengan.

No elimina personajes, partidas ni usuarios, y la experiencia válida se conserva.

### PostgreSQL en Neon

Neon ofrece PostgreSQL administrado y es la base de producción actual. Estos pasos sirven para configurarla desde cero. Revisá en su sitio las condiciones vigentes del plan gratuito, porque pueden cambiar.

**Crear el proyecto**

1. Creá una cuenta en [neon.com](https://neon.com).
2. Creá un proyecto y elegí una región cercana al servicio donde vas a desplegar la API.
3. Usá una versión de PostgreSQL 13 o superior.

**Obtener los datos de conexión**

1. En la consola de Neon, hacé clic en **Connect**.
2. Elegí la rama, la base de datos y el rol.
3. Desactivá **Connection pooling** para obtener el host directo, sin `-pooler`.

**Convertir la cadena al formato de Marrow**

Neon muestra una URI, pero Npgsql documenta el formato clave=valor, y es el que corresponde usar:

```text
URI de Neon:
postgresql://USUARIO:CONTRASEÑA@HOST/BASE?sslmode=require&channel_binding=require

Valor para Marrow:
Host=HOST;Database=BASE;Username=USUARIO;Password=CONTRASEÑA;SSL Mode=Require
```

Si la contraseña de la URI tiene caracteres codificados, como `%40`, escribí el carácter original (`@`). Si contiene `;`, encerrala entre comillas simples.

**Configurar la API desplegada**

Cargá ese valor en el panel del proveedor donde se ejecuta la API, como variable `ConnectionStrings__MarrowDatabase`, junto con las demás variables de la sección 2. No hace falta ningún cambio de código.

Se recomienda el host directo también para la API: es la opción más simple para este proyecto. Con `-pooler`, Neon usa PgBouncer en modo transacción, que tiene limitaciones, y esa combinación no está probada en Marrow.

**Mantener las credenciales fuera de Git**

- No escribas la cadena real en `appsettings*.json`, `docker-compose.yml`, `.env.example` ni la documentación.
- Para trabajar en local, usá `backend/.env`, que Git ignora.
- No uses una variable `VITE_`, porque quedaría visible en el navegador.
- Si la contraseña se expone, cambiala en la consola de Neon y actualizá la variable.

**Precauciones para aplicar las migraciones en Neon**

1. **Usá el host directo:** Neon recomienda la conexión directa para migraciones de esquema.
2. **Comprobá el destino:** confirmá que `ConnectionStrings__MarrowDatabase` apunte a Neon y no a tu base local antes de ejecutar el comando.
3. **Revisá el SQL antes de aplicarlo:** este comando lo genera sin conectarse ni escribir en la base:

```bash
dotnet ef migrations script --idempotent --project backend/Marrow.API --output migraciones.sql
```

4. **Base nueva:** en una base vacía se aplican las siete migraciones, en el orden indicado arriba.
5. **Datos existentes:** las migraciones no copian datos desde la base local. Si cargaste datos antes, hacé un respaldo y comprobá que no haya nombres de usuario ni emails duplicados sin distinguir mayúsculas.
6. **Primer arranque:** después de migrar, el arranque de la API escribe en la base lo indicado en [Qué hace la API al arrancar](#qué-hace-la-api-al-arrancar).
7. **Inactividad:** si el plan suspende la base por inactividad, la primera conexión puede tardar más. Confirmalo en las condiciones de Neon.

## 2. Backend

En Render, una aplicación .NET se despliega con Docker: ver la [sección 6](#6-docker). Para publicarla sin Docker en otro proveedor:

```bash
dotnet publish backend/Marrow.API -c Release -o publish
```

Inicio, desde la carpeta `publish`:

```bash
dotnet Marrow.API.dll
```

Definí estas variables en el proveedor. Hay un ejemplo en `backend/.env.example`.

| Variable | Obligatoria | Uso |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | Sí | `Production` |
| `ConnectionStrings__MarrowDatabase` | Sí | Conexión a PostgreSQL. En Neon: formato clave=valor, host directo y `SSL Mode=Require` |
| `Jwt__SigningKey` | Sí | Firma de los JWT; al menos 32 bytes aleatorios |
| `Cors__AllowedOrigins__0` | Sí | Dominio del frontend, con `https://` y sin barra final; no admite `*` |
| `Cors__AllowedOrigins__1` | No | Otro dominio permitido, si hace falta |
| `AdministradorInicial__NombreUsuario`, `__Email`, `__Password` | No | Crean el primer administrador al arrancar; sin los tres, no se crea |
| `Jwt__Issuer`, `Jwt__Audience`, `Jwt__ExpirationMinutes` | No | Reemplazan los valores de `appsettings.Production.json` |
| `ASPNETCORE_HTTP_PORTS` | Según el proveedor | Puerto en el que escucha la API |

Una forma de generar la clave:

```bash
openssl rand -base64 48
```

En producción, Swagger está desactivado y los errores no muestran detalles técnicos. Si falta una variable obligatoria, la API no arranca y el registro indica cuál es, sin mostrar valores.

## 3. Frontend

`VITE_API_URL` se fija durante el build. Si cambia, hay que volver a construir. Hay un ejemplo en `frontend/marrow-web/.env.example`.

Definí `VITE_API_URL` con la URL pública del backend y ejecutá:

```bash
npm ci --prefix frontend/marrow-web
```

```bash
npm run build --prefix frontend/marrow-web
```

Publicá la carpeta `frontend/marrow-web/dist`. En Render, usá `frontend/marrow-web` como **Root Directory**, `npm ci && npm run build` como comando de build y `dist` como carpeta a publicar. No hace falta configurar reescrituras de rutas, porque la aplicación usa una sola URL.

Las variables `VITE_` quedan visibles en el navegador: nunca pongas secretos en ellas.

## 4. Orden recomendado

Para un despliegue desde cero: la API no arranca en producción sin el dominio del frontend, y el frontend necesita la URL de la API para construirse. Por eso conviene:

1. Crear la base de datos en Neon, o en otro proveedor de PostgreSQL, y aplicar las siete migraciones.
2. Reservar el dominio del frontend en el proveedor.
3. Desplegar el backend, directamente o con Docker, con las variables de la sección 2 y ese dominio en `Cors__AllowedOrigins__0`. Anotar su URL.
4. Revisar en el log del primer arranque que la API haya iniciado y si hizo correcciones o dejó avisos.
5. Construir y publicar el frontend con esa URL en `VITE_API_URL`.
6. Hacer la comprobación final.

El repositorio no incluye la configuración de Render ni un workflow de despliegue continuo.

**Publicaciones siguientes:** con Auto-Deploy en On Commit, cada push a la rama vinculada publica los servicios. Si un cambio modifica el contrato de la API, como hizo `2d328ce` con la respuesta de `PATCH /api/partidas/{id}/movimiento`, hay que comprobar que ambos queden en la misma versión.

## 5. Comprobación final

Sirve para revisar cada despliegue y no registra resultados: no se completó sobre Render para esta guía. Antes del primer despliegue, la API se probó desde una máquina local en modo Production contra Neon: arranque, catálogo de enemigos, registro, inicio de sesión y creación de un personaje.

- [ ] El registro del backend muestra que la API inició en el entorno `Production`.
- [ ] `GET /api/enemigos` devuelve Goblin, Esqueleto y Orco, con 100 de experiencia cada uno.
- [ ] Desde el frontend se puede registrar un usuario e iniciar sesión.
- [ ] Se puede crear un personaje, crear una partida y jugarla hasta el resultado.
- [ ] `/swagger` no está disponible.

## 6. Docker

Es una alternativa para publicar el backend y el frontend. Las imágenes no incluyen secretos y no aplican migraciones. El 9 de noviembre de 2026 se construyeron y se probaron en local con Docker Compose y una base PostgreSQL local. La prueba local no se repitió con `2d328ce`.

**API:** con `backend/.env` creado a partir de `backend/.env.example`:

```bash
docker build -t marrow-api backend/Marrow.API
```

```bash
docker run --env-file backend/.env -p 8080:8080 marrow-api
```

**Frontend:** `VITE_API_URL` se indica al construir la imagen:

```bash
docker build --build-arg VITE_API_URL=https://api.tu-dominio.example -t marrow-web frontend/marrow-web
```

```bash
docker run -p 8081:8080 marrow-web
```

**Prueba local con Docker Compose:** no incluye PostgreSQL. Para este caso, `Cors__AllowedOrigins__0` debe ser `http://localhost:8081`, y una base en tu máquina se alcanza con `Host=host.docker.internal`; Compose declara ese nombre también para Linux. Esa base tiene que aceptar conexiones desde Docker: revisá `listen_addresses` en `postgresql.conf` y las reglas de `pg_hba.conf`.

```bash
docker compose up --build
```

## 7. Integración continua

`.github/workflows/ci.yml` se ejecuta en cada push y Pull Request a `master` o `main`:

- **Backend:** restaura, compila en `Release` y ejecuta las pruebas de `backend/Marrow.sln`.
- **Frontend:** ejecuta `npm ci`, lint y build.

No necesita secretos ni PostgreSQL, y no despliega nada. Quedan fuera `Marrow.API.PostgresTests` y Playwright. Sus ejecuciones en GitHub sobre `79246da` y `2d328ce` fueron exitosas.

## 8. Decisiones y pendientes

| Tema | Estado |
|---|---|
| Dónde se ejecuta el backend | Render, servicio `marrow-api` |
| Dónde se publica el frontend | Render, servicio `marrow-frontend` |
| Dominios y HTTPS | Definen `Cors__AllowedOrigins__0` y `VITE_API_URL`. Por verificar: los valores cargados en Render |
| Base de producción | Neon, con las siete migraciones aplicadas y la conexión comprobada |
| Administrador inicial | Opcional. No se creó en el primer arranque contra Neon. Por verificar: si se configuró después en Render |
| Despliegue continuo | Lo hace Render, con Auto-Deploy en On Commit en ambos servicios. No hay un workflow de despliegue en el repositorio |
| Pruebas pendientes | `Marrow.API.PostgresTests` y Playwright no se ejecutaron sobre `2d328ce` |
| Pruebas E2E en CI | Hoy no corren en CI, porque `playwright.config.js` usa `npm.cmd`, que solo existe en Windows |

## Problemas frecuentes

| Síntoma | Causa probable |
|---|---|
| La API no arranca: "Falta configurar…" | Falta la variable que indica el mensaje |
| La API no arranca: "migraciones pendientes" | Falta el paso `dotnet ef database update` |
| El navegador informa un error de CORS | `Cors__AllowedOrigins__0` no coincide exactamente con el dominio del frontend |
| El build del frontend en Render falla en `npm ci` por falta de `package-lock.json` | El **Root Directory** no es `frontend/marrow-web` |
| "Falta configurar VITE_API_URL" | El frontend se construyó sin esa variable |
| Las sesiones se cerraron tras un despliegue | Cambió `Jwt__SigningKey` |
| Personajes o partidas responden `403` | El token no tiene el rol `Jugador`: iniciá sesión de nuevo. Si persiste, revisá en el log del arranque si falló la asignación del rol |
| La API no conecta con Neon | La cadena sigue en formato URI, le falta `SSL Mode=Require` o la contraseña está mal convertida |
