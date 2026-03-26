# P5.3 — Automatização do ChangeIntelligenceScore: Relatório de Execução

**Data:** 2026-03-26  
**Fase:** P5.3 — Change Intelligence: Score Automático e Rastreável  
**Estado:** CONCLUÍDO

---

## 1. Objetivo

Tornar o `ChangeIntelligenceScore` automático, rastreável e explicável, eliminando a dependência
de cálculo manual. O score passou a ser calculado imediatamente no momento do deploy (via
`NotifyDeployment`) e recalculado quando o blast radius é conhecido (via `CalculateBlastRadius`).

---

## 2. Estado Anterior

| Aspecto | Estado antes do P5.3 |
|---------|----------------------|
| `ChangeIntelligenceScore.Compute()` | Existia, mas requeria pesos manuais explícitos |
| `ComputeChangeScore` handler | Existia, mas era um comando manual — não disparado automaticamente |
| `NotifyDeployment` | Criava release + ChangeEvent + ExternalMarker, mas **nenhum score** era calculado |
| `CalculateBlastRadius` | Calculava blast radius, mas **não recalculava o score** |
| Rastreabilidade do score | Nenhum campo indicava como o score foi derivado |
| BlastRadius → Score | Inexistente — blast radius não influenciava o score automaticamente |

---

## 3. Modelo de Cálculo Automático Adotado

O `ChangeScoreCalculator` implementa um modelo determinístico e explicável com 3 fatores:

### BreakingChangeWeight — derivado do `ChangeLevel`

| ChangeLevel | Peso | Razão |
|-------------|------|-------|
| `Operational` | 0.0 | Sem mudança de contrato |
| `NonBreaking` | 0.1 | Patch — risco mínimo |
| `Additive` | 0.4 | Minor — mudança aditiva |
| `Breaking` | 1.0 | Major — breaking change — risco máximo |
| `Publication` | 0.1 | Publicação sem nova versão |

### BlastRadiusWeight — derivado do `TotalAffectedConsumers`

| Consumidores | Peso | Razão |
|--------------|------|-------|
| (sem relatório) | 0.0 | Blast radius ainda não calculado |
| 0 | 0.0 | Nenhum consumidor afetado |
| 1–5 | 0.3 | Blast radius baixo |
| 6–20 | 0.6 | Blast radius médio |
| 21+ | 1.0 | Blast radius alto |

### EnvironmentWeight — derivado do nome do ambiente

| Ambiente | Peso | Razão |
|----------|------|-------|
| `production` / `prod` | 1.0 | Maior risco operacional |
| `staging` / `uat` / `qa` | 0.6 | Risco moderado |
| `development` / `dev` | 0.2 | Risco baixo |
| outros | 0.4 | Default médio |

### Fórmula

```
Score = round((BreakingChangeWeight + BlastRadiusWeight + EnvironmentWeight) / 3, 4)
```

---

## 4. Ficheiros Alterados

### Domínio

| Ficheiro | Alteração |
|----------|-----------|
| `ChangeIntelligenceScore.cs` | Adicionado campo `ScoreSource` (rastreabilidade); `Compute()` aceita parâmetro opcional `scoreSource` |

### Application

| Ficheiro | Alteração |
|----------|-----------|
| `Abstractions/IChangeScoreCalculator.cs` | **Novo** — abstração do serviço de cálculo automático + record `ScoreFactors` |
| `Services/ChangeScoreCalculator.cs` | **Novo** — implementação determinística dos 3 fatores |
| `Features/NotifyDeployment/NotifyDeployment.cs` | Handler: `IChangeScoreRepository` + `IChangeScoreCalculator` injectados; score calculado automaticamente após criar/enriquecer release; `Response` inclui `AutoScore` e `ScoreSource` |
| `Features/CalculateBlastRadius/CalculateBlastRadius.cs` | Handler: `IChangeScoreRepository` + `IChangeScoreCalculator` injectados; score recalculado com blast radius real após cálculo; `Response` inclui `UpdatedScore` e `ScoreSource` |
| `Features/GetChangeScore/GetChangeScore.cs` | `Response` inclui `ScoreSource` para rastreabilidade |
| `ChangeIntelligence/DependencyInjection.cs` | `IChangeScoreCalculator` registado como `Singleton` |

### Testes

| Ficheiro | Alteração |
|----------|-----------|
| `ChangeIntelligenceApplicationTests.cs` | 3 tests `NotifyDeployment` e 1 test `CalculateBlastRadius` atualizados para injetar `scoreRepository` + `scoreCalculator`; assertions adicionadas para `AutoScore` e `UpdatedScore` |
| `ChangeScoreCalculatorTests.cs` | **Novo** — 6 testes unitários cobrindo todos os fatores e combinações |

---

## 5. Pipeline Automático (pós-P5.3)

```
POST /api/v1/deployments/events  (P5.1)
    │
    └─► NotifyDeployment.Handler
            ├─ Release.Create() ou lookup existente
            ├─ ChangeEvent + ExternalMarker (rastreabilidade)
            └─► ChangeScoreCalculator.Compute(changeLevel, environment, blastRadius=null)
                    │
                    └─► ChangeIntelligenceScore.Compute(..., scoreSource="auto:change_level+environment")
                            └─► scoreRepository.Add() + release.SetChangeScore()

POST /api/v1/releases/{id}/blast-radius  (cálculo manual ou automático)
    │
    └─► CalculateBlastRadius.Handler
            ├─ BlastRadiusReport.Calculate()
            └─► ChangeScoreCalculator.Compute(changeLevel, environment, blastRadius=report)
                    │
                    └─► ChangeIntelligenceScore.Compute(..., scoreSource="auto:change_level+blast_radius+environment")
                            └─► scoreRepository.Add() + release.SetChangeScore()  [score atualizado]

GET /api/v1/releases/{id}/score
    └─► GetChangeScore.Handler → Response { Score, ScoreSource, BreakingChangeWeight, BlastRadiusWeight, EnvironmentWeight }
```

---

## 6. Rastreabilidade do Score

O campo `ScoreSource` na entidade `ChangeIntelligenceScore` permite responder:
- "Como foi calculado este score?" → `"auto:change_level+environment (blast_radius_pending)"`
- "Este score inclui blast radius?" → `"auto:change_level+blast_radius+environment"`
- "Foi cálculo manual?" → `"unknown"` (ComputeChangeScore manual) ou outro valor passado explicitamente

O `GetChangeScore` expõe os 3 pesos individuais + `ScoreSource` na resposta, tornando o score completamente explicável.

---

## 7. Integração com BlastRadiusReport

- **Antes:** `CalculateBlastRadius` calculava o blast radius mas o score permanecia inalterado
- **Depois:** `CalculateBlastRadius` recalcula o score automaticamente com o blast radius real, substituindo o score inicial (que usava `BlastRadiusWeight = 0.0`)

---

## 8. Validação

- ✅ 221/221 testes ChangeGovernance passam (incluindo 6 novos `ChangeScoreCalculatorTests`)
- ✅ Compilação sem erros em todos os projetos alterados
- ✅ `ChangeScoreCalculator` é determinístico — mesmos inputs → mesmo score
- ✅ Score calculado automaticamente em `NotifyDeployment` e recalculado em `CalculateBlastRadius`
- ✅ `ScoreSource` rastreável em todos os caminhos
