using FluentValidation;
using MinhaApi.Application.Produtos.DataTransfer.Requests;

namespace MinhaApi.Application.Produtos.Validators;

public class ListarProdutosRequestValidator : AbstractValidator<ListarProdutosRequest>
{
    // Campos pelos quais a API aceita ordenar. Sem isso o cliente poderia ordenar por
    // qualquer propriedade da entidade, inclusive as que nao quer expor.
    private static readonly string[] CamposOrdenaveis = ["Id", "Nome", "Preco", "Situacao"];

    public ListarProdutosRequestValidator()
    {
        RuleFor(x => x.CpOrd)
            .Must(campo => CamposOrdenaveis.Contains(campo, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Ordenação permitida apenas por: {string.Join(", ", CamposOrdenaveis)}.");
    }
}
