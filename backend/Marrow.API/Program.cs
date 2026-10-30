using Marrow.API.Data;
using Marrow.API.Interfaces;
using Marrow.API.Repositories;
using Marrow.API.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<MarrowDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("MarrowDatabase")));
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IPersonajeRepository, PersonajeRepository>();
builder.Services.AddScoped<IEnemigoRepository, EnemigoRepository>();
builder.Services.AddScoped<IPartidaRepository, PartidaRepository>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IPersonajeService, PersonajeService>();
builder.Services.AddScoped<IEnemigoService, EnemigoService>();
builder.Services.AddScoped<IPartidaService, PartidaService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
