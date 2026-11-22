namespace MinhaApi.Domain.Abstractions;

public class PaginacaoConsulta<TItem>(IReadOnlyList<TItem> itens, int totalItens, int pagina, int tamanhoPagina)
{
    public IReadOnlyList<TItem> Itens { get; } = itens;
    public int TotalItens { get; } = totalItens;
    public int Pagina { get; } = pagina;
    public int TamanhoPagina { get; } = tamanhoPagina;
    public int TotalPaginas => TamanhoPagina > 0 ? (int)Math.Ceiling(TotalItens / (double)TamanhoPagina) : 0;

    public PaginacaoConsulta<TSaida> Mapear<TSaida>(Func<TItem, TSaida> conversor)
        => new([.. Itens.Select(conversor)], TotalItens, Pagina, TamanhoPagina);
}
