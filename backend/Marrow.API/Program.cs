using Marrow.API.Data;
using Marrow.API.Interfaces;
using Marrow.API.Middleware;
using Marrow.API.Models;
using Marrow.API.Repositories;
using Marrow.API.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ManejadorGlobalExcepciones>();

// Los orígenes salen de la configuración de cada entorno. Sin orígenes, no se permite ninguno.
var origenesPermitidos = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(origenesPermitidos)
            .WithMethods(HttpMethods.Get, HttpMethods.Post, HttpMethods.Put, HttpMethods.Patch, HttpMethods.Delete)
            .WithHeaders("Authorization", "Content-Type");
    });
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Marrow API",
        Version = "v1",
        Description = "API del RPG Marrow: autenticación, personajes, enemigos, partidas y combate. "
            + "Para probar los endpoints protegidos: iniciá sesión, copiá el token y pegalo en Authorize."
    });

    var archivoXml = Path.Combine(AppContext.BaseDirectory, $"{typeof(Program).Assembly.GetName().Name}.xml");
    if (File.Exists(archivoXml))
    {
        options.IncludeXmlComments(archivoXml, includeControllerXmlComments: true);
    }

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Pegá solo el token JWT que devuelve el inicio de sesión, sin la palabra Bearer."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document, null)] = new List<string>()
    });
});
builder.Services.AddDbContext<MarrowDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("MarrowDatabase")));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var signingKey = builder.Configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("Falta configurar Jwt:SigningKey mediante User Secrets.");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization();
// AddIdentityCore no agrega cookies: JWT Bearer sigue siendo la autenticación de la API.
builder.Services.AddIdentityCore<Usuario>(options =>
    {
        // Misma política pública que ya exige RegistroUsuarioRequest: al menos 8 caracteres.
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;

        // Vacío: se aceptan los mismos nombres de usuario que antes, incluidos espacios y acentos.
        options.User.AllowedUserNameCharacters = string.Empty;
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole<int>>()
    .AddEntityFrameworkStores<MarrowDbContext>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IPersonajeRepository, PersonajeRepository>();
builder.Services.AddScoped<IEnemigoRepository, EnemigoRepository>();
builder.Services.AddScoped<IPartidaRepository, PartidaRepository>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IPersonajeService, PersonajeService>();
builder.Services.AddScoped<IEnemigoService, EnemigoService>();
builder.Services.AddScoped<IPartidaService, PartidaService>();

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
app.Lifetime.ApplicationStarted.Register(() =>
    logger.LogInformation("API de Marrow iniciada en el entorno {Entorno}.", app.Environment.EnvironmentName));
app.Lifetime.ApplicationStopping.Register(() =>
    logger.LogInformation("API de Marrow deteniéndose."));

// Las pruebas no abren conexiones a una base al arrancar la API.
if (!app.Environment.IsEnvironment("Testing"))
{
    await using var alcance = app.Services.CreateAsyncScope();
    try
    {
        // En Development, la conexión y la clave JWT salen de User Secrets y no se exige nada más.
        if (app.Environment.IsProduction())
        {
            ValidarConfiguracionDeProduccion(app.Configuration, origenesPermitidos);
        }

        await InicializadorDatos.InicializarAsync(
            alcance.ServiceProvider.GetRequiredService<MarrowDbContext>(),
            alcance.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>(),
            alcance.ServiceProvider.GetRequiredService<UserManager<Usuario>>(),
            app.Configuration,
            logger);
    }
    catch (Exception exception)
    {
        logger.LogCritical(exception, "La configuración o los datos iniciales no son válidos: la API no arranca.");
        throw;
    }
}

// Configure the HTTP request pipeline.
// CORS va primero para que el navegador pueda leer también las respuestas de error.
app.UseCors();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// El mensaje nombra las variables de entorno que faltan, nunca sus valores.
static void ValidarConfiguracionDeProduccion(IConfiguration configuracion, string[] origenesPermitidos)
{
    var faltantes = new List<string>();

    if (string.IsNullOrWhiteSpace(configuracion.GetConnectionString("MarrowDatabase")))
    {
        faltantes.Add("ConnectionStrings__MarrowDatabase");
    }

    if (string.IsNullOrWhiteSpace(configuracion["Jwt:SigningKey"]))
    {
        faltantes.Add("Jwt__SigningKey");
    }

    if (string.IsNullOrWhiteSpace(configuracion["Jwt:Issuer"]))
    {
        faltantes.Add("Jwt__Issuer");
    }

    if (string.IsNullOrWhiteSpace(configuracion["Jwt:Audience"]))
    {
        faltantes.Add("Jwt__Audience");
    }

    if (string.IsNullOrWhiteSpace(configuracion["Jwt:ExpirationMinutes"]))
    {
        faltantes.Add("Jwt__ExpirationMinutes");
    }

    if (origenesPermitidos.Length == 0 || origenesPermitidos.Any(string.IsNullOrWhiteSpace))
    {
        faltantes.Add("Cors__AllowedOrigins__0");
    }

    if (faltantes.Count > 0)
    {
        throw new InvalidOperationException($"Falta configurar: {string.Join(", ", faltantes)}.");
    }

    // HMAC-SHA256 exige una clave de al menos 256 bits.
    if (Encoding.UTF8.GetByteCount(configuracion["Jwt:SigningKey"]!) < 32)
    {
        throw new InvalidOperationException("Jwt__SigningKey debe tener al menos 32 bytes.");
    }

    // Permitir cualquier origen en producción dejaría abierta la API a cualquier sitio.
    if (origenesPermitidos.Contains("*"))
    {
        throw new InvalidOperationException("Cors__AllowedOrigins__0 debe indicar el dominio del frontend, no \"*\".");
    }
}
