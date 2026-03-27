# P1.5 — Post-Change Gap Report

**Data de execução:** 2026-03-26  
**Fase:** P1.5 — X-Tenant-Id Header Hardening  
**Estado:** CONCLUÍDO COM LIMITAÇÕES RESIDUAIS DOCUMENTADAS

---

## 1. O que foi resolvido

| Item | Detalhe |
|---|---|
| `X-Tenant-Id` header rejeitado em pedidos não autenticados | `TenantResolutionMiddleware` só aceita header quando `context.User.Identity?.IsAuthenticated == true` |
| JWT claims mantidos como fonte primária e autoritativa | Prioridade 1 intacta — JWT `tenant_id` sempre ganha sobre header |
| Uso residual do header restrito e justificado | Header aceito apenas como fallback controlado para autenticados sem claim tenant_id no JWT |
| Frontend documentado com nova política | Comentário do interceptor em `client.ts` atualizado |
| Testes atualizados e alinhados com nova regra | 2 novos testes; 0 falhas |

---

## 2. O que ficou pendente

### 2.1 Log de tentativas rejeitadas de injeção de tenant header

**Descrição:** Quando um pedido não autenticado tenta injetar `X-Tenant-Id`, o header
é silenciosamente ignorado sem log de auditoria.

**Impacto:** Baixo — o comportamento é seguro. O log seria útil para detecção de
tentativas de exploração, mas não é um requisito crítico desta fase.

**Estado:** PENDENTE — pode ser adicionado como enhancement de auditoria.

---

### 2.2 Sem validação de tenant membership no TenantResolutionMiddleware

**Descrição:** O middleware define o tenant pelo claim do JWT, mas não valida em tempo
real que o utilizador ainda tem membership ativa nesse tenant. A validação ocorre
no `TenantIsolationBehavior` e `TenantEnvironmentContextResolver`, não no middleware.

**Impacto:** Baixo — consistente com a arquitetura atual. Memberships inativas são
tratadas downstream nos handlers de negócio.

**Estado:** ACEITE como limitação arquitetural conhecida.

---

### 2.3 Header X-Environment-Id aceito sem verificação adicional

**Descrição:** O `EnvironmentResolutionMiddleware` aceita `X-Environment-Id` de pedidos
autenticados sem restrição adicional, mas valida que o ambiente pertence ao tenant ativo.
Esta validação é suficiente para a segurança atual.

**Impacto:** Baixo — a validação de ownership está implementada.

**Estado:** ACEITE — comportamento correto e alinhado com a arquitetura.

---

## 3. Limitações residuais

| Limitação | Impacto | Mitigação atual |
|---|---|---|
| Header X-Tenant-Id ainda é enviado pelo frontend em todos os pedidos autenticados (redundante quando JWT tem tenant_id) | MUITO BAIXO | JWT tem prioridade; header ignorado se JWT tem claim |
| TenantResolutionMiddleware não persiste auditoria de tenant switches | BAIXO | Log de Debug ativo para tracing |

---

## 4. O que deve ser tratado em fases seguintes

| Item | Prioridade | Fase sugerida |
|---|---|---|
| Avaliar log de Warning para tentativas de injeção de tenant header rejeitadas | LOW | Futura — endurecimento de auditoria |
| Avaliar se frontend deve parar de enviar X-Tenant-Id quando JWT já contém o claim (otimização) | LOW | Futuro opcional |
| Continuar itens P1 do roadmap (P1.6+) | HIGH | Próxima fase |

---

## 5. Classificação final da fase P1.5

| Dimensão | Estado |
|---|---|
| `X-Tenant-Id` ignorado em pedidos não autenticados | ✅ RESOLVIDO |
| JWT claims como fonte primária intacta | ✅ CONFIRMADO |
| Uso residual do header restrito e justificado | ✅ RESOLVIDO |
| Frontend documentado com nova política | ✅ ATUALIZADO |
| Fluxos pré-auth verificados e não afetados | ✅ CONFIRMADO |
| Build sem erros | ✅ CONFIRMADO |
| 102 testes passam (0 falhas, 2 novos) | ✅ CONFIRMADO |
| Documentação gerada | ✅ SIM |
