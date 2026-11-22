using Microsoft.EntityFrameworkCore;
using MinhaApi.Domain.Usuarios.Entities;

namespace MinhaApi.Infra.Config;

// Scoped: uma instância por requisição HTTP (nunca Singleton).
// O schema é do Liquibase: sem EF Migrations, Migrate() ou EnsureCreated().
public class MinhaApiDbContext(DbContextOptions<MinhaApiDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(MinhaApiDbContext).Assembly);
}
