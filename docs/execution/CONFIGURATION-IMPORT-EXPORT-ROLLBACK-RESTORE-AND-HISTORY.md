# CONFIGURATION-IMPORT-EXPORT-ROLLBACK-RESTORE-AND-HISTORY

## Import/Export de Configuração

### Export
- **Formato**: JSON estruturado com metadados
- **Filtros**: Por escopo (System/Tenant/Environment) e por domínio
- **Conteúdo exportado**:
  - Timestamp da exportação
  - Escopo e domínio selecionados
  - Versão do formato
  - Para cada definição: key, displayName, category, valueType, defaultValue, effectiveValue, isSensitive, resolvedScope, isInherited, isDefault
- **Segurança**: Valores sensíveis são automaticamente mascarados como `***MASKED***`
- **Nome do arquivo**: `nextraceone-config-{domain}-{scope}-{date}.json`

### Import
- **Validação**: Todas as entradas são validadas contra as definições atuais antes da aplicação
- **Preview**: O import mostra um relatório de validação antes de aplicar mudanças
- **Formato aceite**: Formato de exportação NexTraceOne
- **Segurança**: Valores incompatíveis com definições são rejeitados
- **Auditoria**: Toda importação é registada no audit trail

## Rollback e Restore

### Modelo
- Seleção de definição por pesquisa de chave
- Visualização do histórico de versões da definição selecionada
- Para cada versão:
  - Timestamp da mudança
  - Utilizador responsável
  - Ação realizada (Set/Remove/Toggle)
  - Valor anterior e novo valor (com diff visual)
  - Motivo da mudança quando disponível
  - Botão de restore para versões anteriores

### Safeguards
- Rollback gera nova entrada no audit trail
- Valores sensíveis mascarados na visualização
- Preview antes de aplicar rollback
- Validação contra definição atual

## Histórico e Timeline

### Timeline de Mudanças
- Timeline cronológica por definição
- Indicadores visuais por tipo de ação (Set = verde, Remove = vermelho, Toggle = cinzento)
- Escopo, utilizador e timestamp de cada mudança
- Motivo da mudança quando disponível
- Filtro por chave de definição

### Filtros
- Pesquisa por chave
- Período temporal
- Utilizador
- Domínio

### Auditoria
- Origem da ação: UI, API, Import, Rollback
- Masking de valores sensíveis mantido no histórico
