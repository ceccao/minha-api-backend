# MinhaApi — Template Base de API em C#

> **Propósito deste documento:** descrever a arquitetura, as tecnologias e os padrões **como eles realmente estão implementados hoje** — não como um plano teórico. Qualquer entidade nova a ser criada neste template deve seguir os mesmos padrões documentados aqui, usando `Produto` como referência viva.

---

## 1. Visão Geral

Template base de API REST em C#/.NET, organizado em camadas (Domain, Application, Infra, API, CrossCutting, IoC), com persistência via Entity Framework Core/MySQL. O projeto nasceu como estudo prático — várias decisões aqui foram tomadas deliberadamente de forma explícita (em vez de herdar comportamento de classes base "mágicas") para reforçar o entendimento de cada peça na hora de construir.

**Entidade de referência:** `Produto` — implementa o CRUD completo (criar, editar, listar com filtro/paginação/ordenação, recuperar por id, excluir via soft delete) e serve de modelo para qualquer entidade nova.

---

## 2. Stack Tecnológico

| Categoria | Tecnologia |
|---|---|
| Runtime | .NET 10 |
| Web | ASP.NET Core (Controllers, não Minimal API) |
| ORM | Entity Framework Core 9 (mapeamento por `IEntityTypeConfiguration`, sem EF Migrations) |
| Provider MySQL | `Pomelo.EntityFrameworkCore.MySql` 9.x (traz o MySqlConnector) |
| Banco | MySQL 8.x |
| Mapeamento objeto-objeto | Mapster |
| Validação | FluentValidation (injetado explicitamente no `ProdutoService` — ver §5.4) |
| Ordenação dinâmica | System.Linq.Dynamic.Core |
| Logging | Serilog (console + arquivo JSON em produção, enrichment de Correlation ID) |
| Documentação da API | Swashbuckle (Swagger/OpenAPI), com XML comments habilitado |
| Rate limiting | `Microsoft.AspNetCore.RateLimiting` (nativo do .NET) |
| Health checks | `AspNetCore.HealthChecks.MySql` |
| Testes | xUnit + AwesomeAssertions + NSubstitute |
| Versionamento de schema | Liquibase (repositório separado `minha-api-database`) |

---

## 3. Arquitetura de Camadas

```
src/
├── MinhaApi.Api/            → Controllers, Program.cs, Swagger, configuração HTTP
├── MinhaApi.Application/    → Orquestração: Requests/Responses, Profiles (Mapster), Services (wrapper de transação)
├── MinhaApi.CrossCutting/   → Exceptions tipadas, Middlewares, Logging, Enums "universais"
├── MinhaApi.Domain/         → Entidades, Commands, Domain Services (lógica de negócio), interfaces de Repository
├── MinhaApi.Infra/          → DbContext, Mappings (`IEntityTypeConfiguration`), Repositories concretos, UnitOfWork
├── MinhaApi.IoC/            → Composição da injeção de dependência
└── MinhaApi.Jobs/           → Reservado para tarefas agendadas (ainda vazio)
```

### 3.1 Regra de dependências (atual — revisada)

```
CrossCutting  ←  Domain  ←  Application  ←  Infra
                              ↑
                       API  ←  IoC (agrega tudo)
```

> ⚠️ **Diferente do desenho clássico de Clean Architecture:** aqui é o **Domain que referencia o CrossCutting** (não o contrário). Essa troca foi deliberada — o CrossCutting guarda o enum `Situacao` (§6), que é "universal" e precisa ser usado dentro do próprio Domain (na entidade `Produto`). Como um projeto não pode referenciar de volta quem já o referencia (ciclo, erro de build), a solução foi inverter a direção: `CrossCutting` não conhece mais o `Domain`, e o `Domain` passou a conhecer o `CrossCutting`.
>
> **Consequência prática:** o `Domain` hoje pode lançar exceptions do CrossCutting diretamente (`NaoEncontradoException<T>`, por exemplo) — não precisa mais devolver `null` pra Application decidir o que fazer. Ver `ProdutosService.ValidarAsync` (§5.3).

### 3.2 O que NÃO existe (por decisão consciente)

- **Nenhuma classe base de entidade** (tipo `EntidadeBase`). Cada entidade declara `Id`, e os campos que fizerem sentido pra ela, explicitamente. Ver §7 pra decidir o que replicar numa entidade nova.
- **Nenhuma camada `DataTransfer` separada.** Requests/Responses vivem dentro de `Application/Produtos/DataTransfer/`.

---

## 4. Domain — onde a lógica de negócio mora

