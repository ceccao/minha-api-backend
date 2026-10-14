namespace MinhaApi.Domain.Abstractions;

// Resultado paginado devolvido pelo repositorio e pela API.
public class PaginacaoConsulta<TItem>(IReadOnlyList<TItem> itens, int totalItens, int pagina, int tamanhoPagina)
{
    public IReadOnlyList<TItem> Itens { get; } = itens;
    public int TotalItens { get; } = totalItens;
    public int Pagina { get; } = pagina;
    public int TamanhoPagina { get; } = tamanhoPagina;
    public int TotalPaginas => TamanhoPagina > 0 ? (int)Math.Ceiling(TotalItens / (double)TamanhoPagina) : 0;

    // Converte cada item mantendo os dados de paginacao (ex: entidade -> response).
    public PaginacaoConsulta<TSaida> Mapear<TSaida>(Func<TItem, TSaida> conversor)
        => new([.. Itens.Select(conversor)], TotalItens, Pagina, TamanhoPagina);
}
