# Relatório de i18n do Frontend — NexTraceOne

> **Data:** 2025-07-14  
> **Versão:** 2.0  
> **Escopo:** Auditoria completa de internacionalização (4 locales)  
> **Status global:** GAP_IDENTIFIED  
> **Diretório de locales:** `src/frontend/src/locales/`  
> **Ficheiro de configuração:** `src/frontend/src/i18n.ts`

---

## 1. Resumo

| Métrica | Valor |
|---------|-------|
| Locales suportados | 4 (en, pt-PT, pt-BR, es) |
| Locale de referência | en (English) |
| Chaves top-level em en | 63 |
| Chaves em pt-PT | 62 (-1) |
| Chaves em pt-BR | 52 (-11) |
| Chaves em es | 55 (-8) |
| Total de linhas i18n | ~15.739 |
| Status geral | ❌ GAP_IDENTIFIED |
| Prioridade | HIGH |

---

## 2. Cobertura por Locale

| Locale | Ficheiro | Linhas | Chaves top-level | Diferença vs en | Cobertura | Status |
|--------|----------|--------|-----------------|-----------------|-----------|--------|
| en (referência) | `locales/en.json` | 4.810 | 63 | — | 100% | ✅ REFERÊNCIA |
| pt-PT | `locales/pt-PT.json` | 4.029 | 62 | -1 | 98.4% | ⚠️ PARTIAL |
| pt-BR | `locales/pt-BR.json` | 3.092 | 52 | -11 | 82.5% | ❌ GAP_IDENTIFIED |
| es | `locales/es.json` | 3.808 | 55 | -8 | 87.3% | ❌ GAP_IDENTIFIED |

---

## 3. Namespaces em Falta — Detalhe

### 3.1 pt-PT (1 namespace em falta)

| Namespace em falta | Módulo afetado | Impacto | Prioridade |
|-------------------|---------------|---------|------------|
| `agents` | ai-hub (AiAgentsPage, AgentDetailPage) | Utilizadores pt-PT verão textos em inglês na secção de agentes IA | MEDIUM |

### 3.2 pt-BR (11 namespaces em falta)

| Namespace em falta | Módulo afetado | Impacto | Prioridade |
|-------------------|---------------|---------|------------|
| `agents` | ai-hub | Textos de agentes IA em inglês | HIGH |
| `analytics` | product-analytics | Toda a secção de analytics em inglês | HIGH |
| `automation` | operations | Textos de automação em inglês | HIGH |
| `breadcrumbs` | shell (global) | Navegação breadcrumb em inglês | MEDIUM |
| `domainBadges` | governance | Badges de domínio em inglês | MEDIUM |
| `governancePacks` | governance | Packs de governance em inglês | HIGH |
| `integrations` | integrations | Toda a secção de integrações em inglês | HIGH |
| `onboarding` | shared | Fluxo de onboarding em inglês | MEDIUM |
| `persona` | shell (PersonaContext) | Nomes e config de persona em inglês | MEDIUM |
| `productPolish` | shell/UX | Textos de polimento de UX em inglês | LOW |
| `shell` | shell (AppShell) | Elementos estruturais em inglês | HIGH |

### 3.3 es (8 namespaces em falta)

| Namespace em falta | Módulo afetado | Impacto | Prioridade |
|-------------------|---------------|---------|------------|
| `activation` | identity-access (ActivationPage) | Página de ativação em inglês | HIGH |
| `agents` | ai-hub | Textos de agentes IA em inglês | HIGH |
| `domainBadges` | governance | Badges de domínio em inglês | MEDIUM |
| `forgotPassword` | identity-access (ForgotPasswordPage) | Página de recuperação de senha em inglês | HIGH |
| `invitation` | identity-access (InvitationPage) | Página de convite em inglês | HIGH |
| `mfa` | identity-access (MfaPage) | Página de MFA em inglês | HIGH |
| `preview` | shared/feature flags | Textos de preview em inglês | LOW |
| `resetPassword` | identity-access (ResetPasswordPage) | Página de reset de senha em inglês | HIGH |

---

## 4. Status por Módulo e Namespace

### 4.1 Tabela completa

