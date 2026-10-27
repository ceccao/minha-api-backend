using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MinhaApi.Domain.Usuarios.Entities;

namespace MinhaApi.Infra.Usuarios.Mappings;

// Nomes batem com o SQL do Liquibase (minha-api-database, API-0002).
public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("USUARIO");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("ID").ValueGeneratedOnAdd();

        builder.Property(x => x.Nome).HasColumnName("NOME").HasMaxLength(Usuario.NomeTamanhoMaximo).IsRequired();
        builder.Property(x => x.Email).HasColumnName("EMAIL").HasMaxLength(Usuario.EmailTamanhoMaximo).IsRequired();
        builder.Property(x => x.SenhaHash).HasColumnName("SENHA_HASH").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Situacao).HasColumnName("SITUACAO").HasConversion<int>().IsRequired();

        builder.HasIndex(x => x.Email).IsUnique();
    }
}
