namespace MinhaApi.Domain.Abstractions;

// Entidades com controle de concorrencia otimista (evita "lost update").
// O MinhaApiDbContext incrementa Version a cada UPDATE; se outra operacao alterou a
// linha antes, a gravacao falha e o UnitOfWork devolve ConflitoException (409).
// Opcional por entidade: implemente apenas nas editadas em paralelo.
public interface IEntidadeVersionada
{
    int Version { get; }
}
