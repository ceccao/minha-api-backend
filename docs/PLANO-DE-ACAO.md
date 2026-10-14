# Plano de Ação — API Base Template (do zero absoluto)

> **Ponto de partida:** notebook novo, apenas **VS Code** instalado.
> **Ponto de chegada:** solution C# em camadas + MySQL em Docker + Liquibase + frontend Angular + repositório GitHub com CI, tudo rodando localmente e pronto para virar um `dotnet new` template.
>
> **Convenção usada neste documento:** o projeto de exemplo se chama `MinhaApi`. Troque pelo nome real quando for executar.
>
> **Como usar:** execute fase por fase, na ordem. Cada fase tem um **critério de conclusão** — não avance sem cumprir. Fases marcadas com 🔴 exigem **decisão sua** antes de prosseguir.

> 🔁 **Revisão (out/2026) — ORM trocado de NHibernate para Entity Framework Core.** Motivo: EF Core é o ORM de uso muito mais difundido no mercado .NET (mais material, mais vagas, mais exemplos, suporte de primeira classe da Microsoft). O que mudou na prática está concentrado nas **FASES 9 e 10** (Infra/IoC), com ajustes pontuais nas FASES 4, 5, 7, 12 e no README. **O Liquibase continua sendo a única fonte de verdade do schema** — o EF Core é usado só como mapeador/consulta, **sem EF Migrations** (ver FASE 9.0). Quem já executou a FASE 9 com NHibernate: siga o checklist da **FASE 9.6**.

---

## Índice

| Bloco | Fases | Tempo estimado |
|---|---|---|
| **A — Preparação da máquina** | 0 | 2–3h |
| **B — Fundação do projeto** | 1–4 | 3–5h |
| **C — Backend (camadas)** | 5–12 | 12–18h |
| **D — Qualidade e automação** | 13–14 | 3–5h |
| **E — Frontend Angular** | 15–17 | 8–12h |
| **F — Produção e empacotamento** | 18–20 | 5–8h |
| **G — Deploy multi-ambiente (Azure)** | 21 | 4–6h (só quando ativado — ver §21.0) |
| **Total** | | **~33–51h** (4 a 8 dias de trabalho focado), **+4–6h quando o deploy for ativado** |

---

# BLOCO A — Preparação da Máquina

## FASE 0 — Instalação de todo o ferramental

### 0.1 Escolha do terminal (Windows)

Se você está no Windows, instale o **Windows Terminal** e considere o **WSL2** (Ubuntu). O WSL2 facilita scripts `.sh`, é onde o Docker roda melhor no Windows e aproxima o ambiente local do ambiente de produção (Linux).

```powershell
winget install --id Microsoft.WindowsTerminal -e
wsl --install -d Ubuntu
```

> **Decisão:** você pode trabalhar 100% no Windows nativo (usando `dev.ps1` em vez de `dev.sh`). O plano funciona nos dois casos — onde houver diferença, está sinalizado.

---

### 0.2 Softwares obrigatórios

Execute na ordem. No Windows use `winget`; no macOS, `brew install`; no Ubuntu/WSL, `apt`.

| # | Software | Para quê | Comando (Windows / winget) |
|---|---|---|---|
| 1 | **Git** | Controle de versão | `winget install --id Git.Git -e` |
| 2 | **.NET SDK (LTS)** | Backend C# | `winget install --id Microsoft.DotNet.SDK.10 -e` |
| 3 | **Node.js (LTS)** | Runtime do Angular | `winget install --id OpenJS.NodeJS.LTS -e` |
| 4 | **Docker Desktop** | MySQL em container | `winget install --id Docker.DockerDesktop -e` |
| 5 | **Java (JRE/JDK 17+)** | **Obrigatório para o Liquibase** | `winget install --id EclipseAdoptium.Temurin.21.JDK -e` |
| 6 | **Liquibase CLI** | Versionamento de schema | `winget install --id Liquibase.Liquibase -e` |
| 7 | **DBeaver** *(ou MySQL Workbench)* | Cliente visual do banco | `winget install --id DBeaver.DBeaver.Community -e` |
| 8 | **GitHub CLI** *(opcional, recomendado)* | Criar repo pelo terminal | `winget install --id GitHub.cli -e` |

> ⚠️ **Ponto que costuma passar batido:** o **Liquibase depende de Java**. Se pular o item 5, o item 6 instala mas não roda.

> 📌 **Sobre versões:** instale sempre a versão **LTS vigente** do .NET e do Node. Confirme qual é a LTS atual em `dotnet.microsoft.com/download` e `nodejs.org` antes de instalar — os IDs do winget acima podem ter mudado de número.

> ⚠️ **Nota de compatibilidade de hardware:** em algumas máquinas (constatado em notebooks Dell/Alienware com bug de firmware conhecido), a virtualização pode aparecer como "habilitada" no BIOS e mesmo assim o processador reportar `VirtualizationFirmwareEnabled: False` para o Windows — impedindo o WSL2 e o Docker Desktop de funcionar. Se isso acontecer mesmo após habilitar corretamente o VT-x/AMD-V no BIOS (incluindo, em notebooks Dell, revelar as opções escondidas em "Service options" via `Ctrl+Alt+S` na tela Overview do BIOS), a alternativa é **instalar o MySQL nativamente no Windows** (ver nota na FASE 3) e deixar o Docker para quando o firmware for corrigido ou for feita hospedagem em nuvem (o Docker ali roda no servidor, não na sua máquina — ver BLOCO G).

**Reinicie o terminal** (ou a máquina) após instalar tudo, para que o `PATH` seja atualizado.

---

### 0.3 Verificação — rode tudo e confira as saídas

```bash
git --version            # git version 2.x
dotnet --version         # 10.x (ou LTS vigente)
dotnet --list-sdks       # deve listar ao menos um SDK
node --version           # v22.x / v24.x (LTS)
npm --version            # 10.x+
docker --version         # Docker version 27.x+ (se aplicável na sua máquina)
docker compose version   # v2.x (se aplicável na sua máquina)
java -version            # openjdk 21.x
liquibase --version      # Liquibase 4.x+
```

**Critério de conclusão:** todos os comandos respondem sem erro.

---

### 0.4 Angular CLI

```bash
npm install -g @angular/cli
ng version
```

---

### 0.5 Extensões do VS Code

Instale via linha de comando (mais rápido que pela interface):

```bash
# Backend C#
code --install-extension ms-dotnettools.csdevkit
code --install-extension ms-dotnettools.csharp

# Frontend Angular
code --install-extension Angular.ng-template
code --install-extension dbaeumer.vscode-eslint
code --install-extension esbenp.prettier-vscode

# Docker e banco
code --install-extension ms-azuretools.vscode-docker
code --install-extension cweijan.vscode-mysql-client2

# Produtividade geral
code --install-extension EditorConfig.EditorConfig
code --install-extension humao.rest-client
code --install-extension eamodio.gitlens
code --install-extension redhat.vscode-xml

# Se usar WSL
code --install-extension ms-vscode-remote.remote-wsl
```

---

### 0.6 Configuração do Git e do GitHub

```bash
# Identidade — user.email precisa ser um e-mail verificado na sua conta do GitHub
# (ou o e-mail "noreply" que o GitHub oferece), senão os commits não contam como seus.
# user.name é só um rótulo, pode ser qualquer coisa.
git config --global user.name "Seu Nome"
git config --global user.email "seu@email.com"

# Branch padrão e comportamento de pull
git config --global init.defaultBranch main
git config --global pull.rebase false

# Fim de linha (evita bagunça entre Windows e Linux)
git config --global core.autocrlf true      # Windows
# git config --global core.autocrlf input   # macOS / Linux / WSL
```

**Chave SSH para o GitHub:**

```bash
ssh-keygen -t ed25519 -C "seu@email.com"
# Enter em tudo (ou defina uma passphrase)

# Windows (PowerShell):
Get-Content ~/.ssh/id_ed25519.pub | clip
# Linux/macOS/WSL:
cat ~/.ssh/id_ed25519.pub
```

Cole em **GitHub → Settings → SSH and GPG keys → New SSH key**.

Teste:
```bash
ssh -T git@github.com
# Esperado: "Hi <usuario>! You've successfully authenticated..."
```

---

### 0.7 🔴 Configurar Docker Desktop para iniciar com o sistema

*(Este é o passo a passo que ficou pendente na conversa anterior.)*

**Windows / macOS:**
1. Abra o **Docker Desktop**
2. Clique na engrenagem ⚙️ (**Settings**)
3. Aba **General**
4. Marque **"Start Docker Desktop when you sign in to your computer"**
5. *(Windows)* Confirme que **"Use the WSL 2 based engine"** está marcado
6. **Apply & Restart**

**Linux (sem Docker Desktop, usando Docker Engine):**
```bash
sudo systemctl enable docker
sudo systemctl start docker
sudo usermod -aG docker $USER   # rodar docker sem sudo — exige relogar
```

**Como isso se combina com o `restart: always`:**

| Componente | Papel |
|---|---|
| Docker Desktop iniciando com o SO | Garante que o **daemon** está rodando |
| `restart: always` no `docker-compose.yml` | Garante que o **container MySQL** sobe junto com o daemon |
| `dev.sh` / `dev.ps1` | Garantia extra: se por algum motivo o container estiver parado, sobe antes da API |

Com os três combinados, você nunca mais precisa pensar em "subir o banco".

> ⚠️ Se sua máquina não conseguir rodar Docker/WSL2 por limitação de virtualização (ver nota na FASE 0.2), pule esta etapa e siga a alternativa de MySQL nativo descrita na FASE 3.

### 0.8 🔁 Duas pegadinhas descobertas na execução (adicione ao checklist)

**Política de execução do PowerShell** — por padrão, o Windows não deixa rodar scripts `.ps1` (nem os que você mesmo escreveu):
```powershell
Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy RemoteSigned
```
E se o script foi **baixado do navegador** (ex: um `.ps1` que uma IA gerou e você baixou), o Windows marca o arquivo com "Mark of the Web" e continua bloqueando mesmo com `RemoteSigned` — desbloqueie o arquivo especificamente:
```powershell
Unblock-File -Path .\dev.ps1
```

**Certificado de desenvolvimento HTTPS não confiável** — se a API subir mas o navegador/cliente HTTP recusar a conexão HTTPS local (`AuthenticationException` / certificado não confiável nos logs):
```powershell
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```
O segundo comando abre um pop-up do Windows pedindo confirmação — clique "Sim".

**Critério de conclusão da FASE 0:** todos os comandos de verificação passam, o SSH do GitHub autentica, o certificado de desenvolvimento HTTPS está confiável (`dotnet dev-certs https --trust` executado com sucesso), a política de execução do PowerShell permite rodar scripts locais, e (se aplicável na sua máquina) o Docker Desktop está configurado para iniciar com o sistema.

---

# BLOCO B — Fundação do Projeto

## FASE 1 — Repositórios GitHub

### 1.0 Decisão adotada: poly-repo, não monorepo 🔁 (revisado durante a execução)

O template nasceu desenhado como monorepo (um único `minha-api-template` com `src/`, `frontend/`, `database/`), mas foi revisado pra **três repositórios separados** assim que a fase de deploy multi-ambiente (Azure) ficou clara: backend, banco e frontend já são recursos Azure independentes (App Service, Database for MySQL Flexible Server, Static Web App) — faz sentido que também sejam repositórios independentes, com CI/CD e branches próprios.

- **`minha-api-backend`** — o repositório **principal**: guarda `src/`, `tests/`, e a pasta `docs/` com os documentos "guarda-chuva" que falam do projeto inteiro (`README.md`, `PLANO-DE-ACAO.md`, runbooks). É aqui que qualquer pessoa (ou IA) chega primeiro pra entender o projeto como um todo.
- **`minha-api-database`** — só o Liquibase: changelogs, properties por ambiente, e o `docker-compose.yml` do MySQL (faz mais sentido semântico morar aqui que no backend).
- **`minha-api-frontend`** — o workspace Angular. Fica vazio até a FASE 15.

> ⚠️ **Custo de decidir isso mais cedo vs. mais tarde:** dividir um monorepo em vários depois que já tem histórico de commits relevante é bem mais trabalhoso (perde-se `git log`/`git blame` a menos que use `git filter-repo`). Quanto antes essa decisão for tomada, mais barato é executá-la — se você está começando um projeto novo do zero, decida poly-repo vs. monorepo **antes** da FASE 1, não durante.

### 1.1 `minha-api-backend` (repositório principal)

```bash
gh auth login
mkdir minha-api-backend && cd minha-api-backend
git init
gh repo create minha-api-backend --private --source=. --remote=origin
```

Estrutura final pretendida:
```
minha-api-backend/
├── .github/workflows/          # CI (e CD só se este repo vier a ter deploy próprio)
├── src/                         # Projetos C#
├── tests/                       # Projetos de teste
├── docs/
│   ├── README.md
│   ├── PLANO-DE-ACAO.md
│   ├── ATIVAR-DEPLOY-AZURE.md
│   ├── NOVO-CLIENTE-CHECKLIST.md
│   └── rename-project.ps1
├── .editorconfig
├── .gitignore
├── dev.ps1
└── Directory.Build.props
```

**`.gitignore`:**
```bash
dotnet new gitignore
```
Acrescente ao final:
```gitignore
# Ambiente e segredos
.env
.env.local
**/appsettings.Local.json

# IDE
.vs/
.idea/
*.user
```
*(as linhas de `frontend/` e `liquibase.properties` saíram — não existem mais neste repositório, moraram pros outros dois)*

**Branches** (mesmo fluxo dos três repositórios, ver 1.4):
```bash
git checkout -b main
git push -u origin main
git checkout -b dev
git push -u origin dev
git checkout -b hml
git push -u origin hml
git checkout dev
```

**Primeiro commit:**
```bash
git add .
git commit -m "chore: estrutura inicial do repositorio de backend"
git push -u origin main
```

### 1.2 `minha-api-database`

```bash
cd ..
mkdir minha-api-database && cd minha-api-database
git init
gh repo create minha-api-database --private --source=. --remote=origin
```

Estrutura final pretendida (sem pasta `database/` como wrapper — o repositório inteiro já é sobre o banco):
```
minha-api-database/
├── .github/workflows/
├── changelogs/
│   ├── minha-api.changelog-master.xml
│   ├── minha-api.changelog-0.1.0.xml
│   └── v0.1.0/
├── liquibase.dev.properties.example
├── liquibase.hml.properties.example
├── liquibase.prod.properties.example
├── docker-compose.yml           # adormecido se sua maquina nao suportar Docker - ver FASE 0.2/3.3
└── .gitignore
```

**`.gitignore`:**
```gitignore
liquibase.*.properties
!liquibase.*.properties.example
*.log
```

