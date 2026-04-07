> **⚠️ ARCHIVED — April 2026**: Este documento foi gerado como análise pontual de gaps. Muitos dos gaps aqui listados já foram resolvidos. Para o estado atual, consultar `docs/CONSOLIDATED-GAP-ANALYSIS-AND-ACTION-PLAN.md` e `docs/IMPLEMENTATION-STATUS.md`.

# Identity Access — Gaps, Erros e Pendências

## 1. Estado resumido do módulo
185 .cs files, 100% features reais, production-ready. Gap único: interface cross-module `IIdentityModule` sem implementação.

## 2. Gaps críticos
Nenhum.

## 3. Gaps altos
Nenhum.

## 4. Gaps médios

### 4.1 `IIdentityModule` sem implementação
- **Severidade:** MEDIUM
- **Classificação:** INCOMPLETE
- **Descrição:** Interface define 3 métodos (`GetUserByIdAsync`, `GetUserPermissionsAsync`, `ValidateTenantMembershipAsync`). Nenhum `IdentityModuleService` existe.
- **Impacto:** Outros módulos não podem consultar dados de utilizador via contrato cross-module. Actualmente usam `ICurrentUser`/`ICurrentTenant` para contexto do request corrente.
- **Evidência:** `src/modules/identityaccess/NexTraceOne.IdentityAccess.Contracts/ServiceInterfaces/IIdentityModule.cs` — interface definida, zero implementações

## 5. Itens mock / stub / placeholder
Nenhum.

## 6. Erros de desenho / implementação incorreta
Nenhum.

## 7. Gaps de frontend ligados a este módulo
- 10 de 14 páginas sem error handling explícito: `AccessReviewPage`, `ActivationPage`, `DelegationPage`, `ForgotPasswordPage`, `InvitationPage`, `LoginPage`, `MfaPage`, `ResetPasswordPage`, `TenantSelectionPage`, `UnauthorizedPage`
- Nota: Muitas destas são páginas de auth flow onde o error handling pode estar no formulário/mutation, não no nível da página. Verificação manual recomendada.

## 8. Gaps de backend ligados a este módulo
- `IIdentityModule` sem implementação

## 9. Gaps de banco/migração ligados a este módulo
Nenhum — `IdentityDbContext` com migration confirmada.

## 10. Gaps de configuração ligados a este módulo
Nenhum.

## 11. Gaps de documentação ligados a este módulo
Nenhum.

## 12. Gaps de seed/bootstrap ligados a este módulo
- `seed-identity.sql` referenciado mas **NÃO EXISTE** no disco.
- **RISCO para produção:** Não existe estratégia documentada de bootstrap para o admin inicial, primeiro tenant e configuração mínima de identity em produção.

## 13. Ações corretivas obrigatórias
1. Implementar `IIdentityModule` como `IdentityModuleService.cs`
2. Criar `seed-identity.sql` para development OU remover referência
3. Documentar bootstrap de produção (admin inicial, tenant, configuração mínima)
