# Gaps — Testes
> Análise dos gaps em testes unitários, integração e E2E do NexTraceOne.

---

## 1. Frontend — Cobertura Crítica

### Situação
- **13 ficheiros de teste** para **278 páginas** e **338+ componentes**
- Cobertura estimada: **~4%**
- Não há threshold de cobertura mínimo configurado

### Módulos frontend sem qualquer teste unitário
| Módulo | Páginas | Testes |
|--------|---------|--------|
| catalog | 32 | 0 |
| platform-admin | 34 | 0 |
| governance | 35 | 0 |
| change-governance | 24 | 0 |
| ai-hub | 21 | 0 |
| operations | 19 | 0 |
| identity-access | 16 | 0 |
| knowledge | 5 | 0 |

### O que existe (os 13 ficheiros)
- Validadores de builders de contratos (REST, SOAP, Event, Webhook)
- Utilitários (`badge-variants`, `navigation`, `tokenStorage`, `sanitize`)
- `releaseScope.test.ts`

### Impacto
Regressões em qualquer componente de UI são invisíveis até E2E ou teste manual.

---

## 2. Backend — ProductAnalytics Subcoberto

**Módulo:** `tests/modules/productanalytics/NexTraceOne.ProductAnalytics.Tests/`

**Estado:** 4 ficheiros de teste para módulo de product analytics.

**Comparação:**
| Módulo | Ficheiros de teste |
|--------|--------------------|
| Catalog | 168 |
| AIKnowledge | 100 |
| OperationalIntelligence | 74 |
| IdentityAccess | 58 |
| **ProductAnalytics** | **4** |

**Risco:** Features de analytics de produto não têm garantia de correcção.

---

## 3. Testes de Carga (k6) — Não Integrados em CI

**Estado:** 5 cenários k6 existem em `tests/load/`:
- `auth-load`
- `catalog-load`
- `contracts-load`
- `governance-load`
- `mixed-load`

**Gap:** Nenhum destes cenários é executado automaticamente em CI.

**Consequência:** Regressões de performance passam despercebidas. Um deploy pode degradar p95 de 200ms para 2s sem qualquer alerta automático.

**Solução:** Integrar ao menos o cenário `mixed-load` como smoke de performance no workflow `e2e.yml` (nightly).

---

## 4. Sem Threshold de Cobertura em CI

**Ficheiro:** `src/frontend/vite.config.ts` (secção `test.coverage`)

**Estado actual:**
```typescript
coverage: {
  provider: 'v8',
  reporter: ['text', 'html', 'lcov'],
  exclude: ['node_modules', 'dist', ...]
  // Sem threshold definido
}
```

**Gap:** Não há `thresholds` configurados. O CI não falha se cobertura cair para 0%.

**Solução imediata:**
```typescript
thresholds: {
  lines: 30,
  functions: 30,
  branches: 25,
  statements: 30,
}
```

---

## 5. Coverage Histórico Não Rastreado

**Estado:** CI gera relatórios de coverage com `ReportGenerator` e armazena como artefactos por 30 dias.

**Gap:** Não há integração com Codecov, SonarCloud ou similar para rastrear evolução histórica de cobertura.

**Consequência:** Não há dashboard de tendência de cobertura. Não se sabe se cobertura está a subir ou descer ao longo das semanas.

---

## 6. Testes de Integração Frontend — Ausentes

**Estado:** Os 13 testes existentes são unitários puros (sem render de componentes com providers reais).

**Gap:** Não há testes de integração frontend que testem:
- Componente + TanStack Query + mock de API
- Fluxo de login completo (formulário → contexto → redirect)
- Troca de ambiente e invalidação de cache

**Ferramenta sugerida:** Testing Library com MSW (já instalado: `msw@^2.12.10`).

---

## 7. Testes de Observabilidade — Ausentes

**Gap:** Não há testes que validem:
- Que OpenTelemetry está a emitir spans correctamente
- Que logs estruturados têm os campos obrigatórios
- Que métricas de performance são registadas

**Impacto:** Observabilidade pode estar a falhar silenciosamente sem detecção.

---

## 8. Testes de Segurança Automatizados — Parciais

**O que existe:**
- `security.yml` workflow com CodeQL + npm audit + NuGet scan + Trivy

**O que falta:**
- OWASP ZAP ou similar para testes de penetração automatizados
- Testes de autorização (verificar que endpoint X rejeita role Y)
- Testes de rate limiting (verificar que throttling funciona conforme configurado)

---

## 9. Resumo de Prioridades Testes

| Gap | Severidade | Esforço |
|-----|-----------|---------|
| Frontend unit tests ~4% | P0 — Crítico | Muito alto |
| ProductAnalytics subcoberto | P0 — Crítico | Médio |
| Threshold de cobertura no CI | P1 — Alto | Baixo |
| k6 load tests sem CI | P1 — Alto | Médio |
| Coverage histórico (Codecov) | P1 — Alto | Baixo |
| Testes integração frontend (MSW) | P1 — Alto | Alto |
| Testes observabilidade | P2 — Médio | Médio |
| Testes de autorização automatizados | P2 — Médio | Alto |
