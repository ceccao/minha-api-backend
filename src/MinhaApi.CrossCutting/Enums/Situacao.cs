using System.ComponentModel;

namespace MinhaApi.CrossCutting.Enums;

public enum Situacao
{
    [Description("Inativo")]
    Inativo = 0,

    [Description("Ativo")]
    Ativo = 1
}
