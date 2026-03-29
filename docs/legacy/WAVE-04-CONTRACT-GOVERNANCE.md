# Onda 4 — Legacy Contract Governance

> **Duração estimada:** 4-5 semanas
> **Dependências:** Onda 1
> **Risco:** Alto — parser de COBOL copybook é complexo
> **Referência:** [LEGACY-MAINFRAME-WAVES.md](../LEGACY-MAINFRAME-WAVES.md)

---

## Objetivo

Governança completa de contratos legacy — copybooks COBOL, MQ message contracts, fixed record layouts. Após esta onda, será possível importar, versionar, fazer diff semântico e classificar breaking changes em contratos legacy, da mesma forma que para contratos REST/AsyncAPI.

---

## Entregáveis

- [ ] `CopybookParser` — parser de COBOL copybook para definição estruturada de campos
- [ ] `MqMessageDescriptorParser` — parser de descriptors MQ
- [ ] Import de copybooks (arquivo COBOL ou JSON normalizado)
- [ ] Versionamento de copybooks
- [ ] Diff semântico entre versões de copybook
- [ ] Classificação automática: breaking vs non-breaking change
- [ ] Mapeamento copybook ↔ OpenAPI/AsyncAPI (referência cruzada)
- [ ] Catálogo de MQ message contracts
- [ ] Copybook Viewer na UI
- [ ] Copybook Diff Viewer na UI

---

## Impacto Backend

### CopybookParser

O parser de COBOL copybook é o componente mais complexo desta onda.

**Input:** Texto COBOL de copybook
```cobol
       01  CUSTOMER-RECORD.
           05  CUST-ID            PIC 9(10).
           05  CUST-NAME.
               10  FIRST-NAME    PIC X(30).
               10  LAST-NAME     PIC X(30).
           05  CUST-BALANCE       PIC S9(9)V99 COMP-3.
           05  CUST-STATUS        PIC X(1).
               88  ACTIVE         VALUE 'A'.
               88  INACTIVE       VALUE 'I'.
           05  CUST-ADDRESSES     OCCURS 3 TIMES.
               10  ADDR-LINE1    PIC X(50).
               10  ADDR-LINE2    PIC X(50).
               10  ADDR-CITY     PIC X(30).
```

**Output:** `CopybookLayout` com campos estruturados:

```csharp
public sealed record CopybookField(
    int Level,                    // 01, 05, 10, 88
    string Name,                  // CUST-ID, FIRST-NAME
    string? PicClause,            // PIC 9(10), PIC X(30), PIC S9(9)V99
    string DataType,              // "numeric", "alphanumeric", "packed", "binary"
    int Offset,                   // Byte offset from start
    int Length,                   // Length in bytes
    int? DecimalPositions,        // For numeric with V
    bool IsGroup,                 // True for group items (no PIC)
    int? OccursCount,             // OCCURS n TIMES
    bool IsRedefines,             // REDEFINES clause
    string? RedefinesTarget,      // Target of REDEFINES
    List<string>? ConditionValues // 88-level values
);
```

**Scope inicial do parser (Fase 1):**
- ✅ Level numbers (01-49, 66, 77, 88)
- ✅ PIC clauses: `9`, `X`, `A`, `S`, `V`, `COMP`, `COMP-3`
- ✅ Group items (sem PIC)
- ✅ OCCURS (fixo)
- ✅ REDEFINES
- ✅ 88-level conditions
- ⚠️ OCCURS DEPENDING ON — suporte parcial (registar existência, sem cálculo dinâmico)
- ❌ COPY REPLACING — fase futura
- ❌ Reference modification — não aplicável

### Diff Semântico de Copybooks

Comparação campo a campo entre duas versões:

| Tipo de Mudança | Classificação | Exemplo |
|---|---|---|
| Campo adicionado ao final | Non-breaking | Novo campo `CUST-EMAIL` adicionado |
| Campo removido | **Breaking** | `CUST-FAX` removido |
| Campo com tipo alterado | **Breaking** | `PIC X(10)` → `PIC 9(10)` |
| Campo com comprimento reduzido | **Breaking** | `PIC X(30)` → `PIC X(20)` |
| Campo com comprimento aumentado | **Breaking** (altera offsets) | `PIC X(20)` → `PIC X(30)` |
| Campo renomeado | **Breaking** | `CUST-ID` → `CUSTOMER-ID` |
| OCCURS count alterado | **Breaking** | `OCCURS 3` → `OCCURS 5` |
| 88-level value alterado | Non-breaking | Novo valor em condition name |
| Grupo reestruturado | **Breaking** | Sub-campos reorganizados |

**Output do diff:**
```csharp
public sealed record CopybookDiff(
    string CopybookName,
    string VersionFrom,
    string VersionTo,
    ChangeLevel OverallChangeLevel,    // Breaking, Additive, NonBreaking
    IReadOnlyList<CopybookFieldChange> FieldChanges
);

public sealed record CopybookFieldChange(
    string FieldName,
    CopybookFieldChangeType ChangeType, // Added, Removed, Modified, Moved
    string? OldValue,
    string? NewValue,
    bool IsBreaking,
    string Description
);
```

### MQ Message Contract

```csharp
public sealed record MqMessageContract(
    string QueueName,
    string MessageFormat,            // "MQFMT_STRING", "MQFMT_NONE", custom
    string? PayloadSchema,           // Copybook reference or JSON Schema
    string? CopybookReference,       // Link to copybook if applicable
    int? MaxMessageLength,
    string? HeaderFormat,
    string? EncodingScheme
);
```