**Branches e primeiro commit:** mesmo padrão do 1.1 (`main`/`dev`/`hml`, commit inicial com o `.gitignore`).

### 1.3 `minha-api-frontend`

```bash
cd ..
mkdir minha-api-frontend && cd minha-api-frontend
git init
gh repo create minha-api-frontend --private --source=. --remote=origin
```

Fica **vazio de propósito** até a FASE 15 rodar `ng new` (que gera seu próprio `.gitignore` completo). Por enquanto, só um `.gitignore` provisório pra permitir o primeiro commit:
```gitignore
node_modules/
dist/
.angular/
```

**Branches e primeiro commit:** mesmo padrão do 1.1.

### 1.4 Estratégia de branches (igual nos 3 repositórios)

```
feature/* e fix/*  →  PR  →  dev   (dispara deploy automático em DEV, quando a FASE 21 estiver ativa)
dev                →  PR  →  hml   (promoção manual, dispara deploy em HML)
hml                →  PR  →  main  (promoção manual, exige aprovação, dispara deploy em PROD)
```

| Branch | Ambiente (`ASPNETCORE_ENVIRONMENT`) |
|---|---|
| `dev` | `Development` |
| `hml` | `Staging` |
| `main` | `Production` |

Configure depois (após a FASE 14, **em cada um dos três repositórios**): **Settings → Branches → Add rule** em `main` e `hml`, exigindo PR e status check do CI verde. Na FASE 21, `main` também recebe a proteção de **Required reviewers** do GitHub Environment `production`.

**Critério de conclusão:** os três repositórios (`minha-api-backend`, `minha-api-database`, `minha-api-frontend`) existem no GitHub, cada um com as branches `main`/`dev`/`hml` e `.gitignore` correspondente, e o `minha-api-backend` já com a pasta `docs/` contendo os documentos guarda-chuva.

---

## FASE 2 — Solution e Projetos C#

### 2.1 Criar a solution e os projetos

```bash
dotnet new sln -n MinhaApi

# Projetos de produção
dotnet new webapi -controllers -n MinhaApi.Api          -o src/MinhaApi.Api
dotnet new classlib -n MinhaApi.Application  -o src/MinhaApi.Application
dotnet new classlib -n MinhaApi.CrossCutting -o src/MinhaApi.CrossCutting
dotnet new classlib -n MinhaApi.DataTransfer -o src/MinhaApi.DataTransfer
dotnet new classlib -n MinhaApi.Domain       -o src/MinhaApi.Domain
dotnet new classlib -n MinhaApi.Infra        -o src/MinhaApi.Infra
dotnet new classlib -n MinhaApi.IoC          -o src/MinhaApi.IoC
dotnet new classlib -n MinhaApi.Jobs         -o src/MinhaApi.Jobs

# Testes
dotnet new xunit -n MinhaApi.Tests.Unit        -o tests/MinhaApi.Tests.Unit
dotnet new xunit -n MinhaApi.Tests.Integration -o tests/MinhaApi.Tests.Integration
```

> ⚠️ **`-controllers` é obrigatório**: por padrão, `dotnet new webapi` gera um projeto em Minimal API. Este template usa Controllers (ver FASE 11), então a flag é necessária. Depois de criar, apague o código de exemplo (`WeatherForecast.cs` e `Controllers/WeatherForecastController.cs`) — ele só existe pra o template compilar de fábrica.

> **Consumers e Workers:** não crie os `.csproj` agora. Crie só um marcador de intenção:
> ```bash
> mkdir -p src/MinhaApi.Consumers src/MinhaApi.Workers
> echo "# Reservado para consumidores de mensageria (MassTransit). Ver README §4." > src/MinhaApi.Consumers/README.md
> echo "# Reservado para background workers de longa duração. Ver README §4." > src/MinhaApi.Workers/README.md
> ```

### 2.2 Adicionar tudo à solution

> 🔁 **Aviso do que vem pela frente:** o projeto `MinhaApi.DataTransfer` criado acima acaba sendo **fundido dentro de `MinhaApi.Application`** mais tarde na execução real (ver nota no início do BLOCO C e README §2.0). Se estiver começando um projeto do zero e já sabe disso, pode pular a criação do projeto `DataTransfer` separado e ir direto pra estrutura `Application/DataTransfer/` — economiza o retrabalho de fundir depois.

```bash
dotnet sln add $(find src tests -name "*.csproj")
# Windows PowerShell:
# Get-ChildItem -Recurse -Filter *.csproj src,tests | ForEach-Object { dotnet sln add $_.FullName }
```

### 2.3 Referências entre projetos (⚠️ a parte mais importante)

Estas referências **materializam a regra de dependência** do README §2.1. Errar aqui compromete toda a arquitetura.

```bash
# DataTransfer → (nada)
# Domain → (nada)  ← núcleo, não referencia ninguém

# CrossCutting → Domain
dotnet add src/MinhaApi.CrossCutting reference src/MinhaApi.Domain

# Application → Domain, DataTransfer, CrossCutting
dotnet add src/MinhaApi.Application reference src/MinhaApi.Domain src/MinhaApi.DataTransfer src/MinhaApi.CrossCutting

# Infra → Domain, Application, CrossCutting
dotnet add src/MinhaApi.Infra reference src/MinhaApi.Domain src/MinhaApi.Application src/MinhaApi.CrossCutting

# Jobs → Application
dotnet add src/MinhaApi.Jobs reference src/MinhaApi.Application

# IoC → todos (é o compositor)
dotnet add src/MinhaApi.IoC reference src/MinhaApi.Application src/MinhaApi.Infra src/MinhaApi.CrossCutting src/MinhaApi.Domain

# API → IoC, Application, DataTransfer, CrossCutting  (NUNCA Infra diretamente)
dotnet add src/MinhaApi.Api reference src/MinhaApi.IoC src/MinhaApi.Application src/MinhaApi.DataTransfer src/MinhaApi.CrossCutting

# Testes
dotnet add tests/MinhaApi.Tests.Unit reference src/MinhaApi.Domain src/MinhaApi.Application
dotnet add tests/MinhaApi.Tests.Integration reference src/MinhaApi.Api src/MinhaApi.Infra
```

### 2.4 `Directory.Build.props` (configuração comum a todos os projetos)

Crie na **raiz** do repositório:

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
  </PropertyGroup>
</Project>
```

Isso centraliza a versão do framework e ativa `Nullable` + analisadores em **todos** os projetos de uma vez.

> 🔁 **Aviso do que vem pela frente:** `TreatWarningsAsErrors=true` + `AnalysisLevel=latest-recommended` é uma combinação rigorosa — qualquer aviso do analisador vira erro de build. Isso **vai** acontecer várias vezes nas próximas fases (é esperado, não indica erro de arquitetura). Os mais comuns que já apareceram executando este mesmo plano:
> - **CA1305** (`IFormatProvider` ausente) — em `int.Parse(...)` e `.WriteTo.Console(...)` do Serilog. Corrige passando `CultureInfo.InvariantCulture` explicitamente.
> - **CA1848** (usar `LoggerMessage` em vez de `logger.LogWarning/LogError` direto) — exige método `partial` com atributo `[LoggerMessage]` na classe. Fica mais verboso, mas é mais performático — decisão consciente de manter ativo (ver FASE 7).
> - **CA1051** (campo de instância visível) — troque campo público/protegido por propriedade (`{ get; }`), inclusive quando inicializada a partir de um parâmetro de primary constructor.
> - **CA1000** (membro estático em tipo genérico) — separe os factory methods estáticos numa classe auxiliar não-genérica (ver `Result<T>`/`Result` na FASE 8).
>
> Se preferir não lidar com isso agora, dá pra baixar o rigor trocando `AnalysisLevel` pra `latest-minimum` ou desativando regras específicas no `.editorconfig` (isso é decisão explícita da FASE 13) — mas rodando exatamente como está aqui, essas quatro regras vão aparecer.

### 2.5 Validação

```bash
dotnet build
```

**Critério de conclusão:** `Build succeeded` e nenhuma referência circular.

---

## FASE 3 — Banco de dados em Docker (ou nativo, se a máquina não suportar virtualização)

> 📁 **Local:** com a divisão em poly-repo (FASE 1.0), tudo desta fase vive no repositório **`minha-api-database`**, não no backend.

### 3.1 `docker-compose.yml` (raiz do `minha-api-database`)

```yaml
services:
  mysql:
    image: mysql:8.4
    container_name: minhaapi-mysql
    restart: always          # 👈 sobe junto com o Docker Desktop
    environment:
      MYSQL_ROOT_PASSWORD: ${MYSQL_ROOT_PASSWORD:-root123}
      MYSQL_DATABASE: ${MYSQL_DATABASE:-minha_api_dev}
    ports:
      - "3306:3306"
    healthcheck:
      test: ["CMD", "mysqladmin", "ping", "-h", "localhost", "-uroot", "-p${MYSQL_ROOT_PASSWORD:-root123}"]
      interval: 5s
      timeout: 5s
      retries: 10
      start_period: 30s
    volumes:
      - mysql_data:/var/lib/mysql
    command: >
      --character-set-server=utf8mb4
      --collation-server=utf8mb4_unicode_ci
      --default-time-zone=+00:00

volumes:
  mysql_data:
```

> **`--default-time-zone=+00:00`** força o banco a trabalhar em **UTC**, reforçando a decisão do README §7.4.

### 3.2 Subir e validar

```bash
docker compose up -d
docker compose ps           # deve mostrar "healthy"
docker compose logs mysql
```

Conecte pelo DBeaver: `localhost:3306`, usuário `root`, senha `root123`.

### 3.3 🔀 Alternativa: MySQL nativo (se a máquina não suportar Docker/WSL2)

Algumas máquinas (bug de firmware em virtualização, políticas corporativas, etc.) não conseguem rodar Docker Desktop. Nesse caso, instale o MySQL Server diretamente no sistema operacional:

1. Baixe o instalador em `dev.mysql.com/downloads/installer/` (Server only)
2. Configure usuário `root` / senha `root123` (mesmos valores usados no `docker-compose.yml`, pra manter a connection string igual)
3. Marque para iniciar com o sistema (equivalente ao `restart: always` do Docker)
4. Adicione `default-time-zone='+00:00'` na seção `[mysqld]` do arquivo de configuração (`my.ini`), e reinicie o serviço
5. Crie o banco: `CREATE DATABASE minha_api_dev CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;`
6. Se conectar via DBeaver/Liquibase e receber o erro **"Public Key Retrieval is not allowed"** (comum com MySQL 8+): adicione `allowPublicKeyRetrieval=true&useSSL=false` na connection string/URL JDBC

O `docker-compose.yml` continua no repositório mesmo nesse cenário — ele só fica adormecido até a máquina conseguir rodar Docker, ou até o MySQL passar a rodar em nuvem (BLOCO G).

### 3.4 Script `dev.sh` (Linux/macOS/WSL, cenário Docker)

```bash
#!/bin/bash
set -e

echo "🐳 Garantindo que o MySQL está no ar..."
docker compose up -d mysql

echo "⏳ Aguardando healthcheck..."
until [ "$(docker inspect -f '{{.State.Health.Status}}' minhaapi-mysql 2>/dev/null)" = "healthy" ]; do
    printf '.'
    sleep 1
done

echo ""
echo "✅ MySQL pronto."
echo "🚀 Iniciando a API..."
dotnet run --project src/MinhaApi.Api
```

```bash
chmod +x dev.sh
```

### 3.5 Script `dev.ps1` (Windows nativo, cenário Docker)

```powershell
$ErrorActionPreference = "Stop"

Write-Host "🐳 Garantindo que o MySQL está no ar..." -ForegroundColor Cyan
docker compose up -d mysql

Write-Host "⏳ Aguardando healthcheck..." -ForegroundColor Yellow
do {
    Start-Sleep -Seconds 1
    $status = docker inspect -f '{{.State.Health.Status}}' minhaapi-mysql 2>$null
} while ($status -ne "healthy")

Write-Host "✅ MySQL pronto." -ForegroundColor Green
Write-Host "🚀 Iniciando a API..." -ForegroundColor Cyan
dotnet run --project src/MinhaApi.Api
```

### 3.6 Script `dev.ps1` (Windows nativo, cenário MySQL nativo — sem Docker)

```powershell
$ErrorActionPreference = "Stop"

# MySQL roda nativo no Windows (nome do servico pode variar: MySQL80, MySQL84...)
# O Windows ja garante que ele sobe sozinho no boot (Startup Type: Automatic).
Write-Host "Verificando o servico do MySQL..." -ForegroundColor Cyan
$mysqlService = Get-Service -Name "MySQL80" -ErrorAction SilentlyContinue
if ($null -eq $mysqlService) {
    Write-Host "Servico MySQL80 nao encontrado. Verifique o nome do servico com 'Get-Service *mysql*'." -ForegroundColor Red
    exit 1
}
if ($mysqlService.Status -ne "Running") {
    Write-Host "Iniciando o servico MySQL..." -ForegroundColor Yellow
    Start-Service -Name "MySQL80"
}

