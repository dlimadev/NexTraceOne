# Estratégia de Auditoria — Estado Real do NexTraceOne

> **Data:** Agosto 2026
> **Âmbito:** Determinar, com evidência executável, o estado real do produto e o que falta para o fechar.
> **Coletor de evidência:** `scripts/quality/audit-baseline.sh`
> **Baseline medido:** [BASELINE-2026-08-21.md](./BASELINE-2026-08-21.md)

---

## 1. O problema com "validar ficheiro a ficheiro"

O repositório tem **4.402 ficheiros C#** e **1.191 ficheiros TS/TSX**. Ler cada um
isoladamente custa meses e produz pouco sinal: um handler correto lido sozinho não
revela que o endpoint que o deveria expor não existe, que o repositório que injeta
devolve coleções vazias, ou que a migration da sua tabela nunca foi criada.

**Os defeitos deste projeto não vivem dentro dos ficheiros — vivem nas junções entre eles.**

A estratégia abaixo cobre na mesma 100% dos ficheiros, mas organiza-os por significado
em vez de por caminho: cada ficheiro pertence a uma *fatia vertical*, e todo o ficheiro
que não pertence a nenhuma é sinalizado como órfão. A cobertura é total; o custo não é.

---

## 2. A unidade de auditoria: a fatia vertical

A unidade não é a classe nem o ficheiro. É a **fatia** — o caminho completo de uma
capacidade, do clique do utilizador à linha na base de dados:

```
Página FE → chave i18n → cliente API FE → endpoint → Command/Query → Validator
   → Handler → Repositório (iface) → Repositório (impl EF) → EF config → Migration
   → Teste de handler → Teste de FE
```

**13 elos.** Uma fatia só está *pronta* quando os 13 existem e são reais. Cada elo em
falta ou falso tem um veredito distinto:

| Sintoma | Veredito | Ação |
|---|---|---|
| Endpoint sem handler | endpoint morto | remover ou implementar |
| Handler sem endpoint | capacidade inacessível | expor ou remover |
| `IXxxRepository` → `NullXxxRepository` | **bug** (CLAUDE.md §21) | implementar EF Core |
| `IXxxReader` → `NullXxxReader` | legítimo, phase-gated | registar no roadmap |
| Entidade sem EF config | não persiste | configurar |
| EF config sem migration | schema não existe em produção | gerar migration |
| Handler sem teste | não verificado | escrever teste |
| Página FE só com stub MSW | não ligada ao backend | ligar ao cliente real |

Isto transforma "auditar 5.593 ficheiros" em "verificar N fatias com 13 critérios
binários cada" — mecanizável, auditável e paralelizável.

---

## 3. Pirâmide de evidência

Quatro níveis, do mais barato e objetivo ao mais caro e subjetivo. **Nunca subir de
nível sem esgotar o anterior** — cada nível reduz o volume que o seguinte precisa de olhar.

### Nível 0 — Gates executáveis *(minutos, veredito binário)*

O compilador e a suíte de testes são os auditores mais baratos que existem. Nada de
opinião: passa ou falha.

```bash
bash scripts/quality/audit-baseline.sh --with-gates
```

| Gate | Comando | Autoridade |
|---|---|---|
| Build backend | `dotnet build NexTraceOne.sln` | `TreatWarningsAsErrors=true` → zero tolerância |
| Testes backend | `dotnet test NexTraceOne.sln --filter ...` | contrato codificado |
| Typecheck FE | `npx tsc -b` (**não** `--noEmit`, ver §6) | contrato de tipos |
| Build FE | `npm run build` | entregabilidade |
| Lint FE | `npx eslint .` | estilo e regras |
| Testes FE | `npx vitest run` | contrato codificado |
| i18n | `node scripts/quality/check-i18n-coverage.sh` | 4 locales completos |
| Anti-demo | `bash scripts/quality/check-no-demo-artifacts.sh` | dados falsos fora da whitelist |

**Critério de saída:** todos verdes, ou cada vermelho classificado com dono e prazo.