| Namespace | Módulo | en | pt-PT | pt-BR | es | Status geral |
|-----------|--------|:--:|:-----:|:-----:|:--:|:-------------|
| activation | identity-access | ✅ | ✅ | ✅ | ❌ | PARTIAL |
| advancedConfig | configuration | ✅ | ✅ | ✅ | ✅ | OK |
| agents | ai-hub | ✅ | ❌ | ❌ | ❌ | GAP_IDENTIFIED |
| aiAnalysis | ai-hub | ✅ | ✅ | ✅ | ✅ | OK |
| aiHub | ai-hub | ✅ | ✅ | ✅ | ✅ | OK |
| aiIntegrationsConfig | ai-hub | ✅ | ✅ | ✅ | ✅ | OK |
| analytics | product-analytics | ✅ | ✅ | ❌ | ✅ | PARTIAL |
| audit | audit-compliance | ✅ | ✅ | ✅ | ✅ | OK |
| auth | identity-access | ✅ | ✅ | ✅ | ✅ | OK |
| authorization | identity-access | ✅ | ✅ | ✅ | ✅ | OK |
| automation | operations | ✅ | ✅ | ❌ | ✅ | PARTIAL |
| breadcrumbs | shell | ✅ | ✅ | ❌ | ✅ | PARTIAL |
| catalog | catalog | ✅ | ✅ | ✅ | ✅ | OK |
| catalogContractsConfig | catalog | ✅ | ✅ | ✅ | ✅ | OK |
| changeConfidence | change-governance | ✅ | ✅ | ✅ | ✅ | OK |
| commandPalette | shell | ✅ | ✅ | ✅ | ✅ | OK |
| common | shared | ✅ | ✅ | ✅ | ✅ | OK |
| configuration | configuration | ✅ | ✅ | ✅ | ✅ | OK |
| contractGov | contracts | ✅ | ✅ | ✅ | ✅ | OK |
| contracts | contracts | ✅ | ✅ | ✅ | ✅ | OK |
| dashboard | shared | ✅ | ✅ | ✅ | ✅ | OK |
| developerPortal | catalog | ✅ | ✅ | ✅ | ✅ | OK |
| domainBadges | governance | ✅ | ✅ | ❌ | ❌ | GAP_IDENTIFIED |
| environment | shell | ✅ | ✅ | ✅ | ✅ | OK |
| environments | identity-access | ✅ | ✅ | ✅ | ✅ | OK |
| errors | shared | ✅ | ✅ | ✅ | ✅ | OK |
| forgotPassword | identity-access | ✅ | ✅ | ✅ | ❌ | PARTIAL |
| governance | governance | ✅ | ✅ | ✅ | ✅ | OK |
| governanceConfig | governance | ✅ | ✅ | ✅ | ✅ | OK |
| governancePacks | governance | ✅ | ✅ | ❌ | ✅ | PARTIAL |
| header | shell | ✅ | ✅ | ✅ | ✅ | OK |
| identity | identity-access | ✅ | ✅ | ✅ | ✅ | OK |
| incidents | operations | ✅ | ✅ | ✅ | ✅ | OK |
| integrations | integrations | ✅ | ✅ | ❌ | ✅ | PARTIAL |
| invitation | identity-access | ✅ | ✅ | ✅ | ❌ | PARTIAL |
| mfa | identity-access | ✅ | ✅ | ✅ | ❌ | PARTIAL |
| notificationConfig | notifications | ✅ | ✅ | ✅ | ✅ | OK |
| notifications | notifications | ✅ | ✅ | ✅ | ✅ | OK |
| onboarding | shared | ✅ | ✅ | ❌ | ✅ | PARTIAL |
| opsFinOpsConfig | operational-intelligence | ✅ | ✅ | ✅ | ✅ | OK |
| persona | shell | ✅ | ✅ | ❌ | ✅ | PARTIAL |
| platformOps | operations | ✅ | ✅ | ✅ | ✅ | OK |
| preview | shared | ✅ | ✅ | ✅ | ❌ | PARTIAL |
| productPolish | shell/UX | ✅ | ✅ | ❌ | ✅ | PARTIAL |
| promotion | change-governance | ✅ | ✅ | ✅ | ✅ | OK |
| releaseScope | shell | ✅ | ✅ | ✅ | ✅ | OK |
| releases | change-governance | ✅ | ✅ | ✅ | ✅ | OK |
| resetPassword | identity-access | ✅ | ✅ | ✅ | ❌ | PARTIAL |
| shell | shell | ✅ | ✅ | ❌ | ✅ | PARTIAL |
| sidebar | shell | ✅ | ✅ | ✅ | ✅ | OK |
| (+ restantes) | diversos | ✅ | ✅ | ✅ | ✅ | OK |

### 4.2 Resumo de status

| Status | en | pt-PT | pt-BR | es |
|--------|:--:|:-----:|:-----:|:--:|
| OK (todas as chaves) | 63 | 49 | 39 | 42 |
| PARTIAL (alguma falta) | — | — | 13 | 13 |
| GAP_IDENTIFIED (falta namespace) | — | 1 | 11 | 8 |
| MISSING (sem ficheiro) | — | 0 | 0 | 0 |

