using FluentValidation;
using MinhaApi.Application.Autenticacao.DataTransfer;
using MinhaApi.Domain.Usuarios.Entities;

namespace MinhaApi.Application.Autenticacao;

// Valida so o FORMATO. Nunca diz se o usuario existe: isso fica para o login, que
// responde sempre a mesma mensagem generica.
public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    // O BCrypt considera so os primeiros 72 bytes da senha.
    private const int SenhaTamanhoMaximo = 72;

    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-mail é obrigatório.")
            .MaximumLength(Usuario.EmailTamanhoMaximo).WithMessage("E-mail muito longo.");

        RuleFor(x => x.Senha)
            .NotEmpty().WithMessage("Senha é obrigatória.")
            .MaximumLength(SenhaTamanhoMaximo).WithMessage($"Senha deve ter no máximo {SenhaTamanhoMaximo} caracteres.");
    }
}
