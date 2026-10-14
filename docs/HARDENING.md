# Hardening (FASE 19) — estado em 13/10/2026

Resultado da revisão dos checklists do README §6 e §7 contra o código.

## Verificações automáticas

| Verificação | Resultado |
|---|---|
| `dotnet list package --vulnerable --include-transitive` | Nenhuma vulnerabilidade |
| `FromSqlRaw` / `ExecuteSqlRaw` em `src/` | Nenhuma ocorrência |
| `EnableSensitiveDataLogging` em `src/` | Nenhuma ocorrência |
| `DateTime.Now` em `src/` | Nenhuma ocorrência |
| `AllowAnyOrigin` | Nenhum uso (só comentário) |

## Corrigido nesta revisão

- **Rate limiting nunca era aplicado.** A política `Padrao` existia, mas nenhuma rota a usava. Agora `MapControllers().RequireRateLimiting(...)`.
- **CSP bloqueava o Swagger UI em Development.** Corrigido só para Development.
- **Mensagem de erro interna vazava em produção.** O `ExceptionMiddleware` devolvia `exception.Message` de qualquer exceção. Agora só `AppException` e Development mostram a mensagem real.

## Checklist §6.2 (Bloco 1)

| Item | Estado |
|---|---|
| HTTPS + HSTS | ✅ `UseHttpsRedirection` e `UseHsts` fora de Development |
| Hash de senha | ⬜ Depende da FASE 17 (JWT) |
| CSRF | N/A com JWT em header |
| Limite de payload | ✅ Kestrel 10 MB |
| Log forging | ⬜ Revisar quando houver logs com input do usuário |
| Scan de dependências | ✅ CI + Dependabot (`.github/dependabot.yml`) |
| User Secrets | ✅ `UserSecretsId` no projeto Api |
| Idempotência | ⬜ Adiado |
| Timeout de query | ✅ `CommandTimeout` de `MySql:CommandTimeout` |

## Checklist §6.3 (Bloco 2)

| Item | Estado |
|---|---|
| Mass assignment | ✅ `ProdutoRequest` não expõe `Id` nem `Situacao` |
| IDOR | ⬜ Depende da FASE 17; `AcessoNegadoException` existe |
| Erro sem detalhe interno | ✅ Corrigido acima |
| Enumeração de usuários, upload, XML | N/A hoje |
| Auditoria | ⬜ Adiado |

## Pontos de atenção

- `SecurityHeadersMiddleware`: a CSP `default-src 'self'` bloqueava estilo inline e imagens `data:` do Swagger UI (confirmado no navegador). Agora Development usa uma CSP um pouco mais permissiva; os demais ambientes mantêm a restrita.
- `appsettings.Staging.json` e `appsettings.Production.json` ainda têm domínios de exemplo (`seudominio.com`).
- O Dockerfile não foi construído localmente (a máquina não tem Docker). A validação fica para o CI.
- Alguns itens da FASE 19 (Docker scout/trivy, `npm audit`) dependem de Docker ou do frontend.
