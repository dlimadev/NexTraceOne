# P7.4 — Audit & Compliance: Post-change Gap Report

**Data:** 2026-03-27  
**Escopo:** retenção mínima, export auditável mínimo e consulta funcional de auditoria.

---

## 1. O que foi resolvido

- Retenção passou a ser executada por job recorrente (`AuditRetentionJob`) e endpoint de aplicação.
- Políticas de retenção expostas via endpoints (`GET/POST /audit/retention/policies`).
- Export auditável inclui `CorrelationId` e mantém campos mínimos exigidos.
- Consulta auditável suportando:
  - período (`from/to`)
  - tipo/origem (sourceModule/actionType)
  - correlationId
  - resourceType/resourceId
  - chain hash/previous hash/sequence
- `VerifyChainIntegrity` agora sinaliza truncamento de cadeia após retenção.
- Frontend passou a consumir filtros mínimos e export via `AuditPage`.

---

## 2. Pendências remanescentes

- Re-hash completo da cadeia após retenção (para restabelecer genesis).
- Exportação multi-formato (CSV/PDF/Excel) não implementada.
- UI dedicada para configuração de retenção e compliance avançado.
- Estratégia de retenção por módulo/tipo ainda ausente (apenas política global).

---

## 3. Itens explícitos para P7.5

- Normalização da cadeia após retenção (re-hash ou marcador imutável).
- Validação integrada Change/Operations/Identity ↔ Notifications/Audit (P7.5).
- Melhorias de UX para cadeia e export (visualização de hash/linha do tempo).

---

## 4. Limitações residuais

- `VerifyChainIntegrity` aceita cadeia truncada como válida (com `IsTruncated=true`).
- Exportação continua JSON simples com payload e hash (sem assinatura/encapsulamento adicional).

