# Onda 5 — Hybrid Dependency Graph

> **Duração estimada:** 3-4 semanas
> **Dependências:** Ondas 1 e 3
> **Risco:** Médio — complexidade de grafo com muitos tipos de nós
> **Referência:** [LEGACY-MAINFRAME-WAVES.md](../LEGACY-MAINFRAME-WAVES.md)

---

## Objetivo

Estender o grafo de dependências existente para incluir ativos legacy e relações híbridas (moderno ↔ legacy). Após esta onda, o blast radius de uma mudança será calculado percorrendo nós modernos e legacy.

---

## Entregáveis

- [ ] Extensão do `GraphSnapshot` para suportar novos `NodeType` e `EdgeType` values
- [ ] Registro de dependências legacy (program → copybook, job → program, service → MQ queue)
- [ ] Dependências cross-platform (REST API → MQ Queue → CICS Transaction)
- [ ] Legacy dependency sync (API + manual)
- [ ] Visualização do grafo híbrido na UI
- [ ] Blast radius calculation que percorre nós legacy

---

## Impacto Backend

### Extensão do Grafo Existente

O modelo de grafo atual usa `ServiceAsset` (NodeType) + `ConsumerRelationship` (EdgeType). A extensão:

1. **Novos tipos de nós** já adicionados na Onda 0 (`MainframeSystem`, `BatchJob`, `CicsTransaction`, `MqQueue`, `Copybook`)
2. **Novos tipos de arestas** já adicionados na Onda 0 (`Produces`, `Consumes`, `Triggers`, `Schedules`, `Uses`, `BoundTo`)

### Novos Handlers

| Feature | Tipo | Descrição |
|---|---|---|
| `RegisterLegacyDependency` | Command | Regista dependência entre ativos legacy |
| `RegisterHybridDependency` | Command | Regista dependência moderno ↔ legacy |
| `SyncLegacyDependencies` | Command | Sync bulk via API |
| `CalculateHybridBlastRadius` | Query | Blast radius percorrendo nós hybrid |
| `GetHybridGraph` | Query | Grafo completo ou filtrado |

### Ingestion API

Novo endpoint:
```
POST /api/v1/legacy/dependencies
```

```csharp
public sealed record LegacyDependencySyncRequest(
    string? Provider,
    string? CorrelationId,
    List<LegacyDependencyItem> Dependencies
);

public sealed record LegacyDependencyItem(
    string SourceType,      // "Service", "BatchJob", "CobolProgram", etc.
    string SourceName,
    string TargetType,
    string TargetName,
    string RelationType,    // "DependsOn", "Calls", "Produces", "Consumes", etc.
    string? Environment,
    Dictionary<string, string>? Metadata
);
```

### Blast Radius Híbrido

Extensão do cálculo existente de `BlastRadiusReport`:

```
Mudança em Copybook X
  → Programas COBOL que usam X (Uses edge)
    → Transações CICS que executam esses programas (Calls edge)
      → APIs z/OS Connect que expõem essas transações (BoundTo edge)
        → Serviços REST que consomem essas APIs (DependsOn edge)
          → Consumidores frontend/mobile (DependsOn edge)
```

**Extensão do `BlastRadiusReport`:**
```csharp
// Novos campos
public IReadOnlyList<string> AffectedLegacyAssets { get; }
public IReadOnlyList<string> AffectedModernServices { get; }
public int TotalAffectedLegacyAssets { get; }
public bool CrossesPlatformBoundary { get; }  // true se impacta moderno E legacy
```

---

## Impacto Frontend

### Extensão do Dependency Graph

**Página existente:** `/services/graph`

**Novos componentes:**

