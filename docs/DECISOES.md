# Decisões de arquitetura (ADR)

## ADR-001 — NHibernate → Entity Framework Core (out/2026)

- **Motivo:** adoção de mercado do EF Core (material, vagas, suporte da Microsoft).
- **Mantido:** o Liquibase continua único dono do schema. Proibido `dotnet ef migrations`, `Database.Migrate()` e `EnsureCreated()`.
- **Versões:** EF Core 9.0.x + Pomelo 9.0.0 em `net10.0`. Os dois pacotes sobem sempre juntos, agrupados no Dependabot, que ignora versões major deles.

## ADR-002 — Validação no Domain, sem FluentValidation

A validação mora nas entidades e nos Services e lança exception tipada (`RegraDeNegocioException`, `EntidadeInvalidaException`), traduzida em 400 pelo `GlobalExceptionHandler`. Assim uma entidade nova não exige um arquivo de validator. **Custo:** a exception para no primeiro erro encontrado, em vez de listar todos de uma vez.

## ADR-003 — Domain Service

A regra de negócio mora no Domain Service. O Service da Application é um wrapper fino de transação e conversão de DTOs.

## ADR-004 — `.editorconfig` ativo, estilo como sugestão

Regras de estilo (IDE0xxx) são só **sugestões** (sem sublinhado amarelo nem erro de build). Quebram o build apenas nomenclatura, namespace e `using`.

## ADR-005 — Gravação e transação

O repositório só marca a mudança; `IUnitOfWork.CommitAsync` chama `SaveChangesAsync` uma vez. A tradução de `DbUpdateConcurrencyException` para `ConflitoException` mora no `UnitOfWork`.

## ADR-006 — Exceptions com mensagem pronta

Todas as exceptions (menos `RegraDeNegocioException`) montam a própria mensagem a partir de um nome: `new NaoEncontradoException("Cliente")` resulta em "Cliente não foi encontrado." `NaoAutenticadoException` tem mensagem fixa, para não revelar se o usuário existe.

## ADR-007 — Template enxuto (nov/2026)

O template ficou só com o necessário e genérico:

- **Sem entidade de exemplo de negócio:** só `Usuario`, necessária ao login. Novas entidades seguem o checklist do `readme.md`.
- **Sem auditoria (`CriadoEm`/`AtualizadoEm`) nem `Version`** por padrão: cada entidade adiciona se precisar.
- **Sem projetos vazios** (`Jobs`, `Consumers`, `Workers`), sem testes de integração, sem `dev.ps1` (use `dotnet run`), sem `scripts/` e sem `.template.config` (cada projeto novo nasce pelo "Use this template" do GitHub).
- **Logs em arquivo** mantêm só os últimos 14 dias.

## Adiado de propósito

Specification Pattern, envelope de resposta, mensageria, Redis, OpenTelemetry, Keycloak, Testcontainers, testes de integração e deploy na Azure.
