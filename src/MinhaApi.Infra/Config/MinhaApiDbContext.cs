using Microsoft.EntityFrameworkCore;
using MinhaApi.Domain.Produtos.Entities;

namespace MinhaApi.Infra.Config;

// Scoped, UMA instancia por requisicao HTTP (README §7.1). NUNCA Singleton: o
// DbContext nao e thread-safe e acumula entidades rastreadas.
// O schema e do Liquibase - sem EF Migrations, Migrate() ou EnsureCreated().
public class MinhaApiDbContext(DbContextOptions<MinhaApiDbContext> options) : DbContext(options)
{
    public DbSet<Produto> Produtos => Set<Produto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(MinhaApiDbContext).Assembly);
}