### Nível 1 — Inventário mecanizado *(minutos, produz o mapa)*

Contagem e cruzamento dos elos de cada fatia, por script. Não julga qualidade —
mede desequilíbrio e aponta onde investigar.

Já implementado em `audit-baseline.sh`: endpoints, handlers, repositórios (interface
vs implementação), EF configs, migrations, classes `Null*`, marcadores de simulação,
testes fora da solução.

**Critério de saída:** tabela por módulo produzida; cada desequilíbrio com uma linha
de explicação (drift real vs. artefacto da heurística de contagem).

### Nível 2 — Verificação de declarações *(horas, alto retorno)*

O projeto documenta-se bem — `HONEST-GAPS.md`, `IMPLEMENTATION-STATUS.md`,
`MASTER-ACTION-PLAN.md`. **A auditoria não redescobre; verifica.** Para cada afirmação
de estado, procurar a evidência que a confirma ou refuta.

Este nível é o de maior retorno porque documentação desatualizada é mais perigosa que
documentação ausente: ela impede que alguém procure o problema. O baseline já encontrou
três declarações refutadas (§6).

**Critério de saída:** cada linha `READY` em `IMPLEMENTATION-STATUS.md` com evidência
(teste, migration, endpoint) ou reclassificada.

### Nível 3 — Revisão dirigida *(dias, só onde os níveis 0–2 apontaram)*

Leitura humana/assistida **apenas** das fatias que os níveis anteriores sinalizaram.
Nunca leitura exploratória em massa.

Ordem de prioridade: segurança e multi-tenancy → integridade de dados → correção de
domínio → UX.

**Critério de saída:** cada fatia sinalizada com veredito escrito e issue aberta.

---

## 4. Ondas de execução

Cada onda tem critério de verificação explícito. Não avançar sem o cumprir.

| Onda | Objetivo | Critério de verificação |
|---|---|---|
| **0 — Baseline** | Tornar tudo executável e medir | ✅ **Concluída** — ver §5 e §6 |
| **1 — Verde executável** | Zero vermelhos no Nível 0 | build FE passa; `test-frontend` deixa de ser `skipped`; 17 testes backend resolvidos; 188 testes órfãos na solução; serviço Postgres do CI corrigido; `npm audit` verde |
| **2 — Mapa de fatias** | Inventário completo por fatia | toda fatia com os 13 elos classificados; zero ficheiros órfãos não explicados |
| **3 — Verificação de declarações** | Docs = realidade | `IMPLEMENTATION-STATUS.md` e `HONEST-GAPS.md` reconciliados com evidência |
| **4 — Fronteira real/simulado** | Saber o que é produto e o que é demonstração | cada um dos 40 ficheiros com `IsSimulated` e das 16 famílias de stub MSW classificado: produto real, degradação graciosa configurável, ou dívida |
| **5 — Backlog de fecho** | O que falta, quantificado | lista priorizada com esforço, derivada das ondas 2–4 |

**Ordem obrigatória.** A Onda 1 vem primeiro porque enquanto o build do frontend estiver
partido nenhuma outra medição é confiável — não se audita um alvo em movimento.

---

## 5. Como reproduzir o baseline

O ambiente precisa de .NET 10 e das dependências do frontend:

```bash
# .NET 10 SDK (Ubuntu 24.04 — o instalador dot.net pode estar bloqueado por proxy)
apt-get update && apt-get install -y dotnet-sdk-10.0

# Frontend
cd src/frontend && npm install && cd -

# Baseline completo
bash scripts/quality/audit-baseline.sh --with-gates > docs/audit/BASELINE-$(date +%Y-%m-%d).md
```

---

## 6. Achados do baseline (Onda 0)

Medido em `e3eaf52`, 2026-08-21. Detalhe completo em
[BASELINE-2026-08-21.md](./BASELINE-2026-08-21.md).

### O que está sólido

