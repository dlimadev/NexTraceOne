# Revisão de Segurança da Aplicação — NexTraceOne

> Documento gerado como parte do processo contínuo de security review.
> Data da última revisão: Março 2026 (Fase 8 — Segurança e Prontidão Operacional).

---

## Resumo Executivo

O NexTraceOne é uma plataforma sovereign change intelligence, self-hosted e enterprise,
que deve operar em ambientes on-premise controlados pelo cliente. A segurança é tratada
como requisito não funcional obrigatório em todas as camadas: frontend, backend, banco
de dados, build, distribuição e runtime.

A aplicação apresenta **maturidade acima da média** para o estágio de MVP1, com decisões
de segurança bem fundamentadas desde o design. As principais medidas já implementadas incluem:

- Tokens de refresh mantidos exclusivamente em memória (nunca persistidos no browser)
- Access tokens em sessionStorage (escopo de aba, limpo ao fechar)
- Proteção contra open redirect com allowlist de rotas internas
- Security headers completos no backend (CSP, X-Frame-Options, HSTS, etc.)
- CORS restritivo com origens explícitas
- Interceptor de refresh token com proteção contra race conditions
- i18n completo para separação de mensagens técnicas e de UX
- Auditoria imutável com cadeia de hash SHA-256
- Verificação de integridade de assemblies no boot
- Source maps desativados em builds de produção
- **[Fase 8]** Fontes self-hosted (Inter + JetBrains Mono via @fontsource) — sem CDN externo
- **[Fase 8]** CSP endurecida — sem font-src externo
- **[Fase 8]** Infraestrutura de httpOnly cookies + CSRF pronta (opt-in, desabilitada por padrão)

---

## Achados por Severidade

### 🔴 Crítico

**Nenhum achado crítico.** A arquitetura fundamental de segurança está correta.

### 🟠 Alto

| # | Achado | Impacto | Ação | Estado |
|---|--------|---------|------|--------|
| H-1 | Ausência de ErrorBoundary global | Erros não capturados podem expor stack traces no browser | ✅ Implementado ErrorBoundary que suprime detalhes técnicos em produção | **Fechado** |
| H-2 | Ausência de CSP no frontend (HTML) | Sem defense-in-depth contra XSS no frontend | ✅ Adicionado meta tag CSP no index.html | **Fechado** |
| H-3 | Build de produção sem drop_console | console.log pode vazar dados sensíveis em produção | ✅ Configurado terser com drop_console e drop_debugger | **Fechado** |

### 🟡 Médio

| # | Achado | Impacto | Ação | Estado |
|---|--------|---------|------|--------|
| M-1 | Nomes de assets sem hash no build | Facilita mapeamento de estrutura interna | ✅ Configurados nomes de assets com hash para ofuscação | **Fechado** |
| M-2 | Ausência de utilitário de sanitização de URLs | Risco de javascript: injection em links dinâmicos | ✅ Criado módulo sanitize.ts com validação de esquemas | **Fechado** |
| M-3 | Google Fonts carregado de CDN externo | Em on-premise isolado, pode não funcionar; tracking potencial; CSP com exceção externa | ✅ **[Fase 8]** Migrado para @fontsource (self-hosted). Removidas exceções `fonts.googleapis.com` e `fonts.gstatic.com` do CSP. | **Fechado** |
| M-4 | Ausência de meta referrer no HTML | Referrer pode vazar URLs com parâmetros sensíveis | ✅ Adicionado `<meta name="referrer" content="strict-origin-when-cross-origin">` | **Fechado** |
| M-5 | Ausência de infraestrutura CSRF para sessão cookie | Necessário quando/se tokens migrarem para httpOnly cookies | ✅ **[Fase 8]** `CsrfTokenValidator` + `CookieSessionEndpoints` implementados (padrão double-submit). Ativação por feature flag. | **Infra pronta** |

### 🟢 Baixo

| # | Achado | Impacto | Ação | Estado |
|---|--------|---------|------|--------|
| L-1 | Fallback "User" e "Developer" em Sidebar quando user é null | Exposição mínima de UX default | ℹ️ Aceitável — são fallbacks visuais, não dados sensíveis | **Aceite** |
| L-2 | Hardcoded "NexTraceOne" como brand name em componentes | Não é risco de segurança; é parte da identidade visual | ℹ️ Aceitável — não é dado dinâmico | **Aceite** |
| L-3 | Email exibido como primeira letra no avatar | Dado pessoal mínimo, necessário para UX | ℹ️ Aceitável com minimização (primeira letra apenas) | **Aceite** |

---

## O Que Foi Corrigido

### Fases Anteriores
1. **ErrorBoundary global** — Componente que captura erros React sem expor detalhes técnicos.
2. **CSP meta tag** — Content Security Policy no index.html como defense-in-depth.
3. **Meta referrer** — Política de referrer restritiva para prevenir vazamento de URLs.
4. **Build hardening** — terser com drop_console/drop_debugger, nomes de assets com hash.
5. **Utilitário de sanitização** — Validação de URLs contra javascript:/data: injection.

### Fase 8 — Segurança e Prontidão Operacional
6. **Self-hosted fonts** — Migrado de Google Fonts CDN para `@fontsource/inter` e
   `@fontsource/jetbrains-mono`. Produto funciona agora em ambientes on-premise
   completamente isolados da internet. CSP removeu as exceções `fonts.googleapis.com`
   e `fonts.gstatic.com`.
