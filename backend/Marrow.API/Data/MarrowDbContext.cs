using Marrow.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Marrow.API.Data;

public class MarrowDbContext(DbContextOptions<MarrowDbContext> options)
    : IdentityDbContext<Usuario, IdentityRole<int>, int>(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    public DbSet<Personaje> Personajes => Set<Personaje>();

    public DbSet<Enemigo> Enemigos => Set<Enemigo>();

    public DbSet<Partida> Partidas => Set<Partida>();

    public DbSet<Habilidad> Habilidades => Set<Habilidad>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Identity usa la tabla y la columna existentes, para conservar los usuarios y sus relaciones.
        modelBuilder.Entity<Usuario>().ToTable("Usuarios");
        modelBuilder.Entity<Usuario>().HasKey(usuario => usuario.Id);
        modelBuilder.Entity<Usuario>().Ignore(usuario => usuario.NombreUsuario);
        modelBuilder.Entity<Usuario>().Property(usuario => usuario.UserName).HasColumnName("NombreUsuario");
        modelBuilder.Entity<Personaje>().HasKey(personaje => personaje.Id);
        modelBuilder.Entity<Enemigo>().HasKey(enemigo => enemigo.Id);
        modelBuilder.Entity<Partida>().HasKey(partida => partida.Id);

        modelBuilder.Entity<Enemigo>().HasData(DatosIniciales.Enemigos());

        modelBuilder.Entity<Personaje>()
            .HasOne(personaje => personaje.Usuario)
            .WithMany()
            .HasForeignKey(personaje => personaje.UsuarioId);

        modelBuilder.Entity<Partida>()
            .HasOne(partida => partida.Personaje)
            .WithMany()
            .HasForeignKey(partida => partida.PersonajeId);

        modelBuilder.Entity<Partida>()
            .HasOne(partida => partida.Enemigo)
            .WithMany()
            .HasForeignKey(partida => partida.EnemigoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Habilidad>().HasKey(habilidad => habilidad.Id);
        modelBuilder.Entity<Habilidad>().HasIndex(habilidad => habilidad.Nombre).IsUnique();

        // Relación N:M. La clave compuesta impide asignar dos veces la misma habilidad a un personaje.
        modelBuilder.Entity<Personaje>()
            .HasMany(personaje => personaje.Habilidades)
            .WithMany(habilidad => habilidad.Personajes)
            .UsingEntity<Dictionary<string, object>>(
                "PersonajeHabilidades",
                union => union.HasOne<Habilidad>().WithMany().HasForeignKey("HabilidadId"),
                union => union.HasOne<Personaje>().WithMany().HasForeignKey("PersonajeId"),
                union => union.HasKey("PersonajeId", "HabilidadId"));
    }
}