- **Build do backend limpo** — 85 projetos, 0 erros, com `TreatWarningsAsErrors=true`.
- **10.320 testes de backend a passar** em 17 assemblies.
- **2.496 testes de frontend a passar** em 375 ficheiros.
- **i18n completo** — 0 chaves em falta nos 4 locales.
- **Guardrail anti-demo verde** — 0 violações novas fora da whitelist.

Isto não é um projeto vazio com fachada de documentação. A base é real e substancial.

### P0 — bloqueia a entrega

| # | Achado | Evidência |
|---|---|---|
| 1 | **Build de produção do frontend partido** | `npm run build` → `TS2339` em `src/stubs/handlers/changeGovernance.ts:275` (`changeScore` não existe no tipo). O job `build-frontend` do CI falha. |
| 2 | **`npm run typecheck` é um falso-verde** | O script corre `tsc --noEmit`, que resolve `tsconfig.json` — e esse ficheiro tem `"files": []` com project references. Sem `-b`, **verifica zero ficheiros e sai 0**. Foi o que mascarou o achado #1. Corrigir para `tsc -b`. |
| 3 | **188 testes invisíveis ao CI** | 5 projetos de teste fora de `NexTraceOne.sln`, que é o alvo de `dotnet test` no CI: `Selenium.Tests` (115), `BackgroundWorkers.Tests` (39), `OperationalIntelligence.Infrastructure.Tests` (12), `Governance.ArtifactSigning.Tests` (11), `VisualStudio.Tests` (11). Nunca correram; estado desconhecido. |
| 4 | **Ingestion API lança em toda a rota de auto-provisionamento de conector** | `IntegrationConnector.Create` exige `tenantId` quando `isGlobal` é `false` (`IntegrationConnector.cs:124`). Os **12 pontos de chamada em produção**, todos em `NexTraceOne.Ingestion.Api/Endpoints/`, não passam `tenantId` nem `isGlobal`. Qualquer ingestão que encontre o conector ainda inexistente atira `ArgumentException`. Detalhe em §6.1. |

### 6.0 — O CI de `main` está vermelho há um mês

Verificado via GitHub Actions API. A última execução do `ci.yml` em `main`
(run `30196949018`, 2026-07-26, sobre `e3eaf52` — o **head atual** de `main`):

```
validate: success                 build-tools: success
build-backend: success            test-tools: success
test-backend-unit: FAILURE        build-vscode-extension: success
test-backend-integration: FAILURE build-vsix: success
test-backend-e2e: FAILURE         build-frontend: FAILURE
openapi-artifact: FAILURE         test-frontend: SKIPPED
```

Três leituras deste quadro:

1. **`build-frontend` falha com exatamente o mesmo `TS2339`** do P0 #1 — desde 2026-07-26.
   O achado não é novo; é apenas a primeira vez que alguém o regista.
2. **`test-frontend` está `skipped`**, porque depende de `build-frontend`. Os 2.496 testes
   de frontend que passam localmente **nunca correm em CI**. Somando aos 188 testes fora
   da solução, o total de testes escritos que o CI não executa passa de 2.600.
3. **`test-backend-integration` falha com `FATAL: role "root" does not exist`** — configuração
   do serviço PostgreSQL no workflow, não defeito de produto. Mas mascara o estado real dos
   testes de integração, que continua desconhecido.

O `security.yml` está igualmente vermelho em `main` desde pelo menos 2026-07-24, em todas
as execuções (ver P2).

**Um padrão repete-se três vezes neste baseline: gates que não medem o que dizem medir.**

| Gate | O que aparenta | O que faz |
|---|---|---|
| `npm run typecheck` | verifica os tipos do frontend | corre `tsc --noEmit` sobre um `tsconfig` com `"files": []` — verifica **zero** ficheiros e sai 0 |
| `dotnet test NexTraceOne.sln` | corre a suíte de testes | 5 projetos (188 testes) não estão na solução; nunca são executados |
| `.NET Dependency Scan` | falha quando há pacotes vulneráveis | aborta no `.esproj` antes de a verificação correr; a sua própria mensagem de erro nunca dispara |

