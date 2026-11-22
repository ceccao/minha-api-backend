# Contribuindo

## Branches

`feature/*` e `fix/*` → PR → `dev` → PR → `hml` → PR → `main`. As três branches exigem PR e CI verde.

## Commits

Prefixos: `feature:`, `fix:`, `refactor:`, `chore:`, `docs:`, `test:`.

## Antes de abrir PR

```bash
dotnet build
dotnet test
dotnet list package --vulnerable --include-transitive
```

O build falha em violações de nomenclatura, namespace e `using` do `.editorconfig` (`TreatWarningsAsErrors`). Regras de estilo são só sugestões.

## Mudança de schema

Um ticket que altera o banco muda **dois repositórios juntos**: o script SQL com rollback escrito à mão no `minha-api-database`, e a `IEntityTypeConfiguration` da entidade aqui. O EF Core não valida o schema na subida; teste pelo Swagger.

## Nova entidade

Siga o checklist da seção 9 do `readme.md` da raiz.
