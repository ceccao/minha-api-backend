using Microsoft.EntityFrameworkCore;
using MinhaApi.Domain.Abstractions;
using MinhaApi.Domain.Produtos.Entities;
using MinhaApi.Domain.Usuarios.Entities;

namespace MinhaApi.Infra.Config;

// Scoped, UMA instancia por requisicao HTTP (README §7.1). NUNCA Singleton: o
// DbContext nao e thread-safe e acumula entidades rastreadas.
// O schema e do Liquibase - sem EF Migrations, Migrate() ou EnsureCreated().
public class MinhaApiDbContext(DbContextOptions<MinhaApiDbContext> options) : DbContext(options)
{
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(MinhaApiDbContext).Assembly);

    // Sobrescrevemos as duas versoes (sincrona e assincrona) para que a auditoria e o
    // incremento do Version nunca sejam pulados, qualquer que seja a chamada.
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        AplicarRegrasDePersistencia();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        AplicarRegrasDePersistencia();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    // Preenche auditoria (UTC) e incrementa o token de concorrencia num lugar so,
    // para toda entidade que implementa as interfaces. O EF Core nao incrementa um
    // token "int" sozinho.
    private void AplicarRegrasDePersistencia()
    {
        var agora = DateTime.UtcNow;

        foreach (var entrada in ChangeTracker.Entries())
        {
            if (entrada.Entity is IEntidadeAuditavel)
            {
                if (entrada.State == EntityState.Added)
                {
                    entrada.Property(nameof(IEntidadeAuditavel.CriadoEm)).CurrentValue = agora;
                }
                else if (entrada.State == EntityState.Modified)
                {
                    entrada.Property(nameof(IEntidadeAuditavel.AtualizadoEm)).CurrentValue = agora;
                }
            }

            if (entrada.Entity is IEntidadeVersionada && entrada.State == EntityState.Modified)
            {
                var version = entrada.Property(nameof(IEntidadeVersionada.Version));
                version.CurrentValue = (int)version.OriginalValue! + 1;
            }
        }
    }
}