### 4.1 Entidade (`Domain/Produtos/Entities/Produto.cs`)

```csharp
public class Produto
{
    public virtual int Id { get; protected set; }
    public virtual string Nome { get; protected set; }
    public virtual decimal Preco { get; protected set; }
    public virtual Situacao Situacao { get; protected set; }

    protected Produto() { }  // exigencia do EF Core (materializacao)

    public Produto(string nome, decimal preco)
    {
        SetNome(nome);
        SetPreco(preco);
        Ativar();
    }

    public virtual void SetNome(string nome) { /* valida e atribui */ }
    public virtual void SetPreco(decimal preco) { /* valida e atribui */ }
    public virtual void Ativar() => Situacao = Situacao.Ativo;
    public virtual void Inativar() => Situacao = Situacao.Inativo;
}
```

**Convenções da entidade:**
- Propriedades com `protected set` (o EF Core preenche por reflection). `virtual` é opcional: só seria exigido com `UseLazyLoadingProxies`, que não usamos.
- Construtor sem parâmetros `protected` — só pra o EF Core instanciar a entidade ao ler do banco, nunca é chamado pelo código da aplicação.
- Toda regra de invariante (nome obrigatório, tamanho, preço não-negativo) é validada **dentro dos próprios métodos** (`SetNome`/`SetPreco`), lançando `ArgumentException` puro — a entidade não conhece nada além do próprio domínio.
- Status via **enum** (`Situacao`), não `bool` — ver §6.

> ⚠️ **Pendência conhecida:** esta entidade não tem campo de concorrência otimista (`Version`) nem auditoria (`CriadoEm`/`AtualizadoEm`). Se sua entidade nova precisar disso, adicione os campos manualmente e mapeie no `ProdutoMap`-equivalente (ver §8) — não existe mais nenhuma base que forneça isso de graça.

### 4.2 Command (`Domain/Produtos/Commands/ProdutoCommand.cs`)

```csharp
public class ProdutoCommand
{
    public string Nome { get; protected set; }
    public decimal Preco { get; protected set; }
}
```

Um **único Command** serve tanto pra criação quanto pra edição (diferente de um modelo com `InserirXCommand`/`EditarXCommand` separados) — o `Id`, quando necessário, é passado como parâmetro à parte nos métodos do Domain Service, não dentro do Command.

### 4.3 Domain Service (`Domain/Produtos/Services/ProdutosService.cs`)

**Esta é a camada que concentra a lógica de negócio de verdade** — não a Application. Depende só do `IProdutoRepository` (Domain) e, quando precisa, do `CrossCutting` (pra exceptions).

```csharp
public interface IProdutosService
{
    Task<Produto> ValidarAsync(int id, CancellationToken cancellationToken);
    Task<Produto> InserirAsync(ProdutoCommand command, CancellationToken cancellationToken);
    Task<Produto> EditarAsync(int id, ProdutoCommand command, CancellationToken cancellationToken);
    Task<Produto> AtivarAsync(int id, CancellationToken cancellationToken);
    Task<Produto> InativarAsync(int id, CancellationToken cancellationToken);
}
```

- **`ValidarAsync`** é o método central: busca a entidade no repositório e já lança `NaoEncontradoException<Produto>` se não achar — os outros métodos (`EditarAsync`, `AtivarAsync`, `InativarAsync`) reaproveitam ele por dentro, então "não encontrado" é tratado **uma vez só**.
- Retorno é sempre `Task<Produto>` (não-nullable) — não devolve `null` pra Application decidir; quem não encontra, já lança a exception aqui mesmo.
- "Excluir" não existe como conceito — é **`InativarAsync`** (soft delete via `Situacao`).

### 4.4 Repository (interface) e Filtro de listagem

```csharp
public interface IProdutoRepository : IRepositorioBase<Produto>
{
    IQueryable<Produto> Filtrar(ProdutoListarFilter filtro);
}

public class ProdutoListarFilter
{
    public string Nome { get; set; }
}
```

`IRepositorioBase<TEntidade>` (genérico, em `Domain/Abstractions/`) cobre o CRUD comum (`InserirAsync`, `EditarAsync`, `ExcluirAsync`, `RecuperarAsync` por id ou por predicado, `ListarAsync` paginado/ordenado). Quando uma entidade precisa de filtro de listagem com múltiplos critérios opcionais, ela ganha seu próprio `{Entidade}ListarFilter` + método `Filtrar()` na interface específica — devolvendo um `IQueryable<TEntidade>` que a Application usa em conjunto com a paginação (ver §5.2).

---

## 5. Application — orquestração fina, sem lógica de negócio