---

## 5. Análise de Impacto

### 5.1 Impacto por locale

| Locale | Utilizadores afetados | Páginas com fallback inglês | Gravidade |
|--------|----------------------|---------------------------|-----------|
| pt-PT | Portugal | ~2 páginas (agentes IA) | BAIXA |
| pt-BR | Brasil | ~15+ páginas (analytics, automação, integrações, shell, etc.) | ALTA |
| es | Espanha/LATAM | ~8 páginas (auth flow, agentes IA) | ALTA |

### 5.2 Impacto por área funcional

| Área | pt-PT | pt-BR | es | Impacto |
|------|:-----:|:-----:|:--:|---------|
| Autenticação (login, MFA, reset) | ✅ | ✅ | ❌ (5 namespaces) | CRITICAL para es |
| AI Hub (agentes) | ❌ | ❌ | ❌ | ALTO (todos locales) |
| Operations (automação) | ✅ | ❌ | ✅ | MÉDIO para pt-BR |
| Analytics | ✅ | ❌ | ✅ | MÉDIO para pt-BR |
| Governance (packs, badges) | ✅ | ❌ | ❌ | MÉDIO para pt-BR e es |
| Shell/UX (breadcrumbs, persona) | ✅ | ❌ | ✅ | ALTO para pt-BR |
| Integrações | ✅ | ❌ | ✅ | MÉDIO para pt-BR |

---

## 6. Strings Hardcoded

### 6.1 Áreas de risco

Embora a auditoria não tenha identificado strings hardcoded sistematicamente no código fonte, as seguintes áreas merecem verificação manual:

| Área | Risco | Ficheiros a verificar |
|------|-------|----------------------|
| Mensagens de erro inline | MÉDIO | Componentes de formulário |
| Tooltips e placeholders | MÉDIO | Componentes UI |
| Labels de gráficos | BAIXO | Componentes de dados |
| Mensagens de estados vazios | BAIXO | EmptyState usages |
| Console logs com texto de UI | BAIXO | Todos os ficheiros TSX |

### 6.2 Recomendação

Executar uma busca automatizada por strings literais em ficheiros `.tsx`:
```bash
grep -rn '"[A-Z][a-z].*"' src/frontend/src/features/ --include="*.tsx" | grep -v "import\|export\|const\|type\|interface"
```

---

## 7. Recomendações

### 7.1 CRITICAL

| # | Ação | Locale | Esforço |
|---|------|--------|---------|
| 1 | Adicionar namespaces `activation`, `forgotPassword`, `resetPassword`, `invitation`, `mfa` ao es.json | es | Médio |

### 7.2 HIGH

| # | Ação | Locale | Esforço |
|---|------|--------|---------|
| 2 | Adicionar namespace `agents` a pt-PT, pt-BR e es | Todos | Baixo |
| 3 | Adicionar namespaces `analytics`, `automation`, `integrations`, `shell`, `governancePacks` ao pt-BR | pt-BR | Médio |
| 4 | Adicionar namespaces `breadcrumbs`, `onboarding`, `persona` ao pt-BR | pt-BR | Baixo |

### 7.3 MEDIUM

| # | Ação | Locale | Esforço |
|---|------|--------|---------|
| 5 | Adicionar `domainBadges` ao pt-BR e es | pt-BR, es | Baixo |
| 6 | Adicionar `productPolish` ao pt-BR | pt-BR | Baixo |
| 7 | Adicionar `preview` ao es | es | Baixo |

### 7.4 Processo recomendado

1. **Extrair** todas as chaves do `en.json` como referência
2. **Comparar** automaticamente com os outros 3 locales
3. **Gerar** ficheiros de diff com chaves em falta
4. **Traduzir** usando o en.json como base
5. **Validar** com teste automatizado (comparar chaves entre locales)
6. **Implementar** teste CI que falhe quando houver chaves em falta

---

## 8. Proposta de Teste Automatizado

```typescript
// Sugestão de teste para CI
describe('i18n completeness', () => {
  const enKeys = extractKeys(enJson);
  
  ['pt-PT', 'pt-BR', 'es'].forEach(locale => {
    it(`${locale} should have all keys from en`, () => {
      const localeKeys = extractKeys(localeJson[locale]);
      const missing = enKeys.filter(k => !localeKeys.includes(k));
      expect(missing).toEqual([]);
    });
  });
});
```

---

*Documento gerado como parte da auditoria modular do NexTraceOne.*
