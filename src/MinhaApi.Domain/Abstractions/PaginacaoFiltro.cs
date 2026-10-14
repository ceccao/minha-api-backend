namespace MinhaApi.Domain.Abstractions;

public class PaginacaoFiltro
{
    private const int QuantidadeMaxima = 100;
    private const int QuantidadePadrao = 10;

    private int _qt = QuantidadePadrao;
    private int _pg = 1;

    public int Qt
    {
        get => _qt;

        set => _qt = value switch
        {
            < 1 => QuantidadePadrao,
            > QuantidadeMaxima => QuantidadeMaxima,
            _ => value
        };
    }

    public int Pg
    {
        get => _pg;
        set => _pg = value < 1 ? 1 : value;
    }

    public string CpOrd { get; set; }

    public TipoOrdenacao TpOrd { get; set; }

    public PaginacaoFiltro(string cpOrd, TipoOrdenacao tpOrd = TipoOrdenacao.Ascendente)
    {
        CpOrd = cpOrd;
        TpOrd = tpOrd;
    }
}
