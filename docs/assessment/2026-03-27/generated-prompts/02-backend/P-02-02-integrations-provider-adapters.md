# P-02-02 — Criar interfaces de provider adapter e implementar adapter GitHub para Integrações

## 1. Título

Criar abstração IIntegrationProvider e implementar adapter real para GitHub no módulo Integrations.

## 2. Modo de operação

**Implementation**

## 3. Objetivo

O módulo Integrations possui a entidade IntegrationConnector e repositórios para gestão de conectores, mas não tem nenhuma abstração de provider real que permita comunicar com sistemas externos (GitHub, GitLab, Jenkins, Azure DevOps). Este prompt cria a interface IIntegrationProvider e implementa o primeiro adapter concreto para GitHub, estabelecendo o padrão para os restantes.

## 4. Problema atual

- A entidade `IntegrationConnector` em `src/modules/integrations/NexTraceOne.Integrations.Domain/Entities/IntegrationConnector.cs` modela conectores mas sem capacidade de execução real.
- O `IntegrationConnectorRepository.cs` gere persistência mas não invoca nenhum sistema externo.
- Os handlers (`ListIntegrationConnectors`, `GetIntegrationConnector`, `RetryConnector`) operam apenas sobre dados locais.
- Não existe interface `IIntegrationProvider` que abstraia a comunicação com providers externos.
- Os enums `ConnectorStatus`, `ConnectorHealth` em `src/modules/integrations/NexTraceOne.Integrations.Domain/Enums/` existem mas nunca são atualizados por execução real.
- Sem provider adapters, o módulo Integrations não pode ingerir dados reais de deploy, change ou pipeline.

## 5. Escopo permitido

- `src/modules/integrations/NexTraceOne.Integrations.Domain/Abstractions/` — criar IIntegrationProvider interface
- `src/modules/integrations/NexTraceOne.Integrations.Infrastructure/Adapters/` — criar pasta e GitHubIntegrationAdapter
- `src/modules/integrations/NexTraceOne.Integrations.Infrastructure/Adapters/GitHub/` — GitHubAdapter, GitHubOptions, modelos de resposta
- `src/modules/integrations/NexTraceOne.Integrations.Application/Abstractions/` — registar interface se necessário
- `src/modules/integrations/NexTraceOne.Integrations.Infrastructure/DependencyInjection.cs` — registar adapter no DI
- `src/modules/integrations/NexTraceOne.Integrations.Domain/Enums/` — adicionar ProviderType enum se não existir

## 6. Escopo proibido

- Não implementar adapters para GitLab, Jenkins ou Azure DevOps neste prompt (serão prompts separados).
- Não alterar módulos fora de `src/modules/integrations/`.
- Não armazenar tokens/credenciais em código — usar Options pattern com referência a configuração segura.
- Não criar controllers ou endpoints novos — o adapter é consumido internamente pelos handlers existentes.
- Não alterar migrações existentes.

## 7. Ficheiros principais candidatos a alteração

1. `src/modules/integrations/NexTraceOne.Integrations.Domain/Abstractions/IIntegrationProvider.cs` (novo)
2. `src/modules/integrations/NexTraceOne.Integrations.Domain/Enums/ProviderType.cs` (novo, se não existir)
3. `src/modules/integrations/NexTraceOne.Integrations.Infrastructure/Adapters/GitHub/GitHubIntegrationAdapter.cs` (novo)
4. `src/modules/integrations/NexTraceOne.Integrations.Infrastructure/Adapters/GitHub/GitHubOptions.cs` (novo)
5. `src/modules/integrations/NexTraceOne.Integrations.Infrastructure/Adapters/GitHub/GitHubModels.cs` (novo)
6. `src/modules/integrations/NexTraceOne.Integrations.Infrastructure/Adapters/IntegrationProviderFactory.cs` (novo)
7. `src/modules/integrations/NexTraceOne.Integrations.Infrastructure/DependencyInjection.cs`
8. `src/modules/integrations/NexTraceOne.Integrations.Application/Features/RetryConnector/RetryConnector.cs` (adaptar para usar provider real)

