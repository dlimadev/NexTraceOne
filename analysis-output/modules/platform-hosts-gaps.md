# Platform Hosts — Gaps, Erros e Pendências

## 1. Estado resumido do módulo
Plataforma inclui ApiHost (Kestrel), BackgroundWorkers, e potencialmente IngestionHost. Estrutura funcional. Gaps concentrados em seed strategy e documentação de deployment.

## 2. Gaps críticos

### 2.1 6 de 7 Seed SQL Files Missing
- **Severidade:** CRITICAL
- **Classificação:** BROKEN
- **Descrição:** `DevelopmentSeedDataExtensions.cs` (ApiHost) referencia 7 ficheiros SQL. Apenas `seed-incidents.sql` existe.
- **Impacto:** Setup de desenvolvimento falha silenciosamente para 6 módulos.
- **Evidência:** `src/platform/NexTraceOne.ApiHost/DevelopmentSeedDataExtensions.cs` linhas 21-30; `src/platform/NexTraceOne.ApiHost/SeedData/`

## 3. Gaps altos
Nenhum.

## 4. Gaps médios

### 4.1 Outbox processors registados para 21 DbContexts — subutilizados
- **Severidade:** MEDIUM
- **Classificação:** PARTIAL
- **Descrição:** `ModuleOutboxProcessorJob<TContext>` está registado para 21 DbContexts. A infraestrutura funciona mas a maioria dos módulos não publica domain events para o outbox.
- **Impacto:** Processadores correm em ciclos de 5s sem mensagens — overhead desnecessário; integração entre módulos via eventos não funciona.
- **Evidência:** `src/platform/NexTraceOne.BackgroundWorkers/Jobs/ModuleOutboxProcessorJob.cs`

## 5. Itens mock / stub / placeholder
Nenhum no platform host.

## 6. Erros de desenho / implementação incorreta
Nenhum — design do ModuleOutboxProcessorJob é correcto (generic BackgroundService por DbContext).

## 7-10. Gaps de frontend / backend / banco / configuração
N/A ou cobertos em módulos específicos.

## 11. Gaps de documentação ligados a este módulo
- Documentação de deployment para produção precisa ser verificada contra estado real
- Bootstrap mínimo de produção não documentado

## 12. Gaps de seed/bootstrap ligados a este módulo
- 6 ficheiros SQL em falta (detalhado em `00-seed-strategy-gaps.md`)

## 13. Ações corretivas obrigatórias
1. Criar 6 ficheiros SQL de seed OU remover referências do array SeedTargets
2. Documentar bootstrap mínimo de produção
3. Avaliar se outbox processors para módulos que não publicam eventos devem ser desactivados
