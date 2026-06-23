# Plano — Evolução da Camada `tools/` + Segurança (inspiração n8n CyberSec)

> **Status: PLANO (não executado).** Objetivo: consolidar o que evoluir na camada de ferramentas e definir o que, dos workflows de cibersegurança do n8n, faz sentido agregar ao NexTraceOne — sempre via a camada `tools/`.
> Fonte de inspiração: https://github.com/JoasASantos/n8n-CyberSecurity-Workflows (100 blueprints).
> Escopo desta fase: **não** expandir para JetBrains. Focar no que está pronto, pela metade, com erro, e na evolução.

---

## Princípio orientador (tese do plano)

O NexTraceOne **não é** um SIEM/SOC nem plataforma ofensiva. É uma plataforma de **Change Intelligence + governança + supply chain**. Portanto:

1. **Descartamos** todo Red Team/Pentest e a maior parte de SOC/DFIR/detecção-resposta do n8n — não pertencem à identidade do produto.
2. **Aproveitamos** o subconjunto **AppSec/DevSecOps/supply-chain/compliance**, porque ele mapeia quase 1:1 a capacidades que **já existem no backend** e que a camada `tools/` **não expõe**.
3. O mecanismo unificador é: **transformar achados de segurança em _risk signals_** que alimentam o **change confidence score / signed-artifact gate / ServiceRiskProfile** — e **expor isso pelos tools** (CLI `nex`, GitHub Actions, SDK, MCP). Esse é o "agregar" com a cara do NexTraceOne.

---

## Parte 1 — Estado atual da camada `tools/` (pronto / pela metade / com erro)

Consolidado da análise anterior (`docs/tools/ANALISE-CAMADA-TOOLS.md`).

### ✅ Pronto
- **SDK .NET** (5 sub-clientes, resiliência, filtro OpenAPI) — 25 testes.
- **CLI `nex`** (14 comandos) — 106 testes.
- **Extensão VS Code** v0.6.0 (chat, catalog tree, scaffold wizard, MCP, LM tools).
- **GitHub Action** `nexone-change-confidence-gate` (funcional).

### 🟡 Pela metade / inconsistente
- **Resiliência HTTP no CLI**: só `integration` usa o SDK resiliente; demais comandos usam `HttpClient` cru.
- **Cobertura de testes do CLI**: 8/14 comandos com teste (faltam mcp, confidence, compliance, config, health, scaffold).
- **README do VS Code** defasado; versões `clientVersion` hardcoded.
- **Empacotamento NuGet do SDK** incompleto (sem license/repo/readme/símbolos).
- **Extensão VS 2022**: completa em código, mas **fora da solution** → não buildada no CI ("em desenvolvimento").

### 🔴 Com erro
- **URL default do CLI `:8080`** diverge de tudo (`:5000`) → quebra o primeiro uso.

### ⚙️ Fora da camada tools, mas afeta os PRs (infra de CI — pré-existente)
- `Test Backend (E2E/Integration)`: PostgreSQL não cria role `nextraceone` (`ignoring /docker-entrypoint-initdb.d/*`).
- `Playwright E2E`: Firefox não instalado + teste SAML; `Frontend npm Audit`: 6 vulns de deps.
- *(Itens de CI/infra entram como anexo do plano, não como tarefas da camada tools.)*

---

## Parte 2 — Triagem dos 100 workflows n8n

### ❌ Descartados (não fazem sentido para o NexTraceOne)
- **A. Red Team & Pentest (1–30)** — recon, C2, phishing, evasão AV/EDR, movimento lateral, evil twin. Fora da identidade e do escopo legal/ético do produto.
- **B. SOC/DFIR (maioria: 31–67)** — SIEM routing, EDR noise, DNS tunneling, beaconing, ransomware canary, MFA fatigue, brute-force heatmap, honeytokens. São de plataforma de detecção/resposta, que o NexTraceOne não é.

### ✅ Selecionados (fazem sentido — mapeiam à identidade)
Subconjunto **AppSec/DevSecOps/Platform** com aderência ao backend existente:

| # n8n | Workflow | Aderência no NexTraceOne |
|---|---|---|
| 70 | Software Composition Analysis (SCA) | `DependencyScanJob` + OSV/NuGet **já existem** → expor |
| 74 | CICD SBOM + Provenance + Signing | `GenerateSbom` + `CosignArtifactSigner` + `AttachSlsaProvenance` **já existem** → expor |
| 71 | Container Image Policy Gate | `EvaluateSignedArtifactGate` + Trivy (security.yml) → gate via confidence |
| 75 | API Contract Drift Guard | `nex contract diff` + breaking-change **já existem** → virar Action |
| 68 | SAST on PR (Semgrep) | `SonarQubeIngestEndpoints` **já existe** → generalizar p/ SARIF |
| 73 / 90 | Secrets Scanner / token sprawl | **novo** → risk signal + gate |
| 72 | IaC Misconfig (Checkov) | **novo** → ingest como risk signal |
| 85 | Compliance Pack (PCI/SOC2/ISO/NIS2) | relatórios **já existem** (changegovernance) → expor |
| 93 | Vuln Digest c/ EPSS/KEV | `IVulnerabilityAdvisoryReader` (🟡 honest-null) → completar + priorizar |
| 94 | TLS Expiry & Rotation | **novo, opcional** (baixa prioridade) |

