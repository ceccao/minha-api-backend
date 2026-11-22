# Arquitetura

Visão resumida. O detalhe está no [readme.md](../readme.md) da raiz.

## Camadas e regra de dependência

```
CrossCutting  ←  Domain  ←  Application  ←  Infra
                              ↑
                       Api  ←  IoC (agrega tudo)
```

| Camada | Responsabilidade | Não pode depender de |
|---|---|---|
| Domain | Entidades, regras de negócio, contratos de repositório | ORM, Application, Infra |
| Application | Casos de uso: transação, conversão de DTOs, autenticação | Infra |
| CrossCutting | Exceptions tipadas, middlewares, logging, opções compartilhadas | ORM |
| Infra | `MinhaApiDbContext`, mapeamentos EF Core, repositórios, UnitOfWork, hash e JWT | — |
| IoC | Registro de injeção de dependência | — |
| Api | Controllers e pipeline HTTP | Infra (só via IoC) |

## Fluxo de uma escrita

`Controller` → Service (Application: abre a transação) → Domain Service (regra de negócio e validação) → repositório (Infra: só marca a mudança no DbContext) → `IUnitOfWork.CommitAsync` (grava com `SaveChangesAsync` e confirma a transação).

Erros: `AppException` (400, 401, 403, 404, 409) vira `ProblemDetails` no `GlobalExceptionHandler`. Qualquer outra exceção vira 500 com mensagem genérica fora de Development.

## Persistência

- `MinhaApiDbContext` é **Scoped** (um por requisição). Nunca Singleton.
- Mapeamento só por Fluent API (`IEntityTypeConfiguration<T>`), na Infra.
- O schema pertence ao Liquibase (repositório `minha-api-database`). Sem EF Migrations.
