using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MinhaApi.Domain.Produtos.Entities;

namespace MinhaApi.Infra.Produtos.Mappings;

// Os nomes de tabela e coluna precisam bater com o SQL do Liquibase
// (minha-api-database, API-0001/script-01.sql): o EF Core nao valida o schema na
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
    }
}
