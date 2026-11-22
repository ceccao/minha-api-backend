# MinhaApi — Template base de API em C#

Template de API REST em .NET 10, em camadas, com EF Core + MySQL, autenticação JWT e CI no GitHub. Este documento descreve **como o código está hoje**. Para criar uma entidade nova, siga o checklist da seção 9.

Repositórios do conjunto:

| Repositório | Conteúdo |
|---|---|
| `minha-api-backend` | Esta solution (API, testes, `docs/`) |
| `minha-api-database` | Schema do banco versionado com Liquibase |
| `minha-api-frontend` | Frontend Angular |

---

## 1. Stack

| Categoria | Tecnologia |
|---|---|
| Runtime / Web | .NET 10, ASP.NET Core com Controllers |
| ORM / banco | EF Core 9 + Pomelo (MySQL 8), sem EF Migrations |
| Schema | Liquibase, no repositório `minha-api-database` |
| Mapeamento | Mapster (estrito: todo par exige mapeamento explícito) |
| Ordenação dinâmica | System.Linq.Dynamic.Core |
| Logs | Serilog (console em Development; console JSON + arquivo em Staging/Production) |
| Documentação | Swashbuckle (Swagger), só em Development |
| Segurança | JWT Bearer, BCrypt, rate limiting, CORS, cabeçalhos de segurança |
| Testes | xUnit + AwesomeAssertions + NSubstitute |

---

## 2. Como rodar

Pré-requisitos: .NET SDK conforme o `global.json` e um MySQL 8 com o schema aplicado (veja o `README.md` do `minha-api-database`).

Configure os segredos uma vez (User Secrets, nunca no `appsettings` versionado):

```bash
cd src/MinhaApi.Api
dotnet user-secrets set "ConnectionStrings:MySql" "Server=localhost;Port=3306;Database=minha_api_dev;User=root;Password=***"
dotnet user-secrets set "MySql:ServerVersion" "8.0.46"
dotnet user-secrets set "Jwt:Chave" "<texto-aleatorio-com-48+-caracteres>"
dotnet user-secrets set "Seed:AdminEmail" "admin@exemplo.com"
dotnet user-secrets set "Seed:AdminSenha" "<senha-forte>"
```

Rode e abra o Swagger:

```bash
dotnet run --project src/MinhaApi.Api --launch-profile https
```

- Swagger: `https://localhost:7191/swagger` · health: `/health` · HTTP: `http://localhost:5014`.
- O banco pode ser local ou remoto: a API só lê `ConnectionStrings:MySql`. Em Staging/Production use variáveis de ambiente (`ConnectionStrings__MySql`, `Jwt__Chave`, `Cors__OrigensPermitidas__0`...).
- A API **não sobe** sem `Jwt:Chave` (mínimo de 32 caracteres) e, fora de Development, sem `Cors:OrigensPermitidas`.

Testes e verificação antes de um PR:

```bash
dotnet build
dotnet test
```

---

## 3. Camadas

```
src/
├── MinhaApi.Api/            Controllers, Program.cs, pipeline HTTP (Configuration/)
├── MinhaApi.Application/    Casos de uso: transação, conversão de DTOs, autenticação
├── MinhaApi.CrossCutting/   Exceptions, middlewares, logging, enums e opções compartilhadas
├── MinhaApi.Domain/         Entidades, regras de negócio, contratos de repositório
├── MinhaApi.Infra/          DbContext, mapeamentos, repositórios, UnitOfWork, hash e JWT
└── MinhaApi.IoC/            Só injeção de dependência
```

Dependências: `CrossCutting ← Domain ← Application ← Infra`; a `Api` depende do `IoC`, que agrega tudo. O Domain referencia o CrossCutting (e não o contrário) porque usa o enum `Situacao` e as exceptions.

Não existe classe base de entidade: cada entidade declara o `Id` e os campos que fizerem sentido. Também não existe camada `DataTransfer` separada: Requests e Responses ficam dentro da Application, por funcionalidade.

---

## 4. Domain