### 5.1 Contratos (Requests/Response)

```
Application/Produtos/DataTransfer/
├── Requests/
│   ├── ProdutoRequest.cs          (usado tanto pra criar quanto pra editar)
│   └── ListarProdutosRequest.cs   (herda PaginacaoFiltro + campos de filtro)
└── Responses/
    └── ProdutoResponse.cs
```

- **Um único `ProdutoRequest`** pra Criar/Editar (em vez de dois DTOs separados) — mapeado direto pro `ProdutoCommand` via Mapster.
- `ProdutoResponse` espelha exatamente os campos que a entidade tem hoje (`Id`, `Nome`, `Preco`, `Situacao`) — sem campo de auditoria, porque a entidade também não tem.

### 5.2 Service da Application — só transação, validação de entrada e tradução

```csharp
public class ProdutoService(
    IProdutosService produtosService,
    IProdutoRepository produtoRepository,
    IUnitOfWork unitOfWork) : IProdutoService
{
    public async Task<ProdutoResponse> InserirAsync(ProdutoRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var command = request.Adapt<ProdutoCommand>();
            unitOfWork.BeginTransaction();
            var produto = await produtosService.InserirAsync(command, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
            return produto.Adapt<ProdutoResponse>();
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
    // Editar/Excluir seguem o mesmo padrão: abre transação, chama o Domain Service, comita ou desfaz.
}
```

**Regra da casa:** a abertura/commit/rollback da transação acontece **explicitamente dentro do método da Service**, não escondida atrás de um filtro ou middleware. Isso foi decisão deliberada — cada método de escrita mostra claramente o ciclo de vida da transação.

**Listagem não passa pelo Domain Service** — é consulta pura, sem regra de negócio, então a Application chama o `IProdutoRepository` diretamente:

```csharp
public async Task<PaginacaoConsulta<ProdutoResponse>> ListarAsync(ListarProdutosRequest request, CancellationToken cancellationToken)
{
    var filter = request.Adapt<ProdutoListarFilter>();
    var query = produtoRepository.Filtrar(filter);
    var resultado = await produtoRepository.ListarAsync(query, request.Qt, request.Pg, request.CpOrd, request.TpOrd, cancellationToken);
    return resultado.Adapt<PaginacaoConsulta<ProdutoResponse>>();
}
```

### 5.3 Profile (Mapster)

```csharp
public class ProdutoProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Produto, ProdutoResponse>();
        config.NewConfig<ProdutoRequest, ProdutoCommand>();
    }
}
```

Registrado via `TypeAdapterConfig.GlobalSettings.Scan(assembly)` na IoC — qualquer `IRegister` novo na Application é pego automaticamente.

### 5.4 Validação de entrada

Um único `ProdutoRequestValidator` (`AbstractValidator<ProdutoRequest>`) é registrado na DI e **injetado explicitamente** no `ProdutoService` (`FluentValidation.AspNetCore` foi descontinuado, então não há auto-validação do MVC). `InserirAsync` e `EditarAsync` validam o request antes de abrir a transação; se houver erro, lançam `EntidadeInvalidaException`, que o `ExceptionMiddleware` devolve como **400** com a lista `errors`. A validação da própria entidade (`SetNome`/`SetPreco`) continua como segunda barreira de invariante.
---

## 6. CrossCutting

### 6.1 `Situacao` — enum "universal"

```csharp
public enum Situacao
{
    [Description("Inativo")] Inativo = 0,
    [Description("Ativo")] Ativo = 1
}
```

Vive no CrossCutting especificamente porque é usado em **todas as camadas** (Domain, Application, Infra) — é o único tipo do CrossCutting que o Domain conhece hoje (ver §3.1). Substitui o padrão clássico de `bool Ativo` — qualquer entidade nova que precisar de "ativo/inativo" deve usar este enum, não reinventar um booleano.

### 6.2 Exceptions tipadas

| Exception | Status HTTP | Quando usar |
|---|---|---|
| `NaoEncontradoException<T>` | 404 | Recurso não encontrado por id |
| `EntidadeInvalidaException` | 400 | Validação de entrada falhou (lista de erros) |
| `ConflitoException` | 409 | Concorrência otimista (hoje sem uso ativo — ver §7) |
| `AcessoNegadoException` | 403 | Reservada pra quando IDOR/autenticação existir |

Todas herdam de `AppException` (abstrata, define `StatusCode`). O `ExceptionMiddleware` mapeia `AppException` → status certo + `LogWarning`; qualquer outra exception → 500 + `LogError`, sem vazar detalhe interno em produção.

### 6.3 Middlewares

