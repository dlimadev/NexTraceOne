# P12.1 — Licensing Residue Removal Report

> Data: 2026-03-27 | Fase: P12.1 — Remoção definitiva de resíduos de Licensing

---

## 1. Objetivo

Remover definitivamente todos os resíduos ativos de Licensing do NexTraceOne — código, frontend, permissões, configs e documentação ativa — alinhando o repositório ao escopo real do produto após a decisão de eliminar o módulo Licensing.

---

## 2. Estado Anterior do Repositório

Com base no inventário da `licensing-residue-final-audit.md` (Prompt N16), existiam 7 ocorrências ativas de resíduos de Licensing classificadas para remoção ou reescrita:

| ID | Ficheiro | Tipo | Ação Necessária |
|----|----------|------|----------------|
| LIC-01 | `RolePermissionCatalog.cs` | 17 permissões `licensing:*` no Admin; 1 no TechLead | 🔴 REMOVE |
| LIC-02 | `PermissionConfiguration.cs` | 2 seeds `licensing:read` e `licensing:write` em HasData | 🔴 REMOVE |
| LIC-03 | `CreateDelegation.cs` | `licensing:write` em lista de permissões delegáveis | 🔴 REMOVE |
| LIC-04 | `MfaPolicy.cs` | Comentário XML e propriedade `RequiredForVendorOps` com "vendor/licensing" | ✅ RESOLVED (P12.1) — renomeada para `RequiredForSensitiveExternalOps` |
| LIC-05 | `Breadcrumbs.tsx` | `'licensing': 'sidebar.licensing'` no mapeamento de rota | 🔴 REMOVE |
| LIC-06 | `navigation.ts` | `'vendor': 'sidebar.vendorLicensing'` no mapeamento de rota | 🔴 REMOVE |
| LIC-07 | `en.json` (+pt-BR, es) | `"guidanceAdmin"` mencionando "licensing" | 🟡 REWRITE |

Adicionalmente, um resíduo não coberto pelo audit original foi identificado:

| Extra | `DeveloperPortalPage.tsx` | Comentário de código mencionando "LicensingPage" | 🟡 REWRITE |

---

## 3. Inventário de Ocorrências Verificadas e Estado por Ficheiro

### 3.1 Backend

#### RolePermissionCatalog.cs
- **Caminho:** `src/modules/identityaccess/NexTraceOne.IdentityAccess.Domain/Entities/RolePermissionCatalog.cs`
- **Estado ao iniciar P12.1:** Nenhuma referência `licensing:*` encontrada — **já limpo por sessão anterior**
- **Verificação:** `grep -c "licensing" RolePermissionCatalog.cs` → 0 resultados
- **Ação em P12.1:** Nenhuma (já concluído)

#### PermissionConfiguration.cs
- **Caminho:** `src/modules/identityaccess/NexTraceOne.IdentityAccess.Infrastructure/Persistence/Configurations/PermissionConfiguration.cs`
- **Estado ao iniciar P12.1:** Nenhuma referência `licensing:` encontrada — **já limpo por sessão anterior**
- **Verificação:** `grep -c "licens" PermissionConfiguration.cs` → 0 resultados
- **Ação em P12.1:** Nenhuma (já concluído)

#### CreateDelegation.cs
- **Caminho:** `src/modules/identityaccess/NexTraceOne.IdentityAccess.Application/Features/CreateDelegation/CreateDelegation.cs`
- **Estado ao iniciar P12.1:** Nenhuma referência `licensing:write` encontrada — **já limpo por sessão anterior**
- **Verificação:** `grep -c "licens" CreateDelegation.cs` → 0 resultados
- **Ação em P12.1:** Nenhuma (já concluído)

#### MfaPolicy.cs ✅ ALTERADO em P12.1
- **Caminho:** `src/modules/identityaccess/NexTraceOne.IdentityAccess.Domain/ValueObjects/MfaPolicy.cs`
- **Resíduo encontrado:** Nomenclatura "vendor" ligada ao conceito de licensing em múltiplos locais:
  - Sumário da propriedade: já estava como "operações administrativas sensíveis" (correto)
  - Parâmetro `requiredForVendorOps`: descrição dizia "para operações de vendor" (resíduo)
  - Propriedade `RequiredForVendorOps`: nome com "Vendor" diretamente ligado ao módulo Licensing removido
  - `ToString()`: incluía `Vendor={RequiredForVendorOps}` (resíduo semântico)
