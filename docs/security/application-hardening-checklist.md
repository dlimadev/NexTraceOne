# Checklist de Hardening da Aplicação — NexTraceOne

> Checklist operacional para validação de segurança antes de cada release.
> Atualizado em Março 2026.

---

## 1. Content Security Policy (CSP)

- [x] Meta tag CSP no `index.html` como defense-in-depth
- [x] Backend define CSP para API: `default-src 'none'; frame-ancestors 'none'`
- [ ] Proxy/servidor em produção define CSP via header HTTP (substitui meta tag)
- [ ] Nonces para scripts inline se necessário (atualmente não há scripts inline)

**Política recomendada para o frontend em produção (via proxy/servidor):**
```
Content-Security-Policy: default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; font-src 'self'; img-src 'self' data:; connect-src 'self'; frame-ancestors 'none'; form-action 'self'; base-uri 'self';
```

> Em on-premise, remover referências a domínios externos (fonts.googleapis.com) e hospedar fontes localmente.

---

## 2. Security Headers (Backend)

- [x] `X-Content-Type-Options: nosniff`
- [x] `X-Frame-Options: DENY`
- [x] `X-XSS-Protection: 0` (desativado conforme recomendação moderna)
- [x] `Referrer-Policy: strict-origin-when-cross-origin`
- [x] `Permissions-Policy: camera=(), microphone=(), geolocation=(), payment=()`
- [x] `Content-Security-Policy: default-src 'none'; frame-ancestors 'none'` (para API)
- [x] `Cache-Control: no-store` + `Pragma: no-cache` (API responses)
- [x] `Strict-Transport-Security` (em produção, com preload)

---

## 3. Política de Dependências

- [x] Lockfile (`package-lock.json`) versionado no repositório
- [x] 0 vulnerabilidades conhecidas em `npm audit`
- [x] Central package management no .NET (`Directory.Packages.props`)
- [ ] Executar `dotnet list package --vulnerable` em CI/CD
- [ ] Executar `npm audit` em CI/CD
- [ ] Política de atualização de dependências (mínimo trimestral)
- [ ] Verificação de licenças de dependências

---

## 4. Política de Logs

- [x] Logs em inglês para consistência operacional
- [x] Serilog com logger estruturado
- [x] Nenhum dado sensível logado no frontend (console suprimido em produção)
- [x] terser com `drop_console` e `drop_debugger` em builds de produção
- [ ] Verificar que nenhum endpoint loga passwords, tokens ou dados pessoais
- [ ] Configurar retenção de logs (atualmente 30 dias em arquivo)
- [ ] Não persistir PII (dados pessoais identificáveis) em logs

---

## 5. Política de Renderização de HTML

- [x] Nenhum uso de `dangerouslySetInnerHTML` no código do frontend
- [x] i18n com `escapeValue: true` (default seguro)
- [x] React escapa JSX nativamente
- [x] Utilitário `sanitize.ts` para validação de URLs
- [x] Utilitário `escapeHtml` para contextos fora de JSX
- [ ] Se futuras features requerem renderização de Markdown/HTML externo, usar DOMPurify

---

## 6. Política de Storage de Sessão/Tokens

- [x] Refresh token mantido exclusivamente em memória (closure)
- [x] Access token em sessionStorage (escopo de aba)
- [x] Nenhum token em localStorage
- [x] Migração automática de localStorage legado
- [x] `clearAllTokens()` limpa todos os dados de sessão
- [x] Evento `auth:session-expired` para invalidação reativa
- [ ] Migrar para httpOnly cookies (requer mudança no backend)

---

## 7. Política de Links Externos e Redirects

- [x] Allowlist de rotas internas para redirecionamento
- [x] Validação contra protocol-relative URLs (`//`)
- [x] Validação contra esquemas maliciosos (`javascript:`, `data:`)
- [x] Validação de caracteres de controle
- [x] `isExternalUrl()` para detecção de links externos
- [x] `isSafeUrl()` para validação de URLs em atributos
- [ ] Considerar aviso visual para links externos em futuras features

---

## 8. Política de Third-Party Scripts

- [x] Nenhum script de terceiros (analytics, tracking, etc.)
- [x] Fontes carregadas via Google Fonts CDN (único terceiro)
- [ ] Para on-premise, hospedar fontes localmente
- [ ] CSP restringe carregamento de scripts a `'self'`

---

## 9. Política de Minimização de Dados

- [x] Backend retorna apenas dados necessários para cada endpoint
- [x] Frontend não persiste dados além de tokens e IDs de sessão
- [x] Avatar exibe apenas primeira letra do email (minimização)
- [x] Nenhum dado pessoal em localStorage
- [ ] Verificar que exports/downloads não incluem dados desnecessários
- [ ] Verificar que filtros de auditoria não retornam dados pessoais em excesso

---

## 10. Build e Distribuição

- [x] Source maps desativados em produção
- [x] console.log removido via terser em produção
- [x] debugger removido via terser em produção
- [x] Nomes de assets com hash (sem exposição de estrutura interna)
- [x] Verificação de integridade de assemblies .NET no boot
- [ ] Signing de assemblies .NET
- [ ] Checksums SHA-256 de artefatos de distribuição
- [ ] Revisão de `.dockerignore` para container builds (quando aplicável)