Write-Host "MySQL pronto." -ForegroundColor Green
Write-Host "Iniciando a API..." -ForegroundColor Cyan
dotnet run --project src/MinhaApi.Api
```

**Critério de conclusão:** MySQL acessível em `localhost:3306` (via Docker saudável ou serviço nativo rodando), conexão pelo DBeaver funcionando, banco `minha_api_dev` criado com timezone UTC confirmado (`SELECT @@global.time_zone` retorna `+00:00`), e o banco sobe sozinho após reiniciar a máquina.

---

## FASE 4 — Liquibase (versionamento de schema)

> 📁 **Local:** repositório **`minha-api-database`** (ver FASE 1.0/1.2).

### 4.0 Convenção adotada 🔁 (revisado pela segunda vez — SQL puro com rollback manual)

O plano passou por **duas** revisões nessa convenção. Primeiro adotamos changesets nativos em XML (pra manter rollback automático); depois, revendo de novo o modelo que Ceccon usa no trabalho, decidimos conscientemente **voltar pra SQL puro**, aceitando o trade-off:

| | XML nativo | SQL puro (decisão final) |
|---|---|---|
| Legibilidade | Precisa saber a sintaxe do Liquibase | Qualquer DBA lê direto |
| Rollback | Gerado automaticamente (`createTable`/`addColumn`) | **Escrito à mão, sempre** — mais chance de erro/esquecimento |
| Abstração de banco | Independente de dialeto | Preso ao dialeto do MySQL (`COMMENT`, `AUTO_INCREMENT`, etc.) — aceitável, já que o stack é 100% MySQL |

- **Changelog por versão de release** (`0.1.0`, `0.2.0`...) — mantido
- **Scripts organizados por versão + ticket**: `scripts/{versão}/{TICKET}/script-XX.sql` + `script-XX-rollback.sql` — o `{TICKET}` (ex: `API-0001`) é um identificador de rastreabilidade (troque pelo código real do seu sistema de tickets quando tiver um; aqui usamos como placeholder de convenção)
- **Um `liquibase.{ambiente}.properties` por ambiente** — mantido, inalterado
- O changelog XML **continua existindo**, mas só como "índice" que aponta pro `.sql` via `<sqlFile>` — o Liquibase precisa de algum jeito de rastrear o que já foi aplicado (tabela `DATABASECHANGELOG`), então o XML nunca desaparece de vez, só para de conter a DDL em si.

> ⚠️ **Regra da casa, agora ainda mais importante:** como o rollback não é mais gerado sozinho, **todo `script-XX.sql` precisa vir acompanhado do `script-XX-rollback.sql` correspondente**, escrito à mão, testado manualmente (`rollback-count 1` e confirma que desfez direito) antes de dar commit.

> 🔁 **Com a troca para EF Core (out/2026):** o Liquibase continua **sozinho** responsável pelo schema. **Não usar `dotnet ef migrations`, `Database.Migrate()` nem `EnsureCreated()`** — dois mecanismos de versionamento escrevendo no mesmo banco é receita de divergência. O EF Core só lê o schema que o Liquibase criou (ver FASE 9.0).

### 4.1 Estrutura

```
minha-api-database/
├── changelogs/
│   ├── minha-api.changelog-master.xml        # inclui o changelog de cada versao
│   └── minha-api.changelog-0.1.0.xml         # aponta pros scripts via <sqlFile>
├── scripts/
│   └── 0.1.0/
│       └── API-0001/
│           ├── script-01.sql                 # DDL puro (MySQL)
│           └── script-01-rollback.sql        # rollback escrito a mao
├── liquibase.dev.properties                  # NÃO versionar (senha real)
├── liquibase.dev.properties.example          # versionar
├── liquibase.hml.properties.example          # versionar — adormecido até a FASE 21
└── liquibase.prod.properties.example         # versionar — adormecido até a FASE 21
```

### 4.2 `liquibase.dev.properties.example`

```properties
changeLogFile=changelogs/minha-api.changelog-master.xml
url=jdbc:mysql://localhost:3306/minha_api_dev?useUnicode=true&characterEncoding=UTF-8&serverTimezone=UTC&allowPublicKeyRetrieval=true&useSSL=false
username=root
password=COLOQUE_A_SENHA_AQUI
driver=com.mysql.cj.jdbc.Driver
```

> Os parâmetros `allowPublicKeyRetrieval=true&useSSL=false` evitam o erro "Public Key Retrieval is not allowed" comum com o driver de autenticação padrão do MySQL 8+.

Copie para `liquibase.dev.properties` (sem `.example`) e preencha a senha real — esse arquivo fica no `.gitignore`.

### 4.3 `liquibase.hml.properties.example` e `liquibase.prod.properties.example` 🔴 (adormecidos)

Mesmo formato do `dev`, com host/usuário/senha em placeholder. Só viram relevantes na FASE 21.

### 4.4 `changelogs/minha-api.changelog-master.xml`

```xml
<?xml version="1.0" encoding="UTF-8"?>
<databaseChangeLog
    xmlns="http://www.liquibase.org/xml/ns/dbchangelog"
    xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
    xsi:schemaLocation="http://www.liquibase.org/xml/ns/dbchangelog
        http://www.liquibase.org/xml/ns/dbchangelog/dbchangelog-latest.xsd">

    <include file="minha-api.changelog-0.1.0.xml" relativeToChangelogFile="true"/>

</databaseChangeLog>
```

### 4.5 `changelogs/minha-api.changelog-0.1.0.xml` — agora aponta pro SQL via `<sqlFile>`

```xml
<?xml version="1.0" encoding="UTF-8"?>
<databaseChangeLog
    xmlns="http://www.liquibase.org/xml/ns/dbchangelog"
    xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
    xsi:schemaLocation="http://www.liquibase.org/xml/ns/dbchangelog
        http://www.liquibase.org/xml/ns/dbchangelog/dbchangelog-latest.xsd">

    <changeSet id="API-0001-script-01" author="ceccon">
        <sqlFile path="../scripts/0.1.0/API-0001/script-01.sql" relativeToChangelogFile="true"/>
        <rollback>
            <sqlFile path="../scripts/0.1.0/API-0001/script-01-rollback.sql" relativeToChangelogFile="true"/>
        </rollback>
    </changeSet>

</databaseChangeLog>
```

### 4.6 Primeiro script — `scripts/0.1.0/API-0001/script-01.sql`

```sql
-- API-0001: Criação da tabela produto
CREATE TABLE produto (
    id INT NOT NULL AUTO_INCREMENT COMMENT 'Chave primária da tabela',
    nome VARCHAR(100) NOT NULL COMMENT 'Nome do produto',
    preco DECIMAL(18,2) NOT NULL COMMENT 'Preço do produto',
    ativo BOOLEAN NOT NULL DEFAULT TRUE COMMENT 'Status do produto (soft delete: 1 ativo, 0 inativo)',
    criado_em DATETIME(6) NOT NULL COMMENT 'Data/hora de criação, sempre em UTC',
    atualizado_em DATETIME(6) NULL COMMENT 'Data/hora da última atualização, sempre em UTC',
    version INT NOT NULL DEFAULT 0 COMMENT 'Controle de concorrência otimista (incrementado pela aplicação a cada update)',
    PRIMARY KEY (id)
) COMMENT = 'Armazena os produtos cadastrados';
```

E o rollback correspondente, `scripts/0.1.0/API-0001/script-01-rollback.sql`:

```sql
-- Rollback do API-0001/script-01: remove a tabela produto
DROP TABLE produto;
```

> ⚠️ **`id` como `INT`, não `BIGINT`** — decisão revisada durante a execução (junto com a reformulação do `IRepositorioBase<T>`, ver FASE 9). Colunas `ativo` (soft delete), `criado_em`/`atualizado_em` (UTC) e `version` (concorrência otimista) continuam desde o primeiro changeset — decisões do README §5.5, §7.3 e §7.4.

> ⚠️ **Se o `script-01.sql` já foi aplicado em algum banco com o comentário antigo** (`'... (NHibernate)'`): o Liquibase calcula checksum do arquivo, então **editar o texto do comentário depois de aplicado causa erro de validação** (`Checksum changed`) no próximo `update`. Duas saídas: (a) deixe o arquivo como está (o comentário do banco é só documentação) e use o texto novo apenas em bancos criados do zero; ou (b) edite o arquivo e rode `liquibase clear-checksums` nos ambientes já migrados. Em bancos novos não há problema nenhum.

### 4.7 Comandos do dia a dia

```bash
liquibase --defaultsFile=liquibase.dev.properties status                  # o que falta aplicar
liquibase --defaultsFile=liquibase.dev.properties update                  # aplica tudo
liquibase --defaultsFile=liquibase.dev.properties rollback-count 1        # desfaz o último changeset
liquibase --defaultsFile=liquibase.dev.properties history                 # o que já foi aplicado
```

> ⚠️ **`validate` deixou de ser útil pra checar SQL puro** — ele confere a estrutura do XML, não valida a sintaxe do `.sql` referenciado via `<sqlFile>`. Pra saber se o SQL roda de verdade, só aplicando mesmo (`update`) — é por isso que o CI (FASE 14.2) passou a subir um MySQL efêmero em vez de só rodar `validate` contra H2.

### 4.8 Abrindo uma nova versão de schema (0.2.0 em diante)

1. Criar `scripts/0.2.0/{TICKET}/script-01.sql` + `script-01-rollback.sql` (escrito à mão!)
2. Criar `changelogs/minha-api.changelog-0.2.0.xml`, com um `<changeSet>` apontando pro script via `<sqlFile>`
3. Adicionar um `<include>` desse arquivo no `minha-api.changelog-master.xml`
4. **Testar o rollback manualmente** antes de commitar (`update` → `rollback-count 1` → confirma que voltou ao estado anterior)
5. No backend, ajustar a `IEntityTypeConfiguration<T>` da entidade correspondente (FASE 9.2) — o schema muda no Liquibase, o mapeamento muda no EF Core, e os dois precisam andar juntos no mesmo ticket

**Critério de conclusão:** `liquibase --defaultsFile=liquibase.dev.properties update` cria a tabela `produto` (com `id INT`), e `rollback-count 1` a remove de verdade.

> ✅ O `.gitignore` do `minha-api-database` já nasce correto desde a FASE 1.2 — não precisa de ajuste.

---

# BLOCO C — Backend, camada por camada

> 📁 **Local:** todas as fases deste bloco (5 a 12) vivem no repositório **`minha-api-backend`** (FASE 1.0/1.1).

> 🔁 **Nota importante sobre estrutura de pastas:** os diagramas de pasta em cada fase abaixo (5 a 11) mostram a estrutura **inicial**, de quando cada camada foi criada pela primeira vez. Depois que o CRUD de Produto ficou funcionando ponta a ponta, a estrutura foi **revisada** pra um modelo definitivo — pasta por tipo técnico primeiro, subpasta por entidade dentro. A estrutura final (autoritativa) é a documentada no README §2.0, que inclui:
> - **`DataTransfer` fundido dentro de `Application`** (deixou de ser projeto próprio)
> - **`Commands`** no Domain (CQRS leve — `CriarProdutoCommand`/`AtualizarProdutoCommand`, sem MediatR)
> - **`Filters`** no Domain/Repositories (objeto de critério simples, ex: `ProdutoFilter`)
> - **`RecuperarAsync`** no lugar de `ObterPorIdAsync` (nome genérico do `IRepositorioBase<T>`)
> - **`ConflitoException`** traduzida na Infra a partir do `DbUpdateConcurrencyException` do EF Core (o CrossCutting não conhece o ORM)
> - **`Result<T>`** com uso real (`AtualizarPrecoComRetryAsync`, ver README §5.1)
> - **`Infra/Persistence/`** com o `MinhaApiDbContext` e **`Configurations/<Entidade>/`** com as `IEntityTypeConfiguration<T>` (no lugar de `Mappings/` do NHibernate)
>
> Se você estiver clonando o template do zero pra um projeto novo, siga a estrutura do README §2.0 desde o início em vez de reproduzir o caminho histórico das fases abaixo.

## FASE 5 — Domain

```
src/MinhaApi.Domain/
├── Entities/
│   ├── EntidadeBase.cs
│   └── Produto.cs
├── Abstractions/
│   └── IRepositorioBase.cs
├── Repositories/
│   └── IProdutoRepository.cs
└── Exceptions/            (opcional: exceptions de regra de domínio pura)
```

**`EntidadeBase.cs`** — propriedades com `protected set` (encapsulamento; o EF Core consegue preencher setters protegidos por reflection). 🔁 **`virtual` deixou de ser necessário:** era exigência do NHibernate para lazy loading via proxy; o EF Core não precisa (só precisaria se você habilitasse `UseLazyLoadingProxies`, o que **não** fazemos — ver N+1 na FASE 9.4).

```csharp
namespace MinhaApi.Domain.Entities;

public abstract class EntidadeBase
{
    public long Id { get; protected set; }
    public bool Ativo { get; protected set; } = true;
    public DateTime CriadoEm { get; protected set; } = DateTime.UtcNow;
    public DateTime? AtualizadoEm { get; protected set; }
    public int Version { get; protected set; }

    public void Desativar()
    {
        Ativo = false;
        AtualizadoEm = DateTime.UtcNow;
    }
}
```

> ⚠️ **Construtor sem parâmetros:** o EF Core precisa conseguir instanciar a entidade ao materializar uma consulta. Ele aceita um construtor `protected`/`private` sem parâmetros (ou um construtor cujos parâmetros casem com propriedades). Em `Produto`, mantenha um `protected Produto() { }` para o EF além do construtor público de negócio — com `Nullable` habilitado, atenção ao aviso CS8618 (inicialize `Nome` com `= string.Empty;` ou use `null!` apenas nesse construtor).

**`IRepositorioBase.cs`** — o contrato genérico aprovado (README §5.2):

```csharp
namespace MinhaApi.Domain.Abstractions;

public interface IRepositorioBase<TEntidade> where TEntidade : class
{
    Task<TEntidade?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default);
    Task<IEnumerable<TEntidade>> ObterTodosAsync(CancellationToken cancellationToken = default);
    Task InserirAsync(TEntidade entidade, CancellationToken cancellationToken = default);
    Task InserirAsync(IEnumerable<TEntidade> entidades, CancellationToken cancellationToken = default);
    Task AtualizarAsync(TEntidade entidade, CancellationToken cancellationToken = default);
    Task ExcluirAsync(TEntidade entidade, CancellationToken cancellationToken = default);
    Task ExcluirPorIdAsync(long id, CancellationToken cancellationToken = default);
}
```

**Critério de conclusão:** `dotnet build` OK e o Domain **sem nenhum pacote NuGet externo** (o Domain **não** referencia o EF Core — atributos de mapeamento como `[Key]`/`[Column]` ficam proibidos nas entidades; todo mapeamento mora na Infra, via Fluent API).

> 🔁 **Revisado depois:** `EntidadeBase.Id` mudou de `long` pra `int`, e o `IRepositorioBase<T>` ganhou uma reformulação bem maior (`EditarAsync`, `RecuperarAsync` por predicado, `ListarAsync` paginado/ordenado). O código acima é a versão **histórica/inicial** — a versão final e autoritativa está documentada na FASE 9.3.

---

## FASE 6 — DataTransfer

> 🔁 Ver nota no início do Bloco C — este projeto acaba fundido em `Application/DataTransfer/` mais tarde. Estrutura mostrada aqui é a histórica/inicial.

```
src/MinhaApi.DataTransfer/
├── Requests/
│   ├── CriarProdutoRequest.cs
│   └── AtualizarProdutoRequest.cs
├── Responses/
│   └── ProdutoResponse.cs
└── Common/
    ├── PagedResult.cs
    └── PaginacaoRequest.cs
```

> ⚠️ **Mass Assignment (README §6.3):** `CriarProdutoRequest` e `AtualizarProdutoRequest` são **classes separadas**, mesmo que hoje tenham campos parecidos. Nenhuma delas expõe `Id`, `CriadoEm`, `Ativo` ou `Version`.

```csharp
namespace MinhaApi.DataTransfer.Common;

