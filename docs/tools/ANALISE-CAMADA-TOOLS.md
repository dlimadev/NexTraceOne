# Análise da Camada `tools/` — NexTraceOne

> Análise realista e objetiva da camada de ferramentas do NexTraceOne: mapeamento funcional, prontidão, lacunas, melhorias e comparação de mercado.
> Data: 2026-06-22 · Branch: `claude/nextraceone-tools-analysis-5wiyk3`

---

## 0. Método e ressalva de ambiente

- A análise é **estática + funcional por leitura de código** (todos os arquivos da camada `tools/` foram lidos integralmente).
- ⚠️ **Não foi possível compilar/rodar os projetos .NET** neste ambiente: não há `dotnet` SDK instalado (apenas Node 22). Portanto a afirmação "100% funcional" **não pôde ser verificada por build/test reais**. Os testes existentes (`tests/tools/`, `tests/platform/NexTraceOne.CLI.Tests/`) foram inventariados mas não executados.
- A extensão VS Code (TypeScript) pôde ser inspecionada e tem `out/` já compilado no repo.

Para validação real, rodar na máquina/CI com .NET 10:
```bash
dotnet build tools/NexTrace.Sdk/NexTrace.Sdk.csproj
dotnet build tools/NexTraceOne.CLI/NexTraceOne.CLI.csproj
dotnet test tests/tools/NexTrace.Sdk.Tests/
dotnet test tests/platform/NexTraceOne.CLI.Tests/
cd tools/ide-extensions/vscode && npm ci && npm run build && npm test
```

---

## 1. Inventário da camada

| Componente | Tipo | Linguagem | Em CI/solution? | Testes |
|---|---|---|---|---|
| `NexTrace.Sdk/` | Cliente .NET (NuGet) | C# / net10.0 | ✅ Sim | ✅ 25 testes |
| `NexTraceOne.CLI/` | CLI `nex` | C# / net10.0 | ✅ Sim | ✅ 106 testes |
| `ide-extensions/vscode/` | Extensão VS Code | TypeScript | ⚠️ build npm próprio | ⚠️ 1 arquivo (só `utils`) |
| `ide-extensions/visualstudio/` | Extensão VS 2022 (VSIX) | C# / .NET FW 4.8 | ❌ **fora da solution** | ❌ Nenhum |
| `github-action/` | Composite action | bash | ✅ usável | ❌ Nenhum |
| `count-dbcontexts.sh` | Script utilitário | bash | n/a | n/a |

---

## 2. Mapeamento funcional por componente

### 2.1 NexTrace.Sdk (cliente .NET oficial)

Ponto de entrada `NexTraceSdkClient` agrega 5 sub-clientes:

| Sub-cliente | Operações | Estado |
|---|---|---|
| `Services` (ServiceCatalog) | get/list/create/update/delete + scaffold | ✅ Completo |
| `Contracts` | get/create/update/delete/diff/verify/sync/migration-patch | ✅ Completo |
| `Changes` | confidence score, status by-sha, summary, DORA metrics, promotion | ✅ Completo |
| `Compliance` | coverage-matrix por standard | ✅ Mínimo (1 método) |
| `Integrations` | search, detail, list contracts, generate-code, register consumer, impact, **orquestração `GenerateConsumerClientAsync`** | ✅ Robusto |

**Destaques de qualidade:**
- Resiliência via `Microsoft.Extensions.Http.Resilience` (retry exponencial em 5xx/timeout), configurável.
- `OpenApiSpecFilter` — filtra paths/operationIds e **remove schemas órfãos** antes de gerar código (suporta OpenAPI v2/v3, JSON/YAML, trata BOM). Implementação cuidadosa.
- Construtor `internal` com `HttpClient` injetável para testes (`InternalsVisibleTo`).
- DTOs com fallback de nomes (`serviceId`/`id`, `contracts`/`items`) — tolerante a variações da API.

### 2.2 NexTraceOne.CLI (`nex`)

14 comandos de topo, padrão consistente (System.CommandLine + Spectre.Console, exit codes, `--format text|json`):

