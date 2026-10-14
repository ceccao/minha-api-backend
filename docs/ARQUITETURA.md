# Arquitetura

Visão resumida. O detalhe completo está em [README.md](README.md) §2 e no `readme.md` da raiz.

## Camadas e regra de dependência

```
Domain  ←  Application  ←  Infra
   ↑           ↑
CrossCutting   |
               |
         Api  ←  IoC (agrega tudo)
         Jobs, Consumers, Workers  →  Application
```

| Camada | Responsabilidade | Não pode depender de |
|---|---|---|
| Domain | Entidades, commands, domain services, interfaces de repositório | Qualquer pacote de ORM, Application, Infra |
| Application | Casos de uso: validação do request, transação, mapeamento DTO | Infra |
| CrossCutting | Exceptions tipadas, middlewares, logging | Qualquer ORM |
| Infra | `MinhaApiDbContext`, configurações EF Core, repositórios, UnitOfWork | — |
| IoC | Registro de DI | — |
| Api | Controllers, pipeline HTTP | Infra (só via IoC) |

## Fluxo de uma escrita

`Controller` → `ProdutoService` (Application: valida o `ProdutoRequest`, abre a transação) → `ProdutosService` (Domain: regra de negócio) → `IProdutoRepository` (Infra: `SaveChangesAsync`) → commit no `IUnitOfWork`.

Erros: `AppException` (404, 400, 409…) viram `ProblemDetails` no `ExceptionMiddleware`. Qualquer outra exceção vira 500 com mensagem genérica fora de Development.

## Persistência

- `MinhaApiDbContext` é **Scoped** (um por requisição). Nunca Singleton.
- Mapeamento só por Fluent API (`IEntityTypeConfiguration<T>`), na Infra.
- Schema pertence ao Liquibase (repositório `minha-api-database`). Sem EF Migrations.
