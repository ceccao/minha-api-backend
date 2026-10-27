namespace MinhaApi.Application.Autenticacao.DataTransfer;

public class LoginResponse
{
    public required string Token { get; set; }
    public DateTime ExpiraEm { get; set; }
}