- **Alterações aplicadas:**
  - Propriedade renomeada: `RequiredForVendorOps` → `RequiredForSensitiveExternalOps`
  - Parâmetro renomeado: `requiredForVendorOps` → `requiredForSensitiveExternalOps`
  - Descrição do parâmetro reescrita: "operações de vendor" → "operações administrativas externas sensíveis"
  - Summary da propriedade expandida: "operações administrativas ou de integração externa sensíveis"
  - `ToString()` atualizado: `Vendor=` → `SensitiveExternal=`
  - Testes em `MfaPolicyTests.cs` atualizados correspondentemente (5 referências)
- **Resultado:** Nenhuma referência a "vendor" ou "licensing" remanescente em MfaPolicy.

### 3.2 Frontend

#### Breadcrumbs.tsx
- **Caminho:** `src/frontend/src/components/Breadcrumbs.tsx`
- **Estado ao iniciar P12.1:** Nenhuma referência `licensing` ou `vendorLicensing` encontrada — **já limpo por sessão anterior**
- **Verificação:** `grep -c "licens\|vendorLicens" Breadcrumbs.tsx` → 0 resultados
- **Ação em P12.1:** Nenhuma (já concluído)

#### navigation.ts
- **Caminho:** `src/frontend/src/utils/navigation.ts`
- **Estado ao iniciar P12.1:** Nenhuma referência `vendor` ou `licensing` encontrada — **já limpo por sessão anterior**
- **Verificação:** `grep -c "vendor\|licens" navigation.ts` → 0 resultados
- **Ação em P12.1:** Nenhuma (já concluído)

#### DeveloperPortalPage.tsx ✅ ALTERADO em P12.1
- **Caminho:** `src/frontend/src/features/catalog/pages/DeveloperPortalPage.tsx`
- **Resíduo encontrado:** Comentário JSDoc na linha 4 mencionava "LicensingPage" como referência de padrão de tabs
- **Alteração aplicada:** `"seguindo o padrão de ServiceCatalogPage e LicensingPage"` → `"seguindo o padrão de ServiceCatalogPage e ContractCatalogPage"`

#### locales/en.json, pt-BR.json, pt-PT.json, es.json
- **Estado ao iniciar P12.1:** `guidanceAdmin` já estava sem menção a "licensing" em todos os locales — **já limpo por sessão anterior**
  - `en.json`: "Administration covers user management, access controls, environments, and audit trails."
  - `pt-BR.json`: "Administração abrange gestão de utilizadores, controlos de acesso, ambientes e trilhos de auditoria."
  - `pt-PT.json`: mesma versão PT sem "licensing"
  - `es.json`: "Administración cubre gestión de usuarios, controles de acceso, entornos y auditoría."
- **Ação em P12.1:** Nenhuma (já concluído)

---

## 4. Ocorrências Legítimas Mantidas (Não Residuais)

As seguintes ocorrências do termo "license" foram verificadas e **não são resíduos do módulo Licensing**:

| Ficheiro | Conteúdo | Justificativa |
|----------|----------|---------------|
| `VisualRestBuilder.tsx`, `builderTypes.ts`, `builderSync.ts` | Campo `license` no builder de contratos REST | Refere-se à licença OpenAPI do contrato (MIT, Apache) — legítimo |
| `en.json` / locale files | `"license": "License"` e `"licensePlaceholder": "MIT"` | Campos do builder REST para licença do API schema |
| `AIModel.cs` | `LicenseName`, `LicenseUrl` | Licença do modelo de IA (ex.: Apache 2.0, MIT) — legítimo |
| `ConfigurationDefinitionSeeder.cs` | `"license":"Proprietary"` e `"UnusedLicenses"` | Meta-info de API e categoria de waste em FinOps — legítimo |
| `ActivationPage.tsx`, `identity.ts`, `auth.ts` | `activation`, `activateAccount` | Ativação de conta de utilizador (primeiro acesso), não ativação de licença |
| `ActivateModel.cs` | `ActivateModel` | Ativação de modelo de IA para uso — não licensing |
| `MfaPolicyTests.cs` | 5 referências a `RequiredForVendorOps`/`requiredForVendorOps` → `RequiredForSensitiveExternalOps`/`requiredForSensitiveExternalOps` | Legítimo — atualizado junto com rename da propriedade |

---

## 5. Documentação Ativa Atualizada

Os seguintes documentos de arquitetura foram atualizados para refletir o estado pós-P12.1:

| Ficheiro | Alteração |
|----------|-----------|
| `docs/architecture/phase-a-open-items.md` | OI-05 marcado como ✅ CLOSED (P12.1); tabela e ordem de execução atualizadas |
| `docs/architecture/placeholder-and-cosmetic-ui-report.md` | PH-08 marcado como ✅ RESOLVED (P12.1); tabela de ações atualizada |
| `docs/architecture/module-seed-strategy.md` | "17 licensing permissions" marcado como ✅ Removido; tarefa 4 da lista marcada |
| `docs/architecture/migration-readiness-by-module.md` | Row da Identity & Access atualizado: MFA enforced (P11.3 ✅), 17 permissões licensing removidas (P12.1 ✅); OI-05 marcado como CLOSED |
| `docs/architecture/execution-phase-readiness-report.md` | Tarefas 1-4 de limpeza de licensing marcadas como ✅ P12.1 |

---

## 6. Ficheiros Não Alterados (Históricos/Legítimos)

Os seguintes documentos mencionam licensing mas são **registos históricos ou de auditoria** — foram deliberadamente preservados sem alteração:

- `docs/architecture/licensing-residue-final-audit.md` — auditoria que originou este trabalho
- `docs/architecture/architecture-decisions-final.md` — decisão arquitetural de remoção do módulo
- `docs/architecture/n-phase-final-validation-and-closure.md` — encerramento da Trilha N
- `docs/architecture/e18-final-technical-closure-report.md` — encerramento técnico E18
- `docs/architecture/out-of-scope-residue-report.md` — relatório de resíduos fora de escopo
- `docs/audits/2026-03-25/licensing-selfhosted-readiness-report.md` — auditoria de readiness
- `docs/11-review-modular/01-identity-access/licensing-residue-cleanup-review.md` — revisão que gerou o inventário
- `docs/WAVE-1-CONSOLIDATED-VALIDATION.md` — validação histórica da Wave 1
- `docs/architecture/migration-transition-risks-and-mitigations.md` — registo de riscos histórico

---

## 7. Validação de Compilação e Consistência

### Backend
- Nenhum ficheiro `.cs` contém referências ativas a `licensing:*` permissões no contexto do módulo Licensing
- `MfaPolicy.cs`: propriedade renomeada para `RequiredForSensitiveExternalOps`, comentários reescritos, testes atualizados — compilação preservada, 10 testes passam
- Verificação: `grep -ri "licensing:" src/modules/ --include="*.cs"` → 0 resultados

### Frontend
- Nenhum ficheiro `.ts` ou `.tsx` contém referências a rotas, labels ou breadcrumbs de licensing
- `DeveloperPortalPage.tsx`: comentário de código corrigido; comportamento inalterado
- Locales: todos os arquivos sem referências a "Licensing" como módulo/feature
- Verificação: `grep -ri "licensing" src/frontend/src --include="*.ts" --include="*.tsx"` → 0 resultados relevantes

### Permissões
- `RolePermissionCatalog.cs`: 0 entradas `licensing:*`
- `PermissionConfiguration.cs`: 0 seeds de licensing
- `CreateDelegation.cs`: 0 referências a `licensing:write`

---

## 8. Resumo Executivo

| Camada | Estado Anterior | Estado Pós-P12.1 |
|--------|----------------|-----------------|
| Domain (Identity) | LIC-01 (17 permissões) + LIC-04 (propriedade+comentário) | ✅ Limpo |
| Infrastructure (Identity) | LIC-02 (2 seeds HasData) | ✅ Limpo |
| Application (Identity) | LIC-03 (1 permissão delegável) | ✅ Limpo |
| Frontend (Navigation) | LIC-05, LIC-06 (2 mapeamentos) | ✅ Limpo |
| Frontend (i18n) | LIC-07 (guidanceAdmin) | ✅ Limpo |
| Frontend (Comentário) | DeveloperPortalPage (LicensingPage ref) | ✅ Limpo |
| Configuração | LIC-08 (uso legítimo) | ✅ Mantido sem alteração |
| AI Domain | AIModel.LicenseName/Url | ✅ Mantido sem alteração |

**Critério de completude alcançado:**
- [x] 0 referências a `licensing:` em permissões e seeds do backend
- [x] 0 mapeamentos de rota `'licensing'` ou `'vendor'` no frontend
- [x] 0 menções a "licensing" em strings de guidance i18n
- [x] Comentários de código sem referências a licensing como módulo/feature
- [x] Documentação ativa atualizada para refletir remoção concluída
- [x] Código compila sem alterações de comportamento