public record PagedResult<T>(
    IReadOnlyList<T> Itens,
    int Pagina,
    int TamanhoPagina,
    long TotalItens)
{
    public int TotalPaginas => (int)Math.Ceiling(TotalItens / (double)TamanhoPagina);
}
```

---

## FASE 7 — CrossCutting

```
src/MinhaApi.CrossCutting/
├── Exceptions/
│   ├── AppException.cs
│   ├── NaoEncontradoException.cs
│   ├── EntidadeInvalidaException.cs
│   └── ConflitoException.cs
├── Middlewares/
│   ├── ExceptionMiddleware.cs
│   ├── CorrelationIdMiddleware.cs
│   └── SecurityHeadersMiddleware.cs
├── Logging/
│   └── SerilogConfiguration.cs
└── Extensions/
```

### 7.1 Hierarquia de exceptions (README §5.1)

```csharp
namespace MinhaApi.CrossCutting.Exceptions;

public abstract class AppException(string mensagem) : Exception(mensagem)
{
    public abstract int StatusCode { get; }
}

public class NaoEncontradoException<T>(object id)
    : AppException($"{typeof(T).Name} com id '{id}' não foi encontrado.")
{
    public override int StatusCode => StatusCodes.Status404NotFound;
}

public class EntidadeInvalidaException(IEnumerable<string> erros)
    : AppException("Um ou mais erros de validação ocorreram.")
{
    public override int StatusCode => StatusCodes.Status400BadRequest;
    public IEnumerable<string> Erros { get; } = erros;
}

public class ConflitoException(string mensagem) : AppException(mensagem)
{
    public override int StatusCode => StatusCodes.Status409Conflict;
}

public class AcessoNegadoException(string mensagem = "Acesso negado ao recurso.")
    : AppException(mensagem)
{
    public override int StatusCode => StatusCodes.Status403Forbidden;
}
```

> `AcessoNegadoException` é a peça usada na defesa contra **IDOR** (README §6.3).

### 7.2 `ExceptionMiddleware` — ponto único de tradução erro → HTTP → log

Comportamento exigido:
- `AppException` → status da própria exception + `LogWarning` (erro de negócio esperado)
- `Exception` genérica → **500** + `LogError`
- Corpo da resposta sempre em **ProblemDetails (RFC 7807)** com `traceId`
- Em **produção**, nunca expor stack trace, nome de tabela ou detalhe interno (README §6.3)

> 🔁 **O middleware não conhece o ORM.** A primeira versão do plano previa um tratamento específico de `StaleObjectStateException` (NHibernate) aqui. Isso foi removido: a Infra captura a exceção de concorrência do ORM (hoje `DbUpdateConcurrencyException`, do EF Core) e a relança como `ConflitoException` **antes** de ela subir — assim o resultado é **409 Conflict** pelo caminho normal de `AppException`, e o CrossCutting **não referencia nenhum pacote de ORM** (README §7.3).

### 7.3 `CorrelationIdMiddleware`

Lê ou gera um `X-Correlation-Id`, coloca no `LogContext` do Serilog e devolve no header da resposta.

### 7.4 `SecurityHeadersMiddleware`

Adiciona `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy`, `Content-Security-Policy`.

### 7.5 Serilog

- Console em dev, arquivo/JSON estruturado em produção
- **Destructuring com `@`** e **nunca** concatenar input do usuário (anti log forging — README §6.2)
- **Mascarar/omitir** dado pessoal: CPF, e-mail, telefone, token, senha (LGPD — README §6.4)
- ⚠️ **EF Core + dados sensíveis:** por padrão o EF Core **não** loga valores de parâmetros SQL. **Nunca** habilite `EnableSensitiveDataLogging()` fora de desenvolvimento — ele joga valores reais (CPF, e-mail…) nos logs, o que a LGPD trata como incidente (README §6.4).

**Pacotes:**
```bash
cd src/MinhaApi.CrossCutting
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.File
dotnet add package Serilog.Enrichers.CorrelationId
```

---

## FASE 8 — Application

```
src/MinhaApi.Application/
├── Services/
│   ├── IProdutoService.cs
│   └── ProdutoService.cs
├── Validators/
│   ├── CriarProdutoRequestValidator.cs
│   └── AtualizarProdutoRequestValidator.cs
├── Mappings/
│   └── ProdutoMappingConfig.cs
├── Common/
│   └── Result.cs
└── Abstractions/
    ├── IEmailSender.cs
    └── IDateTimeProvider.cs
```

```bash
cd src/MinhaApi.Application
dotnet add package FluentValidation
dotnet add package FluentValidation.DependencyInjectionExtensions
dotnet add package Mapster
dotnet add package Mapster.DependencyInjection
```

### 8.1 Service de exemplo

```csharp
public class ProdutoService(IProdutoRepository repository) : IProdutoService
{
    public async Task<ProdutoResponse> ObterPorIdAsync(long id, CancellationToken ct = default)
    {
        var produto = await repository.ObterPorIdAsync(id, ct)
            ?? throw new NaoEncontradoException<Produto>(id);

        return produto.Adapt<ProdutoResponse>();
    }
}
```

> A Application continua **sem saber** se o ORM é EF Core, NHibernate ou outro — só conhece a interface `IProdutoRepository`. É exatamente por isso que a troca de ORM ficou concentrada na Infra/IoC.

### 8.2 `Result<T>` — só para fluxos ramificados

Implemente a classe, mas **use com parcimônia**: apenas onde o chamador precisa **decidir** algo com base na falha (retry, fallback, compensação). Erros que só param e respondem usam exception tipada.

### 8.3 `IDateTimeProvider`

Abstrair `DateTime.UtcNow` atrás de uma interface torna os testes determinísticos e ajuda a impedir o uso acidental de `DateTime.Now`.

---

## FASE 9 — Infra (Entity Framework Core)

> 🔁 **Fase reescrita na revisão de out/2026** (antes: NHibernate + FluentNHibernate). É a fase com mais mudanças — o resto do plano só sofre ajustes pontuais.

```
src/MinhaApi.Infra/
├── Persistence/
│   ├── MinhaApiDbContext.cs
│   └── Configurations/
│       └── Produtos/
│           └── ProdutoConfiguration.cs
├── Repositories/
│   ├── RepositorioBase.cs
│   └── Produtos/
│       └── ProdutoRepository.cs
└── Services/
    └── SmtpEmailSender.cs
```

### 9.0 Decisões que valem para toda a fase

1. **Liquibase é dono do schema; EF Core só mapeia.** Nada de `dotnet ef migrations add`, `Database.Migrate()` ou `EnsureCreated()`. O pacote `Microsoft.EntityFrameworkCore.Design` (que habilita `dotnet ef`) **não precisa** ser instalado — só se um dia você quiser rodar `dotnet ef dbcontext scaffold` pra gerar entidades a partir de um banco existente (e mesmo assim, num projeto descartável).
2. **Mapeamento só por Fluent API na Infra** (`IEntityTypeConfiguration<T>`), nunca por data annotations nas entidades — mantém o Domain sem dependência de ORM (README §2.1).
3. **Lazy loading desligado** (padrão do EF Core) — carregamento de relacionamentos é sempre explícito, via `Include` (ver 9.4).
4. **Provider MySQL: Pomelo** (`Pomelo.EntityFrameworkCore.MySql`), o provider comunitário mais usado, construído sobre o `MySqlConnector` — o mesmo conector que já estávamos usando. **Não** é preciso pacote de driver separado como era no NHibernate.

> ⚠️ **Compatibilidade de versão com .NET 10 — conferir antes de instalar.** Na última checagem que consegui fazer (out/2026), o Pomelo estável ainda era a linha **9.x** (EF Core 9); a linha 10.x aparecia como pendente. A boa notícia: o próprio Pomelo declara suportar rodar a 9.x em projetos `net10.0` — o `TargetFramework` e a versão do EF Core são coisas independentes. Então a escolha segura hoje é **EF Core 9.x + Pomelo 9.0.x num projeto `net10.0`**. Existe também um fork comunitário (`Microting.EntityFrameworkCore.MySql`) já na linha 10.x, mas é um fork de terceiros — avalie antes de depender dele num template. **Antes de executar, abra a página do Pomelo no NuGet e veja se a 10.x já saiu como estável**; se sim, suba tudo junto pra 10.x e ajuste os comandos abaixo. Não misture major diferente de `Microsoft.EntityFrameworkCore` e do Pomelo.

### 9.1 Pacotes

```bash
cd src/MinhaApi.Infra
dotnet add package Microsoft.EntityFrameworkCore --version "9.*"
dotnet add package Pomelo.EntityFrameworkCore.MySql --version "9.*"
dotnet add package System.Linq.Dynamic.Core
dotnet add package Microsoft.Extensions.Configuration.Abstractions
```

> `MySqlConnector` entra **transitivamente** pelo Pomelo — não precisa adicionar à mão. Os pacotes `NHibernate`, `NHibernate.Driver.MySqlConnector`, `FluentNHibernate` e o `MySqlConnector` explícito **saem** do `.csproj` da Infra.

### 9.2 `MinhaApiDbContext` e mapeamento

**`Persistence/MinhaApiDbContext.cs`:**

```csharp
using Microsoft.EntityFrameworkCore;
using MinhaApi.Domain.Entities;

namespace MinhaApi.Infra.Persistence;

