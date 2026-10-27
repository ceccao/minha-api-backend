namespace MinhaApi.Domain.Abstractions;

// Entidades que registram quando foram criadas e alteradas (sempre em UTC).
// Os valores sao preenchidos pelo MinhaApiDbContext ao gravar; a entidade so expoe.
// Opcional por entidade: implemente apenas nas que precisam.
public interface IEntidadeAuditavel
{
    DateTime CriadoEm { get; }
    DateTime? AtualizadoEm { get; }
}
