using FluentValidation;
using MinhaApi.Application.Produtos.DataTransfer.Requests;
using MinhaApi.Domain.Produtos.Entities;

namespace MinhaApi.Application.Produtos.Validators;

// Valida o FORMATO do que chegou e devolve todos os erros de uma vez (400).
// Os limites vem da entidade, para existir uma regra so.
public class ProdutoRequestValidator : AbstractValidator<ProdutoRequest>
{
    public ProdutoRequestValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MinimumLength(Produto.NomeTamanhoMinimo).WithMessage($"Nome deve ter no mínimo {Produto.NomeTamanhoMinimo} caracteres.")
            .MaximumLength(Produto.NomeTamanhoMaximo).WithMessage($"Nome deve ter no máximo {Produto.NomeTamanhoMaximo} caracteres.");

        RuleFor(x => x.Preco)
            .GreaterThanOrEqualTo(0).WithMessage("Preço não pode ser negativo.");
    }
}
