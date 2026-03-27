# P-04-02 — Criar seed data para módulos Knowledge, Integrations e ProductAnalytics

## 1. Título

Criar dados de seed de desenvolvimento para os módulos Knowledge, Integrations e ProductAnalytics.

## 2. Modo de operação

**Implementation**

## 3. Objetivo

Os módulos mais recentes (Knowledge, Integrations, ProductAnalytics) não têm seed data para desenvolvimento, ao contrário dos módulos mais maduros. Este prompt cria dados realistas de seed que permitem desenvolvimento e testes sem depender de interação manual, usando o padrão já estabelecido em `DevelopmentSeedDataExtensions.cs`.

## 4. Problema atual

- `src/platform/NexTraceOne.ApiHost/DevelopmentSeedDataExtensions.cs` contém seed data para módulos maduros mas não para Knowledge, Integrations nem ProductAnalytics.
- Sem seed data, handlers como GetPersonaUsage, GetJourneys, ListIntegrationConnectors retornam listas vazias.
- Páginas frontend mostram estados vazios permanentes durante desenvolvimento.
- Testes de integração não têm dados base para validar queries.
- O `WebApplicationExtensions.cs` em ApiHost pode ter lógica de inicialização que chama o seed.

## 5. Escopo permitido

- `src/platform/NexTraceOne.ApiHost/DevelopmentSeedDataExtensions.cs` — adicionar métodos de seed
- `src/modules/knowledge/NexTraceOne.Knowledge.Infrastructure/` — seed data via DbContext se padrão existir
- `src/modules/integrations/NexTraceOne.Integrations.Infrastructure/` — seed data
- `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Infrastructure/` — seed data
- `src/platform/NexTraceOne.ApiHost/Program.cs` ou `WebApplicationExtensions.cs` — invocar novos seeds

## 6. Escopo proibido

- Não alterar seed data de módulos já existentes.
- Não criar seed data para produção — apenas desenvolvimento (guard com environment check).
- Não alterar migrações.
- Não alterar entidades de domínio.

## 7. Ficheiros principais candidatos a alteração

1. `src/platform/NexTraceOne.ApiHost/DevelopmentSeedDataExtensions.cs`
2. `src/platform/NexTraceOne.ApiHost/WebApplicationExtensions.cs` (se necessário para invocar)
3. `src/platform/NexTraceOne.ApiHost/Program.cs` (se necessário)
4. Ficheiros de seed específicos por módulo (novos, se padrão exigir)

## 8. Responsabilidades permitidas

- Criar seed data para Knowledge: 3-5 KnowledgeDocuments, 3-5 OperationalNotes, 5-8 KnowledgeRelations.
- Criar seed data para Integrations: 2-3 IntegrationConnectors (GitHub, GitLab mock), 3-5 IngestionSources, 5-10 IngestionExecutions.
- Criar seed data para ProductAnalytics: 20-50 AnalyticsEvents distribuídos por persona, módulo e período.
- Usar dados realistas com nomes de serviços, contratos e equipas consistentes com o seed existente.
- Garantir idempotência — seed não duplica dados se executado múltiplas vezes.
- Proteger com `if (app.Environment.IsDevelopment())`.

## 9. Responsabilidades proibidas

- Não gerar GUIDs aleatórios a cada execução — usar GUIDs fixos para idempotência.
- Não criar dados inconsistentes (ex: referências a serviços que não existem no seed).
- Não usar dados sensíveis ou reais de clientes.

## 10. Critérios de aceite

- [ ] Seed de Knowledge cria documentos, notas e relações.
- [ ] Seed de Integrations cria conectores, fontes e execuções.
- [ ] Seed de ProductAnalytics cria eventos analíticos distribuídos.
- [ ] Seed é idempotente (executar 2x não duplica dados).
- [ ] Seed só executa em ambiente Development.
- [ ] Compilação completa da solution sem erros.
- [ ] Dados são realistas e coerentes com o domínio do NexTraceOne.

## 11. Validações obrigatórias

- Compilação da solution completa.
- Verificar idempotência executando seed 2 vezes e verificando contagem de registos.
- Verificar que o guard de ambiente Development está presente.

## 12. Riscos e cuidados

- GUIDs fixos podem colidir com dados de teste manuais — usar namespace de GUIDs dedicado.
- Referências cruzadas (ex: KnowledgeRelation para um serviço) precisam de IDs consistentes com seed de outros módulos.
- Se DbContext dos módulos não estiver acessível no ApiHost, pode ser necessário usar interfaces de repositório.
- Volume de AnalyticsEvents deve ser suficiente para que agregações mostrem resultados significativos.

## 13. Dependências

- Migrações dos 3 módulos devem estar aplicadas (P-00-07, P-00-08, P-00-09).
- Seed data de módulos existentes (Catalog, Identity) deve existir para referências cruzadas consistentes.

## 14. Próximos prompts sugeridos

- **P-02-03** — Com seed data, os handlers de ProductAnalytics retornarão dados reais.
- **P-03-01** e **P-03-02** — Frontend mostrará dados em vez de estados vazios.
- **P-XX-XX** — Script de seed para ambiente de demonstração (mais dados, cenários completos).