> Observação tangencial (não-core, fica em backlog frio): 34 OAuth App Risk, 45 Cloud Config Drift, 54 IR War Room — só se houver demanda; não são da camada tools.

---

## Parte 3 — O que agregar nos `tools/` (mapeado a backend existente)

Legenda: **[E]** capacidade já existe no backend (só expor) · **[½]** existe pela metade (completar) · **[N]** novo.

### 3.1 CLI `nex` — novo grupo `nex security`
- `nex security deps <service>` **[E]** — vulnerabilidades de dependências (OSV/NuGet) via `EnrichServiceDependencies`. `--min-severity`, `--fail-on high` para CI.
- `nex security sbom <service> [--format cyclonedx|spdx]` **[E]** — gera/baixa SBOM (`GenerateSbom`).
- `nex artifact sign|verify` **[E]** — assina/verifica artefato (Cosign) via `SignArtifact`/`VerifyArtifact`.
- `nex security risk <service>` **[E]** — `ServiceRiskProfile` (risk signals agregados).
- `nex security ingest --type sast|sca|iac|secrets --file results.sarif` **[½/N]** — ingere findings como risk signals (generaliza SonarQube ingest p/ SARIF).
- `nex compliance report --framework pci|iso27001|nis2|fedramp [--pdf]` **[E]** — relatórios de compliance já existentes.

### 3.2 GitHub Actions — família "governance gates" (hoje só existe o confidence gate)
- `nexone-sca-gate` **[E]** — falha PR se vuln >= severidade X (consome `nex security deps --format json`).
- `nexone-contract-drift-gate` **[E]** — falha em breaking change não aprovado (consome `nex contract diff`). **Alto valor, baixo custo.**
- `nexone-sbom-attest` **[E]** — gera SBOM + assina + anexa provenance ao release.
- `nexone-secrets-gate` **[N]** — roda Gitleaks, envia findings como risk signal, gate.
- `nexone-security-ingest` **[½]** — sobe SARIF (Semgrep/Trivy/Checkov) → risk signals do serviço.
- Padronizar: todas com `branding`, outputs e `--format sarif` para a aba Security do GitHub.

### 3.3 SDK — novo `SecurityClient`
- Métodos: `GetDependencyVulnerabilitiesAsync`, `GenerateSbomAsync`, `SignArtifactAsync`/`VerifyArtifactAsync`, `GetServiceRiskProfileAsync`, `IngestSecurityFindingsAsync`, `GetComplianceReportAsync`.
- Reaproveita a resiliência e o padrão de sub-clientes já existentes.

### 3.4 MCP — novas tools para agentes/IDE
- `get_service_risk`, `list_dependency_vulnerabilities`, `get_sbom`, `get_compliance_status`.
- Expostas no servidor MCP (`/api/v1/ai/mcp`) e consumíveis por `nex mcp call`, VS Code LM tools e Copilot. Alinha à tendência Backstage-MCP/Codex-CLI.

### 3.5 Backend — completar bridges (pré-requisito de alguns itens)
- **[½]** `IVulnerabilityAdvisoryReader` → substituir `NullVulnerabilityAdvisoryReader` por leitura real do Catalog (vuln advisories) — habilita item 93 (vuln digest) e o cruzamento risco↔incidente.
- **[N]** Enriquecimento **EPSS/KEV** sobre as CVEs já coletadas (prioridade de exploração) — alimenta `ServiceRiskProfile` e o confidence score.
- **[N]** Modelo de **risk signal de segurança** unificado (SAST/SCA/IaC/Secrets/Image) com origem, severidade e dedupe — o "barramento" que conecta achados → gates.

---

## Parte 4 — Backlog priorizado (sem execução)

> Cada item tem critério de verificação. Esforço: XS/S/M/L. Tudo aguarda aprovação antes de iniciar.

