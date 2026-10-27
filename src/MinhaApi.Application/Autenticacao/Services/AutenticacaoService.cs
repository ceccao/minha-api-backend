using FluentValidation;
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
    IGeradorToken geradorToken,
    IValidator<LoginRequest> validator) : IAutenticacaoService
{
    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var validacao = await validator.ValidateAsync(request, cancellationToken);

        if (!validacao.IsValid)
        {
            throw new EntidadeInvalidaException(validacao.Errors.Select(e => e.ErrorMessage));
        }

        string email = Usuario.NormalizarEmail(request.Email);
        Usuario? usuario = await usuarioRepository.RecuperarAsync(u => u.Email == email, cancellationToken);

        // A verificacao roda SEMPRE (com hash nulo se o usuario nao existe) e a falha tem
        // uma unica mensagem: nem o texto nem o tempo de resposta revelam se o e-mail
        // existe (README §6.3, enumeracao de usuarios).
        bool senhaCorreta = senhaHasher.Verificar(request.Senha, usuario?.SenhaHash);

        if (usuario is null || !senhaCorreta || !usuario.Ativo)
        {
            throw new NaoAutenticadoException();
        }

        TokenGerado token = geradorToken.Gerar(usuario);
        return new LoginResponse { Token = token.Token, ExpiraEm = token.ExpiraEm };
    }
}
