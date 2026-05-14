# Guia de Migração - Query Keys Centralizadas

## 📋 Visão Geral

Este documento descreve a migração de query keys hardcoded para o sistema centralizado `queryKeys` no NexTraceOne frontend.

## 🎯 Objetivo

Eliminar strings soltas espalhadas pelos componentes e garantir invalidação consistente de cache em mutations.

## ✅ Benefícios

1. **Manutenibilidade**: Todas as keys em um único lugar
2. **Type Safety**: TypeScript valida keys automaticamente
3. **Invalidação Fácil**: `queryClient.invalidateQueries({ queryKey: queryKeys.catalog.graph(envId) })`
4. **Consistência**: Padrão uniforme em toda aplicação
5. **Debugging**: Fácil identificar quais queries estão sendo usadas

## 🔍 Antes vs Depois

### ❌ ANTES (Hardcoded)

```typescript
// ServiceCatalogPage.tsx - PROBLEMA
const { data: impactResult } = useQuery({
  queryKey: ['impact', selectedNodeId, impactDepth, activeEnvironmentId],
  queryFn: () => serviceCatalogApi.getImpactPropagation(selectedNodeId!, impactDepth),
});

const createSnapshot = useMutation({
  mutationFn: (label: string) => serviceCatalogApi.createSnapshot(label),
  onSuccess: () => {
    queryClient.invalidateQueries({ queryKey: ['snapshots'] }); // ❌ String solta
  },
});
```

**Problemas:**
- Strings hardcoded difíceis de rastrear
- Risco de typos (`'snapshot'` vs `'snapshots'`)
- Invalidação inconsistente
- Duplicação de lógica

### ✅ DEPOIS (Centralizado)

```typescript
// ServiceCatalogPage.tsx - SOLUÇÃO
import { queryKeys } from '../../../shared/api/queryKeys';

const { data: impactResult } = useQuery({
  queryKey: queryKeys.catalog.impact.propagation(selectedNodeId!, impactDepth, activeEnvironmentId),
  queryFn: () => serviceCatalogApi.getImpactPropagation(selectedNodeId!, impactDepth),
});

const createSnapshot = useMutation({
  mutationFn: (label: string) => serviceCatalogApi.createSnapshot(label),
  onSuccess: () => {
    queryClient.invalidateQueries({ 
      queryKey: queryKeys.catalog.snapshots.all(activeEnvironmentId) 
    }); // ✅ Type-safe
  },
});
```

**Vantagens:**
- Type-safe (autocomplete no IDE)
- Centralizado em um lugar
- Fácil invalidação
- Sem duplicação

---

## 📚 Estrutura do queryKeys

```typescript
export const queryKeys = {
  // Domínio principal
  catalog: {
    all: ['catalog'] as const,
    
    // Sub-domínio
    services: {
      all: () => [...queryKeys.catalog.all, 'services'] as const,
      
      // Queries específicas
      list: (params?: Record<string, unknown>, envId?: string | null) =>
        [...queryKeys.catalog.services.all(), 'list', params, envId] as const,
      detail: (id: string) => [...queryKeys.catalog.services.all(), 'detail', id] as const,
    },
    
    // Outro sub-domínio
    impact: {
      all: () => [...queryKeys.catalog.all, 'impact'] as const,
      propagation: (nodeId: string, depth: number, envId?: string | null) =>
        [...queryKeys.catalog.impact.all(), 'propagation', nodeId, depth, envId] as const,
    },
  },
  
  // Outros domínios...
  incidents: { ... },
  governance: { ... },
  ai: { ... },
  audit: { ... },
} as const;
```

### Padrão de Nomenclatura

```
queryKeys.{dominio}.{subDominio?}.{operacao}(parametros, envId?)
```

**Exemplos:**
```typescript
queryKeys.catalog.graph(envId)
queryKeys.catalog.services.list(params, envId)
queryKeys.catalog.services.detail(id)
queryKeys.incidents.list(params, envId)
queryKeys.governance.finops.summary(params, envId)
queryKeys.ai.models.list()
```

### Regra do environmentId

- **SEMPRE** incluir `envId` como último parâmetro para queries ambiente-específicas
- Isso permite invalidação por prefixo:
  ```typescript
  // Invalida TODAS as queries de catalog, com ou sem envId
  queryClient.invalidateQueries({ queryKey: queryKeys.catalog.all })
  
  // Invalida apenas catalog do ambiente atual
  queryClient.invalidateQueries({ queryKey: queryKeys.catalog.graph(activeEnvironmentId) })
  ```

---

## 🔄 Guia de Migração Passo a Passo

### Passo 1: Identificar Queries Hardcoded

Buscar no código por padrões:
```bash
# Procurar arrays hardcoded em queryKey
grep -r "queryKey: \[" src/frontend/src/features/
```

**Padrões comuns:**
```typescript
queryKey: ['services', id]
queryKey: ['incidents', params, envId]
queryKey: ['catalog', 'graph']
```

### Passo 2: Encontrar Key Equivalente em queryKeys

Consultar `src/frontend/src/shared/api/queryKeys.ts` para encontrar a key correspondente.

**Exemplo de mapeamento:**

| Hardcoded | Centralizado |
|-----------|-------------|
| `['catalog', 'graph', envId]` | `queryKeys.catalog.graph(envId)` |
| `['impact', nodeId, depth, envId]` | `queryKeys.catalog.impact.propagation(nodeId, depth, envId)` |
| `['snapshots', envId]` | `queryKeys.catalog.snapshots.all(envId)` |
| `['incidents', 'list', params, envId]` | `queryKeys.incidents.list(params, envId)` |

