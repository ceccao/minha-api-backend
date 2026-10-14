# Decisões de arquitetura (ADR)

## ADR-001 — NHibernate → Entity Framework Core (out/2026)

- **Motivo:** adoção de mercado do EF Core (material, vagas, suporte da Microsoft).
- **Custo:** reescrita da Infra e do IoC (`MinhaApiDbContext`, `ProdutoConfiguration`, `RepositorioBase`, `UnitOfWork`, `AddInfra`). Domain e Application não foram tocados no mapeamento, o que confirma o valor da regra de dependência.
- **Mantido:** o Liquibase continua único dono do schema. Proibido `dotnet ef migrations`, `Database.Migrate()` e `EnsureCreated()`.
- **Versões:** EF Core 9.0.x + Pomelo 9.0.0 em `net10.0`. O Pomelo estável mais recente (checado em 13/10/2026) é a 9.0.0. Os dois pacotes sobem sempre juntos, agrupados no Dependabot.
- **Validação:** CRUD de `Produto` exercitado contra MySQL 8 real (criar, listar com ordenação, recuperar, editar, 404, 400, excluir).

## ADR-002 — Validação explícita na Application

`FluentValidation.AspNetCore` foi descontinuado. O `ProdutoService` injeta `IValidator<ProdutoRequest>` e lança `EntidadeInvalidaException`, para que o erro passe pelo mesmo `ExceptionMiddleware` (400). Os dois validators duplicados (criar e atualizar) viraram um só, `ProdutoRequestValidator`.

## ADR-003 — Domain Service

A regra de negócio mora em `ProdutoDomainService` (Domain). O `ProdutoService` da Application é um wrapper fino de validação e transação.

## ADR-004 — `.editorconfig` ativo, estilo como sugestão

O arquivo se chamava `editorconfig` (sem ponto) e não era lido. Regras de estilo (IDE0xxx) são só **sugestões** (pontos cinzas no editor, sem sublinhado amarelo nem erro de build). Quebram o build apenas nomenclatura, namespace e `using`, decididos na FASE 13.

## ADR-005 — Revisão técnica (out/2026)

- **Camadas:** a Application valida, abre a transação e converte DTOs; o Domain (`ProdutoDomainService`, entidades) guarda a regra de negócio.
- **Gravação:** o repositório só marca a mudança; `IUnitOfWork.CommitAsync` chama `SaveChangesAsync` uma vez. A tradução de `DbUpdateConcurrencyException` para `ConflitoException` mora no `UnitOfWork`.
- **Exceptions:** `RegraDeNegocioException` (400) para regras violadas, usável em qualquer camada com moderação. A entidade deixou de lançar `ArgumentException` (que virava 500).
- **Nullable** ligado em todos os projetos, incluindo o Domain.
- **Listagem:** filtros por `Nome` e `Situacao`; ordenação só pelos campos permitidos no `ListarProdutosRequestValidator`; um único `ListarAsync(IQueryable, qt, pg, cpOrd, tpOrd)`.
- **Mapster estrito:** `RequireExplicitMapping` e `RequireDestinationMemberSource`; todo par usado fica registrado em `ProdutoProfile`.
- **`MySqlOptions` único**, em `Infra/Config`, registrado e validado no `AddInfra`.
- **`Program.cs` curto:** a configuração HTTP fica em `Api/Configuration/ApiConfiguration.cs` (`AddApi`, `ConfigurarHost`, `UseApi`); o IoC fica só com injeção de dependência.
- **Raiz:** `Directory.Packages.props` (versões centrais), `global.json`, `dev.ps1` só com `dotnet run`, `scripts/verificar-mysql.ps1`; dump de código fora do git.
- **Removidos por não terem uso:** `Result<T>`, `PagedResult`, sobrecargas extras de `ListarAsync`, `Jobs/Class1.cs`.

## Adiado de propósito

Specification Pattern, envelope de resposta, mensageria, Redis, OpenTelemetry, Keycloak, Global Query Filter de soft delete, Testcontainers local (a máquina não tem Docker), deploy Azure (FASE 21).