- **Entidade** (`Domain/{Entidades}/Entities`): propriedades com `protected set`, construtor `protected` sem parâmetros (o EF Core o usa ao ler do banco) e um construtor público que valida e atribui. As regras de invariante ficam dentro da própria entidade e lançam `RegraDeNegocioException`. Referência: `Usuario`.
- **Status** via enum `Situacao` (Ativo/Inativo), não `bool`. A exclusão é lógica: inativar.
- **Command**: um único record por entidade, usado para criar e editar.
- **Domain Service**: concentra a regra de negócio. Busca a entidade e lança `NaoEncontradoException` se não existir, em um método só que os demais reaproveitam.
- **Repositório**: `IRepositorioBase<T>` cobre o CRUD comum (`InserirAsync`, `EditarAsync`, `ExcluirAsync`, `RecuperarAsync` por id ou predicado) e `ListarAsync(query, qt, pg, cpOrd, tpOrd)`, que recebe uma query já filtrada e só aplica ordenação e paginação. A entidade que precisa de filtros ganha uma interface própria com um método `Filtrar()` e uma classe de filtro.
- **Paginação**: `PaginacaoFiltro` (`Qt`, `Pg`, `CpOrd`, `TpOrd`) na entrada, `PaginacaoConsulta<T>` na saída.

---

## 5. Application

- O Service **valida a entrada, abre a transação e converte DTOs**. A regra de negócio fica no Domain.
- A transação é explícita em cada método de escrita: `BeginTransaction`, chamada ao Domain Service, `CommitAsync`; em caso de erro, `RollbackAsync` e relança.
- Consultas de leitura sem regra de negócio podem chamar o repositório direto.
- Mapeamentos Mapster ficam em classes `IRegister` (Profile), encontradas automaticamente pelo `Scan` do IoC. Como o Mapster é estrito, esquecer um mapeamento falha na hora.
- Validação de entrada simples é feita no Service ou na entidade e lança exception: não há validators separados.

---

## 6. CrossCutting

**Exceptions** (todas herdam de `AppException`, que define o status HTTP). Quem lança passa só o nome e a mensagem já vem pronta, exceto `RegraDeNegocioException`:

| Exception | HTTP | Exemplo de uso e mensagem |
|---|---|---|
| `NaoEncontradoException("Cliente")` | 404 | "Cliente não foi encontrado." |
| `ConflitoException("Cliente")` | 409 | "Cliente foi modificado ou removido por outra operação. Recarregue e tente novamente." |
| `EntidadeInvalidaException("E-mail")` | 400 | "E-mail é inválido." |
| `AcessoNegadoException("Pedido")` | 403 | "Você não tem acesso a Pedido." |
| `NaoAutenticadoException()` | 401 | "Credenciais inválidas." (fixa de propósito: não revela se o usuário existe) |
| `RegraDeNegocioException("texto")` | 400 | Mensagem livre, para regras violadas |

O `GlobalExceptionHandler` traduz `AppException` em `ProblemDetails` (RFC 7807, com `traceId`) e registra um warning. Qualquer outra exceção vira 500 com mensagem genérica fora de Development, sem vazar detalhes.

**Middlewares:** `CorrelationIdMiddleware` (gera e propaga `X-Correlation-Id`) e `SecurityHeadersMiddleware` (cabeçalhos de segurança e CSP).

**Logs:** em Staging/Production gravam em `logs/minha-api-AAAAMMDD.json` e mantêm só os últimos 14 dias. A pasta `logs/` está no `.gitignore`.

---

## 7. Infra

- `MinhaApiDbContext` é **Scoped** (um por requisição). O schema pertence ao Liquibase: nada de `dotnet ef migrations`, `Migrate()` ou `EnsureCreated()`. O EF Core não valida o schema na subida; nome de coluna divergente só estoura na primeira query.
- Mapeamento por Fluent API (`IEntityTypeConfiguration<T>`), com nomes de tabela e coluna **iguais** ao SQL do Liquibase. Enums vão como `int` (`HasConversion<int>()`).
- `UnitOfWork` (Scoped): transação e `SaveChangesAsync` em um único ponto. Traduz `DbUpdateConcurrencyException` em `ConflitoException`.
- `RepositorioBase<T>`: as escritas só marcam a mudança; quem grava é o `CommitAsync`.
- `MySqlOptions`: `ServerVersion` explícito e `CommandTimeout`.
- `Auth/`: `BCryptSenhaHasher` e `JwtGeradorToken`.

---

## 8. API

- Controllers em `Controllers/V1/{Area}`, rota `api/v1/[controller]`, sem `.Adapt()` dentro do controller (a conversão acontece na Application).
- Nomes de action terminam em `Async`; por isso `SuppressAsyncSuffixInActionNames = false`, senão o `nameof(...)` usado em `CreatedAtAction` não bate com a rota.
- `/// <summary>` em cada action vira a descrição no Swagger (`GenerateDocumentationFile` ligado no `.csproj`).
- O pipeline fica em `Configuration/ApiConfiguration.cs`. **A ordem importa:** o Swagger vem antes de `UseAuthentication`, senão a política "todo endpoint exige login" barra a própria tela do Swagger.
- `GET /health` é público e só responde Healthy se conseguir falar com o MySQL.

