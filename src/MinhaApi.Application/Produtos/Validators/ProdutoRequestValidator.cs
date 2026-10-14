using FluentValidation;
using MinhaApi.Application.Produtos.DataTransfer.Requests;

namespace MinhaApi.Application.Produtos.Validators;

public class ProdutoRequestValidator : AbstractValidator<ProdutoRequest>
{
    public ProdutoRequestValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MinimumLength(3).WithMessage("Nome deve ter no mínimo 3 caracteres.")
            .MaximumLength(100).WithMessage("Nome deve ter no máximo 100 caracteres.");

        RuleFor(x => x.Preco)
            .GreaterThanOrEqualTo(0).WithMessage("Preço não pode ser negativo.");
    }
}
