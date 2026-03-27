# P-04-01 — Consolidar enum TrendDirection duplicado entre Governance e OperationalIntelligence

## 1. Título

Consolidar o enum TrendDirection duplicado entre os módulos Governance, OperationalIntelligence e ProductAnalytics.

## 2. Modo de operação

**Refactor**

## 3. Objetivo

O enum TrendDirection existe em três locais distintos, violando DRY e criando risco de divergência. Este prompt consolida o enum num único local partilhado e atualiza todas as referências.

## 4. Problema atual

- `src/modules/governance/NexTraceOne.Governance.Domain/Enums/TrendDirection.cs` — definição no Governance.
- `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Domain/Cost/Enums/TrendDirection.cs` — definição no OperationalIntelligence (Cost).
- `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Domain/Reliability/Enums/TrendDirection.cs` — segunda definição no OperationalIntelligence (Reliability).
- `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Domain/Enums/TrendDirection.cs` — definição no ProductAnalytics.
- Quatro definições independentes do mesmo conceito — qualquer alteração precisa ser replicada manualmente.
- Gap identificado no relatório P8.5 do assessment.

## 5. Escopo permitido

- `src/building-blocks/NexTraceOne.BuildingBlocks.Core/` ou local partilhado equivalente — destino do enum consolidado.
- `src/modules/governance/NexTraceOne.Governance.Domain/Enums/TrendDirection.cs` — remover ou redirecionar.
- `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Domain/Cost/Enums/TrendDirection.cs` — remover.
- `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Domain/Reliability/Enums/TrendDirection.cs` — remover.
- `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Domain/Enums/TrendDirection.cs` — remover.
- Todos os ficheiros que referenciam estes enums — atualizar using statements.

## 6. Escopo proibido

- Não alterar o comportamento de nenhum enum — apenas consolidar localização.
- Não alterar migrações (o enum é usado em código C#, não diretamente no schema se for string/int).
- Não renomear valores do enum.
- Não criar abstrações desnecessárias.

## 7. Ficheiros principais candidatos a alteração

1. `src/building-blocks/NexTraceOne.BuildingBlocks.Core/Enums/TrendDirection.cs` (novo — destino consolidado)
2. `src/modules/governance/NexTraceOne.Governance.Domain/Enums/TrendDirection.cs` (remover)
3. `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Domain/Cost/Enums/TrendDirection.cs` (remover)
4. `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Domain/Reliability/Enums/TrendDirection.cs` (remover)
5. `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Domain/Enums/TrendDirection.cs` (remover)

## 8. Responsabilidades permitidas

- Verificar que todos os TrendDirection têm os mesmos valores antes de consolidar.
- Criar enum único em BuildingBlocks.Core (ou Domain partilhado).
- Atualizar todos os using statements nos ficheiros que referenciam as versões locais.
- Remover os 4 ficheiros duplicados.
- Garantir que compilação passa em todos os módulos afetados.

## 9. Responsabilidades proibidas

- Não consolidar outros enums neste prompt — apenas TrendDirection.
- Não alterar lógica de negócio que usa o enum.
- Não criar breaking changes em contratos públicos.

## 10. Critérios de aceite

- [ ] Apenas uma definição de TrendDirection existe no repositório.
- [ ] Todos os módulos que usavam TrendDirection compilam com a referência ao local consolidado.
- [ ] Nenhum ficheiro duplicado permanece.
- [ ] Compilação completa da solution sem erros.
- [ ] Valores do enum idênticos ao original.

## 11. Validações obrigatórias

- `dotnet build` da solution completa.
- Pesquisa por `enum TrendDirection` retorna apenas 1 resultado.
- Verificar que os projetos Domain dos 3 módulos referenciam BuildingBlocks.Core (ou adicionam essa referência).

## 12. Riscos e cuidados

- Se os enums tiverem valores diferentes entre módulos, é necessário escolher o superset e documentar.
- A adição de referência ao BuildingBlocks.Core nos projetos Domain pode introduzir dependências não desejadas — verificar se Core é minimal.
- Serialização JSON/EF Core pode ser afetada se o namespace mudar — verificar configurações de serialização.

## 13. Dependências

- Nenhuma dependência direta de outros prompts.
- Os módulos Governance, OperationalIntelligence e ProductAnalytics devem estar compiláveis.

## 14. Próximos prompts sugeridos

- **P-XX-XX** — Audit de outros enums duplicados entre módulos (RiskLevel, HealthStatus, etc.).
- **P-XX-XX** — Criar shared kernel explícito para tipos partilhados entre bounded contexts.
