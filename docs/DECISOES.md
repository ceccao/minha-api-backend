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

A regra de negócio mora em `ProdutosService` (Domain). O `ProdutoService` da Application é um wrapper fino de validação e transação.

## ADR-004 — `.editorconfig` ativo e regras desligadas

O arquivo se chamava `editorconfig` (sem ponto) e não era lido. Ao ativá-lo, algumas regras ficaram desligadas de propósito: `IDE0058` (ruído em builders fluentes), `IDE0005` (exige `GenerateDocumentationFile`), `IDE0032` e `IDE0290` (falsos positivos em `PaginacaoFiltro`).

## Adiado de propósito

Specification Pattern, envelope de resposta, mensageria, Redis, OpenTelemetry, Keycloak, Global Query Filter de soft delete, Testcontainers local (a máquina não tem Docker), deploy Azure (FASE 21).