### Passo 3: Atualizar Import

Adicionar import no topo do arquivo:
```typescript
import { queryKeys } from '../../../shared/api/queryKeys';
```

**Nota:** Ajustar o caminho relativo conforme a profundidade do arquivo.

### Passo 4: Substituir queryKey

**ANTES:**
```typescript
useQuery({
  queryKey: ['catalog', 'graph', activeEnvironmentId],
  queryFn: () => api.getGraph(),
})
```

**DEPOIS:**
```typescript
useQuery({
  queryKey: queryKeys.catalog.graph(activeEnvironmentId),
  queryFn: () => api.getGraph(),
})
```

### Passo 5: Atualizar Mutations (Invalidação)

**ANTES:**
```typescript
useMutation({
  mutationFn: (data) => api.create(data),
  onSuccess: () => {
    queryClient.invalidateQueries({ queryKey: ['catalog', 'graph'] });
  },
})
```

**DEPOIS:**
```typescript
useMutation({
  mutationFn: (data) => api.create(data),
  onSuccess: () => {
    queryClient.invalidateQueries({ 
      queryKey: queryKeys.catalog.graph(activeEnvironmentId) 
    });
  },
})
```

### Passo 6: Testar

1. Rodar aplicação
2. Navegar para páginas afetadas
3. Verificar que dados carregam corretamente
4. Testar mutations e invalidação de cache

---

## 📋 Checklist de Migração

### Catalog Module
- [x] `ServiceCatalogPage.tsx` - MIGRADO ✅
- [ ] `ServiceDetailPage.tsx`
- [ ] `ContractListPage.tsx`
- [ ] `TemplateLibraryPage.tsx`
- [ ] `SourceOfTruthExplorerPage.tsx`

### Contracts Module
- [ ] `ContractDetailPage.tsx`
- [ ] `ContractPipelinePage.tsx`
- [ ] `DeveloperPortalPage.tsx`

### Operations Module
- [ ] `IncidentListPage.tsx`
- [ ] `IncidentDetailPage.tsx`
- [ ] `RuntimeIntelligenceDashboardPage.tsx`

### Governance Module
- [ ] `GovernanceGatesPage.tsx`
- [ ] `FinOpsDashboardPage.tsx`
- [ ] `RiskAssessmentPage.tsx`

### AI Hub Module
- [ ] `AiCopilotPage.tsx`
- [ ] `AgentMarketplacePage.tsx`
- [ ] `ModelRegistryPage.tsx`

### Audit Module
- [ ] `AuditPage.tsx`

---

## 🛠️ Scripts Úteis

### Buscar queries hardcoded

```bash
# Linux/Mac
grep -rn "queryKey: \[" src/frontend/src/features/ --include="*.tsx"

# Windows PowerShell
Get-ChildItem -Path src/frontend/src/features -Filter *.tsx -Recurse | 
  Select-String -Pattern "queryKey: \[" |
  Select-Object Filename, LineNumber, Line
```

### Validar imports

```bash
# Verificar quais arquivos já usam queryKeys
grep -rn "from.*queryKeys" src/frontend/src/features/ --include="*.tsx"
```

---

## ⚠️ Armadilhas Comuns

### 1. Esquecer environmentId

**ERRADO:**
```typescript
queryKey: queryKeys.catalog.graph()  // ❌ Falta envId
```

**CORRETO:**
```typescript
queryKey: queryKeys.catalog.graph(activeEnvironmentId)  // ✅
```

### 2. Usar string em vez de função

**ERRADO:**
```typescript
queryKey: queryKeys.catalog.services.list  // ❌ Esqueceu de chamar função
```

**CORRETO:**
```typescript
queryKey: queryKeys.catalog.services.list(params, envId)  // ✅ Chamar função
```

### 3. Invalidar com key errada

**ERRADO:**
```typescript
// Mutation cria service, mas invalida graph
onSuccess: () => {
  queryClient.invalidateQueries({ queryKey: queryKeys.catalog.graph(envId) });
}
```

**CORRETO:**
```typescript
// Invalidar a lista de services
onSuccess: () => {
  queryClient.invalidateQueries({ queryKey: queryKeys.catalog.services.list(undefined, envId) });
}
```

---

## 📊 Métricas de Progresso

### Status Atual (2026-05-14)

- **Total de arquivos com queries:** ~50 arquivos
- **Arquivos migrados:** 1 arquivo (ServiceCatalogPage.tsx)
- **Progresso:** 2%

### Meta

- **Curto prazo (1 semana):** Migrar 20 arquivos críticos (40%)
- **Médio prazo (2 semanas):** Migrar todos os arquivos restantes (100%)

---

## 🎓 Referências

- [TanStack Query - Effective React Query Keys](https://tkdodo.eu/blog/effective-react-query-keys)
- [TanStack Query - Colocation](https://tkdodo.eu/blog/colocation-with-react-query)
- [React Query Docs - Query Keys](https://tanstack.com/query/latest/docs/react/guides/query-keys)

---

**Status:** 📝 Em Progresso  
**Última Atualização:** 2026-05-14  
**Responsável:** Equipe Frontend  
**Próxima Revisão:** 2026-05-21