| Componente | Descrição |
|---|---|
| `HybridDependencyNode` | Nó de grafo com ícone/cor diferenciada por tipo (moderno vs legacy) |
| `LegacyNodeIcon` | Ícones específicos: mainframe, batch, MQ, CICS, copybook |
| `CrossPlatformEdge` | Aresta visual diferenciada para conexões moderno ↔ legacy |
| `GraphTypeFilter` | Filtro: "All", "Modern Only", "Legacy Only", "Hybrid" |
| `BlastRadiusLegacyOverlay` | Overlay que destaca nós legacy afetados |

**Filtros adicionais:**
- Tipo de nó: Service, API, BatchJob, CicsTransaction, MqQueue, Copybook, MainframeSystem
- Plataforma: Modern, Legacy, All
- Overlay mode: Health, Risk, ChangeVelocity, **BatchCriticality** (novo)

### Ícones por Tipo de Nó

| Tipo | Ícone sugerido | Cor |
|---|---|---|
| MainframeSystem | 🖥️ Server rack | Azul escuro |
| BatchJob | ⚙️ Gear/clock | Laranja |
| CicsTransaction | 🔄 Transaction | Verde escuro |
| MqQueue | 📨 Message queue | Roxo |
| Copybook | 📋 Document/layout | Amarelo escuro |
| CobolProgram | 💻 Code | Cinza |

---

## Impacto Base de Dados

Extensão das tabelas de grafo existentes:

| Tabela | Alteração |
|---|---|
| `cat_consumer_relationships` | Novos `EdgeType` values (Produces, Consumes, Triggers, etc.) |
| `cat_service_assets` | Novos `ServiceType` values para nós legacy |
| Possível: `cat_graph_node_aliases` | Aliases para matching em sync |

---

## Testes

### Testes Unitários (~30)
- Graph traversal com nós legacy
- Blast radius com path moderno → MQ → CICS → COBOL → copybook
- Filtros por tipo de nó
- Cross-platform boundary detection

### Testes de Integração (~20)
- Registro de dependência legacy
- Sync bulk de dependências
- Blast radius calculation end-to-end
- Grafo visual com dados reais

---

## Critérios de Aceite

1. ✅ Grafo mostra REST API → MQ Queue → CICS Transaction → COBOL Program → Copybook
2. ✅ Blast radius calcula impacto através de nós legacy
3. ✅ Filtros permitem ver apenas legacy, apenas moderno, ou híbrido
4. ✅ Dependências sincronizáveis via API
5. ✅ Ícones diferenciados por tipo de nó
6. ✅ `CrossesPlatformBoundary` calculado corretamente
7. ✅ Performance aceitável com 1000+ nós no grafo

---

## Riscos

| Risco | Severidade | Mitigação |
|---|---|---|
| Grafo pesado com muitos nós legacy | Média | Lazy loading. Clustering. Filtros obrigatórios |
| Dependências não óbvias (e.g., DB2 view → programa) | Média | Suportar import manual e bulk sync |
| Blast radius muito amplo em grafo denso | Média | Limitar profundidade de traversal (configurável) |

---

## Stories

| ID | Story | Prioridade |
|---|---|---|
| W5-S01 | Implementar `RegisterLegacyDependency` command | P1 |
| W5-S02 | Implementar `RegisterHybridDependency` command | P1 |
| W5-S03 | Implementar `SyncLegacyDependencies` via Ingestion API | P1 |
| W5-S04 | Implementar `CalculateHybridBlastRadius` | P0 |
| W5-S05 | Implementar `GetHybridGraph` query com filtros | P1 |
| W5-S06 | Estender `BlastRadiusReport` com campos legacy | P1 |
| W5-S07 | Criar `HybridDependencyNode` component frontend | P1 |
| W5-S08 | Criar `LegacyNodeIcon` set de ícones | P1 |
| W5-S09 | Criar `GraphTypeFilter` (Modern/Legacy/Hybrid) | P1 |
| W5-S10 | Criar `BlastRadiusLegacyOverlay` visual | P2 |
| W5-S11 | Testes de graph traversal com nós legacy (~30) | P1 |
| W5-S12 | Testes de integração blast radius (~20) | P1 |
