using Marrow.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Marrow.API.Data;

public class MarrowDbContext(DbContextOptions<MarrowDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    public DbSet<Personaje> Personajes => Set<Personaje>();

    public DbSet<Enemigo> Enemigos => Set<Enemigo>();

    public DbSet<Partida> Partidas => Set<Partida>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Usuario>().HasKey(usuario => usuario.Id);
        modelBuilder.Entity<Personaje>().HasKey(personaje => personaje.Id);
        modelBuilder.Entity<Enemigo>().HasKey(enemigo => enemigo.Id);
        modelBuilder.Entity<Partida>().HasKey(partida => partida.Id);

        modelBuilder.Entity<Enemigo>().HasData(
            new Enemigo
            {
                Id = 1,
                Nombre = "Goblin",
                Nivel = 1,
                VidaMaxima = 30,
                Ataque = 5,
                Defensa = 2,
                ExperienciaOtorgada = 10
            },
            new Enemigo
            {
                Id = 2,
                Nombre = "Esqueleto",
                Nivel = 2,
                VidaMaxima = 45,
                Ataque = 8,
                Defensa = 3,
                ExperienciaOtorgada = 20
            },
            new Enemigo
            {
                Id = 3,
                Nombre = "Orco",
                Nivel = 3,
                VidaMaxima = 80,
                Ataque = 12,
                Defensa = 5,
                ExperienciaOtorgada = 35
            });

        modelBuilder.Entity<Personaje>()
            .HasOne(personaje => personaje.Usuario)
            .WithMany()
            .HasForeignKey(personaje => personaje.UsuarioId);

        modelBuilder.Entity<Partida>()
            .HasOne(partida => partida.Personaje)
            .WithMany()
            .HasForeignKey(partida => partida.PersonajeId);
    }
}
