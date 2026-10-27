using System.ComponentModel.DataAnnotations;

namespace MinhaApi.CrossCutting.Options;

// Secao "Jwt". A Chave e SEGREDO: User Secrets em dev, variavel de ambiente
// (Jwt__Chave) fora dele. Nunca no appsettings versionado.
public class JwtOptions
{
    public const string Secao = "Jwt";

    [Required]
    [MinLength(32, ErrorMessage = "Jwt:Chave precisa ter pelo menos 32 caracteres.")]
    public string Chave { get; init; } = string.Empty;

    [Required]
    public string Emissor { get; init; } = "MinhaApi";

    [Required]
    public string Audiencia { get; init; } = "MinhaApi";

    [Range(1, 1440)]
    public int ExpiracaoMinutos { get; init; } = 60;
}