Um gate partido é pior que um gate ausente: consome a confiança de um gate real sem
prestar o serviço. Verificar os verificadores é, por isso, o primeiro passo da Onda 1 —
antes de corrigir qualquer defeito de produto.

**Consequência para a estratégia:** `main` não é uma base verde da qual se parte. A Onda 1
não é "manter o verde" — é *alcançá-lo pela primeira vez*. Nenhuma medição de progresso é
significativa enquanto o sinal de CI estiver saturado a vermelho, porque uma regressão nova
é indistinguível do ruído existente.

### 6.1 — Anatomia do P0 #4 (porque a estratégia funciona)

Este achado é o argumento a favor de toda a abordagem, por isso vale detalhá-lo.

A invariante foi adicionada ao agregado:

```csharp
// IntegrationConnector.cs:123
if (!isGlobal && tenantId is null)
    throw new ArgumentException("Tenant-scoped connectors must have a TenantId.");
```

A assinatura tem `Guid? tenantId = null, bool isGlobal = false` — ambos opcionais.
**Nenhum chamador existente foi obrigado a mudar pelo compilador.** O build passa; os
12 pontos de chamada de produção continuam a compilar e passam a lançar em runtime:

```
RuntimeSignalEndpoints.cs:40     ContractSyncEndpoints.cs:32     CostIngestEndpoints.cs:44
RuntimeSignalEndpoints.cs:110    DeploymentEventEndpoints.cs:49  ReleaseIngestEndpoints.cs:577
PromotionEventEndpoints.cs:39    CommitIngestEndpoints.cs:45     ReleaseIngestEndpoints.cs:693
IncidentEndpoints.cs:57          IncidentEndpoints.cs:191        ConsumerSyncEndpoints.cs:32
```

Todos seguem o mesmo padrão de auto-provisionamento:

```csharp
var connector = await connectorRepo.GetByNameAsync("runtime-signals", ct);
if (connector is null)
{
    connector = IntegrationConnector.Create(/* … */, utcNow: clock.UtcNow);  // sem tenantId
    await connectorRepo.AddAsync(connector, ct);
}
```

O ramo só é atingido na **primeira** ingestão de cada tipo de conector — exatamente o
caminho que um ambiente já povoado nunca exercita e que um ambiente novo atinge de imediato.

Três observações sobre o processo:

1. **O build não apanhou** — parâmetros opcionais desativam o compilador como rede de segurança.
2. **Os testes apanharam** — 13 falhas, todas a apontar para a mesma linha. O sinal existia.
3. **Ninguém correu os testes.** Foi preciso instalar o SDK .NET para os executar nesta sessão.

A conclusão que orienta as ondas seguintes: **este projeto não sofre de falta de testes —
sofre de falta de execução de testes.** São 10.337 testes de backend e 2.496 de frontend
escritos. O trabalho da Onda 1 não é escrever mais; é garantir que os que existem correm,
todos, sempre.

### P1 — 17 testes de backend a falhar

| Assembly | Falhas | Causa raiz | Natureza |
|---|---|---|---|
| `Integrations.Tests` | 13 | `IntegrationConnector.Create` lança `ArgumentException: Tenant-scoped connectors must have a TenantId` | **Bug de produção confirmado — promovido a P0 #4.** Ver §6.1. |
| `BuildingBlocks.Security.Tests` | 3 | `AesGcmEncryptor.Decrypt` não lança em Base64 inválido, payload curto nem **ciphertext adulterado** | Contradição de contrato: a implementação é **fail-open por design** (documentado no XML doc, para migrar plaintext legado), os testes exigem `throw`. Decidir qual é a verdade — fail-open silencioso sobre ciphertext adulterado é decisão de segurança que merece escrutínio explícito, não divergência acidental. |
| `Governance.Tests` | 1 | `TestNetworkConnectivity` exige egresso de rede real | Dependente de ambiente, não defeito de produto. Marcar com trait e excluir da suíte unitária. |