| Comando | Fluxo | Backend |
|---|---|---|
| `validate` | valida manifesto de contrato **offline** (regras CLI001–CLI009, semver, métodos HTTP) | local |
| `catalog list/get` | lista/detalha serviços | REST |
| `contract verify/diff/changelog/sync/list/migrate` | governança de contrato | REST |
| `change report/blast-radius/list/promote` | change intelligence | REST |
| `incident list/get/report` | incidentes | REST |
| `confidence score` | gate de confidence em CI (`--min-score`) | REST |
| `compliance check` | cobertura de standard | REST |
| `integration scaffold/register` | **gera cliente consumidor + manifesto `nexone-integration.json` + análise de impacto** | via SDK |
| `report dora/changes-summary` | relatórios DORA | REST |
| `scaffold service/templates/init/register` | golden-path scaffolding | REST |
| `mcp tools/configure/call` | integração MCP (JSON-RPC 2.0; gera config VS Code/Claude Desktop) | REST |
| `health` | conectividade | REST |
| `config set/get` | config local `~/.nex/config.json` | local |
| `completion` | scripts de shell completion | local |

Resolução de configuração em cascata: flag explícita → env var (`NEX_API_URL`, `NEXTRACE_TOKEN`, `NEX_ENVIRONMENT`, `NEX_PERSONA`) → `~/.nex/config.json` → default.

### 2.3 Extensão VS Code (`nextraceone-copilot` v0.6.0)

Bem mais rica que o README sugere. Funcionalidades:
- **Chat panel** lateral (webview) com histórico, render de blocos de código, "Insert at Cursor"/Copy.
- **Chat Participant `@nextraceone`** (VS Code ≥1.90) com slash commands (`/service`, `/change`, `/contract`, `/incident`, `/report`, `/blast-radius`, `/scaffold`, `/generate`, `/migrate`).
- **LanguageModel Tools** (VS Code ≥1.113): `get_service`, `get_contract`, `blast_radius`, `get_incident` — usáveis pelo Copilot.
- **Service Catalog Tree View** com drill-down e ações ("Open in Dashboard", "Ask AI").
- **Scaffold wizard** multi-step (template → nome → team/domain → output → escreve arquivos → registra no catálogo).
- **MCP configure** (global/workspace), **status bar**, **migration patch** wizard, **apply code**.
- Backend único: `POST /api/v1/ai/ide/query` (governado/auditado).

### 2.4 Extensão Visual Studio 2022

Estrutura completa (AsyncPackage, 5 comandos, Tool Windows de Chat e Catalog, Options page, **Error List provider** que faz health-check de governança). **Porém:** o README marca "em desenvolvimento" e o projeto **não está incluído na solution** (`NexTraceOne.sln`) → **não é buildado no CI**. Risco de bit-rot.

### 2.5 GitHub Action — `nexone-change-confidence-gate`

Composite action (bash + `curl` + `jq`) que consulta `/api/v1/changes/{release}/confidence` e falha o workflow se score < threshold. Tem retry com backoff, outputs `score`/`tier`. Sólida, mas é a **única** action e sem `branding` para o Marketplace.

---

## 3. Matriz de prontidão

| Item | Estado | Nota |
|---|---|---|
| SDK — cobertura de domínios | 🟢 Pronto | Compliance é mínimo (só coverage) |
| SDK — resiliência/testes | 🟢 Pronto | 25 testes, retry configurável |
| CLI — comandos | 🟢 Pronto | 14 comandos, padrão consistente |
| CLI — testes | 🟡 Parcial | 8/14 comandos com teste; faltam mcp, confidence, compliance, config, health, scaffold |
| CLI — resiliência HTTP | 🟡 Inconsistente | Só `integration` usa SDK resiliente; demais usam `HttpClient` cru |
| VS Code ext | 🟢 Funcional | Testes só de `utils`; README desatualizado |
| VS 2022 ext | 🔴 Risco | Fora da solution/CI; "em desenvolvimento" |
| GitHub Action | 🟢 Funcional | Só 1 action; sem branding |
| Consistência de defaults | 🔴 Bug | CLI default `:8080` vs resto `:5000` |

