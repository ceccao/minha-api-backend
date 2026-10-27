namespace MinhaApi.Domain.Abstractions;

public interface ISenhaHasher
{
    string Gerar(string senha);

    // hash nulo (usuario inexistente) ainda gasta o mesmo tempo de uma verificacao real,
    // para o tempo de resposta nao revelar se o e-mail existe. Sempre devolve false.
    bool Verificar(string senha, string? hash);
}
