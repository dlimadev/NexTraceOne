# Environment Control — Transition Notes

**Data:** 2026-03-21  
**Status:** Implementação concluída

---

## Compatibilidade Preservada

Todos os comportamentos existentes foram preservados:

1. **`ListEnvironments`** — Compatível com versões anteriores. O campo `isPrimaryProduction` foi adicionado à resposta (novo campo, não é breaking change).
2. **`GrantEnvironmentAccess`** — Inalterado. Continua usando `GetByIdAsync` e validação manual de `TenantId`.
3. **`EnvironmentContext.tsx`** — A interface `ApiEnvironmentResponse` já tinha `isProductionLike?: boolean` como opcional. O novo campo `isPrimaryProduction` deve ser adicionado como opcional na interface do frontend para compatibilidade retroativa (já feito em `EnvironmentItem`).
4. **Migrações** — A migration `AddIsPrimaryProductionToEnvironment` adiciona coluna com `defaultValue: false`, garantindo que ambientes existentes continuem funcionando sem necessidade de backfill manual.
5. **`EnvironmentProfile.Development` como default** — O valor padrão foi mantido como `EnvironmentProfile.Development = 1` no banco.
6. **`Environment.Deactivate()`** — Agora também revoga `IsPrimaryProduction`. Isso é uma mudança de comportamento correta e segura — um ambiente inativo nunca deve ser o produtivo principal.

## O que Foi Alterado

### Entidade `Environment`
- + Campo `IsPrimaryProduction`
- + Método `DesignateAsPrimaryProduction()`
- + Método `RevokePrimaryProductionDesignation()`
- + Método `UpdateBasicInfo(name, sortOrder)`
- ~ Método `Deactivate()` agora revoga `IsPrimaryProduction`
- ~ Factory `Create(...)` aceita parâmetro `isPrimaryProduction`

### `IEnvironmentRepository`
- + `GetByIdForTenantAsync(id, tenantId)`
- + `GetPrimaryProductionAsync(tenantId)`

### `EnvironmentRepository`
- + Implementação dos dois novos métodos acima

### `EnvironmentConfiguration` (EF Core)
- + `Property(x => x.IsPrimaryProduction)` com `HasDefaultValue(false)`
- + `HasIndex` único parcial para `IsPrimaryProduction`

### `ListEnvironments` — resposta
- + Campo `IsPrimaryProduction` no `EnvironmentResponse`

### `IdentityErrors`
- + `PrimaryProductionAlreadyExists`
- + `EnvironmentNotBelongsToTenant`
- + `CannotDesignateInactiveAsPrimaryProduction`

### Novas features
- + `CreateEnvironment`
- + `UpdateEnvironment`
- + `SetPrimaryProductionEnvironment`
- + `GetPrimaryProductionEnvironment`

### API
- + `POST /api/v1/identity/environments`
- + `PUT /api/v1/identity/environments/{id}`
- + `PATCH /api/v1/identity/environments/{id}/primary-production`
- + `GET /api/v1/identity/environments/primary-production`

### Frontend
- + `EnvironmentsPage.tsx` — gestão de ambientes
- + `identityApi.createEnvironment()`
- + `identityApi.updateEnvironment()`
- + `identityApi.setPrimaryProductionEnvironment()`
- + `identityApi.getPrimaryProductionEnvironment()`
- + Rota `/environments` no App.tsx
- + Chaves i18n `environments.*` em 4 locales

### Migrações
- + `20260321200000_AddIsPrimaryProductionToEnvironment.cs`

## O que Ainda Está Parcial

1. **Integração da IA com `GetPrimaryProductionAsync`**: os handlers de IA (`AssessPromotionReadiness`, `CompareEnvironments`) ainda recebem dados do ambiente como parâmetros da chamada. A integração automática com o ambiente produtivo do tenant via repositório está planificada.

2. **Superfície de Readiness/Risk no frontend**: `PromotionPage` e `ReleasesIntelligenceTab` ainda não exibem o assessment estruturado de `AssessPromotionReadiness`. O backend está pronto; falta a integração no frontend.

3. **Ativar/Desativar ambiente via API**: não há endpoint `PATCH /{id}/activate` ou `PATCH /{id}/deactivate`. O `UpdateEnvironment` altera metadados mas não o estado ativo/inativo. Este endpoint pode ser adicionado em iteração futura.

4. **Auditoria de mudanças de ambiente**: criação/edição de ambientes não emite `SecurityEvent` ainda. Recomendado para compliance.

## Próximos Passos

- [ ] Adicionar endpoint de ativar/desativar ambiente
- [ ] Emitir `SecurityEvent` em criação, edição e troca de produção principal
- [ ] Integrar `GetPrimaryProductionAsync` nos handlers de IA
- [ ] Expor `AssessPromotionReadiness` no frontend (PromotionPage)
- [ ] Adicionar paginação ao `ListByTenantAsync` para tenants com muitos ambientes
- [ ] Expor `EnvironmentsPage` no menu lateral (sidebar)