public class MinhaApiDbContext(DbContextOptions<MinhaApiDbContext> options) : DbContext(options)
{
    public DbSet<Produto> Produtos => Set<Produto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(MinhaApiDbContext).Assembly);

    // Concorrência otimista: o EF Core NÃO incrementa um token "int" sozinho
    // (o NHibernate fazia isso). Incrementamos aqui, num lugar só, pra toda entidade.
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<EntidadeBase>()
                     .Where(e => e.State == EntityState.Modified))
        {
            var version = entry.Property(e => e.Version);
            version.CurrentValue = version.OriginalValue + 1;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
```

> ⚠️ Use **somente** as versões assíncronas (`SaveChangesAsync`, `ToListAsync`…) na API. Se alguém chamar o `SaveChanges()` síncrono por engano, o incremento acima **não** roda e o controle de concorrência quebra em silêncio — sobrescreva também o `SaveChanges()` síncrono chamando o mesmo método auxiliar, ou proíba o uso por revisão de código.

**`Persistence/Configurations/Produtos/ProdutoConfiguration.cs`:**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MinhaApi.Domain.Entities.Produtos;

namespace MinhaApi.Infra.Persistence.Configurations.Produtos;

public class ProdutoConfiguration : IEntityTypeConfiguration<Produto>
{
    public void Configure(EntityTypeBuilder<Produto> builder)
    {
        builder.ToTable("produto");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Preco).HasColumnName("preco").HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.Ativo).HasColumnName("ativo").IsRequired();
        builder.Property(x => x.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(x => x.AtualizadoEm).HasColumnName("atualizado_em");

        // Concorrência otimista (lost update) — README §7.3
        builder.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
    }
}
```

> Os nomes de coluna **precisam bater exatamente** com o SQL do Liquibase (FASE 4.6). Como o EF Core não valida o schema na subida, divergência só aparece em runtime, na primeira query — por isso o critério da FASE 11 testa o CRUD de verdade pelo Swagger.

> 🕒 **Pegadinha de UTC:** o EF Core/Pomelo devolve `DateTime` lido do banco com `Kind = Unspecified`, não `Utc`. Se isso importar na serialização da API (ex: ausência do `Z` no JSON), adicione um `ValueConverter` nas propriedades de data que force `DateTime.SpecifyKind(valor, DateTimeKind.Utc)` na leitura — de preferência uma vez só, na configuração da `EntidadeBase`.

### 9.3 `IRepositorioBase<TEntidade>` / `RepositorioBase<TEntidade>` 🔁 (reformulado na execução)

O **contrato não mudou** com a troca de ORM — esse é o ponto da abstração (README §2.2). Ele passou, isso sim, por uma reformulação bem maior que o previsto inicialmente — não é só CRUD básico, ganhou paginação/ordenação/filtro genéricos de verdade:

```csharp
public interface IRepositorioBase<TEntidade> where TEntidade : class
{
    Task InserirAsync(TEntidade entidade, CancellationToken cancellationToken = default);
    Task InserirAsync(IEnumerable<TEntidade> entidades, CancellationToken cancellationToken = default);
    Task EditarAsync(TEntidade entidade, CancellationToken cancellationToken = default);          // 🔁 era AtualizarAsync
    Task ExcluirAsync(TEntidade entidade, CancellationToken cancellationToken = default);
    Task ExcluirAsync(IEnumerable<TEntidade> entidades, CancellationToken cancellationToken = default); // novo: em lote
    Task<TEntidade?> RecuperarAsync(int id, CancellationToken cancellationToken = default);        // 🔁 Id agora e int, nao long
    Task<TEntidade?> RecuperarAsync(Expression<Func<TEntidade, bool>> expressao, CancellationToken cancellationToken = default); // novo: por predicado

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

**Peças novas que dão suporte a esse contrato** (todas em `Domain/Abstractions/`):

- **`TipoOrdenacao`** — enum `Ascendente`/`Descendente`.
- **`PaginacaoConsulta<TEntidade>`** — resultado paginado no nível de Domain/Infra (entidades, não DTO) — distinto do `PagedResult<T>` da Application (que carrega DTOs de resposta), porque Domain/Infra não podem depender de Application.
- **`PaginacaoFiltro`** — consolida `Qt` (com teto de 100, proteção contra DoS — README §6.2), `Pg`, `CpOrd`, `TpOrd` num objeto só.

> ⚠️ **Um risco real que foi corrigido no caminho:** a primeira versão desse `PaginacaoFiltro` (inspirada num padrão que Ceccon usa no trabalho) tinha um método `ObterSqlOrdenacao()` que montava a cláusula `ORDER BY` **concatenando string direto em SQL** — clássico SQL Injection (README §6.1), se `CpOrd` viesse de input do usuário. Removido. A ordenação dinâmica de verdade usa **`System.Linq.Dynamic.Core`** (`query.OrderBy("Preco descending")`), que valida o nome da propriedade via reflection contra `TEntidade` **antes** de montar a expressão — nome inválido gera exceção de parse, nunca executa nada no banco. (Funciona normalmente sobre o `IQueryable` do EF Core.)

**`RecuperarAsync(Expression<Func<TEntidade, bool>>)` — por que existe:** cobre buscas simples por condição (ex: "produto com esse nome") sem precisar criar um método específico por entidade — reduz a necessidade de métodos tipo `ObterPorNomeAsync` espalhados pelos repositórios concretos.

**Combinando múltiplos filtros opcionais sem biblioteca extra:** em vez de combinar vários `Expression<Func<T,bool>>` separados (precisaria de algo tipo `PredicateBuilder`), a Application monta **um único lambda**, neutralizando cada condição não informada:

```csharp
p =>
    (string.IsNullOrWhiteSpace(request.Nome) || p.Nome.Contains(request.Nome)) &&
    (!request.PrecoMinimo.HasValue || p.Preco >= request.PrecoMinimo.Value) &&
    (!request.Ativo.HasValue || p.Ativo == request.Ativo.Value)
```

O EF Core traduz isso como um único `WHERE`, sem precisar de nenhuma lib de combinação de expressões.

**Esqueleto da `RepositorioBase<TEntidade>` com EF Core** (trechos que mudam de verdade em relação ao NHibernate):

```csharp
public class RepositorioBase<TEntidade>(MinhaApiDbContext context) : IRepositorioBase<TEntidade>
    where TEntidade : class
{
    protected MinhaApiDbContext Context { get; } = context;
    protected DbSet<TEntidade> Set => Context.Set<TEntidade>();

    public async Task InserirAsync(TEntidade entidade, CancellationToken cancellationToken = default)
    {
        await Set.AddAsync(entidade, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);
    }

    public Task<TEntidade?> RecuperarAsync(int id, CancellationToken cancellationToken = default)
        => Set.FindAsync([id], cancellationToken).AsTask();   // FindAsync usa o cache do contexto antes de ir ao banco

    public Task<TEntidade?> RecuperarAsync(Expression<Func<TEntidade, bool>> expressao, CancellationToken cancellationToken = default)
        => Set.FirstOrDefaultAsync(expressao, cancellationToken);

    public async Task EditarAsync(TEntidade entidade, CancellationToken cancellationToken = default)
    {
        try
        {
            // Se a entidade já veio rastreada de um RecuperarAsync, o Update é desnecessário
            // (o rastreamento já detectou a mudança) mas inofensivo.
            Context.Update(entidade);
            await Context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Limpa o contexto: o estado rastreado agora está desatualizado, e
            // o retry do Result<T> precisa recarregar do banco de verdade (ver 9.4).
            Context.ChangeTracker.Clear();
            throw new ConflitoException("O recurso foi modificado por outra operação. Recarregue e tente novamente.");
        }
    }

    // ListarAsync: consultas de leitura usam AsNoTracking() — ver 9.4
}
```

**Tradução da exceção de concorrência continua na Infra** (a exceção de origem é que mudou): `EditarAsync` captura `DbUpdateConcurrencyException` e relança como `ConflitoException` (README §7.3).

### 9.4 Cuidados específicos do EF Core ⚠️

| Tema | Regra |
|---|---|
| **N+1 (README §7.2)** | Lazy loading é desligado por padrão — o N+1 no EF Core acontece quando você faz uma consulta **dentro de um `foreach`**, ou quando alguém liga `UseLazyLoadingProxies`. Relacionamento sempre com **`Include`** explícito: `Set.Include(p => p.Cliente)`. Para coleções grandes, avalie `AsSplitQuery()` para evitar explosão cartesiana |
| **`AsNoTracking()`** | Em toda consulta **somente leitura** (listagens, `ListarAsync`) use `AsNoTracking()` — menos memória e mais rápido. Rastreamento só onde vai haver edição |
| **Cache do contexto (identity map)** | Reconsultar uma entidade que o contexto já rastreia **devolve a instância antiga**, não os valores novos do banco. Por isso o `EditarAsync` faz `ChangeTracker.Clear()` ao detectar conflito — sem isso, o retry de `AtualizarPrecoComRetryAsync` reconsultaria a mesma instância velha e falharia de novo, até estourar as tentativas |
| **Contexto não é thread-safe** | Nunca use o mesmo `DbContext` em duas tarefas paralelas (`Task.WhenAll` sobre o mesmo contexto lança `InvalidOperationException`). Uma operação por vez por request |
| **SQL cru** | Se precisar: `FromSql($"... {param}")` / `ExecuteSql($"...")` (interpolação **parametriza**). **Nunca** `FromSqlRaw`/`ExecuteSqlRaw` com string concatenada (README §6.1) |
| **Logs de valores** | `EnableSensitiveDataLogging()` e `EnableDetailedErrors()` só em `Development` (ver FASE 7.5) |
| **Sem migrations** | Reforçando 9.0: schema = Liquibase |

### 9.5 ⚠️ Ciclo de vida e timeouts

| Serviço | Lifetime | Por quê |
|---|---|---|
| `MinhaApiDbContext` | **Scoped** | Uma instância por request (é o padrão de `AddDbContext`). **Nunca Singleton** (README §7.1) |
| Repositórios | **Scoped** | Dependem do `DbContext` |
| Services | **Scoped** | Dependem de repositórios |

> O que era `ISessionFactory` (Singleton, caro de construir) no NHibernate **não tem equivalente que você precise registrar**: o EF Core cuida do modelo e do pool de conexões internamente. Se um dia o desempenho exigir, `AddDbContextPool` reaproveita instâncias de contexto — mas só use se o contexto **não** guardar estado por request.

**Registro e timeout** (vai no `AddInfra()` da FASE 10):

```csharp
services.AddDbContext<MinhaApiDbContext>((provider, options) =>
{
    var connectionString = config.GetConnectionString("MySql");

    // ServerVersion explícita: AutoDetect abre uma conexão no momento da configuração,
    // o que quebra a subida da API (e os testes) se o banco ainda não estiver de pé.
    options.UseMySql(
        connectionString,
        new MySqlServerVersion(new Version(8, 4, 0)),
        mySql => mySql.CommandTimeout(30));      // README §6.2 — query travada não segura o pool
});
```

> Ajuste `new Version(8, 4, 0)` pra versão real do MySQL em cada ambiente (a do `docker-compose.yml` é a 8.4; a instalação nativa pode ser outra — ver FASE 3.3).

### 9.6 🔁 Checklist de migração (se a FASE 9 já foi executada com NHibernate)

1. **Pacotes da Infra:** remover `NHibernate`, `NHibernate.Driver.MySqlConnector`, `FluentNHibernate`, `MySqlConnector`; adicionar os de 9.1.
2. **`Persistence/NHibernateConfiguration.cs`** → apagar. Criar `MinhaApiDbContext` (9.2).
3. **`Mappings/Produtos/ProdutoMap.cs`** (FluentNHibernate `ClassMap`) → trocar por `Persistence/Configurations/Produtos/ProdutoConfiguration.cs` (`IEntityTypeConfiguration`). Mesmos nomes de coluna — **o banco não muda**.
4. **`RepositorioBase`**: trocar `ISession` por `MinhaApiDbContext`; trocar `Session.Query<T>()` por `Set` (`DbSet<T>`) / `Set.AsNoTracking()`; trocar o `catch (StaleObjectStateException)` por `catch (DbUpdateConcurrencyException)` + `ChangeTracker.Clear()`.
5. **Consultas com `Fetch(...)`** → `Include(...)`.
6. **Entidades:** remover `virtual` das propriedades (opcional, mas deixou de fazer sentido) e garantir construtor `protected` sem parâmetros.
7. **IoC (`AddInfra`)**: tirar o registro de `ISessionFactory`/`ISession`; entra `AddDbContext` (9.5). Repositórios continuam `AddScoped`.
8. **`ExceptionMiddleware`/CrossCutting**: já não conhece o ORM — conferir que **nenhum** `using NHibernate` sobrou em lugar nenhum (`grep -ri nhibernate src/`).
9. **Testes:** `ProdutoServiceTests` mocka só `IProdutoRepository` — não deve mudar. Rodar `dotnet test`.
10. **Subir a API e rodar o checklist completo da FASE 11** (CRUD pelo Swagger, incluindo o `GET /health` e a confirmação do soft delete no DBeaver) — a troca de ORM só está validada de verdade quando uma requisição real persiste no MySQL.
11. Testar a **concorrência otimista na mão**: abrir o mesmo produto em duas abas do Swagger, editar nas duas → a segunda deve responder **409**.

**Critério de conclusão:** `dotnet build` OK e **nenhuma referência a NHibernate** no repositório (`grep -ri nhibernate src/ tests/` vazio). Esta fase só é validada de fato na FASE 11 (quando a API sobe e consegue persistir de verdade) — build sozinho não garante que o provider e os nomes de coluna estão certos, já que o EF Core só descobre divergência de schema na primeira query.

---

## FASE 10 — IoC

```csharp
namespace MinhaApi.IoC;

public static class DependencyInjection
{
    public static IServiceCollection AddIoC(this IServiceCollection services, IConfiguration config)
        => services
            .AddCrossCutting(config)
            .AddInfra(config)
            .AddApplication();
}
```

Cada `Add*` fica no seu próprio arquivo de extensão. O `AddInfra()` é onde mora o `AddDbContext<MinhaApiDbContext>(...)` da FASE 9.5 e o registro dos repositórios (`AddScoped<IProdutoRepository, ProdutoRepository>()`). E no `Program.cs`:

```csharp
builder.Services.AddIoC(builder.Configuration);
```

> ⚠️ **Captive Dependency (README §7.6):** ative validação de escopo em desenvolvimento — agora o "Scoped perigoso" é o `MinhaApiDbContext`:
> ```csharp
> builder.Host.UseDefaultServiceProvider((ctx, opts) =>
> {
>     opts.ValidateScopes = ctx.HostingEnvironment.IsDevelopment();
>     opts.ValidateOnBuild = ctx.HostingEnvironment.IsDevelopment();
> });
> ```

---

## FASE 11 — API

```
src/MinhaApi.Api/
├── Controllers/
│   └── V1/ProdutosController.cs
├── Configuration/
│   ├── SwaggerConfiguration.cs
│   ├── CorsConfiguration.cs
│   └── RateLimitConfiguration.cs
├── Options/
│   └── MySqlOptions.cs
├── appsettings.json
├── appsettings.Development.json
├── appsettings.Staging.json
├── appsettings.Production.json
└── Program.cs
```

> `appsettings.Staging.json` = ambiente **HML**. `appsettings.Production.json` = ambiente **PROD** (ver FASE 21 e FASE 1.4).

### 11.0 🔁 Pacotes e decisões descobertas na execução real

```bash
cd src/MinhaApi.Api
dotnet add package Swashbuckle.AspNetCore
dotnet add package AspNetCore.HealthChecks.MySql
cd ../..
```

Três decisões que só ficaram claras executando esta fase de verdade:

1. **`FluentValidation.AspNetCore` está descontinuado** pelo próprio time do FluentValidation (eles recomendam validação explícita no código, não filtro automático de MVC). Em vez de auto-validação, o `ProdutoService` (Application) **injeta os `IValidator<T>`** e chama `ValidateAsync` explicitamente, lançando `EntidadeInvalidaException` em caso de erro — isso mantém o erro de validação passando pelo **mesmo** `ExceptionMiddleware` de sempre, em vez de dois mecanismos de erro coexistindo. Não instale `FluentValidation.AspNetCore`.
2. **Health check real, não só "o processo está de pé"**: `AspNetCore.HealthChecks.MySql` registrado com a mesma connection string do EF Core faz o `GET /health` só responder `Healthy` se a API conseguir de fato falar com o banco.
3. **Rate limiting nativo do .NET** (`Microsoft.AspNetCore.RateLimiting`) exige **dois** `using` diferentes — um de `System.Threading.RateLimiting` (tipos base como `QueueProcessingOrder`) e outro de `Microsoft.AspNetCore.RateLimiting` (os métodos de extensão como `AddFixedWindowLimiter`). Esquecer o segundo dá erro de compilação confuso (`CS1061`).

### 11.1 Checklist do `Program.cs`

Ordem do pipeline importa. Configure, nesta sequência:

```
1.  Serilog
2.  AddIoC()
3.  Options Pattern + ValidateOnStart()
4.  Controllers + FluentValidation
5.  Swagger (registro)
6.  CORS (origens explícitas — nunca AllowAnyOrigin em produção)
7.  Rate Limiting
8.  Health Checks (/health, /health/ready)
9.  Limites do Kestrel (MaxRequestBodySize)
10. Authentication / Authorization (JWT — fase posterior)
---- app.Build() ----
11. CorrelationIdMiddleware
12. ExceptionMiddleware
13. SecurityHeadersMiddleware
14. UseHttpsRedirection + UseHsts (produção)
15. UseCors
16. UseRateLimiter
17. UseAuthentication + UseAuthorization
18. MapControllers + MapHealthChecks
19. Swagger UI  →  SOMENTE se IsDevelopment()
20. Graceful Shutdown (ApplicationStopping)
```

### 11.2 Configuração multi-ambiente (README §9)

**`appsettings.json`** — comum, sem segredo:
```json
{
  "MySql": { "CommandTimeout": 30 },
  "Serilog": { "MinimumLevel": { "Default": "Information" } }
}
```

**`appsettings.Development.json`**:
```json
{
  "Serilog": { "MinimumLevel": { "Default": "Debug" } },
  "Cors": { "OrigensPermitidas": [ "http://localhost:4200" ] }
}
```

**`appsettings.Staging.json`** — ambiente HML, connection string vazia de propósito:
```json
{
  "ConnectionStrings": { "MySql": "" },
  "Serilog": { "MinimumLevel": { "Default": "Information" } },
  "Cors": { "OrigensPermitidas": [ "https://hml.seudominio.com" ] }
}
```

**`appsettings.Production.json`** — connection string **vazia de propósito** (vem de variável de ambiente):
```json
{
  "ConnectionStrings": { "MySql": "" },
  "Serilog": { "MinimumLevel": { "Default": "Warning" } },
  "Cors": { "OrigensPermitidas": [ "https://seudominio.com" ] }
}
```

> Na Azure (FASE 21), a connection string real é configurada em **App Service → Configuration → Application settings**, chave `ConnectionStrings__MySql` — nunca commitada.

### 11.3 User Secrets (desde o primeiro commit — README §6.2)

```bash
cd src/MinhaApi.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:MySql" "Server=localhost;Port=3306;Database=minha_api_dev;User=root;Password=root123;"
```

Em produção, a mesma chave vem como variável de ambiente:
```bash
export ConnectionStrings__MySql="Server=...;Password=***"
```

> A connection string no formato `Server=...;Port=...;Database=...;User=...;Password=...;` é a do **MySqlConnector** — o mesmo formato serve pro Pomelo, sem mudança.

### 11.4 Options com validação na inicialização

```csharp
builder.Services
    .AddOptions<MySqlOptions>()
    .Bind(builder.Configuration.GetSection("MySql"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

### 11.5 Controller de exemplo

```csharp
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class ProdutosController(IProdutoService service) : ControllerBase
{
    [HttpGet("{id:long}")]
    [ProducesResponseType<ProdutoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterPorId(long id, CancellationToken ct)
        => Ok(await service.ObterPorIdAsync(id, ct));   // DTO puro, sem envelope

    [HttpPost]
    [ProducesResponseType<ProdutoResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar(CriarProdutoRequest request, CancellationToken ct)
    {
        var produto = await service.CriarAsync(request, ct);
        return CreatedAtAction(nameof(ObterPorId), new { id = produto.Id }, produto);
    }
}
```

> Repare: **sem `try/catch`**. O `ExceptionMiddleware` cuida de tudo. E **sem envelope** — DTO puro + status HTTP correto (README §5.3).

**Critério de conclusão** (checklist testado manualmente pelo Swagger, não só "buildou"):
- [ ] `./dev.sh` (ou `dev.ps1`) sobe o banco e a API sem erro
- [ ] Swagger abre em `/swagger` com os 5 endpoints de Produtos
- [ ] `GET /health` responde `Healthy` (confirma conexão real com o MySQL, não só "o processo está de pé")
- [ ] `POST` cria produto → **201** com o produto (id gerado)
- [ ] `GET` por id existente → **200**; id inexistente → **404** com `ProblemDetails` + `traceId`
- [ ] `PUT` com dado inválido (ex: nome vazio) → **400** com a mensagem do FluentValidation
- [ ] `PUT` concorrente (mesmo produto editado em duas abas do Swagger) → a segunda responde **409** (valida o `IsConcurrencyToken` + o incremento no `SaveChangesAsync`)
- [ ] `DELETE` → **204**; produto continua na tabela com `ativo = 0` (soft delete), confirmável pelo DBeaver

---

## FASE 12 — Testes

> 🔁 **Escopo revisado durante a execução:** por decisão consciente, a cobertura inicial cobre **só Domain (entidades) e Application (services)** — validators (regras simples, baixo risco), integração (precisa de Docker) e IDOR (precisa da FASE 17/JWT) ficam adiados de propósito. Ver §12.3 abaixo.

### 12.0 🔴 `FluentAssertions` mudou de licença — use `AwesomeAssertions`

**Não instale `FluentAssertions`.** A partir da v8 (jan/2025), a biblioteca passou a exigir **licença paga da Xceed** (~US$130/desenvolvedor/ano) pra uso **comercial** — só é grátis pra uso não-comercial. Como este template pode virar produto/serviço vendido a cliente, isso é risco de compliance real, não só técnico.

**Use `AwesomeAssertions`** em vez disso: um fork comunitário mantido ativamente, sob licença **Apache 2.0** (grátis pra sempre, inclusive uso comercial), com **API idêntica** — só muda o nome do pacote e o `using`.

```bash
cd tests/MinhaApi.Tests.Unit
dotnet add package AwesomeAssertions
dotnet add package NSubstitute
dotnet add package Bogus

cd ../MinhaApi.Tests.Integration
dotnet add package AwesomeAssertions
dotnet add package Testcontainers.MySql
dotnet add package Microsoft.AspNetCore.Mvc.Testing
```

> ⚠️ O pacote precisa ser trocado nos **dois** projetos de teste — `FluentAssertions` costuma entrar no bloco de pacotes dos dois de uma vez, então é fácil esquecer um.

Nos arquivos de teste: `using AwesomeAssertions;` no lugar de `using FluentAssertions;` — todo o resto do código (`.Should().Be(...)`, `.Should().Throw<T>()`, etc.) funciona idêntico.

### 12.1 `tests/Directory.Build.props` — silenciar CA1707 nos testes

Os projetos de teste herdam o `Directory.Build.props` raiz (`TreatWarningsAsErrors` + analisadores). Nomes de teste com underscore (`Metodo_Cenario_Resultado`, convenção padrão em testes .NET) disparam a regra **CA1707** ("identificadores não devem ter underscore"). Crie um `Directory.Build.props` **só dentro de `tests/`** desativando essa regra específica, sem afetar o resto da solution:

```xml
<Project>
  <PropertyGroup>
    <!-- Nomes de teste com underscore (Metodo_Cenario_Resultado) sao uma convencao
         proposital de legibilidade, nao um erro de nomenclatura - desativamos so
         essa regra aqui, sem afetar o resto da solution. -->
    <NoWarn>$(NoWarn);CA1707</NoWarn>
  </PropertyGroup>
</Project>
```

### 12.2 Estrutura de pastas dos testes (espelha a estrutura por entidade do README §2.0)

```
tests/MinhaApi.Tests.Unit/
├── Domain/Produtos/ProdutoTests.cs
└── Application/Produtos/ProdutoServiceTests.cs
```

### 12.3 Cobertura mínima do template (servem de exemplo para projetos derivados)

| Tipo | O que testar | Status |
|---|---|---|
| Unitário | Regra de negócio da entidade `Produto` (construtor, `AtualizarPreco`, `AtualizarNome`, `Desativar`) | ✅ Implementado |
| Unitário | `ProdutoService` com repositório e validators mockados (NSubstitute) — inclui o fluxo de retry do `Result<T>` | ✅ Implementado |
| Unitário | Validators do FluentValidation | ⬜ Adiado (regras simples, baixo risco) |
| Integração | `RepositorioBase` contra MySQL real via Testcontainers | ⬜ Adiado (precisa de Docker — ver nota abaixo) |
| Integração | Endpoint completo via `WebApplicationFactory` | ⬜ Adiado |
| Integração | ⚠️ **Teste de IDOR**: usuário A **não** consegue acessar recurso do usuário B | ⬜ Adiado até a FASE 17 (precisa de autenticação) |
| Integração | ⚠️ **Teste de concorrência otimista**: dois updates concorrentes → 409 | ⬜ Adiado (precisa de Docker) |

```bash
dotnet test
```

> ⚠️ **Nota do Testcontainers em máquinas sem Docker/virtualização:** este pacote precisa de Docker rodando pra subir o MySQL efêmero do teste. Se sua máquina não suporta Docker localmente (ver nota da FASE 0.2), os testes de integração dessa suíte só rodam no CI (GitHub Actions, que tem Docker nativo nos runners) — não localmente.

> 🔁 **Com EF Core, a tentação é usar o provider `InMemory` (ou SQLite em memória) nos testes de integração. Não use para este template:** eles não executam SQL real do MySQL e **não detectam conflito de concorrência otimista** como o banco de verdade — o teste ficaria verde enquanto produção quebra. O Testcontainers com MySQL real continua sendo a escolha certa pra qualquer teste que toque o `DbContext`. Os testes unitários da Application seguem mockando só `IProdutoRepository`, sem tocar em EF.

**Critério de conclusão (dentro do escopo revisado):** `dotnet test` roda os testes de `Produto` e `ProdutoService` e todos passam, sem nenhum aviso de licença comercial na saída (confirma que `AwesomeAssertions` está em uso, não `FluentAssertions`).

---

# BLOCO D — Qualidade e Automação

## FASE 13 — EditorConfig e Analisadores ✅ (decisões fechadas)

A maior parte das decisões já tinha **precedente de fato** no código escrito nas fases anteriores — em vez de decidir do zero, confirmamos o que já estava em uso e só decidimos o que realmente estava em aberto.

| Decisão | Escolha | Origem |
|---|---|---|
| Campos privados | `_camelCase` para campos de **instância**; `PascalCase` para `const`/`static readonly` (convenção padrão do ecossistema .NET pra "constante nomeada") | Precedente (`_pagina`, `TamanhoPaginaPadrao`, `JsonOptions`) |
| Indentação | 4 espaços | Precedente |
| Chaves `{` | Linha nova (padrão C#/Allman) | Precedente |
| `var` vs tipo explícito | `var` quando o tipo é óbvio (tipo nativo ou aparente pela direita); tipo explícito quando não é | Precedente |
| `this.` explícito | Não usar | Precedente |
| Namespace | File-scoped (`namespace X;`) | Precedente, desde a FASE 5 |
| Idioma de nomes | Híbrido — domínio em português (`Produto`, `Nome`, `Preco`), técnico em inglês (`Service`, `Repository`, `Command`, `Filter`) | Precedente |
| Severidade das violações | `error` | Já em `Directory.Build.props` desde a FASE 2 |
| Ordem dos `using` | System primeiro, resto agrupado junto (sem separar em blocos por ponto-e-vírgula/linha em branco) | Decisão nova |
| Tamanho máximo de arquivo/método | **Sem limite formal** — fica a critério do code review, não travado por analisador | Decisão nova |
| StyleCop.Analyzers / SonarAnalyzer.CSharp | **Não instalar** — o `.editorconfig` + analisadores nativos do .NET já bastam | Decisão nova |

### 13.1 `.editorconfig` (raiz do repositório, ao lado do `.sln`)

```ini
root = true

[*]
charset = utf-8
end_of_line = crlf
insert_final_newline = true
trim_trailing_whitespace = true
indent_style = space
indent_size = 4

[*.{json,yml,yaml,xml,csproj,props}]
indent_size = 2

[*.md]
trim_trailing_whitespace = false

[*.cs]
indent_size = 4
tab_width = 4

# Namespace file-scoped
csharp_style_namespace_declarations = file_scoped:error

# Chaves em linha nova (Allman)
csharp_new_line_before_open_brace = all
csharp_new_line_before_else = true
csharp_new_line_before_catch = true
csharp_new_line_before_finally = true
csharp_new_line_before_members_in_object_initializers = true
csharp_new_line_before_members_in_anonymous_types = true

# this. explícito: não usar
dotnet_style_qualification_for_field = false:error
dotnet_style_qualification_for_property = false:error
dotnet_style_qualification_for_method = false:error
dotnet_style_qualification_for_event = false:error

# var quando obvio, tipo explicito quando nao
csharp_style_var_for_built_in_types = true:suggestion
csharp_style_var_when_type_is_apparent = true:suggestion
csharp_style_var_elsewhere = false:suggestion

# Campos privados const -> PascalCase
dotnet_naming_symbols.campo_privado_const.applicable_kinds = field
dotnet_naming_symbols.campo_privado_const.applicable_accessibilities = private
dotnet_naming_symbols.campo_privado_const.required_modifiers = const

dotnet_naming_style.pascal_case.capitalization = pascal_case

dotnet_naming_rule.campo_privado_const_pascal_case.symbols = campo_privado_const
dotnet_naming_rule.campo_privado_const_pascal_case.style = pascal_case
dotnet_naming_rule.campo_privado_const_pascal_case.severity = error

# Campos privados static readonly -> PascalCase
dotnet_naming_symbols.campo_privado_static_readonly.applicable_kinds = field
dotnet_naming_symbols.campo_privado_static_readonly.applicable_accessibilities = private
dotnet_naming_symbols.campo_privado_static_readonly.required_modifiers = static,readonly

dotnet_naming_rule.campo_privado_static_readonly_pascal_case.symbols = campo_privado_static_readonly
dotnet_naming_rule.campo_privado_static_readonly_pascal_case.style = pascal_case
dotnet_naming_rule.campo_privado_static_readonly_pascal_case.severity = error

# Campos privados de instancia (o caso geral) -> _camelCase
dotnet_naming_symbols.campo_privado_instancia.applicable_kinds = field
dotnet_naming_symbols.campo_privado_instancia.applicable_accessibilities = private

dotnet_naming_style.underscore_camel_case.capitalization = camel_case
dotnet_naming_style.underscore_camel_case.required_prefix = _

dotnet_naming_rule.campo_privado_instancia_underscore.symbols = campo_privado_instancia
dotnet_naming_rule.campo_privado_instancia_underscore.style = underscore_camel_case
dotnet_naming_rule.campo_privado_instancia_underscore.severity = error

# Ordem dos usings: System primeiro, resto agrupado junto
dotnet_sort_system_directives_first = true
dotnet_separate_import_directive_groups = false
csharp_using_directive_placement = outside_namespace:error

# Severidade padrao das regras de estilo (IDE0xxx)
dotnet_analyzer_diagnostic.severity = warning

csharp_prefer_braces = true:suggestion
csharp_style_expression_bodied_methods = when_on_single_line:suggestion
csharp_style_expression_bodied_properties = true:suggestion
dotnet_style_readonly_field = true:suggestion
dotnet_style_require_accessibility_modifiers = always:suggestion
```

> ⚠️ **Nota importante sobre a regra de campo privado:** ela é dividida em **3 níveis** (const → PascalCase, static readonly → PascalCase, instância → `_camelCase`), avaliados nessa ordem. Se fosse uma regra única "todo campo privado é `_camelCase`", ela quebraria retroativamente código já escrito (`TamanhoPaginaPadrao`, `JsonOptions`), que segue a convenção padrão do .NET de tratar constantes como `PascalCase`.

**Critério de conclusão:** `dotnet build` passa sem novo erro depois de adicionar o `.editorconfig` — o refinamento de 3 níveis evita quebrar o código já escrito.

---

## FASE 14 — GitHub Actions (CI) ✅

> 🔁 **Revisado pra poly-repo:** com 3 repositórios separados (FASE 1.0), cada um ganha seu **próprio** `ci.yml` — não existe mais um workflow único com jobs de `backend` e `frontend` lado a lado.

> ⚠️ **Dois bugs reais descobertos executando esta fase** (corrigidos nos exemplos abaixo):
> 1. **`actions/checkout@v4` está com os dias contados:** o GitHub vai **remover completamente o Node.js 20** dos runners em **16/setembro/2026**, e `@v4` roda nessa versão. Use **`@v4` → `@v5`** em todo workflow (já corrigido abaixo). O mesmo vale pra `actions/setup-node` (`@v4` → `@v5`) quando a FASE 15 criar o CI do frontend.
> 2. **Instalar Liquibase via `curl` + script não funciona entre steps:** cada `run:` do GitHub Actions é um processo de shell novo — o `PATH` atualizado por um script de instalação num step **não persiste** pro próximo step, dando erro `127` ("comando não encontrado"). A correção é usar a **action oficial `liquibase/setup-liquibase`**, que resolve isso corretamente (e já cuida de cache entre execuções).

### 14.1 `minha-api-backend/.github/workflows/ci.yml`

```yaml
name: CI

on:
  push:
    branches: [ main, hml, dev ]
  pull_request:
    branches: [ main, hml, dev ]

jobs:
  backend:
    name: Build + testes
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v5

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.x'

      - name: Restore
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore --configuration Release

      - name: Testes
        run: dotnet test --no-build --configuration Release --verbosity normal

      - name: Scan de dependências vulneráveis
        run: |
          dotnet list package --vulnerable --include-transitive 2>&1 | tee vuln.txt
          ! grep -q "has the following vulnerable packages" vuln.txt
```

**`.github/dependabot.yml`** (README §6.2):
```yaml
version: 2
updates:
  - package-ecosystem: "nuget"
    directory: "/"
    schedule: { interval: "weekly" }
    # EF Core e Pomelo precisam subir JUNTOS (mesmo major). Agrupar evita PRs separados
    # que quebram o build individualmente.
    groups:
      efcore:
        patterns:
          - "Microsoft.EntityFrameworkCore*"
          - "Pomelo.EntityFrameworkCore.MySql*"
  - package-ecosystem: "github-actions"
    directory: "/"
    schedule: { interval: "weekly" }
  - package-ecosystem: "docker"
    directory: "/"
    schedule: { interval: "weekly" }
```

### 14.2 `minha-api-database/.github/workflows/ci.yml`

🔁 **Revisado de novo:** com a mudança da FASE 4 pra SQL puro (dialeto MySQL — `COMMENT`, `AUTO_INCREMENT`), validar contra H2 em memória deixou de fazer sentido (H2 não entende essa sintaxe). O CI agora sobe um **MySQL efêmero** (container de serviço, só na duração do job) e roda `update` de verdade contra ele — um smoke test real, não só "o XML está bem formado". Também precisa instalar o driver do MySQL via **LPM** (Liquibase Package Manager), já que o Community 5.0+ não vem mais com nenhum driver embutido (nem a edição Secure inclui o do MySQL, por causa da licença do driver oficial da Oracle):

```yaml
name: CI

on:
  push:
    branches: [ main, hml, dev ]
  pull_request:
    branches: [ main, hml, dev ]

jobs:
  validar-changelogs:
    name: Validar changelogs do Liquibase
    runs-on: ubuntu-latest

    services:
      mysql:
        image: mysql:8.4
        env:
          MYSQL_ROOT_PASSWORD: root123
          MYSQL_DATABASE: minha_api_ci
        ports:
          - 3306:3306
        options: >-
          --health-cmd="mysqladmin ping -h localhost -uroot -proot123"
          --health-interval=5s
          --health-timeout=5s
          --health-retries=10

    steps:
      - uses: actions/checkout@v5

      - name: Instalar Liquibase
        uses: liquibase/setup-liquibase@v3
        with:
          version: '5.0.4'
          edition: 'community'

      - name: Instalar driver do MySQL via LPM
        run: liquibase lpm add mysql --global

      - name: Aplicar changelogs no MySQL efêmero
        run: |
          liquibase update \
            --changelog-file=changelogs/minha-api.changelog-master.xml \
            --url="jdbc:mysql://localhost:3306/minha_api_ci?allowPublicKeyRetrieval=true&useSSL=false" \
            --username=root \
            --password=root123
```

**`.github/dependabot.yml`:**
```yaml
version: 2
updates:
  - package-ecosystem: "github-actions"
    directory: "/"
    schedule: { interval: "weekly" }
  - package-ecosystem: "docker"
    directory: "/"
    schedule: { interval: "weekly" }
```

### 14.3 `minha-api-frontend/.github/workflows/ci.yml`

> ⏸️ **Adiado pra FASE 15:** o repositório do frontend ainda está vazio (sem `package.json`) — criar esse workflow agora faria o `npm ci` falhar em toda execução. Crie só quando o `ng new` já tiver rodado.

```yaml
name: CI

on:
  push:
    branches: [ main, hml, dev ]
  pull_request:
    branches: [ main, hml, dev ]

jobs:
  frontend:
    name: Build + lint
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v5

      - uses: actions/setup-node@v5
        with:
          node-version: 'lts/*'
          cache: 'npm'
          cache-dependency-path: package-lock.json

      - run: npm ci
      - run: npm run lint
      - run: npm run build
```

**`.github/dependabot.yml`:**
```yaml
version: 2
updates:
  - package-ecosystem: "npm"
    directory: "/"
    schedule: { interval: "weekly" }
  - package-ecosystem: "github-actions"
    directory: "/"
    schedule: { interval: "weekly" }
```

---

Depois de criar os três, em **cada um dos repositórios**: **Settings → Branches → Add rule** em `main` e `hml` → exigir PR + status check do CI verde.

---

# BLOCO E — Frontend Angular

## FASE 15 — Criar o workspace

> 📁 **Local:** repositório **`minha-api-frontend`** (FASE 1.0/1.3). Rode direto na raiz — não precisa mais de `cd frontend`.

```bash
ng new minha-api-frontend --directory . --routing --style=scss --ssr=false --package-manager=npm
```

### 15.1 Estrutura de pastas

```
src/app/
├── core/                       # singletons — importados uma vez
│   ├── interceptors/
│   │   ├── auth.interceptor.ts
│   │   ├── correlation-id.interceptor.ts
│   │   └── error.interceptor.ts
│   ├── guards/
│   │   └── auth.guard.ts
│   ├── services/
│   │   └── api-base.service.ts
│   └── models/
│       ├── problem-details.model.ts
│       └── paged-result.model.ts
├── shared/                     # componentes/pipes reutilizáveis
│   ├── components/
│   └── pipes/
├── features/                   # um módulo por domínio
│   └── produtos/
│       ├── components/
│       ├── services/produto.service.ts
│       └── models/produto.model.ts
└── environments/
```

### 15.2 Tipagem espelhando os contratos da API

```typescript
// core/models/problem-details.model.ts
export interface ProblemDetails {
  type?: string;
  title: string;
  status: number;
  detail?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
}

// core/models/paged-result.model.ts
export interface PagedResult<T> {
  itens: T[];
  pagina: number;
  tamanhoPagina: number;
  totalItens: number;
  totalPaginas: number;
}
```

> Esses tipos são o **espelho** do `ProblemDetails` (RFC 7807) e do `PagedResult<T>` do backend. Manter os dois lados alinhados é o que faz o contrato da API valer a pena.

### 15.3 `environments` (três ambientes — DEV/HML/PROD)

```typescript
// environments/environment.ts (dev)
export const environment = {
  production: false,
  apiUrl: '/api/v1'          // usa o proxy
};

// environments/environment.staging.ts (HML)
export const environment = {
  production: false,
  apiUrl: 'https://api-hml.seudominio.com/api/v1'
};

// environments/environment.prod.ts (PROD)
export const environment = {
  production: true,
  apiUrl: 'https://api.seudominio.com/api/v1'
};
```

No `angular.json`, adicione uma configuração `staging` em `projects.<nome>.architect.build.configurations`, espelhando a `production` mas apontando `fileReplacements` para `environment.staging.ts`. Isso permite `ng build --configuration=staging`.

### 15.4 Proxy para o backend (evita CORS em dev)

**`proxy.conf.json`** (raiz do `minha-api-frontend`):
```json
{
  "/api": {
    "target": "https://localhost:7001",
    "secure": false,
    "changeOrigin": true
  }
}
```

**`angular.json`** → em `serve.options`, adicione `"proxyConfig": "proxy.conf.json"`.

### 15.5 Interceptors

| Interceptor | Responsabilidade |
|---|---|
| `correlation-id` | Gera `X-Correlation-Id` e envia em toda requisição — casa com o middleware do backend e permite rastrear um erro ponta a ponta |
| `auth` | Injeta o `Authorization: Bearer <token>` |
| `error` | Captura `HttpErrorResponse`, lê o `ProblemDetails`, exibe mensagem amigável e loga o `traceId` |

### 15.6 Lint e formatação

```bash
ng add @angular-eslint/schematics
npm install -D prettier eslint-config-prettier
```

**`.prettierrc`:**
```json
{
  "singleQuote": true,
  "printWidth": 120,
  "tabWidth": 2,
  "semi": true,
  "trailingComma": "all"
}
```

**`package.json`** → scripts:
```json
{
  "scripts": {
    "start": "ng serve --proxy-config proxy.conf.json",
    "build": "ng build --configuration production",
    "build:staging": "ng build --configuration staging",
    "lint": "ng lint",
    "format": "prettier --write \"src/**/*.{ts,html,scss}\"",
    "test": "ng test --watch=false --browsers=ChromeHeadless"
  }
}
```

---

## FASE 16 — Integrar frontend e backend

### 16.1 CORS no backend

✅ Já implementado na FASE 11 (`Configuration/CorsConfiguration.cs`, com `Cors:OrigensPermitidas` vindo do `appsettings.Development.json`). Esta seção agora é só uma **verificação**: confirme que `http://localhost:4200` está na lista de origens permitidas do ambiente Development.

> ⚠️ **Nunca** `AllowAnyOrigin()` em produção (README §6.1).

### 16.2 Rodar tudo junto

🔁 **Revisado pra poly-repo:** como backend e frontend agora são repositórios separados (clonados como pastas irmãs, ex: `C:\dev\minha-api-backend` e `C:\dev\minha-api-frontend`), não dá mais pra ter um único script `dev-full.sh` chamando `cd frontend`. O jeito mais simples é **dois terminais**:

**Terminal 1** (dentro de `minha-api-backend`):
```powershell
.\dev.ps1
```

**Terminal 2** (dentro de `minha-api-frontend`):
```powershell
npm start
```

Se quiser automatizar mesmo assim, dá pra criar um script na pasta **pai** (fora dos dois repositórios) que abre ambos em janelas de terminal separadas — mas isso é conveniência local sua, não faz parte de nenhum dos repositórios (evita acoplar os repos um ao outro via script).

### 16.3 Teste ponta a ponta

- Frontend em `http://localhost:4200` lista produtos vindos do MySQL
- Um erro 404 do backend chega ao front como `ProblemDetails` e vira mensagem amigável
- O `X-Correlation-Id` gerado no front aparece nos logs do backend

**Critério de conclusão:** CRUD completo funcionando do Angular até o MySQL, com erro tratado e correlação de log rastreável.

---

## FASE 17 — Autenticação JWT (ponta a ponta)

1. Endpoint `POST /api/v1/auth/login`
2. Hash de senha com **BCrypt.Net-Next** (README §6.2)
3. ⚠️ **Mensagem idêntica** para "usuário não existe" e "senha errada" (anti-enumeração)
4. Emissão de JWT com claims (`sub`, `role`, `tenantId`)
5. `JwtBearer` configurado no backend
6. Rate limiting **específico e mais rígido** no endpoint de login (anti força bruta)
7. `auth.interceptor` + `auth.guard` no Angular
8. ⚠️ **Validação de posse do recurso** na Application usando o `sub`/`tenantId` do token — **defesa contra IDOR** (README §6.3). 💡 Se o template virar multi-tenant, o EF Core oferece **Global Query Filters** (`HasQueryFilter(e => e.TenantId == tenantAtual)`) como camada extra de defesa por cima da validação explícita — avalie junto com a decisão da pendência #4

```bash
cd src/MinhaApi.Api
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
cd ../MinhaApi.Infra
dotnet add package BCrypt.Net-Next
```

---

# BLOCO F — Produção e Empacotamento

## FASE 18 — Dockerfile da API (multi-stage, non-root)

**`src/MinhaApi.Api/Dockerfile`** (README §6.5):

```dockerfile
# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

COPY *.sln Directory.Build.props ./
COPY src/ ./src/
RUN dotnet restore src/MinhaApi.Api/MinhaApi.Api.csproj
RUN dotnet publish src/MinhaApi.Api/MinhaApi.Api.csproj \
    -c Release -o /app --no-restore

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# usuário non-root
RUN adduser --disabled-password --gecos '' --uid 1001 appuser
COPY --from=build --chown=appuser:appuser /app .
USER appuser

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "MinhaApi.Api.dll"]
```

> Este Dockerfile continua útil mesmo em máquinas locais sem suporte a Docker: ele roda no **runner do GitHub Actions** (que tem Docker nativo) durante o CI/CD, e no **Azure App Service** se você optar por "Web App for Containers" em vez de deploy direto do binário publicado (ver FASE 21).

**`.dockerignore`:**
```
bin/
obj/
.git/
**/appsettings.Development.json
```

### 18.1 `docker-compose.override.yml` (simular produção localmente)

Permite rodar API + banco containerizados, sem perder o modo de debug nativo do dia a dia. (Só aplicável em máquinas com suporte a Docker/virtualização — ver nota da FASE 0.2.)

---

## FASE 19 — Endurecimento final de segurança

Percorra os checklists do README §6 marcando item por item:

- [ ] §6.2 — Bloco 1 completo (9 itens)
- [ ] §6.3 — Bloco 2 completo (7 itens)
- [ ] §6.4 — LGPD (4 itens)
- [ ] §6.5 — Imagem Docker (3 itens)
- [ ] §7 — Cuidados de EF Core/produção (7 itens)

**Validações práticas recomendadas** (rodar em cada repositório correspondente):
```bash
# No minha-api-backend:
dotnet list package --vulnerable --include-transitive
docker scout cves minha-api:latest      # ou trivy image minha-api:latest
grep -rniE "FromSqlRaw|ExecuteSqlRaw" src/   # cada ocorrência precisa ser revisada (SQL Injection)
grep -rn "EnableSensitiveDataLogging" src/   # só pode existir sob IsDevelopment()

# No minha-api-frontend:
npm audit
```

---

## FASE 20 — Empacotar como `dotnet new` template

Este é o objetivo final: transformar tudo isso em algo reutilizável em um comando.

> 🔁 **Revisado pra poly-repo:** o mecanismo `dotnet new` só faz sentido pro **`minha-api-backend`** (é uma ferramenta do ecossistema .NET, não existe equivalente nativo pro Angular nem pro Liquibase). Os símbolos `IncluirFrontend`/`IncluirJobs` do template original não fazem mais sentido, já que frontend é um repositório totalmente separado agora — o fluxo de "usar isso pra um cliente novo" está documentado por completo no `docs/NOVO-CLIENTE-CHECKLIST.md` (clonar os 3 repositórios + rodar `rename-project.ps1` em cada um), que substitui a necessidade do parâmetro `IncluirFrontend`.

### 20.1 `.template.config/template.json` (só no `minha-api-backend`)

```json
{
  "$schema": "http://json.schemastore.org/template",
  "author": "Ceccon",
  "classifications": [ "Web", "API", "Clean Architecture" ],
  "identity": "Ceccon.ApiTemplate",
  "name": "API Base Template Backend (camadas + EF Core + MySQL)",
  "shortName": "cecconapi",
  "sourceName": "MinhaApi",
  "preferNameDirectory": true,
  "symbols": {
    "IncluirJwt":  { "type": "parameter", "datatype": "bool", "defaultValue": "true" },
    "IncluirJobs": { "type": "parameter", "datatype": "bool", "defaultValue": "false" }
  }
}
```

> **`sourceName`** é a peça-chave: tudo que se chama `MinhaApi` (namespaces, pastas, arquivos) será renomeado automaticamente — isso inclui o `MinhaApiDbContext`, que vira `SistemaFinanceiroDbContext` num projeto derivado. Alternativa/complemento mais simples, e a que temos usado na prática: o script `rename-project.ps1` (ver `docs/`), que troca pastas/arquivos/conteúdo de um nome pro outro depois do clone — funciona nos três repositórios, não só no backend, sem precisar do mecanismo de templates do `dotnet new`.

### 20.2 Instalar e usar (backend)

```bash
dotnet new install ./caminho-do-template
dotnet new cecconapi -n SistemaFinanceiro --IncluirJwt true
```

Pro banco e frontend, siga o `docs/NOVO-CLIENTE-CHECKLIST.md` (clone + `rename-project.ps1`).

### 20.3 Documentação final em `docs/` (dentro do `minha-api-backend`)

- `README.md` (o documento de contexto — já existe)
- `PLANO-DE-ACAO.md` (este documento — já existe)
- `ARQUITETURA.md` — diagrama e regra de dependência
- `DECISOES.md` — ADRs (Architecture Decision Records) do que foi adiado e por quê. **Primeiro ADR a registrar: "NHibernate → EF Core (out/2026)"** — motivo (adoção de mercado), custo (reescrita da Infra/IoC), e a decisão de manter Liquibase como dono do schema
- `CONTRIBUINDO.md` — padrões de commit, branch e PR
- `ATIVAR-DEPLOY-AZURE.md` — runbook de ativação do deploy (ver FASE 21) — já existe
- `NOVO-CLIENTE-CHECKLIST.md` — checklist de como clonar os 3 repositórios pra um projeto novo — já existe
- `rename-project.ps1` — script de renomeação pós-clone — já existe

---

# BLOCO G — Deploy Multi-Ambiente (Azure)

## FASE 21 — Deploy real em DEV / HML / PROD via Azure 🔴 (adormecida até ativação)

> **Esta fase começa inativa por decisão consciente.** O repositório atual é uma base/template local — ele não será hospedado ele mesmo. Esta fase é o **roteiro pronto** para quando houver um projeto derivado real (com cliente, com necessidade de ambiente ativo). Até lá, tudo aqui fica versionado e documentado, mas sem custo e sem recursos provisionados.

### 21.0 🔴 Decisão de negócio pendente (antes de ativar de verdade)

Antes de provisionar qualquer recurso Azure para um projeto real, decida:

- **Modelo A — SaaS hospedado por você:** você mantém a infraestrutura, cobra assinatura do cliente. Implica decidir também se é **multi-tenant** (um deploy só, múltiplos clientes — IDOR fica ainda mais crítico, ver README §6.3) ou um deploy isolado por cliente.
- **Modelo B — Entrega para o cliente hospedar:** você entrega o software (imagem Docker da FASE 18 + instruções), o cliente é responsável pela própria infraestrutura. Vira projeto/licença, não assinatura recorrente.

Essa decisão muda o desenho de multi-tenancy e não precisa ser tomada agora — mas **precisa** ser tomada antes do primeiro cliente real, porque afeta como o IDOR e o isolamento de dados são desenhados desde a FASE 6/7.

### 21.1 Recursos Azure a provisionar (por ambiente ativado)

| Peça | Serviço Azure | Observação |
|---|---|---|
| API (.NET) | Azure App Service (Linux, Basic B1 pra começar) | Um por ambiente: `minhaapi-api-dev`, `-hml`, `-prod` |
| Banco (MySQL) | Azure Database for MySQL Flexible Server (Burstable B1MS) | Um servidor por ambiente, ou um servidor com 3 schemas lógicos (mais barato, menos isolado) |
| Frontend (Angular) | Azure Static Web Apps | Tier gratuito atende bem no início |

> Custo aproximado por ambiente ativo, após o período gratuito da Azure (12 meses): ~US$12–15/mês (MySQL Flexible Server B1MS) + variável para o App Service conforme região/SO (conferir sempre na [Calculadora de Preços da Azure](https://azure.microsoft.com/pricing/calculator/)). Static Web Apps no tier gratuito não soma custo.

> 🔁 **Azure MySQL Flexible Server + EF Core/Pomelo:** o Flexible Server exige **SSL por padrão**. Na connection string de HML/PROD adicione `SslMode=Required;` (formato MySqlConnector). Atualize também o `new MySqlServerVersion(...)` do `AddDbContext` (FASE 9.5) para a versão que o servidor Azure estiver rodando — idealmente lida de configuração, não fixa no código, se os ambientes divergirem.

### 21.2 Estratégia de branches → ambiente (definida na FASE 1.4)

```
dev    → push direto ou PR de feature/*        → auto-deploy em minhaapi-api-dev
hml    → PR de dev → hml (promoção manual)     → auto-deploy em minhaapi-api-hml
main   → PR de hml → main (aprovação obrigatória) → auto-deploy em minhaapi-api-prod
```

### 21.3 `cd.yml` por repositório — pipelines de deploy (adormecidos)

🔁 **Revisado pra poly-repo:** cada repositório faz deploy do **próprio** recurso Azure, de forma independente — não existe mais um `cd.yml` único que builda API e aplica migration juntos.

**`minha-api-backend/.github/workflows/cd.yml`** (deploy da API no App Service):
```yaml
name: CD - Deploy Azure App Service

on:
  workflow_dispatch:
    inputs:
      environment:
        description: "Ambiente de destino"
        required: true
        type: choice
        options: [development, homologacao, production]
  # push:
  #   branches: [dev, hml, main]   # descomentar quando ativar (docs/ATIVAR-DEPLOY-AZURE.md)

permissions:
  id-token: write
  contents: read

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    environment: ${{ inputs.environment || 'development' }}
    steps:
      - uses: actions/checkout@v5
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "10.x"
      - run: dotnet restore
      - run: dotnet publish src/MinhaApi.Api/MinhaApi.Api.csproj -c Release -o ./publish
      - uses: azure/login@v2
        with:
          client-id: ${{ secrets.AZURE_CLIENT_ID }}
          tenant-id: ${{ secrets.AZURE_TENANT_ID }}
          subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
      - uses: azure/webapps-deploy@v3
        with:
          app-name: ${{ secrets.AZURE_APP_SERVICE_NAME }}
          package: ./publish
```

**`minha-api-database/.github/workflows/cd.yml`** (aplica migrations — roda **antes** do deploy do backend em produção):
```yaml
name: CD - Aplicar Migrations

on:
  workflow_dispatch:
    inputs:
      environment:
        description: "Ambiente de destino"
        required: true
        type: choice
        options: [development, homologacao, production]
  # push:
  #   branches: [dev, hml, main]

jobs:
  migrate:
    runs-on: ubuntu-latest
    environment: ${{ inputs.environment || 'development' }}
    steps:
      - uses: actions/checkout@v5
      - name: Instalar Liquibase
        uses: liquibase/setup-liquibase@v3
        with:
          version: '5.0.4'
          edition: 'community'
      - name: Aplicar migrations
        run: |
          liquibase --url="${{ secrets.LIQUIBASE_URL }}" \
                    --username="${{ secrets.LIQUIBASE_USER }}" \
                    --password="${{ secrets.LIQUIBASE_PASSWORD }}" \
                    update
```

**`minha-api-frontend/.github/workflows/cd.yml`** (deploy no Azure Static Web Apps):
```yaml
name: CD - Deploy Static Web App

on:
  workflow_dispatch:
    inputs:
      environment:
        description: "Ambiente de destino"
        required: true
        type: choice
        options: [development, homologacao, production]
  # push:
  #   branches: [dev, hml, main]

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    environment: ${{ inputs.environment || 'development' }}
    steps:
      - uses: actions/checkout@v5
      - uses: Azure/static-web-apps-deploy@v1
        with:
          azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN }}
          repo_token: ${{ secrets.GITHUB_TOKEN }}
          action: "upload"
          app_location: "/"
          output_location: "dist"
```

> Os três arquivos já existem em cada repositório desde já, com o gatilho `workflow_dispatch` (manual), propositalmente inertes — não disparam sozinhos e não geram ruído no GitHub Actions enquanto não forem ativados. Quando ativar (FASE 21.4), a ordem importa: dispare **primeiro** o `cd.yml` do banco (migrations), **depois** o do backend.

### 21.4 Runbook de ativação

O passo a passo completo (provisionar recursos, configurar OIDC, criar GitHub Environments e secrets, preencher connection strings, ativar o gatilho automático) está em **`docs/ATIVAR-DEPLOY-AZURE.md`**. Siga-o quando a decisão da FASE 21.0 estiver tomada e houver um projeto real pra ativar.

### 21.5 Pendências que ainda vão precisar de ajuste na hora de ativar

- [x] ~~Instalação do Liquibase no runner do GitHub Actions~~ — resolvido no `cd.yml` do `minha-api-database` (21.3)
- [ ] `appsettings.Staging.json`/`appsettings.Production.json` com conteúdo real de CORS/Serilog (a FASE 11 já cria a estrutura, mas o conteúdo fino é revisado na ativação)
- [ ] Decidir banco físico por ambiente vs. servidor único com múltiplos schemas (impacta custo e isolamento)
- [ ] Os **GitHub Environment Secrets** agora precisam ser cadastrados em **cada um dos três repositórios** separadamente (cada um só tem acesso aos secrets que ele mesmo precisa — o backend não precisa saber a senha do banco, por exemplo, só a connection string via `LIQUIBASE_URL` fica isolada no repo do banco)
- [ ] `SslMode=Required` na connection string de HML/PROD e `MySqlServerVersion` conferido contra o servidor Azure (ver nota em 21.1)

**Critério de conclusão (só se aplica quando ativada):** CRUD completo acessível publicamente em DEV, com deploy automático funcionando ao dar push em `dev`; promoção manual testada até HML; PROD com aprovação obrigatória configurada e testada ao menos uma vez.

---

# Resumo Visual do Caminho

```
FASE 0   Instalações ────────────── 2–3h    🔴 Docker Desktop no boot (ou MySQL nativo)
FASE 1   3 Repositórios GitHub ──── 1–2h    poly-repo: backend/database/frontend, branches dev/hml/main
FASE 2   Solution + camadas ─────── 2h      ⚠️ referências = a arquitetura
FASE 3   MySQL em Docker/nativo ─── 1h      restart: always (ou serviço nativo)
FASE 4   Liquibase ──────────────── 1–2h    ⚠️ rollback obrigatório; dono único do schema
FASE 5   Domain ─────────────────── 2h
FASE 6   DataTransfer ───────────── 1h      ⚠️ anti mass assignment
FASE 7   CrossCutting ───────────── 3h      exceptions + middlewares
FASE 8   Application ────────────── 3h
FASE 9   Infra + EF Core ────────── 4h      ⚠️ DbContext Scoped, Include, concurrency token (🔁 trocado de NHibernate)
FASE 10  IoC ────────────────────── 1h      ⚠️ ValidateScopes
FASE 11  API ────────────────────── 4h      pipeline + multi-ambiente (3 appsettings)
FASE 12  Testes ─────────────────── 3h      ⚠️ escopo revisado: só Domain+Application por ora; AwesomeAssertions no lugar do FluentAssertions
FASE 13  EditorConfig ───────────── 2h      ✅ decisões fechadas (ver tabela na fase)
FASE 14  CI + Dependabot ────────── 2h      ✅ gatilhos em dev/hml/main, actions/checkout@v5
FASE 15  Angular ────────────────── 4h      3 environments (dev/staging/prod)
FASE 16  Integração front/back ──── 3h
FASE 17  JWT ponta a ponta ──────── 4h      ⚠️ IDOR + anti-enumeração
FASE 18  Dockerfile ─────────────── 2h      multi-stage + non-root
FASE 19  Hardening ──────────────── 3h
FASE 20  dotnet new template ────── 3h
FASE 21  Deploy Azure (DEV/HML/PROD) 4–6h   🔴 adormecida até ativação (BLOCO G)
```

---

# Marcos (para não perder o ritmo)

| Marco | Você consegue... | Ao final da |
|---|---|---|
| **M1 — Ambiente pronto** | Rodar todos os comandos de verificação | FASE 0 |
| **M2 — Esqueleto vivo** | `dotnet build` OK com todas as camadas ligadas | FASE 2 |
| **M3 — Banco funcionando** | Criar/reverter tabelas via Liquibase | FASE 4 |
| **M4 — API respondendo** | CRUD completo no Swagger contra o MySQL | FASE 11 |
| **M5 — Rede de segurança** | `dotnet test` verde e CI rodando no PR | FASE 14 |
| **M6 — Stack completa** | Angular conversando com a API | FASE 16 |
| **M7 — Template pronto** | `dotnet new cecconapi -n NovoProjeto` | FASE 20 |
| **M8 — No ar de verdade** | Deploy automático funcionando em DEV/HML/PROD na Azure | FASE 21 (quando ativada) |

---

# Duas Coisas Que Não Podem Ser Adiadas

Se em algum momento o tempo apertar e você precisar cortar escopo, **estes dois não entram no corte** — são os que dão prejuízo silencioso:

1. **`DbContext` como Scoped** (FASE 9) — errar aqui vaza dados entre usuários e só aparece em produção (é o padrão do `AddDbContext`; o risco é alguém "otimizar" registrando como Singleton)
2. **User Secrets desde o primeiro commit** (FASE 11) — segredo que entra no histórico do Git precisa ser rotacionado, não apenas removido

---

# Pendências Que Voltam Para Você

| # | Pendência | Fase |
|---|---|---|
| 1 | ~~🔴 Preencher a tabela de decisões do EditorConfig~~ ✅ Resolvido | 13 |
| 2 | Confirmar SO de trabalho (Windows nativo vs WSL2) — define `dev.sh` ou `dev.ps1` | 0 |
| 3 | Confirmar LTS vigente do .NET e do Node no momento da instalação | 0 |
| 4 | Definir se o template terá multi-tenant desde o início (afeta o desenho do IDOR) | 17 |
| 5 | Escolher Hangfire ou Quartz.NET quando ativar a camada Jobs | futuro |
| 6 | 🔴 Decidir modelo de negócio: SaaS hospedado por você vs. entrega para o cliente hospedar — afeta ativação da FASE 21 e o desenho de multi-tenancy (ligado ao item 4) | 21 |
| 7 | Provisionar recursos Azure reais (App Service, MySQL Flexible Server, Static Web Apps) quando houver um projeto/cliente real — roteiro em `docs/ATIVAR-DEPLOY-AZURE.md` | 21 |
| 8 | 🔁 **Conferir no NuGet se o `Pomelo.EntityFrameworkCore.MySql` 10.x já saiu estável** antes de instalar; se não, ficar com EF Core 9.x + Pomelo 9.x em `net10.0` (ver FASE 9.0) e reavaliar quando a 10.x sair | 9 |
| 9 | 🔁 Se a FASE 9 já foi executada com NHibernate: rodar o checklist da FASE 9.6 e registrar o ADR da troca em `docs/DECISOES.md` | 9 / 20 |