### Fase 0 — Corrigir o que está quebrado/inconsistente nos tools (higiene)
| # | Tarefa | Esf. | Verificação |
|---|---|---|---|
| 0.1 | CLI default `:8080 → :5000` | XS | `nex health` conecta ao ApiHost local sem `--url` |
| 0.2 | Factory HTTP resiliente único no CLI | S | Comandos reusam retry/backoff; teste de retry passa |
| 0.3 | Testes p/ 6 comandos sem cobertura | M | Cobertura de comandos 14/14 |
| 0.4 | Atualizar README VS Code + derivar versão do `package.json` | XS | Doc reflete features; sem versão hardcoded |
| 0.5 | Decidir CI da extensão VS 2022 (incluir condicional Windows ou marcar experimental) | S | Build status definido e documentado |

### Fase 1 — Expor capacidades de segurança já existentes (alto ROI, baixo risco)
| # | Tarefa | Dep. | Esf. | Verificação |
|---|---|---|---|---|
| 1.1 | SDK `SecurityClient` (deps, sbom, sign/verify, risk, compliance) | — | M | Testes de cliente com `MockHttpMessageHandler` |
| 1.2 | CLI `nex security deps/sbom/risk` + `nex artifact sign/verify` | 1.1 | M | Saída text/json; `--fail-on` retorna exit code correto |
| 1.3 | CLI `nex compliance report --framework` | 1.1 | S | Gera relatório PCI/ISO/NIS2 |
| 1.4 | Action `nexone-contract-drift-gate` | — | S | PR com breaking change falha; sem drift passa |
| 1.5 | Action `nexone-sca-gate` | 1.2 | S | Vuln high bloqueia; output SARIF na aba Security |

### Fase 2 — Novos achados como risk signals + gates (precisa backend)
| # | Tarefa | Dep. | Esf. | Verificação |
|---|---|---|---|---|
| 2.1 | Backend: modelo unificado de security risk signal | — | M | Persistência + dedupe testados |
| 2.2 | Backend: completar `IVulnerabilityAdvisoryReader` (bridge Catalog) | 2.1 | M | Reader retorna dados reais; remove honest-null |
| 2.3 | Backend: enriquecimento EPSS/KEV | 2.2 | M | CVE recebe score EPSS + flag KEV |
| 2.4 | CLI `nex security ingest` (SARIF) + Action `nexone-security-ingest` | 2.1 | M | Semgrep/Trivy/Checkov viram risk signals |
| 2.5 | Action `nexone-secrets-gate` (Gitleaks) | 2.1 | S | Secret detectado → gate + risk signal |
| 2.6 | Action `nexone-sbom-attest` (SBOM+sign+provenance) | 1.2 | M | Release com SBOM assinado + provenance |
| 2.7 | MCP tools de segurança | 1.1 | S | `nex mcp call get_service_risk` retorna perfil |

### Fase 3 — Vuln digest & priorização (fecha o ciclo de Change Intelligence)
| # | Tarefa | Dep. | Esf. | Verificação |
|---|---|---|---|---|
| 3.1 | Confidence score considera security risk signals (EPSS/KEV) | 2.3 | M | Score cai com CVE KEV ativa no serviço |
| 3.2 | `nex report security-digest` (CVE→asset→exploitability) | 3.1 | M | Digest prioriza por EPSS/KEV |
| 3.3 | (Opcional) `nex security tls-expiry` | — | S | Lista certs expirando |

---

## Parte 5 — Princípios de segurança do plano (não negociáveis)
- **Secrets nunca hardcoded** — tokens via `~/.nex/config.json`/env/Credentials; Actions usam `secrets.*`.
- **AirGap-aware** — toda chamada externa (OSV, EPSS/KEV, NuGet) respeita `AirGapHttpMessageHandler`; modo offline degrada com aviso, não falha.
- **Least privilege** — Actions com `permissions:` mínimas (`security-events: write` só onde sobe SARIF).
- **SARIF first** — findings de segurança saem em SARIF para anotação nativa no GitHub.
- **Multi-tenant** — todo dado de risco respeita RLS/tenant (sem vazamento cross-tenant nos readers).
- **Sem execução ofensiva** — nada de scanners ativos/exploração; apenas análise de artefatos, deps e contratos do próprio tenant.

---

## Resumo executivo
- **Descartar** Red Team + SOC/DFIR do n8n; **aproveitar** o eixo AppSec/DevSecOps/supply-chain.
- O maior ganho **não é** construir scanners: é **expor pelos `tools/` o núcleo de segurança que o NexTraceOne já tem** (SBOM, Cosign, SLSA, dependency scan, risk profiles, compliance) e **conectá-lo ao change confidence** como _risk signals_/gates.
- Sequência: **Fase 0** (consertar tools) → **Fase 1** (expor o que existe) → **Fase 2** (novos achados→signals) → **Fase 3** (priorização EPSS/KEV no confidence).
- **Nada será executado** até aprovação. Recomendo começar pela **Fase 0 + itens 1.4 (contract-drift-gate) e 1.1/1.2 (SecurityClient/CLI)** — maior valor com menor risco.
</content>
