using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MinhaApi.Domain.Produtos.Entities;
using MinhaApi.Infra.Config;

namespace MinhaApi.Infra.Produtos.Mappings;

// Os nomes de tabela e coluna precisam bater com o SQL do Liquibase
// (minha-api-database, API-0001 e API-0003): o EF Core nao valida o schema na
// subida, divergencia so aparece na primeira query.
public class ProdutoConfiguration : IEntityTypeConfiguration<Produto>
{
    public void Configure(EntityTypeBuilder<Produto> builder)
    {
        builder.ToTable("PRODUTO");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("ID").ValueGeneratedOnAdd();

        builder.Property(x => x.Nome).HasColumnName("NOME").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Preco).HasColumnName("PRECO").HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.Situacao).HasColumnName("SITUACAO").HasConversion<int>().IsRequired();

        // Auditoria: preenchida pelo MinhaApiDbContext (UTC).
        builder.Property(x => x.CriadoEm).HasColumnName("CRIADO_EM").HasConversion(ConversoresUtc.Utc).IsRequired();
        builder.Property(x => x.AtualizadoEm).HasColumnName("ATUALIZADO_EM").HasConversion(ConversoresUtc.UtcNulavel);

        // Concorrencia otimista: o UPDATE sai com WHERE ID = ? AND VERSION = ?.
        builder.Property(x => x.Version).HasColumnName("VERSION").IsConcurrencyToken();
    }
}
