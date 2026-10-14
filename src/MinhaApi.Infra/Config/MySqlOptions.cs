using System.ComponentModel.DataAnnotations;

namespace MinhaApi.Infra.Config;

// Secao "MySql" do appsettings. A connection string fica a parte, em
// ConnectionStrings:MySql (e segredo: User Secrets em dev, variavel de ambiente em producao).
public class MySqlOptions
{
    public const string Secao = "MySql";

    [Range(1, 300)]
    public int CommandTimeout { get; init; } = 30;

    // Versao do servidor explicita (nao AutoDetect, que abre conexao na configuracao).
    // Ajuste por ambiente: instalacao nativa, Docker, Azure.
    [Required]
    [RegularExpression(@"^\d+\.\d+(\.\d+)?$", ErrorMessage = "ServerVersion deve ser algo como 8.0.0.")]
    public string ServerVersion { get; init; } = "8.0.0";
}
