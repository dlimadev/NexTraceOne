# Notas de Integração Backend/Infra para Segurança — NexTraceOne

> Documento que registra mudanças exigidas ou recomendadas em backend e infraestrutura
> como resultado da revisão de segurança do frontend e da aplicação.
> Atualizado em Março 2026.

---

## 1. Mudanças Exigidas no Backend

### 1.1. Rate Limiting em Endpoints de Autenticação (Prioridade: Alta)

**Contexto:** Os endpoints `/api/v1/identity/auth/login` e `/api/v1/identity/auth/refresh`
não possuem rate limiting explícito. Em produção, isso permite ataques de força bruta.

**Recomendação:**
- Implementar rate limiting via `AspNetCoreRateLimit` ou middleware customizado
- Limitar a 5 tentativas por IP por minuto no endpoint de login
- Limitar a 10 tentativas por IP por minuto no endpoint de refresh
- Retornar 429 Too Many Requests com header `Retry-After`

### 1.2. Configuração de Cookies Seguros (Prioridade: Média)

**Contexto:** A estratégia atual de tokens é segura (sessionStorage + memória),
mas a migração para httpOnly cookies proporcionaria proteção adicional contra XSS.

**Recomendação para futuro:**
- Access token como httpOnly cookie com `Secure`, `SameSite=Strict`
- Implementar CSRF token se migrar para cookies
- Manter refresh token em httpOnly cookie (nunca acessível por JS)

### 1.3. Validação de Segredos no Startup (Prioridade: Alta)

**Contexto:** A aplicação deve falhar rápido se segredos obrigatórios não estiverem configurados.

**Recomendação:**
- Verificar que `Jwt:Secret` não está vazio e tem comprimento mínimo
- Verificar que `ConnectionStrings:NexTraceOne` está configurado
- Logar aviso se `IntegrityCheck` está desativado em produção
- Usar `IStartupFilter` ou `IHostedService` para validação

---

## 2. Mudanças Exigidas na Infraestrutura

### 2.1. HTTPS Obrigatório (Prioridade: Crítica)

**Contexto:** O backend configura HSTS em produção, mas a infraestrutura deve
garantir que o certificado TLS está corretamente configurado.

**Requisitos:**
- Certificado TLS válido para o domínio da aplicação
- TLS 1.2 mínimo (recomendado TLS 1.3)
- Redirecionamento HTTP → HTTPS
- HSTS com `max-age=63072000; includeSubDomains; preload`

### 2.2. PostgreSQL Hardening (Prioridade: Alta)

**Requisitos:**
- SSL/TLS para conexões ao banco de dados
- Encryption at rest para dados sensíveis
- Backup criptografado
- Usuário de aplicação com mínimo de privilégios necessários
- Audit logging no banco de dados
- Connection pooling com timeouts adequados

### 2.3. Reverse Proxy com Security Headers (Prioridade: Alta)

**Contexto:** O backend define security headers, mas um proxy reverso (nginx, Caddy, etc.)
oferece uma camada adicional de proteção e pode servir o frontend estático.

**Recomendação:**
- Proxy reverso para servir frontend (estático) e backend (API)
- CSP para frontend definido no proxy (mais seguro que meta tag)
- Rate limiting no proxy como camada adicional
- Request size limits
- Timeout adequados

---

## 3. Mudanças na Configuração de CI/CD

### 3.1. Pipeline de Build de Produção

**Checklist:**
- [ ] Build com configuração `Release`
- [ ] `DebugType=none` e `DebugSymbols=false`
- [ ] `GenerateDocumentationFile=false`
- [ ] Frontend com `npm run build` (source maps desativados)
- [ ] Não incluir `appsettings.Development.json` no artefato
- [ ] Não incluir testes, docs de desenvolvimento, .git
- [ ] Gerar checksums SHA-256 dos artefatos
- [ ] Armazenar artefatos em repositório seguro

### 3.2. Verificação de Dependências em CI/CD

**Checklist:**
- [ ] `npm audit` no frontend
- [ ] `dotnet list package --vulnerable` no backend
- [ ] Falhar o pipeline em vulnerabilidades críticas ou altas
- [ ] Alertas para vulnerabilidades médias

---

## 4. Monitoramento e Observabilidade

### 4.1. Métricas de Segurança Recomendadas

| Métrica | Finalidade |
|---------|------------|
| Tentativas de login falhadas por IP | Detecção de brute force |
| Tokens expirados/revogados | Monitoramento de sessões |
| Requisições 401/403 | Detecção de acessos não autorizados |
| Erros 500 | Detecção de problemas internos |
| Tamanho de payload anômalo | Detecção de ataques de injeção |

### 4.2. Alertas Recomendados

- Login failures > 10 em 5 minutos por IP
- Erros 500 > 5 em 1 minuto
- Integrity check failure no boot
- Certificado TLS próximo de expirar (< 30 dias)

---

## 5. Priorização de Implementação

| Prioridade | Item | Responsável |
|------------|------|-------------|
| P0 | HTTPS + TLS obrigatório | Infra |
| P0 | Validação de segredos no startup | Backend |
| P1 | Rate limiting em auth endpoints | Backend |
| P1 | PostgreSQL hardening | Infra |
| P1 | Pipeline de build de produção | DevOps |
| P2 | Reverse proxy com security headers | Infra |
| P2 | httpOnly cookies para tokens | Backend |
| P2 | Verificação de dependências em CI/CD | DevOps |
| P3 | Métricas e alertas de segurança | Infra + Backend |