- **`CorrelationIdMiddleware`** — gera/propaga `X-Correlation-Id`, injeta no contexto de log do Serilog.
- **`ExceptionMiddleware`** — ponto único de tradução exception → HTTP (ProblemDetails RFC 7807).
- **`SecurityHeadersMiddleware`** — headers de segurança básicos (`X-Content-Type-Options`, `X-Frame-Options`, CSP).

---

## 7. Infra

### 7.1 Mapeamento EF Core

```csharp
public class ProdutoConfiguration : IEntityTypeConfiguration<Produto>
{
    public void Configure(EntityTypeBuilder<Produto> builder)
    {
        builder.ToTable("PRODUTO");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("ID").ValueGeneratedOnAdd();
        builder.Property(x => x.Nome).HasColumnName("NOME").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Preco).HasColumnName("PRECO").HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.Situacao).HasColumnName("SITUACAO").HasConversion<int>().IsRequired();
    }
}
```

`Situacao` (enum) é mapeado como `int` via `.HasConversion<int>()`. O schema é do Liquibase: **nada de `dotnet ef migrations`, `Migrate()` ou `EnsureCreated()`**, e o EF Core não valida o schema na subida — divergência de nome de coluna só aparece na primeira query.

> ⚠️ **Sem token de concorrência** — esta entidade não tem controle de concorrência otimista. Se sua entidade nova precisar (recomendado pra qualquer recurso editável concorrentemente), adicione o campo `Version` na entidade, mapeie com `.IsConcurrencyToken()` e incremente-o no `SaveChangesAsync` do `MinhaApiDbContext` (o EF Core não incrementa token `int` sozinho). O `RepositorioBase.EditarAsync` já traduz `DbUpdateConcurrencyException` em `ConflitoException`.

### 7.2 `IUnitOfWork` / `UnitOfWork`

```csharp
public interface IUnitOfWork
{
    void BeginTransaction();                                        // sincrono - mantido assim por simplicidade
    Task CommitAsync(CancellationToken cancellationToken = default);   // async de verdade - I/O com o banco
    Task RollbackAsync(CancellationToken cancellationToken = default); // idem
}
```

Registrado como **Scoped** — usa o mesmo `MinhaApiDbContext` da requisição (também Scoped).

### 7.3 `RepositorioBase<TEntidade>`

Implementação genérica de `IRepositorioBase<T>`. Ponto de destaque: o overload `ListarAsync(IQueryable<TEntidade> query, int qt, int pg, string cpOrd, TipoOrdenacao tpOrd, CancellationToken)` recebe uma query **já filtrada** (via `IProdutoRepository.Filtrar`) e só aplica paginação + ordenação por cima — separando claramente "filtrar" (responsabilidade da entidade específica) de "paginar/ordenar" (responsabilidade genérica).

A ordenação dinâmica usa `System.Linq.Dynamic.Core` (`query.OrderBy("Preco descending")`) — valida o nome do campo via reflection antes de montar a query, evitando SQL Injection por concatenação de string.

---

## 8. API

### 8.1 Controller

```csharp
[ApiController]
[Route("api/v1/[controller]")]
public class ProdutosController(IProdutoService produtoService) : ControllerBase
{
    /// <summary>
    /// Cria um novo produto.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> InserirAsync(ProdutoRequest request, CancellationToken cancellationToken)
    {
        var produto = await produtoService.InserirAsync(request, cancellationToken);
        return CreatedAtAction(nameof(RecuperarAsync), new { id = produto.Id }, produto);
    }
}
```

**Convenções:**
- `/// <summary>` em **toda** action — vira descrição no Swagger (precisa de `GenerateDocumentationFile` habilitado no `.csproj` + `IncludeXmlComments` no `SwaggerConfiguration`).
- **Nenhum `.Adapt<>()` dentro do Controller** — o Controller só repassa o Request pra Application; toda conversão acontece lá dentro.
- Nomes de método terminam em `Async` (`InserirAsync`, `RecuperarAsync`, etc.) — e por isso o `Program.cs` **precisa** desabilitar a convenção padrão do ASP.NET Core de remover o sufixo:

```csharp
// Sem isso, nameof(RecuperarAsync) dentro de CreatedAtAction nao bate com o nome
// real da rota (que por padrao vira so "Recuperar") - "No route matches".
builder.Services.AddControllers(options =>
{
    options.SuppressAsyncSuffixInActionNames = false;
});
```

### 8.2 Swagger com XML comments