### P2 — dívida e drift

| Achado | Medida |
|---|---|
| **NuGet: 2 pacotes com vulnerabilidade alta** | `Microsoft.OpenApi` 2.0.0 (GHSA-v5pm-xwqc-g5wc) e `SSH.NET` 2025.1.0 (GHSA-q939-rpr3-3284) — 7 avisos `NU1903` |
| **O gate `.NET Dependency Scan` aborta antes de verificar** | O passo corre `dotnet list NexTraceOne.sln package --vulnerable \| tee …` e só depois faz `grep` pelo veredito. O shell do GitHub Actions é `bash -eo pipefail`, e `dotnet list` sai com código 1 porque `src/frontend/nextraceone.frontend.esproj` usa `package.config` em vez de `PackageReference`. O passo morre nessa linha: **o `grep` e a mensagem `::error::Vulnerable NuGet packages detected` nunca chegam a executar.** As vulnerabilidades são reais (confirmado localmente: 5 projetos afetados), mas o log atribui a falha ao `.esproj`, não a elas. Corrigir com `--no-restore` num filtro de projetos ou excluindo o `.esproj` do comando. |
| **npm: 13 vulnerabilidades, 9 altas** | `npm audit --audit-level=high` falha. `react-router` ≤7.18.1 (5 avisos, incl. open redirect e XSS), `undici` ≤7.28.0 (11 avisos), `vite` ≤7.3.3 (2 avisos). O job `Frontend npm Audit` está vermelho em **todas** as execuções em `main` desde pelo menos 2026-07-24. `npm audit fix` resolve, mas exige subir `react-router` — validar impacto antes. |
| **4 `Null*Repository`** | Todos em `aiknowledge`: `NullAiAnalyticsRepository`, `NullAiSearchRepository`, `NullAiUsageEntryRepository`, `NullVectorStoreRepository`. Pela regra de ouro do CLAUDE.md §21, `Null*Repository` é bug — mas os 87 `Null*Reader` são legítimos. Classificar caso a caso. |
| **`notifications` sem migrations** | 6 implementações de repositório e 6 EF configs, **0 migrations**. O schema não existe. |
| **3.837 chaves i18n extra** | Chaves nos locales sem correspondência no base — drift, não bloqueio. |
| **174 avisos de ESLint** | 0 erros. Maioria são `eslint-disable` inúteis. |

### P3 — documentação desalinhada da realidade

Este é o achado com mais impacto na confiança do plano, porque contamina qualquer
decisão tomada a partir dos documentos:

| Documento | Afirma | Realidade |
|---|---|---|
| `CLAUDE.md` §5 | "12 Bounded Contexts", cada um com 5 projetos próprios | **9 módulos.** `auditcompliance` foi absorvido por `governance`; `knowledge` e `productanalytics` por `catalog`. A consolidação aconteceu; o documento não acompanhou. |
| `MASTER-ACTION-PLAN.md` | "Backend v1.0.0 READY (12 módulos)" | Mesma contagem desatualizada. |
| `HONEST-GAPS.md` | "🟢 Zero gaps abertos — pronto para v1.0.0" (Maio 2026) | O build de produção do frontend está partido e 188 testes nunca correram. A afirmação não é sustentável sem qualificação. |
| `analysis_v*.txt` (raiz) | Análise endpoint↔handler por 12 módulos | Referem caminhos que já não existem (`src/modules/productanalytics/`). Obsoletos — substituídos por `audit-baseline.sh`. |

---

## 7. Princípio operacional

> **Uma afirmação sobre o estado do projeto só conta se estiver ligada a um comando
> que qualquer pessoa possa correr e obter o mesmo resultado.**

Tudo neste documento é reproduzível por `audit-baseline.sh`. À medida que as ondas
avançam, cada conclusão nova entra no script ou não entra na auditoria — é assim que
o retrato se mantém verdadeiro em vez de envelhecer como os documentos da §P3.
