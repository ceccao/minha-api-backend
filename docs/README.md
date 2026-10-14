# API Base Template — Solution Genérica em C#

> **Propósito deste documento:** servir como fonte única de contexto do projeto. Qualquer pessoa (ou assistente de IA) que ler este README deve conseguir entender *o que* está sendo construído, *por que* cada decisão foi tomada e *o que ainda falta*, sem precisar de contexto externo.

---

## 1. Visão Geral

Este projeto é a construção de uma **solution genérica de API em C#**, que servirá como **ponto de partida padrão (template)** para todas as APIs futuras.

**Filosofia adotada:** começar simples e funcional, evoluindo em fases — conectando banco, adicionando melhorias, endurecendo segurança e ampliando capacidades progressivamente. Nada de over-engineering na largada.

**Objetivo final:** empacotar a base como um `dotnet new` template, de forma que cada novo projeto nasça com arquitetura, segurança, observabilidade e DevEx já configurados.

### 1.1 Estrutura de repositórios 🔁 (revisado — poly-repo)

O projeto **"MinhaApi"** é composto por **três repositórios GitHub separados**, não um monorepo:

| Repositório | Conteúdo |
|---|---|
| **`minha-api-backend`** | Esta solution C# (todas as camadas descritas na §2), testes, e a pasta `docs/` com **este README, o `PLANO-DE-ACAO.md` e os runbooks** — é o repositório "guarda-chuva" que qualquer pessoa (ou IA) deve ler primeiro |
| **`minha-api-database`** | Changelogs do Liquibase, properties por ambiente, `docker-compose.yml` do MySQL |
| **`minha-api-frontend`** | Workspace Angular |

Cada um tem as próprias branches (`main`/`dev`/`hml`) e pipeline de CI/CD — eles espelham os recursos Azure que serão provisionados de forma independente (App Service, Database for MySQL Flexible Server, Static Web App). Ver `PLANO-DE-ACAO.md` FASE 1.0 para o racional completo da decisão.

---

## 2. Arquitetura de Camadas

🔁 **Revisado durante a execução:** a solution passou por duas reorganizações depois que o CRUD de Produto já estava funcionando — (1) pastas organizadas **por entidade** dentro de cada camada (`Produtos/`, e a próxima entidade que entrar ganha a sua), e (2) o projeto `DataTransfer` foi **fundido dentro de `Application`** (deixou de ser uma camada própria), e um conceito de **Command** (estilo CQRS leve, sem MediatR) foi introduzido no Domain. 🔁 **Revisão posterior (out/2026):** o ORM foi trocado de **NHibernate para Entity Framework Core** — a mudança ficou concentrada na Infra e no IoC (Domain e Application não foram tocados, que é justamente o benefício da regra de dependência). A tabela e o diagrama abaixo já refletem o estado atual.

A solution (dentro do `minha-api-backend`) é organizada nas seguintes camadas:

| Camada | Responsabilidade |
|---|---|
| **API** | Porta de entrada HTTP. Controllers, `Program.cs`, Swagger, pipeline de middlewares. Converte `Request` → `Command` antes de chamar a Application. |
| **Application** | Orquestração / casos de uso. Contém `Services/` (services + validators, que agora validam **Commands**, não mais Requests diretamente), `DataTransfer/` (Requests/Responses — **absorveu o projeto DataTransfer**), `Profiles/` (mapeamentos Mapster) e `Common/` (Result Pattern). |
| **Consumers** *(futuro)* | Consumidores de fila/mensageria. |
| **CrossCutting** | Utilitários transversais: logging, exceptions tipadas, middlewares reutilizáveis. **Não depende de nenhum ORM** (ver §7.3 — a tradução de exceção de concorrência mora na Infra). |
| **Domain** | Núcleo. `Entities/`, `Commands/` (objetos de intenção estilo CQRS leve), `Repositories/` (interfaces + `Filters/` para critérios de consulta simples), `Services/Interfaces/` (reservado, sem domain service ainda). **Não depende de ninguém** — nem do EF Core (nada de data annotations de mapeamento nas entidades). |
| **Infra** | Implementação técnica: **EF Core** (`MinhaApiDbContext`, `Configurations/` com `IEntityTypeConfiguration<T>`), `Repositories/` concretos. Traduz exceções específicas do EF Core (ex: `DbUpdateConcurrencyException`) para os tipos do CrossCutting antes de deixá-las subir. |
| **IoC** | Composição da injeção de dependência (`AddApplication()`, `AddInfra()`, `AddCrossCutting()`). |
| **Jobs** | Tarefas agendadas (Hangfire/Quartz). Consome os mesmos services da Application. |
| **Workers** *(futuro)* | Background services de longa duração. |
| **Testes** | Testes unitários e de integração. |

### 2.0 Estrutura de pastas por entidade (dentro de cada camada)

Cada camada organiza suas pastas **por tipo técnico primeiro, por entidade dentro** — as duas coisas juntas, não uma no lugar da outra:

