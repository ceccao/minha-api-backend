using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MinhaApi.Infra.Config;

// O Pomelo devolve DateTime lido do banco com Kind = Unspecified. Como gravamos sempre
// em UTC, marcamos como Utc na leitura para o JSON sair com o "Z" no final.
public static class ConversoresUtc
{
    public static readonly ValueConverter<DateTime, DateTime> Utc = new(
        valor => valor,
        valor => DateTime.SpecifyKind(valor, DateTimeKind.Utc));

    public static readonly ValueConverter<DateTime?, DateTime?> UtcNulavel = new(
        valor => valor,
        valor => valor.HasValue ? DateTime.SpecifyKind(valor.Value, DateTimeKind.Utc) : valor);
}
