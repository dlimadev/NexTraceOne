# P-00-01 — Adicionar CancellationToken ao módulo IdentityAccess

## Modo de operação

Refactor

## Objetivo

Adicionar `CancellationToken` a todos os métodos assíncronos do módulo IdentityAccess (~24 métodos).
Este refactor elimina uma violação sistemática das boas práticas .NET, garantindo que todas as operações
async possam ser canceladas de forma cooperativa quando o pedido HTTP é abortado ou o timeout é atingido.

## Problema atual

O módulo IdentityAccess contém 24 métodos `async Task` ou `async Task<T>` que não recebem
`CancellationToken`. Isto impede o cancelamento cooperativo de operações longas ou abandonadas,
podendo manter recursos ocupados desnecessariamente no servidor. Ficheiros afetados incluem handlers
de features (OidcCallback, ListMyTenants, ListEnvironments), middlewares (EnvironmentResolutionMiddleware),
resolvers (EnvironmentProfileResolver, TenantEnvironmentContextResolver), validators (EnvironmentAccessValidator),
repositórios (SecurityEventRepository, TenantRepository, EnvironmentRepository, TenantMembershipRepository,
RoleRepository) e serviços de infraestrutura (OidcProviderService).

## Escopo permitido

- `src/modules/identityaccess/` — apenas este módulo
- Application/Features/**/*.cs — handlers MediatR
- Infrastructure/Persistence/**/*.cs — repositórios
- Infrastructure/Services/**/*.cs — serviços de infraestrutura
- Interfaces e abstrações correspondentes em Application/ e Domain/

## Escopo proibido

- Outros módulos (catalog, notifications, aiknowledge, etc.)
- Ficheiros de migração existentes
- Ficheiros de configuração do host ou Program.cs
- Testes de outros módulos

## Ficheiros principais candidatos a alteração

- `Application/Features/OidcCallback/OidcCallback.cs` (2 métodos: ResolveOrProvisionUserAsync, AutoProvisionMembershipAsync)
- `Application/Features/ListMyTenants/ListMyTenants.cs` (Handle)
- `Application/Features/ListEnvironments/ListEnvironments.cs` (Handle)
- `Application/SecurityEventAuditBehavior.cs` (Handle)
- `Infrastructure/Authorization/EnvironmentAccessAuthorizationHandler.cs` (HandleRequirementAsync)
- `Infrastructure/Middleware/EnvironmentResolutionMiddleware.cs` (InvokeAsync, TryResolveAndSetContextAsync)
- `Infrastructure/Services/EnvironmentProfileResolver.cs` (ResolveProfileAsync, IsProductionLikeAsync)
- `Infrastructure/Services/TenantEnvironmentContextResolver.cs` (ResolveAsync, ListActiveContextsForTenantAsync)
- `Infrastructure/Services/EnvironmentAccessValidator.cs` (ValidateAsync, HasAccessAsync)
- `Infrastructure/Persistence/SecurityEventRepository.cs` (ListByTenantAsync)
- `Infrastructure/Persistence/TenantRepository.cs` (GetByIdsAsync)
- `Infrastructure/Persistence/EnvironmentRepository.cs` (GetAccessAsync, ListUserAccessesAsync, ListExpiredAccessesAsync)
- `Infrastructure/Persistence/TenantMembershipRepository.cs` (ListByTenantAsync)
- `Infrastructure/Persistence/RoleRepository.cs` (GetByIdsAsync)
- `Infrastructure/Services/OidcProviderService.cs` (ExchangeCodeAsync, ExchangeCodeForIdTokenAsync)

## Responsabilidades permitidas

- Adicionar parâmetro `CancellationToken cancellationToken = default` a cada método async
- Propagar o token para chamadas `await` internas (EF Core, HttpClient, etc.)
- Atualizar interfaces/abstrações correspondentes
- Atualizar chamadas internas ao módulo que precisem propagar o token

## Responsabilidades proibidas

- Alterar lógica de negócio
- Adicionar novas funcionalidades
- Refatorar nomes, estrutura de pastas ou responsabilidades
- Alterar contratos públicos da API (endpoints HTTP)

## Critérios de aceite

1. Todos os 24 métodos async do módulo têm `CancellationToken` na assinatura
2. O token é propagado para `SaveChangesAsync`, `ToListAsync`, `FirstOrDefaultAsync`, `SendAsync`, etc.
3. O módulo compila sem erros
4. Testes existentes do módulo compilam e passam
5. Nenhuma regressão introduzida na solução completa (`dotnet build NexTraceOne.sln`)

## Validações obrigatórias

- `dotnet build src/modules/identityaccess/` — sem erros
- `dotnet build NexTraceOne.sln` — sem erros
- Pesquisa `grep -r "async Task" src/modules/identityaccess/ | grep -v CancellationToken` retorna zero resultados

## Riscos e cuidados

- Interfaces partilhadas com outros módulos podem precisar de atualização coordenada
- Middleware e authorization handlers recebem CancellationToken do HttpContext — usar `context.RequestAborted`
- OidcProviderService usa HttpClient — garantir que o token chega ao `SendAsync`

## Dependências

- Nenhuma dependência de outros prompts
- Este é o primeiro prompt da fase de fundação

## Próximos prompts sugeridos

- P-00-02 (CancellationToken no módulo Catalog)
- P-00-06 (CancellationToken nos módulos restantes — fecha a série)