```
Domain/
├── Commands/Produtos/          (CriarProdutoCommand, AtualizarProdutoCommand)
├── Entities/Produtos/          (Produto — EntidadeBase fica solta, é a base de todas)
├── Repositories/
│   ├── Filters/Produtos/       (ProdutoFilter — critério de consulta simples)
│   └── Produtos/               (IProdutoRepository)
└── Services/Interfaces/        (reservado, sem domain service ainda)

Application/
├── DataTransfer/
│   ├── Requests/Produtos/      (CriarProdutoRequest, AtualizarProdutoRequest, AtualizarPrecoRequest)
│   ├── Responses/Produtos/     (ProdutoResponse)
│   └── Common/                 (PagedResult<T>, PaginacaoRequest — genéricos)
├── Profiles/Produtos/          (ProdutoMappingConfig)
├── Services/
│   ├── Interfaces/Produtos/    (IProdutoService)
│   └── Produtos/               (ProdutoService + os validators de Command)
└── Common/                     (Result<T>/Result — genérico)

Infra/
├── Persistence/
│   ├── MinhaApiDbContext.cs    (DbContext — fica solto, é de todas as entidades)
│   └── Configurations/Produtos/ (ProdutoConfiguration — IEntityTypeConfiguration<Produto>)
└── Repositories/
    ├── RepositorioBase.cs      (genérico, fica solto)
    └── Produtos/               (ProdutoRepository)
```

### 2.1 Regra de Ouro das Dependências

```
Domain  ←  Application  ←  Infra
   ↑           ↑
CrossCutting   |
               |
         API  ←  IoC (agrega tudo)
         Jobs, Consumers, Workers  →  Application
```

- **Domain** nunca conhece ninguém.
- **Infra** conhece Domain/Application, mas **nunca** o contrário.
- **API** não referencia Infra diretamente — só via interface + IoC resolvendo em runtime.
- **IoC** depende de todo mundo, mas ninguém depende dele (exceto a API, para montar o `Program.cs`).

> `DataTransfer` saiu do diagrama como camada própria — seus tipos agora moram dentro de `Application`.

### 2.2 Abstrações

O princípio que sustenta a regra acima: **depender de contrato (interface), não de implementação concreta**.

```csharp
// DOMAIN — contrato, sem saber nada de EF Core
public interface IProdutoRepository : IRepositorioBase<Produto>
{
    Task<IEnumerable<Produto>> ObterComFiltroAsync(ProdutoFilter filtro, CancellationToken ct = default);
}

// INFRA — implementação usando EF Core
public class ProdutoRepository : RepositorioBase<Produto>, IProdutoRepository { /* usa MinhaApiDbContext */ }

// APPLICATION — só conhece a interface, e trabalha com Command, não Request
public class ProdutoService(IProdutoRepository repository) { /* ... */ }

// IOC — decide quem entra em runtime
services.AddScoped<IProdutoRepository, ProdutoRepository>();
```

**Benefícios concretos:** testabilidade (mock sem banco real), troca de tecnologia sem reescrever regra de negócio (comprovado na prática: a troca NHibernate → EF Core não tocou Domain nem Application), e trabalho paralelo (Application codifica contra a interface enquanto Infra ainda está sendo feita).

---

## 3. Stack Definida

| Categoria | Tecnologia | Status |
|---|---|---|
| Runtime | .NET (LTS vigente) | Definido |
| Web | ASP.NET Core | Definido |
| ORM | **Entity Framework Core** *(🔁 substituiu o NHibernate em out/2026 — adoção de mercado)* | Definido |
| Provider MySQL | **Pomelo.EntityFrameworkCore.MySql** (sobre o MySqlConnector) | Definido — ver ressalva de versão abaixo |
| Banco | **MySQL** | Definido |
| Mapeamento | **Mapster** | Definido |
| Versionamento de schema | **Liquibase** (substitui FluentMigrator) — **único dono do schema; sem EF Migrations** | Definido |
| Frontend | **Angular + TypeScript** | Definido |
| Containerização | Docker + docker-compose | Definido |
| CI | GitHub Actions | Definido |
| Hospedagem | **Azure** (App Service + Database for MySQL Flexible Server + Static Web Apps) | Definido — adormecido até FASE 21 ser ativada |

> ⚠️ **Ressalva de versão (EF Core × Pomelo × .NET 10):** na última checagem (out/2026), o Pomelo estável ainda era a linha **9.x** (EF Core 9); a 10.x aparecia como pendente. O Pomelo declara suportar a 9.x rodando em projetos `net10.0` (versão do framework e versão do EF Core são independentes). Escolha segura hoje: **EF Core 9.x + Pomelo 9.x num projeto `net10.0`**. Conferir no NuGet antes de instalar; se a 10.x já estiver estável, subir os dois juntos — nunca misturar majors diferentes entre `Microsoft.EntityFrameworkCore` e o Pomelo.

---

## 4. Bibliotecas por Fase

| Fase | Camada | Lib | Categoria |
|---|---|---|---|
| 1 | API | Swashbuckle.AspNetCore | Documentação |
| 1 | CrossCutting | Serilog + Serilog.AspNetCore | Logging |
| 1 | Application | FluentValidation | Validação (invocada explicitamente na Application — ver §5.1) |
| 1 | API | AspNetCore.HealthChecks.MySql | Health Checks |
| 1 | Testes | xUnit | Testes |
| 1 | Testes | **AwesomeAssertions** *(não FluentAssertions)* | Testes |
| 2 | Infra | **Microsoft.EntityFrameworkCore** + **Pomelo.EntityFrameworkCore.MySql** *(traz o MySqlConnector junto)* | Persistência |
| 2 | Infra | **System.Linq.Dynamic.Core** | Ordenação dinâmica segura no `ListarAsync` genérico |
| 2 | Application/Infra | Mapster | Mapeamento |
| 2 | Testes | NSubstitute | Mocking |
| 2 | Testes | Bogus | Massa de dados fake |
| 2 | Testes | Testcontainers | MySQL real em testes de integração |
| 3 | API | Microsoft.AspNetCore.Authentication.JwtBearer | Autenticação |
| 3 | CrossCutting/Infra | Microsoft.Extensions.Http.Resilience (Polly) | Resiliência |
| 4 | CrossCutting | IMemoryCache (nativo) | Cache simples |
| 4 | CrossCutting | Microsoft.Extensions.Caching.StackExchangeRedis | Cache distribuído |
| 5 | Jobs | Hangfire *(ou Quartz.NET)* | Jobs agendados |
| 6 | Consumers/Workers | MassTransit *(ou Rebus)* | Mensageria |
| 7 | CrossCutting/API | OpenTelemetry.Extensions.Hosting | Observabilidade |
| 7 | API | Duende IdentityServer / Keycloak (OIDC) | Auth avançado |

