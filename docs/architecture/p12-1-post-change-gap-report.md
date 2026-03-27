# P12.1 — Post-Change Gap Report

> Data: 2026-03-27 | Fase: P12.1 — Remoção definitiva de resíduos de Licensing

---

## 1. O que foi resolvido em P12.1

### Residuos de Código Eliminados

| ID | Localização | Resolução |
|----|-------------|-----------|
| LIC-01 | `RolePermissionCatalog.cs` — 17 permissões `licensing:*` | ✅ Resolvido (limpeza anterior confirmada) |
| LIC-02 | `PermissionConfiguration.cs` — seeds HasData `licensing:read`/`write` | ✅ Resolvido (limpeza anterior confirmada) |
| LIC-03 | `CreateDelegation.cs` — `licensing:write` na lista de delegação | ✅ Resolvido (limpeza anterior confirmada) |
| LIC-04 | `MfaPolicy.cs` — comentário XML "operações de vendor" | ✅ Resolvido em P12.1 (comentários reescritos) |
| LIC-05 | `Breadcrumbs.tsx` — mapeamento `'licensing'` | ✅ Resolvido (limpeza anterior confirmada) |
| LIC-06 | `navigation.ts` — mapeamento `'vendor': 'sidebar.vendorLicensing'` | ✅ Resolvido (limpeza anterior confirmada) |
| LIC-07 | `en.json` e outros locales — `guidanceAdmin` com "licensing" | ✅ Resolvido (limpeza anterior confirmada) |
| Extra | `DeveloperPortalPage.tsx` — comentário mencionando "LicensingPage" | ✅ Resolvido em P12.1 |

### Documentação Ativa Atualizada

| Documento | Atualização |
|-----------|-------------|
| `phase-a-open-items.md` | OI-05 marcado CLOSED |
| `placeholder-and-cosmetic-ui-report.md` | PH-08 marcado RESOLVED |
| `module-seed-strategy.md` | Tarefa de remoção de licensing marcada como feita |
| `migration-readiness-by-module.md` | Identity row e OI-05 atualizados |
| `execution-phase-readiness-report.md` | Tarefas 1-4 de limpeza marcadas como P12.1 done |

---

## 2. O que ainda ficou pendente

### 2.1 Propriedade `RequiredForSensitiveExternalOps` em MfaPolicy.cs

- **Situação:** A propriedade foi renomeada de `RequiredForVendorOps` para `RequiredForSensitiveExternalOps` em P12.1. Todos os testes foram atualizados correspondentemente.
- **Estado:** ✅ Resolvido — nenhuma referência a "Vendor" remanescente em MfaPolicy.

### 2.2 Documentos Históricos de Auditoria

- Os seguintes documentos **continuam a existir** e mencionam licensing no contexto histórico:
  - `docs/architecture/licensing-residue-final-audit.md`
  - `docs/audits/2026-03-25/licensing-selfhosted-readiness-report.md`
  - `docs/11-review-modular/01-identity-access/licensing-residue-cleanup-review.md`
  - `docs/architecture/n-phase-final-validation-and-closure.md`
- **Justificativa:** São registos de auditoria e encerramento de fases. Preservar como histórico é a decisão correta — não são documentos "ativos" que prometem licensing como feature futura.
- **Recomendação:** Se desejado, podem ser movidos para `docs/archive/` numa fase de arrumação geral da documentação.

### 2.3 WAVE-1-CONSOLIDATED-VALIDATION.md

- Este documento menciona "Licensing" no contexto de remoção histórica da CommercialGovernance.
- Não constitui uma promessa ativa de licensing — é um registo de validação do Wave 1.
- **Recomendação:** Manter como histórico; pode ser movido para `docs/archive/` no futuro.

### 2.4 Docs de Arquitetura com Referências Contextuais

- `docs/architecture/migration-transition-risks-and-mitigations.md` — R-03 ainda documenta o risco histórico de resíduos de licensing
- `docs/architecture/migration-removal-prerequisites.md` — linha 120 lista licensing como pré-condição (histórica)
- `docs/architecture/new-baseline-validation-strategy.md` — E-03 e E-06 mencionam licensing no contexto de validação
- **Justificativa:** São contextos históricos de risco e planejamento. Não são promessas ativas.
- **Recomendação:** Deixar como estão — são referências de planejamento passado que são úteis para rastreabilidade.

---

## 3. Limitações Residuais

| Item | Limitação | Impacto |
|------|-----------|---------|
| Docs históricos | Menções a licensing em contexto de auditoria/remoção | Nenhum — são registos corretos |
| Nomenclatura `ForSaaS()`/`ForSelfHosted()` em MfaPolicy | Referências a "SaaS" e "self-hosted" — relacionadas ao contexto de deployment, não licensing | Nenhum — deployment flavors são válidos |

---

## 4. O que fica para P12.2

P12.2 está descrito como a limpeza do **resíduo self-hosted enterprise** — separado do licensing mas relacionado:

- Remover referências a `self-hosted` e `on-premise` como modos que requereriam configurações de licensing/entitlements
- Revisar documentação de deployment para não sugerir necessidade de license activation
- Limpar qualquer referência a heartbeat/activation no contexto de infrastructure deployment
- Avaliar se `MfaPolicy.ForSelfHosted()` deve ser renomeado ou preservado
- Revisar `releaseScope.ts` para garantir que não há gating baseado em conceitos de deployment tier
- Mover documentos históricos de licensing/self-hosted para `docs/archive/` se aplicável

---

## 5. Critérios de Aceite — Verificação Final

| Critério | Estado |
|----------|--------|
| Resíduos ativos de Licensing removidos do código | ✅ CUMPRIDO |
| Resíduos ativos de Licensing removidos da UI | ✅ CUMPRIDO |
| Documentação ativa não promete Licensing/Entitlements | ✅ CUMPRIDO |
| Permissões/configs/contracts ligados a Licensing removidos | ✅ CUMPRIDO |
| Código compila com novo estado | ✅ CUMPRIDO |
| Relatório final da execução produzido | ✅ CUMPRIDO |

---

## 6. Próximos Passos Recomendados

1. **P12.2** — Limpeza do resíduo self-hosted enterprise (conforme problema statement)
2. **Futuro** — Renomear `RequiredForVendorOps` para `RequiredForSensitiveExternalOps` quando convenient
3. **Futuro** — Mover docs históricos de licensing para `docs/archive/` numa passagem de housekeeping geral