`.csproj` da API precisa de:
```xml
<GenerateDocumentationFile>true</GenerateDocumentationFile>
<NoWarn>$(NoWarn);1591</NoWarn>
```
O `NoWarn` evita que o compilador exija comentário em **todo** membro público do projeto (`MySqlOptions`, `CorsConfiguration`, etc.) — só documentamos as actions da controller, que é o que aparece no Swagger.

---

## 9. Testes

- **Domain** (`ProdutoTests.cs`): testa a entidade isoladamente — construtor, `SetNome`/`SetPreco`, `Ativar`/`Inativar`, sem nenhum mock (não precisa, a entidade não tem dependência).
- **Application** (`ProdutoServiceTests.cs`): mocka `IProdutosService`, `IProdutoRepository` e `IUnitOfWork` (via NSubstitute) — testa que a Service chama o método certo do Domain Service, faz commit/rollback corretamente, e repropaga exceptions.
- **Integração**: ainda não implementado (placeholder `UnitTest1.cs`) — fica pra quando houver Docker disponível (Testcontainers).

**Padrão de mock de transação:** como `IUnitOfWork` é injetado e mockado, dá pra verificar que um método de escrita chamou `CommitAsync` (caminho feliz) ou `RollbackAsync` (quando o Domain Service lança exception) — ver `InserirAsyncQuandoDomainServiceLancaExceptionDeveFazerRollbackERepropagar`.

---

## 10. Checklist — como criar uma entidade nova seguindo este template

1. **Domain:**
   - `Entities/{Entidade}.cs` — propriedades `virtual` + `protected set`, construtor vazio `protected`, construtor real chamando os `SetX` de validação.
   - Decida se precisa de `Situacao` (soft delete/status), `Version` (concorrência otimista) e/ou `CriadoEm`/`AtualizadoEm` (auditoria) — **nada disso vem de graça**, adicione manualmente se fizer sentido pro caso de uso.
   - `Commands/{Entidade}Command.cs` — um Command só, reaproveitado pra criar/editar.
   - `Repositories/I{Entidade}Repository.cs` — herda `IRepositorioBase<T>`; adicione `Filtrar()` + `{Entidade}ListarFilter` se a listagem precisar de múltiplos critérios opcionais.
   - `Services/Interfaces/I{Entidade}sService.cs` + `Services/{Entidade}sService.cs` — método `ValidarAsync` central (busca + `NaoEncontradoException`), os demais reaproveitam ele.

2. **Infra:**
   - `Mappings/{Entidade}Configuration.cs` — `IEntityTypeConfiguration<T>` mapeando todas as colunas (enums como int via `.HasConversion<int>()`); o `MinhaApiDbContext` descobre sozinho. Os nomes de coluna precisam bater com o SQL do Liquibase.
   - `Repositories/{Entidade}Repository.cs` — herda `RepositorioBase<T>`, implementa `Filtrar()` se existir.

3. **Application:**
   - `DataTransfer/Requests/{Entidade}Request.cs` (+ `Listar{Entidade}sRequest.cs` herdando `PaginacaoFiltro`).
   - `DataTransfer/Responses/{Entidade}Response.cs`.
   - `Profiles/{Entidade}Profile.cs` — configs de Mapster (`Entidade→Response`, `Request→Command`).
   - `Services/{Entidade}Service.cs` — wrapper fino: valida (quando reconectado, ver §5.4), abre transação, chama o Domain Service, comita/desfaz, mapeia resposta.

4. **API:**
   - `Controllers/V1/{Entidades}Controller.cs` — sem `.Adapt()`, com `/// <summary>` em cada action.

5. **IoC:** registrar `I{Entidade}sService`/`{Entidade}sService` e `I{Entidade}Repository`/`{Entidade}Repository`.

6. **Banco:** novo script Liquibase criando a tabela (ver repositório `minha-api-database`).

7. **Testes:** entidade isolada (Domain) + Service com mocks (Application), seguindo os mesmos exemplos de `Produto`.

---

## 11. Débitos técnicos conhecidos (registrados de propósito, não esquecidos)

| Item | Situação |
|---|---|
| Validação de entrada (FluentValidation) | ✅ Conectada na Service (§5.4) |
| Concorrência otimista (`Version`) | Removida da entidade `Produto` — reintroduzir por entidade, se necessário |
| Auditoria (`CriadoEm`/`AtualizadoEm`) | Removida da entidade `Produto` — reintroduzir por entidade, se necessário |
| Autenticação/Autorização | Não implementada ainda |
| IDOR (posse de recurso) | `AcessoNegadoException` existe, mas nada a usa ainda |
| Testes de integração | Placeholder apenas, sem Docker disponível no ambiente atual |
