using MinhaApi.Application.Autenticacao.DataTransfer;
using MinhaApi.Application.Autenticacao.Services.Interfaces;
using MinhaApi.CrossCutting.Exceptions;
using MinhaApi.Domain.Abstractions;
using MinhaApi.Domain.Usuarios.Entities;
using MinhaApi.Domain.Usuarios.Repositories;

namespace MinhaApi.Application.Autenticacao.Services;

public class AutenticacaoService(
    IUsuarioRepository usuarioRepository,
    ISenhaHasher senhaHasher,
    IGeradorToken geradorToken) : IAutenticacaoService
{
    // O BCrypt considera só os primeiros 72 bytes da senha.
    private const int SenhaTamanhoMaximo = 72;

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        ValidarEntrada(request);

        string email = Usuario.NormalizarEmail(request.Email);
        Usuario? usuario = await usuarioRepository.RecuperarAsync(u => u.Email == email, cancellationToken);

        // A verificação roda sempre (com hash nulo se o usuário não existe) e a falha tem uma
        // única mensagem: nem o texto nem o tempo de resposta revelam se o e-mail existe.
        bool senhaCorreta = senhaHasher.Verificar(request.Senha, usuario?.SenhaHash);

        if (usuario is null || !senhaCorreta || !usuario.Ativo)
        {
            throw new NaoAutenticadoException();
        }

        TokenGerado token = geradorToken.Gerar(usuario);
        return new LoginResponse { Token = token.Token, ExpiraEm = token.ExpiraEm };
    }

    private static void ValidarEntrada(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || request.Email.Length > Usuario.EmailTamanhoMaximo)
        {
            throw new EntidadeInvalidaException("E-mail");
        }

        if (string.IsNullOrEmpty(request.Senha) || request.Senha.Length > SenhaTamanhoMaximo)
        {
            throw new EntidadeInvalidaException("Senha");
        }
    }
}