---

## 9. Checklist: criar uma entidade nova

Troque `{Entidade}` pelo nome (ex.: `Cliente`).

1. **Banco** (`minha-api-database`): script em `scripts/{versão}/{TICKET}/script-01.sql` e `script-01-rollback.sql`, e o `changeSet` no changelog da versão.
2. **Domain:** `{Entidade}` (propriedades `protected set`, construtor `protected` vazio, construtor real validando), `{Entidade}Command`, `I{Entidade}Repository` (herda `IRepositorioBase<T>`; acrescente `Filtrar()` se precisar de filtros) e `{Entidade}DomainService` com a interface.
3. **Infra:** `{Entidade}Configuration` (mapeamento) e `{Entidade}Repository` (herda `RepositorioBase<T>`). Adicione o `DbSet` no `MinhaApiDbContext`.
4. **Application:** Request, Response, `{Entidade}Profile` (Mapster) e `{Entidade}Service`.
5. **API:** `Controllers/V1/{Entidades}/{Entidades}Controller.cs`.
6. **IoC:** registrar o repositório (`InfraDependencyInjection`) e os services (`ApplicationDependencyInjection`).
7. **Testes:** a entidade isolada e o Service com mocks.

Decida por entidade se ela precisa de campos de auditoria (`CriadoEm`/`AtualizadoEm`) ou de controle de concorrência (`Version`): **nada disso vem pronto**. Se precisar, adicione os campos, mapeie (`IsConcurrencyToken()` para o `Version`) e preencha/incremente no `SaveChanges` do `DbContext`; o `UnitOfWork` já traduz o conflito.

Campos `string` não anuláveis: o `Nullable` está ligado, e a forma de inicializar (`required`, `= null!`, `= string.Empty`, construtor ou `string?`) fica a critério de quem estiver codando.

---

## 10. Autenticação (JWT, single-tenant)

Fluxo: `POST /api/v1/auth/login` com `{ "email", "senha" }` devolve `{ "token", "expiraEm" }`. O cliente envia `Authorization: Bearer <token>` nas demais chamadas.

- **Seguro por padrão:** a `FallbackPolicy` exige usuário autenticado em todo endpoint. Só o login e o `/health` são públicos.
- **Senha:** hash BCrypt (fator 12). A senha nunca é guardada nem logada.
- **Anti-enumeração:** usuário inexistente, senha errada e usuário inativo respondem a mesma `NaoAutenticadoException`, e a verificação do hash roda sempre, para o tempo de resposta também não denunciar.
- **Rate limit:** 100 requisições por minuto por IP e 5 por minuto no login (429). Atrás de proxy, configure `UseForwardedHeaders` no deploy.
- **Token:** HMAC-SHA256 com `sub`, `email` e `jti`; emissor, audiência e validade conferidos; expira em `Jwt:ExpiracaoMinutos` (60 por padrão).
- **Primeiro usuário:** não há cadastro aberto. Na subida, se a tabela `USUARIO` estiver vazia e `Seed:AdminEmail`/`Seed:AdminSenha` existirem, o administrador é criado. Depois, remova a senha da configuração.
- **Swagger:** o botão **Authorize** aceita o token.

**Token de desenvolvimento (só em Development):** `POST /api/v1/auth/token-desenvolvimento` devolve um token sem pedir login, para testar no Swagger. A rota não existe em Staging/Production.

**Single-tenant:** todo usuário autenticado enxerga os mesmos dados. Se uma entidade passar a ter dono, valide a posse na Application e lance `AcessoNegadoException`; se o template virar multi-tenant, leve o tenant no token e valide a posse em cada caso de uso.

---

## 11. Testes

`tests/MinhaApi.Tests.Unit` cobre o Domain (`Usuario`), a Application (`AutenticacaoService`), o hash, o JWT e as mensagens das exceptions. Não há testes de integração por decisão do projeto: o mapeamento do EF Core contra o schema só é validado ao rodar a API de verdade, então teste um CRUD novo pelo Swagger antes de confiar nele.

---

## 12. Mais documentação

Veja a pasta `docs/`: `ARQUITETURA.md`, `DECISOES.md` (por que cada escolha), `CONTRIBUINDO.md` (branches e PRs) e `HARDENING.md` (segurança).
