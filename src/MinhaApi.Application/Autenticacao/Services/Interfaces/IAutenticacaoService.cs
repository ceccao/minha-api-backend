using MinhaApi.Application.Autenticacao.DataTransfer;

namespace MinhaApi.Application.Autenticacao.Services.Interfaces;

public interface IAutenticacaoService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
}
