# Wave 5 — Segurança, Rede & Air-Gap

> **Prioridade:** Alta
> **Esforço estimado:** M (Medium)
> **Módulos impactados:** `identityaccess`, `auditcompliance`, `building-blocks/Security`
> **Referência:** [INDEX.md](./INDEX.md)
> **Estado (Maio 2026):** W5-01 PARCIAL | W5-02 NAO IMPLEMENTADO | W5-03 IMPLEMENTADO | W5-04 NAO IMPLEMENTADO | W5-05 NAO IMPLEMENTADO | W5-06 NAO IMPLEMENTADO

---

## Contexto

Em 2026, Zero Trust é o modelo de segurança obrigatório para empresas enterprise.
O NexTraceOne já tem uma base sólida (JWT, RBAC, Break Glass, JIT, Audit Trail SHA-256).
O que falta é adaptar estas capacidades para os padrões específicos de redes
corporativas on-prem: proxies, CAs internas, isolamento de rede e auditoria de
tráfego externo.

Benchmark de mercado:
- Tabnine: air-gapped com zero telemetria como promessa central de produto
- Coder: network isolation mode documentado e testado
- Zero Trust (ISACA 2026): continuous verification + audit trail como pilares

---

## W5-01 — Network Isolation Mode (Air-Gap Garantido)

### Problema
Não existe garantia formal de que o produto não faz chamadas externas em modo
air-gapped. Equipas de segurança enterprise exigem esta garantia documentada
e verificável.

### Solução
Flag de configuração `Platform__NetworkIsolation__Mode`:

| Modo | Comportamento |
|---|---|
| `Off` (padrão) | Comportamento normal — chamadas externas permitidas |
| `Restricted` | Apenas chamadas explicitamente configuradas (webhooks, integrações) |
| `AirGap` | Zero chamadas externas; qualquer tentativa gera alerta de auditoria |

**Em modo `AirGap`:**
- OpenAI e modelos externos bloqueados (apenas Ollama local permitido)
- Webhooks de saída desactivados
- Integrações CI/CD em modo inbound-only
- Update check desactivado
- Qualquer tentativa de chamada externa registada como `SecurityEvent.NetworkViolation`
- Dashboard mostra confirmação visual do modo air-gap activo

**Lista completa de chamadas externas opcionais:**

| Chamada | Propósito | Controlada por |
|---|---|---|
| OpenAI API | AI externo | `AiRuntime__OpenAI__Enabled` |
| Webhooks de saída | Notificações externas | `Webhooks__OutboundEnabled` |
| GitHub/GitLab webhooks | Integração CI/CD | configuração de integração |
| SMTP | Notificações email | `Smtp__Host` |
| OTel Collector | Observabilidade | `OpenTelemetry__Endpoint` |
| Ollama remoto | LLM em servidor separado | `AiRuntime__Ollama__Host` |

### Estado de Implementação (Maio 2026): PARCIAL
Capability `AirGapped` existe em `TenantCapabilities.cs`. Feature `GetNetworkPolicy` com leitura de modo
`Off/Restricted/AirGap` via config `Platform:NetworkPolicy:Mode`. Endpoint `GET /api/v1/admin/network-policy`
retorna lista de chamadas activas e bloqueadas. O bloqueio efectivo de chamadas HTTP na camada de client
e o banner UI não foram confirmados.

### Critério de aceite
- [ ] Modo AirGap bloqueia todas as chamadas externas na camada de HTTP client
- [ ] Tentativas de chamada bloqueadas geram `SecurityEvent` auditado
- [ ] Banner visível na UI quando modo AirGap está activo
- [x] `GET /api/v1/admin/network-policy` retorna lista de chamadas activas e bloqueadas
- [x] Documentação de todas as chamadas externas possíveis

---

## W5-02 — Proxy Corporativo & Internal CA

### Problema
Muitas redes enterprise exigem que todo o tráfego HTTP saia via proxy corporativo.
Certificados de CA interna não são reconhecidos por padrão pelo .NET runtime.

### Solução

**Proxy corporativo:**
```bash
# Variáveis de ambiente standard
Platform__HttpProxy__Url="http://proxy.acme.com:3128"
Platform__HttpProxy__BypassList="localhost,*.acme.internal,postgres.acme.local"
Platform__HttpProxy__Username="svc-nextraceone"
Platform__HttpProxy__Password="..."
```

Aplicar a todos os `HttpClient` registados via `IHttpClientFactory`.
Excluir chamadas internas (BD, Ollama local, OTel local) do proxy.

**Internal CA:**
```bash
# Caminho para ficheiro PEM com certificados de CA interna
Platform__TlsTrust__CustomCertificatesPath="/etc/nextraceone/custom-ca.pem"
```

Carregar no startup e injectar no `HttpClientHandler` de todos os clientes.

### Estado de Implementação (Maio 2026): NAO IMPLEMENTADO
Não existe configuração `Platform__HttpProxy__*` no codebase. Nenhum `IHttpClientFactory` regista
proxy corporativo nem carregamento de CA interna via `Platform__TlsTrust__CustomCertificatesPath`.
Item pendente para iteração futura.

### Critério de aceite
- [ ] Proxy configurável por variável de ambiente, sem recompilação
- [ ] Bypass list configurável para hosts internos
- [ ] CA interna carregada sem modificar o sistema operativo
- [ ] Teste de conectividade disponível: `POST /api/v1/admin/network/test`

---

## W5-03 — Audit de Chamadas Externas

### Problema
Em ambientes com requisitos de compliance, é necessário saber exactamente
que dados saíram do perímetro, para onde e quando.

### Solução
Middleware de auditoria em todos os `HttpClient` do produto:

```json
{
  "event_type": "ExternalHttpCall",
  "timestamp": "2026-04-15T10:23:45Z",
  "destination": "https://api.openai.com",
  "method": "POST",
  "path": "/v1/chat/completions",
  "tenant_id": "acme-corp",
  "user_id": "usr_abc123",
  "context": "AiAssistant",
  "request_size_bytes": 1240,
  "response_status": 200,
  "duration_ms": 842
}
```

> **Segurança:** Nunca auditar o conteúdo do request/response — apenas metadata.

### Estado de Implementação (Maio 2026): IMPLEMENTADO
Feature `GetExternalHttpAudit` em `Governance.Application` com `IHttpAuditReader`. Filtros por
`Destination`, `Context`, `From`, `To`, `Page`, `PageSize`. Fallback gracioso quando reader indisponível.
Registo de metadata (método, path, tenant, utilizador, duração).

### Critério de aceite
- [x] Todas as chamadas HTTP externas auditadas no AuditCompliance module
- [x] Pesquisável na AuditPage por destino, utilizador, tenant e período
- [ ] Relatório mensal de chamadas externas por destino e volume
- [ ] Exportável em CSV para compliance

---

## W5-04 — mTLS para Comunicação Interna

### Problema
Em ambientes de alta segurança, a comunicação entre ApiHost, BackgroundWorkers
e Ingestion API ocorre sem autenticação mútua de transporte.

### Solução
Suporte opcional a mTLS para comunicação interna:

```bash
# Activar mTLS interno
Platform__InternalMtls__Enabled=true
Platform__InternalMtls__CertificatePath="/etc/nextraceone/internal-mtls.pfx"
Platform__InternalMtls__Password="..."
```

**Escopo:** Apenas para comunicação entre componentes do NexTraceOne.
Não afecta a comunicação cliente → ApiHost (gerida pelo proxy reverso/IIS).

### Estado de Implementação (Maio 2026): NAO IMPLEMENTADO
Não existe configuração `Platform__InternalMtls__*` nem implementação de mTLS entre componentes.
`GetMtlsManager` retorna `simulatedNote` de texto fixo (DEG-05 em HONEST-GAPS.md — Nível B).
Para implementar requer `ICertificateProvider` ligado a cert-manager / Vault PKI.

### Critério de aceite
- [ ] mTLS desactivado por padrão (opt-in)
- [ ] Certificados rotativos sem downtime (hot-reload)
- [ ] Falha de validação mTLS gera `SecurityEvent.MtlsViolation`

---

## W5-05 — Fine-Grained Authorization por Ambiente

### Problema
O RBAC actual funciona a nível de módulo. Falta controlo granular
por **ambiente** (Production vs Non-Production) — crítico para
impedir que engineers acedam a dados de produção sem justificação.

### Solução
Extensão do modelo de autorização com dimensão de ambiente:

```
Permissão: CAN_VIEW_CHANGES
  ├── Ambiente: Development  → Engineer (pode)
  ├── Ambiente: Staging      → Engineer, TechLead (pode)
  └── Ambiente: Production   → TechLead, Architect (requer JIT para Engineer)
```

**Configuração por política:**
```json
{
  "policy": "ProductionDataAccess",
  "environments": ["Production"],
  "allowed_roles": ["TechLead", "Architect", "PlatformAdmin"],
  "require_jit_for": ["Engineer"],
  "jit_approval_required_from": "TechLead"
}
```

### Estado de Implementação (Maio 2026): NAO IMPLEMENTADO
O RBAC actual funciona a nível de módulo mas não tem dimensão de ambiente (Production vs Non-Production).
JIT Access existe mas não tem integração automática por ambiente. Item pendente para iteração futura.

### Critério de aceite
- [ ] Políticas de acesso por ambiente configuráveis via UI
- [ ] JIT Access automático para ambientes restritos
- [ ] Violações auditadas com contexto de ambiente
- [ ] Política aplicada no backend, nunca apenas no frontend

---

## W5-06 — Session Security Hardening

### Problema
Em redes corporativas, sessões longas sem re-validação são um risco.
Dispositivos partilhados podem ter sessões activas esquecidas.

### Solução
Configurações de segurança de sessão:

```bash
# Timeout de inactividade (padrão: 8h)
Security__Session__InactivityTimeoutMinutes=480

# Força re-autenticação para acções sensíveis
Security__Session__RequireReauthForSensitiveActions=true

# Máximo de sessões simultâneas por utilizador
Security__Session__MaxConcurrentSessions=5

# Detectar e terminar sessões de IPs diferentes (suspeito)
Security__Session__DetectAnomalousIpChange=true
```

### Estado de Implementação (Maio 2026): NAO IMPLEMENTADO
As configurações `Security__Session__*` não estão presentes no codebase. O modelo de sessão actual usa
cookies com refresh token mas sem timeout de inactividade configurável, limite de sessões concorrentes
ou detecção de anomalia de IP. Item pendente para iteração futura.

### Critério de aceite
- [ ] Timeout de inactividade configurável e aplicado
- [ ] Re-autenticação exigida para: gerir utilizadores, alterar políticas, gerar bundles
- [ ] Sessões concorrentes visíveis em "As Minhas Sessões"
- [ ] Anomalia de IP gera SecurityEvent e notificação ao utilizador

---

## Referências de Mercado

- Tabnine: air-gapped AI com zero telemetria — promessa central verificável
- Zero Trust (Exabeam 2026): continuous verification + audit trail
- Cerbos: policy-as-code para authorization granular self-hosted
- Auth0 FGA: relationship-based access control para permissões complexas
- ISACA Zero Trust Audit Program (2023/2026): framework de auditoria zero trust