---

## 4. Bugs e inconsistências concretas

1. **🔴 URL default divergente do CLI.** `CliConfig.ResolveUrl` e `ConfigCommand` usam `http://localhost:8080`, enquanto ApiHost (CLAUDE.md), SDK (`NexTraceSdkOptions`) e ambas as extensões usam `:5000`. Primeiro uso do CLI falha "out of the box" contra um servidor local padrão. → **Corrigir para `:5000`.**

2. **🟡 Resiliência inconsistente no CLI.** `CatalogApiClient`, `McpCommand`, `ConfidenceCommand`, `HealthCommand` criam `HttpClient` simples (sem retry). Apenas `IntegrationCommand` reaproveita o SDK resiliente. → Padronizar via SDK ou um factory comum.

3. **🟡 README do VS Code defasado.** Lista só "Ask AI" e "Configure" e diz `^1.85.0`; o `package.json` exige `^1.90.0` e a extensão tem chat panel, catalog tree, scaffold, MCP, LM tools, migration. → Atualizar doc.

4. **🟡 Extensão VS 2022 fora da solution.** Não compila no CI → regressões silenciosas. → Incluir na solution com `Condition` de plataforma (Windows) ou pipeline VSIX dedicado.

5. **🟡 Cobertura de testes desigual no CLI.** 6 comandos sem teste (incl. `mcp call`, que constrói payload JSON-RPC, e `scaffold`).

6. **🟢 (menor) Empacotamento NuGet do SDK incompleto.** `NexTrace.Sdk.csproj` tem `PackageId`/`Version`/`Description`/`Authors`, mas falta `PackageReadmeFile`, `RepositoryUrl`, `PackageLicenseExpression`, `PackageTags`, símbolos (`snupkg`). README diz "dotnet add package" — implica publicação.

7. **🟢 (menor) Versões hardcoded** nas extensões (`clientVersion: '0.6.0'`/`'0.5.0'` espalhados no `extension.ts`) — derivar de `package.json`.