> ⚠️ **`Microsoft.EntityFrameworkCore.Design` não é instalado** — ele só serve pro `dotnet ef` (migrations/scaffold), e não usamos EF Migrations (o Liquibase é o dono do schema — ver §7.7).
>
> ⚠️ **`FluentValidation.AspNetCore`** (auto-validação via filtro do MVC) foi **descontinuado** pelo próprio time do FluentValidation — não instalar. Ver §5.1 para a abordagem adotada em vez disso.
>
> 🔴 **`FluentAssertions` mudou de licença (jan/2025):** a partir da v8, exige **licença paga da Xceed** (~US$130/dev/ano) pra uso comercial — só é grátis pra uso não-comercial. Como este template pode virar produto/serviço vendido, usamos **`AwesomeAssertions`** em vez disso: fork comunitário sob licença Apache 2.0 (grátis pra sempre, inclusive comercial), API idêntica — só muda o nome do pacote e o `using`. Ver FASE 12.0 do plano.

---

## 5. Decisões de Arquitetura Tomadas

### 5.1 Tratamento de Erros — **Modelo Híbrido**

| Cenário | Abordagem |
|---|---|
| Erro que só precisa **parar e responder** (404, 400, 409) | **Exception tipada** (`NaoEncontradoException<T>`, `EntidadeInvalidaException`, `ConflitoException`) + **middleware central** |
| Fluxo de negócio com **múltiplos caminhos** (retry, fallback, compensação) | **Result Pattern** (`Result<T>`) |
| Erro **técnico de infraestrutura** (banco caiu, timeout) | Exception "crua" → 500 genérico |

