# Hardening — checklist de segurança

Estado do template. Revise antes de colocar um projeto novo em produção.

## Verificações automáticas

| Verificação | Estado |
|---|---|
| `dotnet list package --vulnerable --include-transitive` (CI) | Falha o build se houver vulnerabilidade |
| `FromSqlRaw` / `ExecuteSqlRaw` em `src/` | Nenhuma ocorrência |
| `EnableSensitiveDataLogging` em `src/` | Nenhuma ocorrência |
| `AllowAnyOrigin` | Nenhum uso |

## Checklist

| Item | Estado |
|---|---|
| HTTPS + HSTS | ✅ `UseHttpsRedirection` e `UseHsts` fora de Development |
| Hash de senha | ✅ BCrypt, fator 12 |
| Autenticação | ✅ JWT; todo endpoint exige login, exceto login e `/health` |
| Rate limiting | ✅ 100/min por IP e 5/min no login |
| CORS | ✅ Origens explícitas; a API não sobe fora de Development sem elas |
| Limite de payload | ✅ Kestrel 10 MB |
| Erro sem detalhe interno | ✅ Só `AppException` e Development mostram a mensagem real |
| Anti-enumeração de usuários | ✅ Mesma resposta e tempo parecido no login |
| Segredos | ✅ User Secrets em dev; variáveis de ambiente fora dele |
| Scan de dependências | ✅ CI + Dependabot mensal |
| Timeout de query | ✅ `MySql:CommandTimeout` |
| IDOR | ⬜ Single-tenant; valide a posse se uma entidade passar a ter dono |
| Log forging | ⬜ Revisar quando houver logs com entrada do usuário |
| Idempotência, auditoria | ⬜ Adiado |

## Pontos de atenção

- `SecurityHeadersMiddleware`: em Development a CSP é mais permissiva para o Swagger UI; os demais ambientes mantêm a restrita.
- `appsettings.Staging.json` e `appsettings.Production.json` têm domínios de exemplo (`seudominio.com`): troque por projeto.
- Atrás de proxy ou load balancer, configure `UseForwardedHeaders` para o rate limit enxergar o IP real.
- O Dockerfile é validado apenas pelo CI.