8. **🟢 (menor) `--lang` do `integration scaffold`** só aceita `csharp` (o gerador backend só suporta C#) — documentado, mas limita adoção poliglota.

---

## 5. Melhorias e novas funcionalidades sugeridas

**Curto prazo (consistência / 100% funcional):**
- Corrigir default `:8080 → :5000` (#1).
- Factory HTTP resiliente único para todos os comandos do CLI (#2).
- Atualizar READMEs (VS Code #3) e fechar lacunas de teste (#5).
- Decidir destino da extensão VS 2022: entrar no CI ou marcar explicitamente como experimental fora de `tools/` ativos (#4).

**Médio prazo (valor):**
- **Geração de cliente multilíngua** no `integration scaffold` (TypeScript/Java/Python/Go) — hoje só C#. Pode delegar a Kiota/openapi-generator.
- **`nex auth login`** (device-code/OIDC) em vez de colar token manual — UX e segurança.
- **Action de validação de contrato** (`nex validate` como GitHub Action) e **action de coverage de compliance**, formando um conjunto além do confidence gate.
- **MCP server local (`nex mcp serve`)** expondo catálogo/contratos/mudanças como ferramentas MCP — alinhado à tendência Backstage MCP / Codex CLI (ver §6).
- **Saída SARIF** no `validate`/`contract verify` para anotações nativas no GitHub.
- **Telemetria/diagnóstico** (`nex doctor`) verificando versão, conectividade, token, capabilities do tenant.

**Longo prazo:**
- Plugin para JetBrains (Rider/IntelliJ) — paridade com VS Code/VS.
- Distribuição: Homebrew/winget/`dotnet tool install -g nex`, e publicação do SDK no NuGet.org com SemVer + changelog.

---

## 6. Comparação de mercado

A camada `tools/` posiciona o NexTraceOne na categoria **Internal Developer Platform (IDP) / Developer Portal**, competindo conceitualmente com Backstage, Port, Cortex e OpsLevel.

| Capacidade | NexTraceOne | Backstage | Port | Cortex / OpsLevel |
|---|---|---|---|---|
| Catálogo de serviços | ✅ (CLI + ext + SDK) | ✅ (referência, ~89% share) | ✅ | ✅ |
| Scaffolder / golden paths | ✅ (CLI + ext wizard + templates) | ✅ (Scaffolder, padrão) | ✅ | parcial |
| Gate em CI/CD | ✅ (confidence gate + `confidence --min-score`) | via plugins | ✅ | ✅ (checks/scorecards) |
| Integração MCP / IA | ✅ (MCP config + chat + LM tools) | ✅ (MCP server recente) | parcial | parcial |
| Geração de cliente a partir de contrato | ✅ (integration scaffold) | ⚠️ via plugin | ⚠️ | ❌ |
| Change Intelligence / DORA | ✅ (nativo, diferencial) | via plugins | parcial | scorecards |
| IDE extensions oficiais | ✅ VS Code + VS 2022 | comunidade | limitado | limitado |

**Leitura objetiva:**
- **Diferenciais reais do NexTraceOne:** o eixo *Change Intelligence* (confidence score, blast radius, promoção, DORA) integrado ao catálogo e a **geração de cliente consumidor dirigida por contrato** não são "out of the box" nos concorrentes — costumam exigir plugins/custom. As extensões IDE oficiais com IA governada também são um plus.
- **Tendência de mercado a seguir:** Backstage agora expõe **MCP server** (catálogo, scaffolder, TechDocs) consumível por CLIs/agentes (ex.: Codex CLI executando golden paths por linguagem natural). O NexTraceOne já tem `nex mcp` no lado cliente; o próximo passo natural é **`nex mcp serve`** e/ou consolidar o `/api/v1/ai/mcp` como superfície de primeira classe.
- **Onde concorrentes ganham:** *time-to-value* (OpsLevel: catálogo em 30–45 dias com auto-discovery), **auto-discovery** de serviços via repositórios/CI (reduz manutenção manual do catálogo — dor conhecida do Cortex) e maturidade do ecossistema de plugins (Backstage).
- **Recomendação de produto:** investir em (a) **auto-discovery** de serviços (scanner de repo/CI) para reduzir entrada manual; (b) **multilíngua** na geração de clientes; (c) **MCP server** como superfície primária para agentes; (d) conjunto de **GitHub Actions** (validate, compliance, confidence) para "shift-left" da governança.

---

## 7. Roadmap priorizado

| Prio | Ação | Esforço | Impacto |
|---|---|---|---|
| P0 | Corrigir default `:8080→:5000` | XS | Alto (UX 1º uso) |
| P0 | Decidir CI da extensão VS 2022 | S | Alto (evita bit-rot) |
| P1 | Factory HTTP resiliente no CLI | S | Médio |
| P1 | Testes para os 6 comandos sem cobertura | M | Médio |
| P1 | Atualizar READMEs (VS Code/VS) | XS | Médio |
| P2 | `nex auth login` (OIDC/device-code) | M | Alto |
| P2 | Geração de cliente multilíngua | L | Alto |
| P2 | `nex mcp serve` (MCP server) | L | Alto (estratégico) |
| P3 | Actions validate/compliance + SARIF | M | Médio |
| P3 | Auto-discovery de serviços | XL | Alto (produto) |
| P3 | Plugin JetBrains | XL | Médio |

---

## Fontes (comparação de mercado)
- [Backstage Software Catalog](https://backstage.io/docs/features/software-catalog/)
- [Backstage Scaffolder / Software Templates](https://backstage.io/)
- [Codex CLI + Backstage MCP (golden paths)](https://codex.danielvaughan.com/2026/05/17/codex-cli-backstage-mcp-developer-portal-golden-path-templates-catalog-integration/)
- [Platform Engineering Tools Compared (Encore)](https://encore.dev/articles/platform-engineering-tools)
- [Top Backstage Alternatives 2025 (Port)](https://www.port.io/blog/top-backstage-alternatives)
- [OpsLevel vs Cortex](https://www.opslevel.com/opslevel-vs-cortex-io)
</invoke>