### Commands e Handlers

| Feature | Tipo |
|---|---|
| `ImportCopybookLayout` | Command — faz parse e persiste |
| `VersionCopybook` | Command — cria nova versão |
| `DiffCopybookVersions` | Query — compara duas versões |
| `ClassifyCopybookChange` | Command — classifica breaking/non-breaking |
| `MapCopybookToContract` | Command — cria referência cruzada |
| `RegisterMqContract` | Command — regista contrato MQ |
| `ListLegacyContracts` | Query — lista contratos legacy |
| `GetCopybookDetail` | Query — detalhe com campos e versões |

---

## Impacto Frontend

### Nova Página: Copybook Viewer

**Rota:** `/contracts/legacy/:copybookId`
**Persona:** Engineer, Architect

**Componentes:**
- `CopybookViewer` — visualização hierárquica dos campos
- `CopybookFieldsTable` — tabela com nome, tipo, offset, comprimento, PIC clause
- `CopybookVersionSelector` — dropdown para selecionar versão
- `CopybookMetadata` — ownership, última alteração, programas que usam

### Nova Página: Copybook Diff Viewer

**Rota:** `/contracts/legacy/:copybookId/diff`
**Persona:** Engineer, Architect, CAB

**Componentes:**
- `CopybookDiffViewer` — diff visual entre versões
- `CopybookDiffSummary` — resumo: N campos adicionados, N removidos, breaking/non-breaking
- `CopybookDiffFieldList` — lista de mudanças campo a campo com cores (verde/vermelho)
- `BreakingChangeAlert` — alerta visual quando há breaking change

### Extensão do Contract Catalog

- Novos tipos de contrato na lista: Copybook, MQ Message, Fixed Layout
- Filtro por tipo de contrato (REST, SOAP, Event, Copybook, MQ)
- Ícones diferenciados para contratos legacy
- Badge "Breaking" quando última versão tem breaking change

---

## Impacto Base de Dados

### Novas Tabelas ou Extensões

Se `Copybook` e `CopybookField` já foram criados na Onda 1, esta onda adiciona:

| Tabela | Descrição |
|---|---|
| `cat_copybook_versions` | Histórico de versões de copybook |
| `cat_copybook_diffs` | Diffs entre versões |
| `cat_mq_contracts` | Contratos de mensagens MQ |
| `cat_copybook_contract_mappings` | Mapeamento copybook ↔ OpenAPI/AsyncAPI |

---

## Testes

### Testes Unitários (~80)
- CopybookParser: PIC clauses variadas, OCCURS, REDEFINES, groups, 88-levels
- Diff: cada tipo de mudança (add, remove, modify, move)
- Classification: breaking vs non-breaking
- MqMessageDescriptorParser: formatos variados
- Validators: inputs inválidos

### Testes de Integração (~10)
- Import → parse → persist → retrieve
- Version → diff → classify
- MQ contract registration e retrieval

---

## Critérios de Aceite

1. ✅ Parser de copybook funcional para PIC S9, PIC X, OCCURS, REDEFINES
2. ✅ Diff mostra campos adicionados/removidos/alterados
3. ✅ Breaking change detectado automaticamente
4. ✅ Copybook visualizável com campos, tipos e offsets
5. ✅ Versionamento funcional com histórico
6. ✅ Mapeamento copybook ↔ OpenAPI funcional
7. ✅ MQ message contracts registáveis e consultáveis
8. ✅ UI com viewer e diff viewer completos

---

## Riscos

| Risco | Severidade | Mitigação |
|---|---|---|
| COBOL copybook syntax muito variada | Alta | Começar com subset. Suportar import JSON como fallback. Iterar parser |
| Copybooks com COPY REPLACING | Média | Fase futura. Documentar limitação |
| Cálculo de offsets com COMP/COMP-3 | Média | Lookup table de tamanhos por tipo |
| Encoding EBCDIC vs ASCII | Média | Assumir conversão já feita pelo export tool |

---

## Stories

| ID | Story | Prioridade |
|---|---|---|
| W4-S01 | Implementar `CopybookParser` (Fase 1 — PIC, OCCURS, REDEFINES, 88) | P0 |
| W4-S02 | Criar modelo `CopybookField` e `CopybookLayout` | P0 |
| W4-S03 | Implementar `ImportCopybookLayout` command + handler | P1 |
| W4-S04 | Implementar versionamento de copybooks | P1 |
| W4-S05 | Implementar `DiffCopybookVersions` query | P1 |
| W4-S06 | Implementar `ClassifyCopybookChange` (breaking detection) | P1 |
| W4-S07 | Implementar `MqMessageDescriptorParser` | P2 |
| W4-S08 | Implementar `RegisterMqContract` command | P2 |
| W4-S09 | Implementar `MapCopybookToContract` (cross-reference) | P2 |
| W4-S10 | Criar `CopybookViewer` page frontend | P1 |
| W4-S11 | Criar `CopybookDiffViewer` page frontend | P1 |
| W4-S12 | Criar `CopybookFieldsTable` component | P1 |
| W4-S13 | Extensão do Contract Catalog com tipos legacy | P1 |
| W4-S14 | Criar migrações para novas tabelas | P1 |
| W4-S15 | Criar testes para CopybookParser (~40 testes) | P0 |
| W4-S16 | Criar testes para diff e classification (~20 testes) | P1 |
| W4-S17 | Criar testes de integração (~10) | P2 |
