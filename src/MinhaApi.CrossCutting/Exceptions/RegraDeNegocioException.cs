using Microsoft.AspNetCore.Http;

namespace MinhaApi.CrossCutting.Exceptions;

// Uma regra de negocio foi violada (ex: preco negativo, nome fora do tamanho).
// Pode ser lancada no Domain e em qualquer outra camada, mas com moderacao: use as
// outras exceptions quando o caso for mais especifico (NaoEncontrado, Conflito...).
public class RegraDeNegocioException(string mensagem) : AppException(mensagem)
{
    public override int StatusCode => StatusCodes.Status400BadRequest;
}