## 8. Responsabilidades permitidas

- Definir IIntegrationProvider com métodos: TestConnectionAsync, FetchRecentEventsAsync, GetProviderHealthAsync.
- Implementar GitHubIntegrationAdapter usando HttpClient com autenticação via token (Options pattern).
- Criar IntegrationProviderFactory que resolva o adapter correto com base no ProviderType do conector.
- Registar HttpClient tipado para GitHub no DI com politica de retry (Polly se já disponível).
- Criar modelos de resposta canónicos (IntegrationEvent, ProviderHealthResult) no domínio.
- Usar CancellationToken em todas as operações async.
- Adicionar logging estruturado com Serilog para cada chamada ao provider.

## 9. Responsabilidades proibidas

- Não implementar webhook receiver neste prompt (será prompt separado de ingestão push).
- Não criar background jobs para polling automático (será prompt de workers com Quartz.NET).
- Não armazenar credenciais em appsettings.json em texto simples — referenciar variável de ambiente ou secret.
- Não criar acoplamento direto entre o adapter e outros módulos (catalog, changes).

## 10. Critérios de aceite

- [ ] Interface IIntegrationProvider definida com pelo menos 3 métodos (TestConnection, FetchEvents, GetHealth).
- [ ] GitHubIntegrationAdapter implementa IIntegrationProvider e comunica com GitHub REST API v3.
- [ ] IntegrationProviderFactory resolve o adapter correto por ProviderType.
- [ ] DI está configurado com HttpClient tipado para GitHub.
- [ ] Compilação completa da solution sem erros.
- [ ] Logs estruturados emitidos para cada operação do adapter.
- [ ] Modelos de resposta canónicos (não específicos de GitHub) definidos no domínio.

## 11. Validações obrigatórias

- Compilação do módulo Integrations (todos os projetos: Domain, Application, Infrastructure, Contracts, API).
- Compilação da solution NexTraceOne.sln completa.
- Verificar que handlers existentes (ListIntegrationConnectors, GetIntegrationConnector) continuam a funcionar.
- Verificar que o DI resolve IIntegrationProvider corretamente para ProviderType.GitHub.

## 12. Riscos e cuidados

- GitHub API tem rate limits — o adapter deve respeitar headers X-RateLimit e fazer backoff.
- Tokens de acesso devem ser validados antes de chamar a API (fail-fast com erro claro).
- O modelo canónico de IntegrationEvent deve ser genérico o suficiente para funcionar com GitLab/Jenkins futuramente.
- Evitar dependência direta no pacote Octokit — preferir HttpClient puro para manter controlo e reduzir dependências.
- Credenciais nunca devem aparecer em logs — usar LoggerMessage com parâmetros mascarados.

## 13. Dependências

- **P-00-08** — Migração do módulo Integrations deve estar aplicada (IntegrationsDbContext com tabelas criadas).
- A entidade IntegrationConnector deve ter campo para ProviderType e credenciais encriptadas.
- O módulo Security em `src/building-blocks/NexTraceOne.BuildingBlocks.Security/Encryption/AesGcmEncryptor.cs` pode ser usado para encriptar/desencriptar tokens.

## 14. Próximos prompts sugeridos

- **P-02-06** — Event publishing para módulo Integrations (publicar eventos quando dados são ingeridos).
- **P-XX-XX** — Adapter para GitLab (segundo provider seguindo o mesmo padrão IIntegrationProvider).
- **P-XX-XX** — Adapter para Jenkins (terceiro provider).
- **P-XX-XX** — Webhook receiver para ingestão push de eventos de deploy/change.
- **P-XX-XX** — Background job com Quartz.NET para polling periódico de providers.