**Racional:** exceptions tipadas atravessam a cadeia `Controller → Service → Repository` automaticamente, sem boilerplate de propagação (C# não tem operador `?` do Rust). O Result Pattern só ganha quando é preciso **ramificar decisão** com base no erro. O middleware central mapeia tipo de exceção → status HTTP → nível de log em um único lugar.

```csharp
public abstract class AppException : Exception
{
    public abstract int StatusCode { get; }
}

public class NaoEncontradoException<T> : AppException
{
    public override int StatusCode => 404;
    public NaoEncontradoException(object id)
        : base($"{typeof(T).Name} com id '{id}' não foi encontrado.") { }
}
```

No middleware: `AppException` → `LogWarning` (erro esperado, não polui alerta); `Exception` → `LogError` (aciona alerta real).

🔁 **Validação (FluentValidation) — revisado na execução:** `FluentValidation.AspNetCore` (auto-validação via filtro automático do MVC) foi descontinuado pelo próprio time do FluentValidation, que recomenda validação explícita no código. A abordagem adotada: a **Application injeta `IValidator<T>`** diretamente nos Services e chama `ValidateAsync` explicitamente antes de tocar no Domain, lançando `EntidadeInvalidaException` em caso de erro — assim o erro de validação passa pelo **mesmo** `ExceptionMiddleware` de todo o resto, em vez de dois mecanismos de erro coexistindo (o filtro automático de `[ApiController]`/`ModelState` e o do FluentValidation).

🔁 **Command em vez de Request na Application — revisado na execução:** o Controller converte o `Request` (contrato de API) em um `Command` (Domain, intenção de negócio) antes de chamar a Application. Os validators passaram a validar o `Command`, não mais o `Request` diretamente. Isso é CQRS **leve**: só o objeto de comando, sem MediatR/handlers separados — a Service continua processando tudo direto, sem infraestrutura nova.

🔁 **`Result<T>` com exemplo real — revisado na execução:** o padrão estava documentado mas sem nenhum uso de fato (dead code). Exemplo concreto implementado: `ProdutoService.AtualizarPrecoComRetryAsync` — tenta atualizar o preço, e se bater em conflito de concorrência otimista (`ConflitoException`), decide sozinho se tenta de novo (recarrega o estado mais recente) ou desiste após N tentativas, devolvendo `Result.Ok`/`Result.Falha` pro chamador em vez de deixar a exception estourar. O Controller só transforma isso em status HTTP (200 ou 409) na borda — é onde a abstração do `Result<T>` termina. 🔁 *Com o EF Core, o "recarrega o estado mais recente" só funciona porque a Infra faz `ChangeTracker.Clear()` ao detectar o conflito — sem isso o contexto devolveria a instância antiga do cache (ver §7.3).*

### 5.2 Repository Genérico — **APROVADO, reformulado na execução**

`IRepositorioBase<TEntidade>` / `RepositorioBase<TEntidade>` com métodos padrão, todos com `CancellationToken`:

```csharp
public interface IRepositorioBase<TEntidade> where TEntidade : class
{
    Task InserirAsync(TEntidade entidade, CancellationToken cancellationToken = default);
    Task InserirAsync(IEnumerable<TEntidade> entidades, CancellationToken cancellationToken = default);
    Task EditarAsync(TEntidade entidade, CancellationToken cancellationToken = default);
    Task ExcluirAsync(TEntidade entidade, CancellationToken cancellationToken = default);
    Task ExcluirAsync(IEnumerable<TEntidade> entidades, CancellationToken cancellationToken = default);
    Task<TEntidade?> RecuperarAsync(int id, CancellationToken cancellationToken = default);
    Task<TEntidade?> RecuperarAsync(Expression<Func<TEntidade, bool>> expressao, CancellationToken cancellationToken = default);

    Task<PaginacaoConsulta<TEntidade>> ListarAsync(
        PaginacaoFiltro paginacao,
        Expression<Func<TEntidade, bool>>? filtro = null,
        CancellationToken cancellationToken = default);

    Task<PaginacaoConsulta<TEntidade>> ListarAsync(
        int qt, int pg, (string Campo, TipoOrdenacao Tipo)[] ordenacao,
        Expression<Func<TEntidade, bool>>? filtro = null,
        CancellationToken cancellationToken = default);
}
```

> 🔁 **Histórico de revisões:** `ObterPorIdAsync` → `RecuperarAsync` (nome genérico); `Id` das entidades de `long` → `int`; `AtualizarAsync` → `EditarAsync`; ganhou `ExcluirAsync` em lote, `RecuperarAsync` por predicado (`Expression<Func<T,bool>>`, reduz a necessidade de métodos tipo `ObterPorNomeAsync` por entidade), e `ListarAsync` com paginação/ordenação genéricas de verdade (usando `System.Linq.Dynamic.Core` pra ordenar por nome de campo com segurança). **O contrato não mudou com a troca de ORM** — só a implementação na Infra.

**Peças de apoio (`Domain/Abstractions/`):** `TipoOrdenacao` (enum `Ascendente`/`Descendente`), `PaginacaoConsulta<T>` (resultado paginado de entidades — distinto do `PagedResult<T>` da Application, que carrega DTOs), `PaginacaoFiltro` (consolida `Qt`/`Pg`/`CpOrd`/`TpOrd`, com teto de `Qt` em 100 — proteção contra DoS, README §6.2).

🔴 **Alerta de segurança evitado:** uma primeira versão do `PaginacaoFiltro` (inspirada num padrão usado no trabalho) tinha um método que montava `ORDER BY` **concatenando string em SQL** — SQL Injection clássico se o nome do campo viesse de input do usuário. Removido. A ordenação de verdade usa `System.Linq.Dynamic.Core` (`query.OrderBy("Preco descending")`), que valida o nome da propriedade via reflection contra a entidade **antes** de montar a query — nome inválido gera exceção de parse, nunca chega a executar nada no banco.

Filtros combináveis (múltiplos campos opcionais) não precisam de biblioteca de combinação de `Expression` — um único lambda com cada condição neutralizada resolve:
```csharp
p => (string.IsNullOrWhiteSpace(nome) || p.Nome.Contains(nome))
  && (!precoMin.HasValue || p.Preco >= precoMin.Value)
```

⚠️ **Ressalva conhecida:** parte da comunidade DDD critica o "Generic Repository" por empurrar lógica de query pra fora do repositório. Aceito conscientemente pelo ganho de produtividade num template genérico. (Com EF Core a crítica é ainda mais comum — o `DbContext` já é, ele próprio, um Unit of Work + repositório. Mantemos a abstração porque ela isola Application/Domain do ORM, o que a troca NHibernate → EF Core só confirmou ser valioso.)

⚠️ **Validação NÃO mora no Repository.** Repository é sobre persistência; validação é regra de negócio → fica na Application via FluentValidation (chamada explicitamente sobre o **Command**, ver §5.1).

### 5.3 Envelope de Resposta Padronizado — **ADIADO**

**Decisão: não usar** `ApiResponse<T>` genérico (`{ sucesso, dados, erros }`) na versão inicial.

**Racional:** envelopar tudo duplica o que o status HTTP já comunica e cria ambiguidade (`200 OK` com `"sucesso": false`). Para CRUD simples com um consumidor, é redundância.

**O que usar em vez disso:**
- Sucesso → **DTO puro** + status HTTP correto (`Ok()`, `CreatedAtAction()`)
- Erro → **ProblemDetails (RFC 7807)**, padrão nativo do ASP.NET Core
- `traceId` → via **header** (`X-Trace-Id`), não no corpo

**Guardado para implementação futura, apenas em casos específicos:**
- Operações em lote (bulk) com sucesso parcial → `207 Multi-Status` + envelope
- `PagedResult<T>` em listagens (metadados de paginação)

### 5.4 Specification Pattern — **ADIADO**

Considerado essencial apenas quando as regras de consulta/negócio ficarem muito complexas. Não é prioridade da versão inicial simples do template.

### 5.5 Padrões Complementares (a avaliar por fase)

- **Unit of Work** — para commit/rollback atômico entre múltiplos repositórios (o `DbContext` do EF Core **já é** um Unit of Work: `SaveChangesAsync()` persiste tudo numa transação; para controle explícito, `Database.BeginTransactionAsync()`)
- **Soft Delete** — campo `Ativo`/`DeletedAt`. ✅ **Adotado** desde a `EntidadeBase` (campo `Ativo` + método `Desativar()`) — usado no fluxo de exclusão de Produto em vez de apagar a linha. O EF Core oferece **Global Query Filter** (`HasQueryFilter(e => e.Ativo)`) como filtro automático, mas **não foi aplicado** por enquanto: o filtro opcional `Ativo` da listagem (§5.2) precisa poder enxergar inativos
- **PagedResult\<T\>** — paginação genérica em listagens. ✅ **Implementado**
- **Result\<T\>** — fluxos com múltiplos caminhos que exigem decisão do chamador. ✅ **Implementado com exemplo real** — `ProdutoService.AtualizarPrecoComRetryAsync` (retry em conflito de concorrência otimista, ver §5.1)
- **Filtros/Ordenação genéricos** — ✅ **Implementado** como objeto `Filter` simples (ex: `ProdutoFilter`), não o Specification Pattern completo (§5.4)
- **Correlation ID** — middleware gerando ID único por request, presente em todos os logs. ✅ **Implementado**
- **Options Pattern** — configs tipadas e validadas. ✅ **Implementado** (`MySqlOptions`)
- **Feature Flags** — ligar/desligar funcionalidade sem redeploy

---

## 6. Segurança

**Princípio central:** segurança **não é uma camada** — é responsabilidade transversal. Cada camada protege sua própria porta (*defense in depth*).

### 6.1 Mapa: Ameaça → Camada

| Ameaça | Camada | Técnica |
|---|---|---|
| SQL Injection | Infra | LINQ/EF Core parametrizado; SQL cru só com `FromSql($"...")`/`ExecuteSql($"...")` (interpolação **parametriza**); **nunca** `FromSqlRaw`/`ExecuteSqlRaw` com string concatenada |
| Autenticação | API | JwtBearer |
| Autorização | API + Application | Policies/Claims + regra de negócio |
| Payload malicioso / DoS | Application | FluentValidation (limites, formatos) |
| Força bruta / DDoS básico | API | Rate Limiting nativo (.NET 7+) |
| Vazamento de dados sensíveis | DataTransfer + CrossCutting | DTOs (nunca entidade crua) + logging cuidadoso |
| Segredos expostos | Infra/API | User Secrets, variáveis de ambiente, Key Vault |
| CORS mal configurado | API | Middleware CORS restritivo |
| Headers HTTP fracos | API | Middleware de security headers |

### 6.2 Checklist de Segurança — Bloco 1 (implementar cedo)

- [ ] **HTTPS obrigatório + HSTS** (`UseHttpsRedirection()`, `UseHsts()`) — migrar depois quebra integrações antigas
- [ ] **Hash de senha com BCrypt.Net-Next ou Argon2** — nunca MD5/SHA1; trocar depois exige re-hash de todos
- [ ] **Anti-forgery / CSRF** — se algum dia usar cookies de autenticação (não necessário com JWT em header)
- [ ] **Limite de tamanho de payload** (Kestrel `MaxRequestBodySize`, `IFormOptions`)
- [ ] **Sanitização de logs (log forging)** — destructuring do Serilog (`@`), nunca concatenar input cru
- [ ] **Scan de dependências vulneráveis** — Dependabot + `dotnet list package --vulnerable` no CI
- [ ] **User Secrets desde o commit 1** — segredo que já vazou no histórico do Git precisa ser rotacionado, não só removido
- [ ] **Idempotência em endpoints de escrita** — header `Idempotency-Key` em operações críticas
- [ ] **Timeout de queries + circuit breaker no DB** — evita esgotamento do pool de conexões (EF Core: `CommandTimeout` no `UseMySql`)

**Top 3 que não podem ser adiados:** HTTPS/HSTS, User Secrets desde o commit 1, Dependabot/scan no CI.

### 6.3 Checklist de Segurança — Bloco 2 (vulnerabilidades fora do fluxo óbvio)

- [ ] **Mass Assignment / Over-posting** — DTOs de request específicos; nunca reaproveitar DTO de criação para edição de campos sensíveis (`Id`, `CriadoEm`, `Ativo`)
- [ ] **IDOR (Insecure Direct Object Reference)** ⚠️ **CRÍTICO** — validar **posse do recurso**, não só autenticação. Regra da casa: todo método da Application que busca por ID recebe também `UsuarioLogadoId`/`TenantId` e valida propriedade. É a vulnerabilidade #1 do OWASP API Security Top 10 e passa despercebida em code review porque o código "funciona". *(Se o template virar multi-tenant, o EF Core oferece Global Query Filters por `TenantId` como camada extra de defesa — nunca como substituto da validação explícita.)*
- [ ] **Mensagens de erro sem detalhe interno em produção** — sem stack trace, nome de tabela ou versão de lib; só mensagem genérica + `traceId`. Diferenciar via `IsDevelopment()`
- [ ] **Enumeração de usuários** — login e recuperação de senha devem retornar mensagem idêntica para "usuário inexistente" e "senha errada"
- [ ] **Deserialização insegura** — cuidado com XML (XXE) e parsers customizados
- [ ] **Upload de arquivo** — validar *magic number* (não confiar em extensão/`Content-Type`), sanitizar nome (path traversal `../../`)
- [ ] **Auditoria** — tabela/log com `UsuarioId`, `Ação`, `EntidadeAfetada`, `DataHora` para operações sensíveis. Impossível reconstruir retroativamente

### 6.4 LGPD / Proteção de Dados Pessoais

- [ ] **Minimização de dados** — só coletar/armazenar o estritamente necessário
- [ ] **Direito ao esquecimento** — mecanismo de exclusão/anonimização. ⚠️ Conflita com Soft Delete → prever campo `AnonimizadoEm` para dados sensíveis
- [ ] **Log não pode vazar dado pessoal** — LGPD trata como incidente de segurança. ⚠️ Com EF Core: **nunca** `EnableSensitiveDataLogging()` fora de `Development` (ele joga valores reais de parâmetros SQL nos logs)
- [ ] **Criptografia em repouso** para campos sensíveis (CPF, dados de saúde) — não só a senha

### 6.5 Segurança da Imagem Docker

- [ ] **Multi-stage build** — imagem final com runtime (`aspnet`), não SDK completo
- [ ] **Usuário non-root** dentro do container
- [ ] Menor superfície de ataque possível

---

## 7. Cuidados Específicos de EF Core e Produção

### 7.1 DbContext-per-Request ⚠️ CRÍTICO

`MinhaApiDbContext` **deve** viver como **Scoped** (uma instância por requisição HTTP) — que é o padrão do `AddDbContext`:

```csharp
services.AddDbContext<MinhaApiDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 4, 0)),
        mySql => mySql.CommandTimeout(30)));
```

**Se alguém "otimizar" registrando como Singleton:** vazamento de dados entre usuários, `InvalidOperationException` de concorrência (o `DbContext` **não é thread-safe**) e memory leak (o rastreador de entidades acumula indefinidamente). Erro clássico, e o mesmo princípio valia pro `ISession` do NHibernate.

Diferença em relação ao NHibernate: **não existe `ISessionFactory` pra você registrar** — o EF Core cuida do modelo e do pool de conexões internamente. `ServerVersion` explícita (não `AutoDetect`): o `AutoDetect` abre conexão na hora da configuração e quebra a subida da API se o banco ainda não estiver de pé.

### 7.2 Problema N+1 (Lazy Loading) ⚠️ CRÍTICO

Lazy loading é **desligado por padrão** no EF Core — o N+1 aparece quando há consulta **dentro de um loop**, ou se alguém habilitar `UseLazyLoadingProxies`. Regra: relacionamento sempre com `Include` explícito.

```csharp
// ❌ 1 + N queries
var pedidos = await context.Pedidos.AsNoTracking().ToListAsync();
foreach (var p in pedidos)
{
    var cliente = await context.Clientes.FindAsync(p.ClienteId);   // query por item
}

// ✅ 1 query
var pedidos = await context.Pedidos.AsNoTracking().Include(p => p.Cliente).ToListAsync();
```

Para coleções grandes, `AsSplitQuery()` evita explosão cartesiana. Consultas **somente leitura** usam `AsNoTracking()`.

Causa #1 de "a API ficou lenta do nada". Compila, passa nos testes, funciona liso em dev com poucos dados — só aparece com volume real.

### 7.3 Concorrência Otimista (Lost Update)

Campo `Version` (INT) na entidade, configurado com `IsConcurrencyToken()` na `IEntityTypeConfiguration`. ⚠️ **Diferença importante:** o NHibernate incrementava a versão sozinho; o **EF Core não incrementa um token `int`** — o `MinhaApiDbContext` sobrescreve o `SaveChangesAsync` e faz `Version = OriginalValue + 1` para toda `EntidadeBase` modificada, num lugar só. O `UPDATE` sai com `WHERE id = ? AND version = ?`; se outra operação já alterou a linha, o EF Core lança `DbUpdateConcurrencyException`.

🔁 **Tradução na Infra:** o `RepositorioBase.EditarAsync` captura o `DbUpdateConcurrencyException` do EF Core e relança como `ConflitoException` (tipo do próprio CrossCutting) **antes** de deixar a exceção subir. Vantagem: o `ExceptionMiddleware` não precisa conhecer o ORM — ele só lida com `AppException` e subtipos, então **o CrossCutting não referencia nenhum pacote de ORM**. Só a Infra sabe que esse erro específico existe.

```csharp
// Infra/Repositories/RepositorioBase.cs
try
{
    Context.Update(entidade);
    await Context.SaveChangesAsync(cancellationToken);
}
catch (DbUpdateConcurrencyException)
{
    // Estado rastreado ficou desatualizado: limpa pra que o retry recarregue do banco de verdade.
    Context.ChangeTracker.Clear();
    throw new ConflitoException("O recurso foi modificado por outra operação. Recarregue e tente novamente.");
}
```

⚠️ **O `ChangeTracker.Clear()` é essencial pro retry do `Result<T>` (§5.1):** reconsultar uma entidade que o contexto já rastreia devolve a **instância antiga do cache** (identity map), não os valores novos do banco. Sem a limpeza, `AtualizarPrecoComRetryAsync` reconsultaria o mesmo objeto velho e falharia de novo em todas as tentativas.

⚠️ Use só as versões **assíncronas** (`SaveChangesAsync`): se alguém chamar o `SaveChanges()` síncrono, o incremento do `Version` não roda (a menos que o método síncrono também seja sobrescrito) e a proteção de concorrência quebra em silêncio.

🔁 **Provider MySQL — o que mudou em relação ao NHibernate:** não existe mais a pegadinha do driver separado (`NHibernate.Driver.MySqlConnector` — build passava, runtime quebrava). O `Pomelo.EntityFrameworkCore.MySql` traz o `MySqlConnector` junto. A nova pegadinha é **de versão**: o Pomelo precisa casar o *major* com o `Microsoft.EntityFrameworkCore` (ver ressalva na §3), e o Dependabot deve agrupar os dois pacotes pra não abrir PRs separados que quebram o build.

🕒 Pegadinha de UTC: o Pomelo devolve `DateTime` lido do banco com `Kind = Unspecified`. Se isso afetar a serialização (ausência do `Z` no JSON), usar um `ValueConverter` que force `DateTimeKind.Utc` na leitura.

### 7.4 Datas sempre em UTC

Sempre `DateTime.UtcNow`, nunca `DateTime.Now`. Idealmente forçado por analisador de código. Conversão para fuso local é responsabilidade da camada de apresentação.

### 7.5 Graceful Shutdown

```csharp
app.Lifetime.ApplicationStopping.Register(() => { /* drenar requests em andamento */ });
```

Evita erro 500 para o usuário que estava no meio de uma operação durante um deploy/restart de container.

### 7.6 Captive Dependency ⚠️

Registrar um Singleton que depende de um Scoped (ex: `MinhaApiDbContext`) compila mas quebra em produção — o contexto do primeiro request fica preso para sempre. **Mitigação:** analisador de DI acusando isso em tempo de build + `ValidateScopes = true` em ambiente de desenvolvimento.

### 7.7 Rollback de Schema

Versionamento via **Liquibase**. 🔁 **Revisado (segunda vez):** changesets começaram em XML nativo (rollback automático), depois voltamos pra **SQL puro** (`<sqlFile>` apontando pra `.sql` versionado por versão/ticket) — rollback deixou de ser gerado automaticamente, agora é **escrito à mão** e precisa ser testado manualmente antes de cada commit (ver PLANO-DE-ACAO.md FASE 4.0 para o trade-off completo). ⚠️ **Liquibase CLI exige Java (JRE) instalado.** ⚠️ **Liquibase Community 5.0+ não vem com nenhum driver de banco embutido** (nem MySQL na edição Secure, por licença) — precisa instalar via **LPM** (`liquibase lpm add mysql --global`).

🔁 **Com o EF Core: Liquibase continua sendo o ÚNICO dono do schema.** Proibido usar `dotnet ef migrations`, `Database.Migrate()` e `EnsureCreated()` — dois mecanismos de versionamento escrevendo no mesmo banco geram divergência. Consequência prática: o EF Core **não valida** o schema na subida — nome de coluna divergente entre o SQL do Liquibase e a `IEntityTypeConfiguration` só explode em runtime, na primeira query. Por isso toda mudança de schema e a mudança correspondente no mapeamento andam **juntas, no mesmo ticket**, e o critério de aceite da API é testado de verdade pelo Swagger.

---

## 8. DevEx (Developer Experience)

### 8.1 Docker + docker-compose

MySQL local sem instalação nativa. `docker-compose up -d` e pronto — "clonou, rodou, funcionou".

**Decisão tomada:** usar `restart: always` no serviço MySQL, para que o container suba automaticamente com o Docker Desktop no boot do sistema, eliminando o passo manual. O script `dev.ps1` fica como garantia caso o container esteja parado.

🔁 **Alternativa em uso:** em máquinas com bug de firmware de virtualização (impedindo WSL2/Docker Desktop), o MySQL roda **nativo no Windows** como serviço — mesmo efeito do `restart: always` via "Startup Type: Automatic" do Windows. Ver `PLANO-DE-ACAO.md` FASE 0.2/3.3.

### 8.2 EditorConfig + Analisadores ✅

`.editorconfig` + analisadores nativos do .NET (sem StyleCop/SonarAnalyzer — decisão explícita de manter simples), aplicando formatação e nomenclatura no save e falhando o build em violações.

✅ **Decisões fechadas (FASE 13):** campos privados `_camelCase` (instância) / `PascalCase` (`const`/`static readonly`), 4 espaços, chaves em linha nova (Allman), `this.` nunca explícito, namespace file-scoped, nomes híbridos (domínio em português, técnico em inglês), severidade `error`, `using` com System primeiro. Sem limite formal de tamanho de arquivo/método (fica a critério do code review). Ver a tabela completa e o `.editorconfig` gerado na FASE 13 do plano de ação.

🔁 **Regras já encontradas na prática** com `TreatWarningsAsErrors=true` + `AnalysisLevel=latest-recommended` (ver `PLANO-DE-ACAO.md` FASE 2.5 para a lista com correções): CA1305 (IFormatProvider), CA1848 (LoggerMessage), CA1051 (campo visível), CA1000 (membro estático em tipo genérico), CA1707 (underscore em nome — desativada só nos projetos de teste, é convenção proposital).

### 8.3 GitHub Actions (CI) ✅

Build + testes automáticos a cada push/PR, com merge bloqueado em caso de falha. Com a estrutura poly-repo (§1.1), cada repositório tem seu **próprio** `ci.yml` — backend (build+teste+scan de vulnerabilidades) e banco (validação de changelogs) já rodando verde; frontend fica pra FASE 15.

🔁 **Dois aprendizados reais da execução:**
- **`actions/checkout@v4` precisa virar `@v5`** — o GitHub remove o Node.js 20 dos runners em 16/set/2026, e `@v4` roda nessa versão. Vale o mesmo pra `actions/setup-node`.
- **Instalar Liquibase via `curl` + script não funciona entre steps do CI** — cada `run:` é um shell novo, então o `PATH` atualizado num step não chega no próximo (erro 127, "comando não encontrado"). A correção é usar a action oficial `liquibase/setup-liquibase`, que resolve isso e ainda cacheia entre execuções.

---

## 9. Multi-ambiente e Configuração

**Objetivo:** o mesmo código-fonte roda em dev, staging e produção **sem alteração de código** — só configuração externa (princípio III do 12-Factor App).

### 9.1 Ordem de Prioridade

```
1. appsettings.json                 (menor prioridade)
2. appsettings.{Ambiente}.json
3. Variáveis de ambiente
4. Argumentos de linha de comando   (maior prioridade)
```

### 9.2 Itens Aprovados

- **appsettings por ambiente** (Development / Staging / Production)
- **Variáveis de ambiente** sobrescrevendo appsettings — formato `ConnectionStrings__MySql` (duplo underscore = hierarquia)
- **User Secrets em dev** — `dotnet user-secrets`, fora do repositório
- **`IHostEnvironment`** para comportamento condicional (Swagger e stack trace só em dev)
- **`ValidateOnStart()`** — API não sobe se config obrigatória faltar; erro claro no deploy em vez de falha silenciosa em runtime

```csharp
builder.Services
    .AddOptions<MySqlOptions>()
    .Bind(builder.Configuration.GetSection("MySql"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

---

## 10. Estado Atual do Projeto

| Item | Status |
|---|---|
| Levantamento de tópicos e decisões | ✅ Concluído |
| Plano de ação detalhado | ✅ Ver `PLANO-DE-ACAO.md` (atualizado para EF Core em out/2026) |
| Ambiente de desenvolvimento | ✅ Concluído (FASE 0) |
| Repositórios GitHub | ✅ Concluído — 3 repositórios poly-repo (FASE 1) |
| Solution / camadas (Domain → API) | ✅ Concluído e testado ponta a ponta (FASES 2–11) — **com NHibernate** |
| Banco de dados / Liquibase | ✅ Concluído (FASE 3–4) — MySQL nativo (bug de virtualização na máquina atual) |
| **Migração do código NHibernate → EF Core** | ⬜ **A fazer** — documentação já atualizada; código segue o checklist da FASE 9.6 do plano (afeta só Infra, IoC e pacotes) |
| Testes automatizados | ✅ Concluído com escopo revisado (FASE 12) — Domain + Application; integração/IDOR adiados |
| Personalização das regras do EditorConfig | ✅ Concluído (FASE 13) |
| CI (GitHub Actions) | ✅ Concluído no backend e banco (FASE 14); frontend pendente da FASE 15 |
| Frontend Angular | ⬜ A fazer (FASES 15–16) |
| Autenticação JWT | ⬜ A fazer (FASE 17) |
| Dockerfile / hardening / template `dotnet new` | ⬜ A fazer (FASES 18–20) |
| Deploy Azure (DEV/HML/PROD) | 🔴 Desenhado, adormecido até ativação (FASE 21) |
| Decisão de modelo de negócio (SaaS vs. entrega ao cliente) | 🔴 Pendente |
| Confirmar versão estável do Pomelo para EF Core 10 | 🔴 Pendente (ver ressalva na §3) |

---

## 11. Itens Explicitamente Adiados

Não são esquecimentos — são decisões conscientes de escopo:

| Item | Motivo |
|---|---|
| Specification Pattern | Só quando as regras de consulta ficarem complexas |
| Envelope de resposta genérico | Redundante com HTTP + ProblemDetails para CRUD simples |
| Mensageria (MassTransit/Rebus) | Quando Consumers/Workers forem ativados |
| Cache distribuído (Redis) | Quando houver necessidade real de escala |
| OpenTelemetry | Quando a observabilidade básica já estiver madura |
| Duende IdentityServer / Keycloak | Só se surgir cenário SSO/multi-API |
| .NET Aspire | Alternativa futura ao docker-compose manual |
| EF Migrations | Descartado enquanto o Liquibase for o dono do schema (SQL puro com rollback manual) |
| Global Query Filter de soft delete | Conflita com o filtro opcional `Ativo` na listagem; reavaliar com multi-tenant |

---

## 12. Glossário Rápido

| Termo | Significado |
|---|---|
| **IDOR** | Acessar recurso de outro usuário só trocando o ID na URL |
| **N+1** | Uma query extra por item do loop, por causa de carregamento sob demanda de relacionamento |
| **Captive Dependency** | Singleton que "prende" uma dependência Scoped |
| **Lost Update** | Dois usuários editam o mesmo registro; o segundo sobrescreve o primeiro |
| **Log Forging** | Injetar quebras de linha no input para forjar entradas falsas no log |
| **Mass Assignment** | Enviar campos no payload que não deveriam ser alteráveis |
| **DbContext-per-Request** | Uma instância de `DbContext` (unidade de trabalho do EF Core) por requisição HTTP |
| **Change Tracker** | Rastreador do EF Core que guarda as entidades carregadas e detecta o que mudou; também é o "cache" que devolve a instância antiga numa reconsulta |
| **Concurrency Token** | Coluna (aqui `Version`) que o EF Core inclui no `WHERE` do UPDATE pra detectar edição concorrente |
| **Command** | Objeto que representa a intenção completa de uma ação de negócio (ex: `CriarProdutoCommand`) — CQRS leve, sem handler/mediator separado |
| **Filter object** | Objeto simples agrupando critérios opcionais de consulta (ex: `ProdutoFilter`) — mais simples que o Specification Pattern completo |
| **DevEx** | Fricção (ou ausência dela) no dia a dia de quem desenvolve |
| **Poly-repo** | Vários repositórios independentes (um por peça deployável) em vez de um monorepo único |
