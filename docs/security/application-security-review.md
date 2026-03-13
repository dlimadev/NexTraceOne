# Revisão de Segurança da Aplicação — NexTraceOne

> Documento gerado como parte do processo contínuo de security review.
> Data da última revisão: Março 2026.

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

---

## Achados por Severidade

### 🔴 Crítico

**Nenhum achado crítico.** A arquitetura fundamental de segurança está correta.

### 🟠 Alto

| # | Achado | Impacto | Ação |
|---|--------|---------|------|
| H-1 | Ausência de ErrorBoundary global | Erros não capturados podem expor stack traces no browser | ✅ Implementado ErrorBoundary que suprime detalhes técnicos em produção |
| H-2 | Ausência de CSP no frontend (HTML) | Sem defense-in-depth contra XSS no frontend | ✅ Adicionado meta tag CSP no index.html |
| H-3 | Build de produção sem drop_console | console.log pode vazar dados sensíveis em produção | ✅ Configurado terser com drop_console e drop_debugger |

### 🟡 Médio

| # | Achado | Impacto | Ação |
|---|--------|---------|------|
| M-1 | Nomes de assets sem hash no build | Facilita mapeamento de estrutura interna | ✅ Configurados nomes de assets com hash para ofuscação |
| M-2 | Ausência de utilitário de sanitização de URLs | Risco de javascript: injection em links dinâmicos | ✅ Criado módulo sanitize.ts com validação de esquemas |
| M-3 | Google Fonts carregado de CDN externo | Em on-premise isolado, pode não funcionar; tracking potencial | ⚠️ Documentado como risco residual — recomendação: hospedar fontes localmente |
| M-4 | Ausência de meta referrer no HTML | Referrer pode vazar URLs com parâmetros sensíveis | ✅ Adicionado `<meta name="referrer" content="strict-origin-when-cross-origin">` |

### 🟢 Baixo

| # | Achado | Impacto | Ação |
|---|--------|---------|------|
| L-1 | Fallback "User" e "Developer" em Sidebar quando user é null | Exposição mínima de UX default | ℹ️ Aceitável — são fallbacks visuais, não dados sensíveis |
| L-2 | Hardcoded "NexTraceOne" como brand name em componentes | Não é risco de segurança; é parte da identidade visual | ℹ️ Aceitável — não é dado dinâmico |
| L-3 | Email exibido como primeira letra no avatar | Dado pessoal mínimo, necessário para UX | ℹ️ Aceitável com minimização (primeira letra apenas) |

---

## O Que Foi Corrigido

1. **ErrorBoundary global** — Componente que captura erros React sem expor detalhes técnicos.
   Registra no console apenas em desenvolvimento.
2. **CSP meta tag** — Content Security Policy no index.html como defense-in-depth.
3. **Meta referrer** — Política de referrer restritiva para prevenir vazamento de URLs.
4. **Build hardening** — terser com drop_console/drop_debugger, nomes de assets com hash.
5. **Utilitário de sanitização** — Validação de URLs contra javascript:/data: injection.
6. **Documentação de segurança** — Diretrizes permanentes no Copilot Instructions.

---

## Riscos Residuais

1. **sessionStorage acessível via XSS na mesma origem** — Mitigação definitiva requer
   migração para httpOnly cookies (backend change). Access token tem duração curta (60 min).

2. **Código JavaScript client-side pode ser inspecionado** — Em aplicações web, não é
   possível proteger completamente o código client-side. A mitigação é: não incluir segredos,
   desativar source maps, minificar e ofuscar.

3. **Google Fonts CDN** — Em ambientes on-premise sem internet, as fontes não carregam.
   Recomendação: para deploys on-premise, hospedar fontes localmente.

4. **CORS baseado em configuração** — Se mal configurado pelo operador on-premise, pode
   abrir superfície de ataque. O backend já valida que nenhuma origem contém wildcard.

---

## Decisões Arquiteturais

1. **Refresh token em memória** — Trade-off: perde sessão ao recarregar a página, mas
   elimina risco de exfiltração via XSS do storage do browser.

2. **CSP strict no backend para API** — `default-src 'none'; frame-ancestors 'none'`
   garante que endpoints API não servem conteúdo executável.

3. **Separação de mensagens técnicas e UX** — Backend retorna códigos i18n; frontend
   resolve a mensagem final. Stack traces nunca chegam ao usuário.

4. **Verificação de integridade de assemblies** — AssemblyIntegrityChecker no boot
   para detecção de adulteração em ambiente on-premise.

---

## Recomendações para Backend/Infra

1. **Rate limiting** — Implementar rate limiting nos endpoints de autenticação.
2. **httpOnly cookies** — Migrar tokens para cookies httpOnly com Secure e SameSite=Strict.
3. **CSRF protection** — Se migrar para cookies, implementar CSRF tokens.
4. **Audit de dependências** — Executar `dotnet list package --vulnerable` periodicamente.
5. **Signing de artefatos** — Assinar assemblies .NET e artefatos de distribuição.
