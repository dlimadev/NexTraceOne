# P-05-01 — Rever e consolidar connection strings no appsettings.json

## 1. Título

Rever as 24 connection strings em appsettings.json e consolidar onde módulos partilham a mesma base de dados física.

## 2. Modo de operação

**Stabilization**

## 3. Objetivo

O ficheiro appsettings.json contém 24 connection strings distintas, todas apontando para a mesma base de dados PostgreSQL (`Database=nextraceone`). Esta configuração, embora funcional, cria confusão, dificulta manutenção e pode causar exaustão do pool de conexões. Este prompt revê a configuração e propõe consolidação onde seguro.

## 4. Problema atual

- `src/platform/NexTraceOne.ApiHost/appsettings.json` linhas 2-24: 24 connection strings com nomes diferentes mas valores idênticos:
  - NexTraceOne, IdentityDatabase, CatalogDatabase, ContractsDatabase, DeveloperPortalDatabase, ChangeIntelligenceDatabase, WorkflowDatabase, RulesetGovernanceDatabase, PromotionDatabase, IncidentDatabase, CostIntelligenceDatabase, RuntimeIntelligenceDatabase, ReliabilityDatabase, AuditDatabase, AiGovernanceDatabase, GovernanceDatabase, IntegrationsDatabase, ProductAnalyticsDatabase, ExternalAiDatabase, AiOrchestrationDatabase, AutomationDatabase, ConfigurationDatabase, e mais.
- Todas apontam para `Host=localhost;Port=5432;Database=nextraceone;Maximum Pool Size=10`.
- Com 24 connection strings × Pool Size=10 = potencial de 240 conexões para o mesmo banco.
- Classes Options em cada módulo referem o nome específico da connection string.

## 5. Escopo permitido

- `src/platform/NexTraceOne.ApiHost/appsettings.json`
- `src/platform/NexTraceOne.ApiHost/appsettings.Development.json` (se existir)
- Classes de Options/configuração em cada módulo Infrastructure que lêem o nome da connection string.
- Documentação de decisão.

## 6. Escopo proibido

- Não alterar DbContexts — apenas a configuração de connection strings.
- Não alterar migrações.
- Não remover a capacidade de cada módulo ter DB separado no futuro (manter flexibilidade).
- Não alterar lógica de negócio.
- Não alterar código fora de configuração e DI.

## 7. Ficheiros principais candidatos a alteração

1. `src/platform/NexTraceOne.ApiHost/appsettings.json`
2. `src/platform/NexTraceOne.ApiHost/appsettings.Development.json` (se existir)
3. Classes DependencyInjection.cs de cada módulo Infrastructure (onde o nome da connection string é lido)

## 8. Responsabilidades permitidas

- Analisar quais connection strings são efetivamente distintas vs. duplicadas.
- Propor agrupamento por schema boundary: uma connection string por grupo de módulos relacionados.
- Criar connection string "NexTraceOne" como fallback padrão.
- Manter nomes de connection string específicos por módulo como override opcional.
- Ajustar Maximum Pool Size para valores realistas considerando a consolidação.
- Documentar a decisão e o mapeamento módulo→connection string.

## 9. Responsabilidades proibidas

- Não forçar todos os módulos a usar uma única connection string se houver justificativa para separação.
- Não remover connection strings sem verificar que o código não depende do nome específico.
- Não alterar credenciais ou hosts.

## 10. Critérios de aceite

- [ ] Número de connection strings reduzido de 24 para um valor justificado (estimativa: 3-5).
- [ ] Cada módulo consegue resolver a sua connection string (fallback chain funcional).
- [ ] Maximum Pool Size ajustado para valor realista.
- [ ] Compilação completa sem erros.
- [ ] Aplicação arranca sem erros de conexão.
- [ ] Documentação da decisão incluída como comentário ou doc.

## 11. Validações obrigatórias

- Compilação da solution completa.
- Arranque da aplicação sem erros (se possível testar localmente).
- Verificar que cada DbContext resolve a connection string corretamente.
- Verificar que o total de conexões potenciais é razoável.

## 12. Riscos e cuidados

- Se um módulo precisa futuramente de DB separado, a consolidação não deve impedir isso.
- O padrão fallback (módulo usa nome específico → se não existir, usa "NexTraceOne") deve ser explícito.
- Alterar nomes de connection strings pode quebrar configuração em docker-compose.yml ou docker-compose.override.yml.
- DesignTimeFactory de cada módulo pode depender do nome específico para migrations.

## 13. Dependências

- Nenhuma dependência de outros prompts.
- Todos os módulos devem estar compiláveis.

## 14. Próximos prompts sugeridos

- **P-05-02** — Plano de migração de API keys de configuração para PostgreSQL encriptado.
- **P-XX-XX** — Documentação de deployment e configuração para self-hosted.
- **P-XX-XX** — Health check de conexões de base de dados no arranque.
