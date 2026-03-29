# Onda 6 — Core Change Intelligence Legacy

> **Duração estimada:** 4-5 semanas
> **Dependências:** Ondas 4 e 5
> **Risco:** Alto — requer dados reais para validação de scoring
> **Referência:** [LEGACY-MAINFRAME-WAVES.md](../LEGACY-MAINFRAME-WAVES.md)

---

## Objetivo

Change intelligence completa para mudanças em core systems. Após esta onda, uma mudança num copybook ou transação CICS terá o mesmo nível de análise de risco, blast radius e governance que uma mudança numa API REST.

---

## Entregáveis

- [ ] Novos `ChangeType` values no modelo existente
- [ ] `NotifyMainframeChange` command (similar a `NotifyDeployment`)
- [ ] `AnalyzeCopybookChangeImpact` (quais programas são afetados)
- [ ] `CalculateLegacyBlastRadius` (propagação por grafo híbrido)
- [ ] Risk scoring para mudanças legacy
- [ ] Evidence pack para mudanças legacy
- [ ] Integração com workflow de aprovação existente
- [ ] Legacy change timeline na UI
- [ ] CAB summary view para mudanças legacy

---

## Impacto Backend

### Extensão do Release Entity

O `Release` existente é extensível via metadata. Campos adicionais para contexto mainframe:

```csharp
// Novos campos no Release (ou metadata dictionary)
public string? MainframeLpar { get; }          // LPAR onde a mudança ocorre
public string? MainframeRegion { get; }        // CICS/IMS region
public string? AffectedJobName { get; }        // Job batch afetado
public string? AffectedCopybookName { get; }   // Copybook alterado
public string? AffectedTransactionId { get; }  // Transação CICS afetada
```

### Novos Handlers

| Feature | Tipo | Descrição |
|---|---|---|
| `NotifyMainframeChange` | Command | Recebe notificação de mudança mainframe e cria Release |
| `AnalyzeCopybookChangeImpact` | Command | Análise: quais programas usam este copybook |
| `CalculateLegacyBlastRadius` | Command | Blast radius percorrendo grafo híbrido |
| `ScoreLegacyChangeRisk` | Command | Risk scoring baseado em criticidade, programas, janela |
| `GenerateLegacyEvidencePack` | Command | Evidence pack com impacto, programas, blast radius |
| `GetLegacyChangeImpactSummary` | Query | Resumo executivo para CAB |

### Ingestion API

Extensão do endpoint de deployment ou novo endpoint dedicado:

```
POST /api/v1/mainframe/changes
```

```csharp
public sealed record MainframeChangeRequest(
    string? Provider,                // "manual", "endevor", "changeman", "git"
    string? CorrelationId,
    string ChangeType,               // "CopybookChange", "BatchJobChange", etc.
    string AssetName,                // Nome do ativo alterado
    string? AssetType,               // "Copybook", "CobolProgram", "CicsTransaction"
    string? SystemName,
    string? LparName,
    string? Version,
    string? Description,
    string? ChangedBy,
    string? Environment,
    DateTimeOffset? ChangedAt,
    Dictionary<string, string>? Metadata
);
```

### Risk Scoring para Legacy

Factores de risco para mudanças legacy:

| Factor | Peso | Descrição |
|---|---|---|
| **Criticality do ativo** | 0.25 | Critical > High > Medium > Low |
| **Número de programas afetados** | 0.20 | Mais programas = mais risco |
| **Blast radius cross-platform** | 0.15 | Se afeta serviços modernos = mais risco |
| **Breaking change em copybook** | 0.15 | Breaking = risco alto |
| **Janela operacional** | 0.10 | Fora de janela = risco alto |
| **Histórico de incidentes** | 0.10 | Se ativo já teve incidentes recentes |
| **Ambiente** | 0.05 | Production > Pre-prod > Dev |

**Fórmula:**
```
RiskScore = Σ(factor_i × weight_i) → normalizado para 0.0-1.0
```

### Evidence Pack Legacy

O evidence pack para mudanças legacy deve incluir:

1. **Mudança** — o que mudou, quem, quando, porquê
2. **Impacto** — programas afetados, transações afetadas
3. **Blast radius** — consumidores afetados (legacy + moderno)
4. **Risk score** — score normalizado com breakdown de factores
5. **Histórico** — mudanças anteriores no mesmo ativo
6. **SLA impact** — se afeta batch com SLA
7. **Janela operacional** — validação de janela
8. **Recomendação** — "approve", "review", "block"

### CAB Summary

Resumo executivo para Change Advisory Board:

```csharp
public sealed record CabSummaryDto(
    string ChangeDescription,
    string AssetName,
    string AssetType,
    string RiskLevel,               // "Low", "Medium", "High", "Critical"
    decimal RiskScore,
    int AffectedProgramsCount,
    int AffectedServicesCount,
    int AffectedConsumersCount,
    bool IsBreakingChange,
    bool CrossesPlatformBoundary,
    string? SlaImpact,
    string? OperationalWindowStatus,
    string Recommendation,          // "Approve", "Review Required", "Block"
    IReadOnlyList<string> TopAffectedAssets
);
```

---

## Impacto Frontend

### Nova Página: Legacy Change Impact

**Rota:** `/changes/legacy-impact/:changeId`
**Persona:** Architect, CAB

**Secções:**
1. **Summary** — resumo da mudança com risk badge
2. **Impact Analysis** — programas e transações afetados (lista)
3. **Blast Radius** — visualização gráfica dos nós afetados
4. **Risk Breakdown** — cada factor de risco com peso e valor
5. **Evidence Pack** — download de evidence pack
6. **History** — mudanças anteriores no mesmo ativo
7. **Recommendation** — ação recomendada com justificação

### Extensão da Releases Page

- Filtro por tipo: "All", "Modern", "Legacy"
- Badge de tipo de mudança (Deployment, CopybookChange, BatchJobChange, etc.)
- Coluna "Platform" (Modern, Legacy, Hybrid)
- Risk score visível na lista

### CAB Summary Component

Componente reutilizável para apresentar resumo CAB:
- Card com semáforo (verde/amarelo/vermelho)
- Métricas de impacto
- Recomendação com botão de ação

---

## Impacto Base de Dados

Extensão de tabelas existentes:

| Tabela | Alteração |
|---|---|
| `chg_releases` | Novos `ChangeType` values. Campos metadata para contexto mainframe |
| `chg_blast_radius_reports` | Novos campos: `AffectedLegacyAssets`, `CrossesPlatformBoundary` |
| `chg_change_intelligence_scores` | Risk factors legacy |
| `chg_evidence_packs` | Evidence items legacy |

---

## Testes

### Testes Unitários (~40)
- Risk scoring: cada factor, peso, normalização
- Copybook impact analysis: programas afetados
- Blast radius: propagação por grafo híbrido
- CAB summary: geração de resumo

### Testes de Integração (~20)
- NotifyMainframeChange → Release criada → Risk scored → Blast radius calculado
- Evidence pack gerado com dados corretos
- Workflow triggered para breaking change

---

## Critérios de Aceite

1. ✅ Mudança em copybook gera lista de programas impactados
2. ✅ Blast radius inclui consumidores modernos de APIs expostas pelo mainframe
3. ✅ Risk score considera criticidade, programas, janela operacional
4. ✅ Workflow de aprovação funcional para mudanças legacy
5. ✅ CAB summary mostra impacto em linguagem executiva
6. ✅ Evidence pack downloadable com dados completos
7. ✅ Releases page permite filtrar por tipo (modern/legacy)

---

## Riscos

| Risco | Severidade | Mitigação |
|---|---|---|
| Risk scoring sem dados reais para calibrar | Alta | Pesos default configuráveis. Ajuste iterativo |
| Blast radius muito amplo para ativos core | Média | Limitar profundidade. Grouping por domínio |
| CAB summary precisa de linguagem não técnica | Baixa | Templates com i18n. Revisão por product |

---

## Stories

| ID | Story | Prioridade |
|---|---|---|
| W6-S01 | Implementar `NotifyMainframeChange` command + handler | P0 |
| W6-S02 | Implementar `AnalyzeCopybookChangeImpact` | P0 |
| W6-S03 | Implementar `CalculateLegacyBlastRadius` | P0 |
| W6-S04 | Implementar `ScoreLegacyChangeRisk` com factores | P1 |
| W6-S05 | Implementar `GenerateLegacyEvidencePack` | P1 |
| W6-S06 | Implementar `GetLegacyChangeImpactSummary` (CAB summary) | P1 |
| W6-S07 | Criar endpoint `POST /api/v1/mainframe/changes` | P1 |
| W6-S08 | Criar `LegacyChangeImpactPage` frontend | P1 |
| W6-S09 | Extensão da Releases page com filtro legacy | P1 |
| W6-S10 | Criar `CabSummaryComponent` | P2 |
| W6-S11 | Integrar com workflow de aprovação existente | P1 |
| W6-S12 | Criar migrações para extensão de tabelas | P1 |
| W6-S13 | Testes unitários de risk scoring (~20) | P1 |
| W6-S14 | Testes de integração end-to-end (~20) | P1 |