7. **Infraestrutura CSRF** — `CsrfTokenValidator` (padrão double-submit cookie) e
   `CookieSessionEndpoints` implementados. Prontos para a migração de sessão.
8. **Documentação de variáveis de ambiente** — `docs/ENVIRONMENT-VARIABLES.md` criado
   com todas as variáveis obrigatórias, opcionais, health endpoints e checklist de deploy.
9. **DEPLOYMENT-ARCHITECTURE.md** — Expandido de 4 linhas para documento operacional
   completo com topologia, componentes, sequência de startup, headers de segurança.
10. **SECURITY-ARCHITECTURE.md** — Expandido com controles implementados, gaps documentados,
    plano de migração e estado pós-Fase 8.

---

## Riscos Residuais

### R-1 — sessionStorage acessível via XSS (MÉDIO — mitigação parcial)

**Descrição:** O access token em sessionStorage é acessível por JavaScript no mesmo origin.
Em caso de XSS na mesma aba/origin, o token pode ser lido.

**Mitigação atual:**
- sessionStorage é de escopo de aba (não partilhado entre abas ou persiste).
- Refresh token NUNCA está em storage — apenas em memória.
- Access token tem duração curta (60 minutos).
- CSP restringe execução de JavaScript não autorizado.
- ErrorBoundary suprime stack traces.

**Mitigação definitiva (planeada):**
- Migrar para httpOnly cookie via `POST /api/v1/identity/cookie-session`.
- A infraestrutura de backend está implementada (Fase 8).
- Ativação requer: frontend migrado + validação em staging + cutover monitorizado.
- Ver `docs/SECURITY-ARCHITECTURE.md` → "Plano de Migração" para passos detalhados.

**Plano de ativação:**
```
appsettings.json: Auth:CookieSession:Enabled = true  (staging first)
```

### R-2 — Encryption at rest incompleta (MÉDIO)

**Descrição:** `AesGcmEncryptor` (AES-256-GCM) e `EncryptedStringConverter` existem,
mas não estão aplicados a campos sensíveis nas entidades (ex.: tokens, PIIs).

**Mitigação atual:**
- `NEXTRACE_ENCRYPTION_KEY` obrigatório em produção (startup falha sem ele).
- PostgreSQL access control protege acesso direto ao DB.

**Próximo passo:** Identificar e marcar campos sensíveis com `[Encrypted]` e aplicar
o `EncryptedStringConverter` nas configurações EF Core dos módulos relevantes.
Requer migration de schema e rotação dos dados existentes.

### R-3 — Código JavaScript client-side inspecionável (BAIXO — aceite)

Em aplicações web, o código client-side não pode ser completamente protegido.
A mitigação é: não incluir segredos, desativar source maps, minificar e ofuscar.
Todas estas mitigações estão implementadas.

### R-4 — CORS baseado em configuração operacional (MÉDIO se mal configurado)

Se o operador on-premise configurar `*` nas origens, a proteção CORS é nula.
**Mitigação:** O backend valida em startup que nenhuma origem contém wildcard
(`WebApplicationBuilderExtensions.AddCorsConfiguration`). Não é possível iniciar
com wildcard + AllowCredentials.

### R-5 — `appsettings.json` com credentials de desenvolvimento (BAIXO)

O ficheiro `appsettings.json` commitado contém passwords de desenvolvimento (`ouro18`).
Este é um pattern comum para desenvolvimento local mas **nunca deve chegar a produção**.
**Mitigação:** Documentado no checklist de deploy. As variáveis de produção devem ser
configuradas via environment variables (que sobrescrevem o appsettings.json).

---

## Decisões Arquiteturais de Segurança

1. **Refresh token em memória** — Trade-off: perde sessão ao recarregar a página, mas
   elimina risco de exfiltração via XSS do storage do browser.

2. **CSP strict no backend para API** — `default-src 'none'; frame-ancestors 'none'`
   garante que endpoints API não servem conteúdo executável.

3. **Separação de mensagens técnicas e UX** — Backend retorna códigos i18n; frontend
   resolve a mensagem final. Stack traces nunca chegam ao usuário.

4. **Verificação de integridade de assemblies** — AssemblyIntegrityChecker no boot
   para detecção de adulteração em ambiente on-premise.

5. **Feature flag para cookie session** — A migração para httpOnly cookies é um
   rollout controlado, não uma substituição imediata, para garantir continuidade
   de serviço e permitir validação graduada.

6. **Bearer token sem CSRF** — Com Authorization header, CSRF não é um vetor.
   Browsers não enviam headers personalizados automaticamente cross-origin.
   A proteção CSRF é necessária apenas no modelo de cookie (implementada na Fase 8).

---

## Recomendações para Próximas Fases

1. **Encryption at rest** — Aplicar `EncryptedStringConverter` aos campos mais sensíveis:
   - `IdentityAccess`: tokens de break glass, tokens de delegação, refresh tokens persistidos
   - `ChangeGovernance`: evidence pack attachments com dados sensíveis
2. **Signing de artefatos** — Implementar no pipeline CI/CD com cosign (containers) e
   Authenticode ou equivalent para assemblies .NET.
3. **Dependency audit** — Adicionar `dotnet list package --vulnerable` ao CI.
4. **Penetration testing** — Recomendado antes de primeiro deploy em cliente enterprise.
5. **SAST** — Considerar integração de análise estática (Semgrep, CodeQL) ao CI.

---

*Documento atualizado na Fase 8 — Segurança e Prontidão Operacional.*
*Última atualização: Março 2026.*
