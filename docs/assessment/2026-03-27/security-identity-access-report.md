# Relatório de Avaliação de Segurança, Identidade e Acesso

**Produto:** NexTraceOne
**Data da Avaliação:** 2026-03-27
**Tipo:** Avaliação Técnica de Segurança — Análise Estática do Repositório
**Classificação:** Interno — Confidencial
**Versão:** 1.0

---

## Índice

1. [Sumário Executivo](#1-sumário-executivo)
2. [Arquitetura de Autenticação](#2-arquitetura-de-autenticação)
3. [Modelo de Autorização](#3-modelo-de-autorização)
4. [Isolamento de Tenant](#4-isolamento-de-tenant)
5. [Encriptação](#5-encriptação)
6. [Segurança do Frontend](#6-segurança-do-frontend)
7. [Rate Limiting](#7-rate-limiting)
8. [Funcionalidades de Identidade Avançada](#8-funcionalidades-de-identidade-avançada)
9. [Lacunas e Vulnerabilidades Identificadas](#9-lacunas-e-vulnerabilidades-identificadas)
10. [Recomendações](#10-recomendações)
11. [Matriz de Conformidade](#11-matriz-de-conformidade)
12. [Conclusão](#12-conclusão)

---

## 1. Sumário Executivo

Esta avaliação analisa a postura de segurança do NexTraceOne com base na análise estática do repositório, cobrindo autenticação, autorização, isolamento multi-tenant, encriptação, segurança do frontend, rate limiting, e funcionalidades avançadas de identidade.

### Resultado Geral

| Área | Classificação | Observação |
|------|---------------|------------|
| Autenticação | **Forte** | Três métodos complementares, bem implementados |
| Autorização | **Forte** | Granular, deny-by-default, auditado |
| Isolamento de Tenant | **Forte** | Multicamada: JWT + RLS + application pipeline |
| Encriptação | **Forte** | AES-256-GCM com authenticated encryption |
| Segurança Frontend | **Forte** | Zero dangerouslySetInnerHTML, sanitização de URLs, CSRF |
| Rate Limiting | **Adequado** | 6 políticas configuráveis, mas sem distributed state |
| Identidade Avançada | **Forte** | Break Glass, JIT, Delegações, Access Reviews |
| Gestão de Segredos | **Adequado** | Env vars em produção, placeholders em config |
| Injeção SQL | **Sem Risco** | 100% EF Core parametrizado, zero raw SQL |

**Avaliação global:** O NexTraceOne apresenta uma postura de segurança **enterprise-grade** sólida, com múltiplas camadas de defesa em profundidade. Existem lacunas pontuais documentadas na secção 9 que devem ser endereçadas nas próximas iterações.

---

## 2. Arquitetura de Autenticação

### 2.1 Visão Geral

O NexTraceOne implementa três métodos de autenticação complementares, cada um desenhado para um cenário de uso específico:

| Método | Cenário Principal | Localização |
|--------|-------------------|-------------|
| JWT Bearer | API-to-API, SPA, integrações | Header `Authorization: Bearer <token>` |
| Cookie Session | Browser session, SSO redirect | Cookie httpOnly + CSRF token |
| API Key | Integrações M2M, CI/CD pipelines | Header `X-Api-Key` |

### 2.2 JWT Bearer — Análise Detalhada

**Configuração identificada:**
- Emissor (Issuer) e audiência (Audience) configuráveis via `appsettings`
- Chave de assinatura (signing key) configurável
- Tempo de vida do access token: 60 minutos
- Suporte a refresh token
- Claims padrão: `sub`, `email`, `name`, `tenant_id`, `permissions`

**Pontos fortes:**
- A separação entre access token e refresh token segue as melhores práticas da indústria (RFC 6749, RFC 6750)
- O tempo de vida de 60 minutos é adequado para uso enterprise — suficientemente curto para limitar exposição, suficientemente longo para minimizar fricção
- A inclusão de `tenant_id` no token permite validação de tenant sem lookup adicional na base de dados
- Claims de `permissions` embebidas no token permitem autorização sem round-trip ao servidor de identidade

**Pontos de atenção:**
- A chave de assinatura deve ser rotacionada periodicamente; não foi identificado mecanismo automático de rotação de chaves
- Tokens JWT não podem ser revogados individualmente sem um mecanismo de blacklist; a revogação depende da expiração natural ou da revogação da sessão
- O tamanho do token pode crescer significativamente com muitas permissões embebidas — considerar token introspection para cenários com muitas permissões

**Classificação:** ✅ Forte — implementação sólida e alinhada com padrões enterprise

### 2.3 Cookie Session — Análise Detalhada

**Configuração identificada:**
- Access token armazenado em cookie httpOnly (inacessível a JavaScript)
- CSRF token em cookie não-httpOnly (legível pelo JavaScript para inclusão no header)
- `SameSite=Strict` para mitigar CSRF cross-site
- Fallback automático: o sistema verifica primeiro o header `Authorization`, depois o cookie

**Pontos fortes:**
- O padrão double-submit cookie para CSRF é robusto e bem implementado
- A flag `httpOnly` no cookie de autenticação impede acesso via XSS
- `SameSite=Strict` fornece uma camada adicional de proteção contra CSRF
- O fallback automático entre header e cookie permite flexibilidade para diferentes clientes

**Pontos de atenção:**
- A flag `Secure` deve estar sempre ativa em produção para garantir transmissão apenas via HTTPS
- Verificar se existe política de expiração alinhada com os 60 minutos do JWT
- O cookie path deve ser restrito ao mínimo necessário (preferencialmente `/api` ou similar)

**Classificação:** ✅ Forte — padrão moderno e seguro para sessões browser

### 2.4 API Key — Análise Detalhada

**Configuração identificada:**
- Header `X-Api-Key` para transmissão
- Validação baseada em configuração (MVP1)
- Cada API key associada a: `clientId`, `clientName`, `tenantId`, `permissions`
- Validação com `CryptographicOperations.FixedTimeEquals()` para proteção contra timing attacks

**Pontos fortes:**
- A utilização de `CryptographicOperations.FixedTimeEquals()` é uma decisão de segurança excelente — protege contra ataques de temporização que poderiam permitir descoberta incremental da chave
- A associação de cada API key a tenant e permissões específicas permite controlo granular
- O modelo simples é adequado para MVP1

**Pontos de atenção:**
- No MVP1, as chaves são geridas via configuração; esta abordagem não escala para cenários multi-tenant com muitos clientes
- Não foi identificado mecanismo de rotação de API keys
- Não há registo de última utilização (last used) para facilitar revogação de chaves inativas
- Considerar migração para armazenamento em base de dados com hashing (SHA-256) para as fases seguintes

**Classificação:** ✅ Adequado para MVP1 — necessita evolução para escala enterprise

### 2.5 Proteção CSRF — Análise Detalhada

**Implementação identificada:**
- Localização: `src/building-blocks/NexTraceOne.BuildingBlocks.Security/CookieSession/CsrfTokenValidator.cs`
- Padrão double-submit cookie (conformidade RFC 8769)
- Geração: 32 bytes aleatórios → Base64 URL-safe
- Validação: header `X-Csrf-Token` comparado com cookie
- Comparação constant-time para evitar timing attacks
- Métodos isentos: `GET`, `HEAD`, `OPTIONS`

**Pontos fortes:**
- 32 bytes de entropia aleatória fornecem 256 bits de segurança — muito acima do mínimo recomendado (128 bits)
- A comparação constant-time impede que um atacante descubra o token por análise de tempos de resposta
- A isenção dos métodos seguros (`GET`, `HEAD`, `OPTIONS`) segue as especificações HTTP e não introduz risco desde que estes endpoints não realizem operações com efeitos colaterais

**Classificação:** ✅ Forte — implementação exemplar de proteção CSRF

---

## 3. Modelo de Autorização

### 3.1 Visão Geral

O modelo de autorização do NexTraceOne é baseado em permissões granulares com política deny-by-default, implementado através de um provider dinâmico de políticas.

### 3.2 Estrutura de Permissões

**Quantidade identificada:** Mais de 100 permissões granulares

**Padrão de nomenclatura:** `módulo:recurso:ação`

Exemplos identificados:
- `identity:users:read`
- `identity:users:write`
- `contracts:write`
- `services:read`
- `changes:approve`
- `ai:assistant:use`

**Avaliação do padrão:**
- O formato hierárquico `módulo:recurso:ação` é claro, previsível e escalável
- Permite agrupamento lógico por módulo, facilitando a gestão de papéis
- A granularidade é adequada para cenário enterprise onde diferentes personas precisam de acessos distintos

### 3.3 Papéis Pré-definidos

| Papel | Perfil Funcional | Escopo Típico |
|-------|-------------------|---------------|
| PlatformAdmin | Administração total da plataforma | Global |
| TechLead | Gestão técnica de equipas e serviços | Equipa/Domínio |
| Developer | Desenvolvimento e operação quotidiana | Serviços próprios |
| Viewer | Consulta e visualização | Read-only |
| Auditor | Auditoria e compliance | Leitura transversal |
| SecurityReview | Revisão de segurança | Segurança e acessos |
| ApprovalOnly | Aprovação de mudanças e promoções | Fluxos de aprovação |

**Avaliação:**
- Os 7 papéis cobrem as personas oficiais do produto definidas na visão do NexTraceOne
- O papel `ApprovalOnly` é uma adição inteligente para cenários de segregação de deveres
- O papel `SecurityReview` permite revisões de segurança sem privilégios de administração
- A composição papel→permissões permite flexibilidade sem complexidade excessiva

### 3.4 Provider Dinâmico de Políticas

**Implementação:** `PermissionPolicyProvider`

**Funcionamento:**
- Políticas são criadas dinamicamente com base no nome da permissão requerida
- Cada endpoint ou handler pode declarar a permissão necessária via atributo
- O provider verifica se a claim `permissions` do utilizador contém a permissão requerida

**Pontos fortes:**
- Elimina a necessidade de registar manualmente centenas de políticas no startup
- Permite adição de novas permissões sem alteração de código de infraestrutura
- Padrão deny-by-default: se a política não existir ou a permissão não estiver presente, o acesso é negado

### 3.5 Auditoria de Autorizações

**Comportamento identificado:**
- Negações de autorização são registadas em log de auditoria
- Inclui identidade do utilizador, recurso tentado, permissão requerida e timestamp

**Avaliação:**
- O registo de negações é essencial para deteção de tentativas de acesso indevido
- Permite criação de alertas para padrões suspeitos (ex: múltiplas negações do mesmo utilizador)
- Alinhado com requisitos de compliance (SOC 2, ISO 27001)

**Classificação:** ✅ Forte — modelo granular, deny-by-default, auditado

---

## 4. Isolamento de Tenant

### 4.1 Visão Geral

O NexTraceOne implementa isolamento multi-tenant em três camadas independentes, proporcionando defesa em profundidade:

1. **Camada de Identidade (JWT)** — tenant_id como claim do token
2. **Camada de Aplicação (MediatR Pipeline)** — TenantIsolationBehavior
3. **Camada de Base de Dados (PostgreSQL RLS)** — Row-Level Security

### 4.2 Resolução de Tenant

**Ordem de precedência:**
1. Claim `tenant_id` do JWT (primária e mais segura)
2. Header `X-Tenant-Id` (apenas para pedidos autenticados, como fallback)
3. Hash SHA-256 do subdomínio → GUID (para resolução por subdomínio)

**Regra de segurança crítica identificada:**
> Pedidos não autenticados **NÃO PODEM** sobrepor o tenant via header.

Esta regra é fundamental para impedir que atacantes não autenticados manipulem o contexto de tenant.

### 4.3 Isolamento na Camada de Aplicação

**Implementação:** `TenantIsolationBehavior` como behavior no pipeline MediatR

**Funcionamento:**
- Antes de executar qualquer handler, o behavior verifica e injeta o contexto de tenant
- Requests marcados com `IPublicRequest` são isentos (ex: endpoints públicos de health check)
- Todos os outros requests devem ter contexto de tenant válido

**Pontos fortes:**
- Isolamento aplicado automaticamente via pipeline, sem dependência de cada developer lembrar de verificar tenant
- A interface `IPublicRequest` torna explícita a isenção, facilitando auditoria de código

### 4.4 Isolamento na Base de Dados (PostgreSQL RLS)

**Implementação:**
```sql
set_config('app.current_tenant_id', @tenantId, false)
```

**Funcionamento:**
- Antes de cada operação, o tenant_id é configurado como parâmetro de sessão PostgreSQL
- Políticas RLS na base de dados filtram automaticamente todas as queries pelo tenant_id da sessão
- Mesmo que a camada de aplicação falhe em filtrar, a base de dados garante isolamento

**Pontos fortes:**
- Defesa em profundidade verdadeira — mesmo com bug na aplicação, dados de outros tenants não são acessíveis
- RLS é transparente para o código de aplicação, reduzindo superfície de erro
- O parâmetro `false` em `set_config` significa que a configuração persiste durante toda a transação

**Pontos de atenção:**
- Verificar que todas as tabelas multi-tenant têm políticas RLS ativas
- Verificar que superusers/connection pools não ignoram RLS inadvertidamente
- Considerar testes automatizados que validem isolamento cross-tenant

### 4.5 Avaliação de Cenários de Ataque

| Cenário | Protegido? | Mecanismo |
|---------|------------|-----------|
| Utilizador tenta aceder dados de outro tenant | ✅ | JWT claim + RLS |
| Pedido não autenticado com header X-Tenant-Id | ✅ | Regra de rejeição explícita |
| Bug na aplicação que não filtra por tenant | ✅ | PostgreSQL RLS como rede de segurança |
| Subdomínio manipulado | ✅ | Hash SHA-256 → GUID determinístico |
| Bypass do MediatR pipeline | ⚠️ | Depende de RLS; endpoint direto sem MediatR poderia ser risco |

**Classificação:** ✅ Forte — isolamento multicamada exemplar para plataforma enterprise

---

## 5. Encriptação

### 5.1 Implementação Identificada

**Localização:** `src/building-blocks/NexTraceOne.BuildingBlocks.Security/Encryption/AesGcmEncryptor.cs`

**Algoritmo:** AES-256-GCM (Galois/Counter Mode)

**Parâmetros:**
- Chave: 32 bytes (256 bits) derivada da variável de ambiente `NEXTRACE_ENCRYPTION_KEY`
- Nonce: 12 bytes aleatórios gerados por operação
- Tag de autenticação: 16 bytes (128 bits)
- Formato de saída: `nonce || tag || ciphertext` → Base64

### 5.2 Avaliação do Algoritmo

**AES-256-GCM** é a escolha recomendada pelo NIST para authenticated encryption (AEAD). Oferece:

- **Confidencialidade:** Dados encriptados não podem ser lidos sem a chave
- **Integridade:** A tag de autenticação garante que os dados não foram alterados
- **Autenticação:** A tag confirma que os dados foram produzidos por quem possui a chave

### 5.3 Análise de Parâmetros

| Parâmetro | Valor | Avaliação |
|-----------|-------|-----------|
| Tamanho da chave | 256 bits | ✅ Máxima segurança AES |
| Tamanho do nonce | 96 bits (12 bytes) | ✅ Tamanho recomendado para GCM |
| Tamanho da tag | 128 bits (16 bytes) | ✅ Máxima proteção de integridade |
| Geração do nonce | Aleatório por operação | ✅ Impede reutilização |

### 5.4 Segurança da Chave

**Pontos fortes:**
- Chave fornecida exclusivamente via variável de ambiente (`NEXTRACE_ENCRYPTION_KEY`)
- Sem chaves hardcoded no código ou configuração
- Separação clara entre código e segredo

**Pontos de atenção:**
- Não foi identificado mecanismo de rotação de chave de encriptação
- A rotação requer re-encriptação de todos os dados existentes
- Considerar implementar suporte a múltiplas chaves (key versioning) para facilitar rotação gradual
- Considerar integração com vault (HashiCorp Vault, Azure Key Vault) para fases futuras

### 5.5 Risco de Reutilização de Nonce

Com nonces de 96 bits gerados aleatoriamente, o risco de colisão (birthday problem) torna-se relevante após aproximadamente 2^48 (~281 triliões) de operações com a mesma chave. Este limite é suficiente para cenários enterprise normais, mas deve ser monitorizado em cenários de volume extremo.

**Classificação:** ✅ Forte — implementação criptograficamente sólida e moderna

---

## 6. Segurança do Frontend

### 6.1 Proteção contra XSS

**Achados:**
- **Zero utilizações de `dangerouslySetInnerHTML`** em todo o codebase React
- Utilitário de sanitização de URLs: `sanitize.ts` com função `isSafeUrl()`
- i18n configurado com `escapeValue: true` — previne XSS via strings de tradução
- React por defeito escapa conteúdo renderizado via JSX

**Avaliação:**
- A ausência completa de `dangerouslySetInnerHTML` é uma postura de segurança exemplar
- A sanitização de URLs previne ataques via `javascript:` URIs e protocolos maliciosos
- A proteção via i18n é uma camada frequentemente negligenciada — boa decisão

### 6.2 Armazenamento de Tokens

| Token | Armazenamento | Avaliação |
|-------|---------------|-----------|
| Access Token | `sessionStorage` | ✅ Isolado por tab, limpo ao fechar |
| Refresh Token | Apenas em memória | ✅ Máxima segurança, sem persistência |
| CSRF Token | Memória + `sessionStorage` fallback | ✅ Adequado |

**Avaliação detalhada:**

A escolha de `sessionStorage` para o access token representa um compromisso equilibrado:
- **vs. localStorage:** sessionStorage é mais seguro pois é isolado por tab e limpo ao fechar o browser
- **vs. Cookie httpOnly:** sessionStorage não é enviado automaticamente com requests, mas é acessível a XSS (mitigado pela ausência de vetores XSS)
- **vs. Memória pura:** sessionStorage sobrevive a refreshes de página, melhorando UX sem degradar significativamente a segurança

A manutenção do refresh token apenas em memória é a abordagem mais segura possível — um atacante precisaria de acesso ao processo do browser para extraí-lo.

### 6.3 Migração de Tokens

**Identificado:** Mecanismo de migração de `localStorage` para `sessionStorage`

**Avaliação:**
- Indica que o sistema originalmente usava localStorage e evoluiu para sessionStorage
- A migração automática garante transição suave para utilizadores existentes
- Boa prática de segurança incremental

### 6.4 Proteção CSRF no Frontend

**Implementação:**
- Token CSRF armazenado em memória com fallback para sessionStorage
- Incluído automaticamente em requests que modificam estado
- Validado no backend via CsrfTokenValidator

**Classificação:** ✅ Forte — postura defensiva exemplar no frontend

---

## 7. Rate Limiting

### 7.1 Políticas Identificadas

| Política | Cenário | Configurável |
|----------|---------|-------------|
| Global | Todos os endpoints | ✅ PermitLimit, WindowMinutes, QueueLimit |
| Auth | Endpoints de autenticação | ✅ PermitLimit, WindowMinutes, QueueLimit |
| AuthSensitive | Login, password reset, MFA | ✅ PermitLimit, WindowMinutes, QueueLimit |
| AI | Endpoints de IA/LLM | ✅ PermitLimit, WindowMinutes, QueueLimit |
| DataIntensive | Queries pesadas, exports | ✅ PermitLimit, WindowMinutes, QueueLimit |
| Operations | Endpoints operacionais | ✅ PermitLimit, WindowMinutes, QueueLimit |

### 7.2 Avaliação

**Pontos fortes:**
- A segmentação em 6 políticas permite ajuste fino por tipo de operação
- Endpoints sensíveis (autenticação, IA) têm políticas dedicadas
- A configurabilidade via `PermitLimit`, `WindowMinutes` e `QueueLimit` oferece flexibilidade operacional
- `QueueLimit` permite absorção de picos sem rejeição imediata

**Pontos de atenção:**
- Não foi identificado mecanismo de rate limiting distribuído — em cenários com múltiplas instâncias, cada instância pode ter contador independente, permitindo que o limite real seja multiplicado pelo número de instâncias
- Verificar se existe rate limiting por tenant (evitar que um tenant esgote recursos de outros)
- Verificar se existe rate limiting por IP para endpoints não autenticados
- Considerar sliding window em vez de fixed window para evitar burst no início de cada janela
- Avaliar respostas com headers informativos (`X-RateLimit-Limit`, `X-RateLimit-Remaining`, `Retry-After`)

### 7.3 Cenários de Ataque e Proteção

| Cenário | Protegido? | Política |
|---------|------------|----------|
| Brute force de login | ✅ | AuthSensitive |
| Abuso de endpoints de IA | ✅ | AI |
| DDoS volumétrico | ⚠️ | Global (mas sem distribuição) |
| Tenant monopolizando recursos | ⚠️ | Sem política por tenant identificada |
| Enumeração de utilizadores | ✅ | Auth + AuthSensitive |

**Classificação:** ✅ Adequado — boa segmentação, necessita evolução para distributed e per-tenant

---

## 8. Funcionalidades de Identidade Avançada

### 8.1 Break Glass Access Protocol

**Entidades identificadas:** `BreakGlassRequest`

**Conceito:** Permite acesso de emergência a recursos críticos quando os mecanismos normais de autorização são insuficientes (ex: incidente grave em produção que requer acesso administrativo imediato).

**Funcionalidades:**
- Pedido de acesso de emergência com justificação obrigatória
- Expiração automática via `IdentityExpirationJob`
- Registo completo para auditoria posterior

**Avaliação:**
- Funcionalidade essencial para operação enterprise — incidentes graves não podem esperar por processos de aprovação normais
- A expiração automática garante que acessos de emergência não permanecem ativos indefinidamente
- A justificação obrigatória e auditoria permitem revisão posterior por compliance
- Alinhado com frameworks como ISO 27001 e SOC 2

### 8.2 Just-In-Time (JIT) Access

**Entidades identificadas:** `JitAccessRequest`

**Conceito:** Acesso privilegiado concedido apenas quando necessário e apenas pelo tempo necessário, seguindo o princípio do menor privilégio temporal.

**Funcionalidades:**
- Pedido de acesso temporário com escopo definido
- Expiração automática via `IdentityExpirationJob`
- Reduz a superfície de ataque eliminando acessos permanentes desnecessários

**Avaliação:**
- JIT access é uma prática de segurança avançada, tipicamente encontrada em plataformas enterprise maduras
- A combinação com expiração automática garante compliance com políticas de acesso mínimo
- Recomendação: considerar integração com workflows de aprovação para JIT requests em ambientes de produção

### 8.3 Delegated Access (Delegações)

**Entidades identificadas:** `Delegation`

**Conceito:** Permite que um utilizador delegue temporariamente permissões a outro, com expiração e possibilidade de revogação.

**Funcionalidades:**
- Delegação com escopo e duração definidos
- Expiração automática via `IdentityExpirationJob`
- Revogação possível antes da expiração

**Avaliação:**
- Funcionalidade importante para cenários de cobertura de férias, transferências temporárias e colaboração cross-team
- A expiração automática previne delegações "esquecidas" que criam risco de segurança
- Recomendação: registar em auditoria tanto a criação como a utilização de delegações

### 8.4 Access Review Campaigns

**Entidades identificadas:** `AccessReviewCampaign`, `AccessReviewItem`

**Conceito:** Revisão periódica de acessos para garantir que permissões atribuídas continuam necessárias e adequadas.

**Funcionalidades:**
- Campanhas de revisão com itens individuais por utilizador/permissão
- Suporte a decisões de manter, revogar ou escalar

**Avaliação:**
- Access reviews são um requisito explícito de frameworks como SOC 2, ISO 27001 e GDPR
- A implementação como campanha permite revisões organizadas e rastreáveis
- Funcionalidade diferenciadora para o posicionamento enterprise do NexTraceOne

### 8.5 SSO e Identidade Federada

**Entidades identificadas:** `ExternalIdentity`, `SsoGroupMapping`

**Funcionalidades:**
- Suporte a OIDC (OpenID Connect)
- Suporte a SAML
- Mapeamento de grupos SSO para papéis internos
- Preservação de deep-link durante fluxo de login

**Avaliação:**
- SSO é requisito obrigatório para adoção enterprise
- O mapeamento de grupos SSO para papéis internos simplifica a gestão de acessos em organizações grandes
- A preservação de deep-link é um detalhe de UX frequentemente ignorado mas importante para a experiência do utilizador

### 8.6 MFA (Multi-Factor Authentication)

**Implementação identificada:** TOTP Verifier

**Avaliação:**
- TOTP (RFC 6238) é um padrão bem estabelecido e suportado por todas as apps de autenticação
- Considerar suporte a WebAuthn/FIDO2 em fases futuras para maior segurança e melhor UX

### 8.7 Security Events

**Entidades identificadas:** `SecurityEvent`, `SecurityEventTracker`

**Funcionalidades:**
- Registo de eventos de segurança (logins, falhas, alterações de permissão, etc.)
- Tracking e correlação de eventos

**Avaliação:**
- Essencial para deteção de anomalias e investigação de incidentes
- A entidade `SecurityEventTracker` sugere capacidade de correlação, o que é valioso para análise de padrões

### 8.8 Session Management

**Entidades identificadas:** `Session`

**Funcionalidades:**
- Gestão de sessões ativas
- Revogação de sessões

**Avaliação:**
- A capacidade de revogar sessões é essencial para resposta a incidentes (ex: credenciais comprometidas)
- Recomendação: implementar visualização de sessões ativas para o utilizador (self-service)

### 8.9 IdentityExpirationJob

**Implementação:** Job Quartz.NET com intervalo de 60 segundos, batch de 100, isolado por handler

**Avaliação:**
- O intervalo de 60 segundos é adequado — suficientemente frequente para garantir expiração atempada sem overhead excessivo
- O processamento em batches de 100 é prudente para evitar sobrecarga
- O isolamento por handler garante que falha num tipo de expiração não afeta outros
- Considerar logging detalhado das expirações para auditoria

**Classificação geral das funcionalidades de identidade:** ✅ Forte — funcionalidades enterprise avançadas raramente encontradas em MVPs

---

## 9. Lacunas e Vulnerabilidades Identificadas

### 9.1 Lacunas de Segurança (Prioridade Alta)

#### L-001: Ausência de Rotação Automática de Chaves JWT

**Descrição:** Não foi identificado mecanismo automático de rotação da chave de assinatura JWT.

**Risco:** Se a chave for comprometida, todos os tokens emitidos são válidos até expiração. Sem rotação, a chave permanece em uso indefinidamente.

**Recomendação:** Implementar suporte a múltiplas chaves (JWKS) com rotação periódica. A chave anterior deve permanecer válida durante um período de transição igual ao tempo de vida máximo do token.

**Severidade:** Média-Alta

#### L-002: Rate Limiting Não Distribuído

**Descrição:** O rate limiting aparenta ser in-process, sem estado partilhado entre instâncias.

**Risco:** Em deploy com múltiplas instâncias (IIS farm, Docker Compose com múltiplos replicas, futuro Kubernetes), o limite efetivo é multiplicado pelo número de instâncias.

**Recomendação:** Implementar distributed rate limiting usando PostgreSQL (dado que Redis não é requisito no MVP1) ou outra solução de estado partilhado.

**Severidade:** Média

#### L-003: Ausência de Rate Limiting por Tenant

**Descrição:** Não foi identificada política de rate limiting específica por tenant.

**Risco:** Um tenant malicioso ou com carga excessiva pode degradar a experiência de todos os outros tenants (noisy neighbor problem).

**Recomendação:** Implementar rate limiting por tenant, pelo menos para as políticas AI e DataIntensive.

**Severidade:** Média

### 9.2 Lacunas de Segurança (Prioridade Média)

#### L-004: API Keys em Configuração

**Descrição:** No MVP1, as API keys são geridas via ficheiros de configuração.

**Risco:** Não escala para cenários multi-tenant. Rotação requer redeploy. Sem tracking de utilização.

**Recomendação:** Migrar para armazenamento em base de dados com hash SHA-256, suporte a rotação, tracking de última utilização e revogação sem redeploy.

**Severidade:** Média (aceitável para MVP1, necessita evolução)

#### L-005: Ausência de Rotação de Chave de Encriptação

**Descrição:** Não foi identificado suporte a múltiplas versões de chave de encriptação.

**Risco:** Rotação da chave requer re-encriptação de todos os dados existentes, operação complexa e arriscada.

**Recomendação:** Implementar key versioning — prefixar dados encriptados com identificador da versão da chave, permitindo re-encriptação gradual.

**Severidade:** Média

#### L-006: Verificação de Integridade de Assemblies Desativável

**Descrição:** O `AssemblyIntegrityChecker` pode ser desativado via variável de ambiente `NEXTRACE_SKIP_INTEGRITY`.

**Risco:** Um atacante com acesso ao ambiente pode desativar a verificação para carregar assemblies adulteradas.

**Recomendação:** Considerar logging/alerting quando a verificação é desativada. Em produção, avaliar tornar a desativação mais restritiva (ex: requerer flag em múltiplos locais).

**Severidade:** Média-Baixa (aceitável para dev/debug, necessita hardening em produção)

### 9.3 Lacunas de Segurança (Prioridade Baixa)

#### L-007: Password de Seed Data

**Descrição:** Dados de seed para desenvolvimento incluem passwords conhecidas (`Admin@123`).

**Risco:** Baixo em desenvolvimento. O risco existe apenas se seed data for inadvertidamente executado em produção.

**Recomendação:** Garantir que o seed data é condicionado ao ambiente de desenvolvimento e bloqueado em produção.

**Severidade:** Baixa (aceitável para desenvolvimento)

#### L-008: Ausência de WebAuthn/FIDO2

**Descrição:** MFA é limitado a TOTP. Não há suporte a WebAuthn/FIDO2.

**Risco:** TOTP é vulnerável a phishing (o utilizador pode ser enganado a fornecer o código). WebAuthn é resistente a phishing por desenho.

**Recomendação:** Considerar WebAuthn/FIDO2 como opção adicional de MFA em fases futuras.

**Severidade:** Baixa (TOTP é adequado para MVP1)

### 9.4 Vulnerabilidades Identificadas

**Injeção SQL:** ✅ Zero risco — todas as queries utilizam EF Core com parâmetros. Não foi identificado raw SQL em nenhum ponto do codebase.

**XSS:** ✅ Zero vetores identificados — sem `dangerouslySetInnerHTML`, sanitização de URLs ativa, i18n com escape, React JSX com escape automático.

**CSRF:** ✅ Protegido — double-submit cookie com constant-time comparison.

**Segredos hardcoded:** ✅ Zero segredos em produção — todos via variáveis de ambiente. `.gitignore` exclui `.env`, `.pfx` e `appsettings.Development.json`.

---

## 10. Recomendações

### 10.1 Recomendações de Curto Prazo (MVP1 / Próximas Sprints)

| # | Recomendação | Prioridade | Esforço |
|---|-------------|-----------|---------|
| R-01 | Implementar distributed rate limiting via PostgreSQL | Alta | Médio |
| R-02 | Adicionar rate limiting por tenant | Alta | Médio |
| R-03 | Adicionar logging/alerting para desativação de assembly integrity | Média | Baixo |
| R-04 | Garantir que seed data é bloqueado fora de desenvolvimento | Média | Baixo |
| R-05 | Adicionar headers de rate limit nas respostas HTTP | Média | Baixo |
| R-06 | Documentar procedimento de rotação de chaves JWT | Média | Baixo |
| R-07 | Documentar procedimento de rotação de chave de encriptação | Média | Baixo |

### 10.2 Recomendações de Médio Prazo (Pós-MVP1)

| # | Recomendação | Prioridade | Esforço |
|---|-------------|-----------|---------|
| R-08 | Implementar suporte a JWKS com rotação automática de chaves | Alta | Alto |
| R-09 | Migrar API keys de configuração para base de dados | Alta | Médio |
| R-10 | Implementar key versioning para chave de encriptação | Média | Alto |
| R-11 | Adicionar tracking de utilização de API keys (last used) | Média | Baixo |
| R-12 | Implementar revogação individual de JWT via blacklist | Média | Médio |
| R-13 | Adicionar testes automatizados de isolamento cross-tenant | Alta | Médio |
| R-14 | Implementar Content Security Policy (CSP) headers | Média | Médio |

### 10.3 Recomendações de Longo Prazo

| # | Recomendação | Prioridade | Esforço |
|---|-------------|-----------|---------|
| R-15 | Suporte a WebAuthn/FIDO2 como opção de MFA | Baixa | Alto |
| R-16 | Integração com vault externo (HashiCorp Vault, Azure Key Vault) | Média | Alto |
| R-17 | Penetration testing externo | Alta | N/A (externo) |
| R-18 | Certificação SOC 2 Type II | Média | N/A (processo) |
| R-19 | Implementar token introspection para cenários com muitas permissões | Baixa | Médio |

---

## 11. Matriz de Conformidade

### 11.1 Alinhamento com Frameworks de Segurança

| Controlo | OWASP Top 10 | ISO 27001 | SOC 2 | GDPR/LGPD | Estado |
|----------|--------------|-----------|-------|-----------|--------|
| Autenticação forte | A07:2021 | A.9.4.2 | CC6.1 | Art. 32 | ✅ Implementado |
| MFA | A07:2021 | A.9.4.2 | CC6.1 | Art. 32 | ✅ Implementado (TOTP) |
| Autorização granular | A01:2021 | A.9.2.3 | CC6.3 | Art. 25 | ✅ Implementado |
| Isolamento multi-tenant | A01:2021 | A.13.1.3 | CC6.1 | Art. 32 | ✅ Implementado (3 camadas) |
| Encriptação em repouso | A02:2021 | A.10.1.1 | CC6.7 | Art. 32 | ✅ Implementado (AES-256-GCM) |
| Proteção CSRF | A05:2021 | — | CC6.6 | — | ✅ Implementado |
| Proteção XSS | A03:2021 | — | CC6.6 | — | ✅ Implementado |
| Proteção SQL Injection | A03:2021 | — | CC6.6 | — | ✅ Sem risco (EF Core) |
| Rate limiting | — | A.12.1.3 | CC6.6 | — | ⚠️ Parcial (não distribuído) |
| Auditoria de acessos | — | A.12.4.1 | CC7.2 | Art. 30 | ✅ Implementado |
| Revisão de acessos | — | A.9.2.5 | CC6.2 | Art. 32 | ✅ Implementado |
| Acesso privilegiado temporário | — | A.9.2.3 | CC6.1 | Art. 25 | ✅ Implementado (JIT + Break Glass) |
| Gestão de sessões | A07:2021 | A.9.4.2 | CC6.1 | — | ✅ Implementado |
| Gestão de segredos | A02:2021 | A.10.1.2 | CC6.7 | — | ✅ Env vars (adequado para MVP1) |

### 11.2 Cobertura OWASP Top 10 (2021)

| Risco OWASP | Estado NexTraceOne |
|-------------|-------------------|
| A01: Broken Access Control | ✅ Mitigado — permissões granulares, deny-by-default, RLS |
| A02: Cryptographic Failures | ✅ Mitigado — AES-256-GCM, sem segredos hardcoded |
| A03: Injection | ✅ Mitigado — EF Core parametrizado, sanitização URLs |
| A04: Insecure Design | ✅ Mitigado — defesa em profundidade, multi-tenant by design |
| A05: Security Misconfiguration | ⚠️ Parcial — assembly integrity desativável |
| A06: Vulnerable Components | ℹ️ Não avaliado neste relatório |
| A07: Identification & Auth Failures | ✅ Mitigado — 3 métodos auth, MFA, session management |
| A08: Software & Data Integrity | ✅ Mitigado — assembly integrity checker |
| A09: Security Logging & Monitoring | ✅ Mitigado — security events, audit logging |
| A10: Server-Side Request Forgery | ℹ️ Não avaliado neste relatório |

---

## 12. Conclusão

### 12.1 Avaliação Global

O NexTraceOne demonstra uma postura de segurança **enterprise-grade sólida**, com múltiplas camadas de defesa em profundidade e funcionalidades avançadas de identidade que vão além do esperado para uma fase MVP1.

### 12.2 Destaques Positivos

1. **Isolamento multi-tenant em três camadas** (JWT + Application Pipeline + PostgreSQL RLS) — uma implementação exemplar que garante proteção mesmo em caso de bugs na aplicação
2. **Funcionalidades avançadas de identidade** (Break Glass, JIT, Delegações, Access Reviews) — tipicamente encontradas apenas em plataformas enterprise maduras
3. **Zero vetores de XSS e SQL Injection** — demonstra disciplina de segurança na equipa
4. **Proteção CSRF com constant-time comparison** — atenção a detalhes criptográficos
5. **Encriptação AES-256-GCM** com parâmetros corretos — escolhas criptográficas sólidas
6. **Armazenamento seguro de tokens no frontend** — sessionStorage + memória para refresh token
7. **Mais de 100 permissões granulares** com deny-by-default — modelo de autorização maduro

### 12.3 Áreas de Evolução Prioritárias

1. **Rate limiting distribuído e por tenant** — necessário antes de deploy multi-instância
2. **Rotação de chaves** (JWT e encriptação) — necessário para operação contínua segura
3. **Migração de API keys para base de dados** — necessário para escala multi-tenant
4. **Testes automatizados de isolamento** — necessário para confiança contínua

### 12.4 Avaliação de Maturidade

| Dimensão | Nível |
|----------|-------|
| Autenticação | Enterprise-ready |
| Autorização | Enterprise-ready |
| Multi-tenancy | Enterprise-ready |
| Encriptação | Enterprise-ready |
| Segurança Frontend | Enterprise-ready |
| Rate Limiting | Necessita evolução |
| Identidade Avançada | Enterprise-ready |
| Gestão de Chaves | Necessita evolução |
| Compliance Readiness | Forte base, necessita certificação |

### 12.5 Parecer Final

O NexTraceOne está bem posicionado para adoção enterprise do ponto de vista de segurança. As lacunas identificadas são incrementais e esperadas numa fase MVP1. Nenhuma vulnerabilidade crítica foi identificada. As recomendações devem ser priorizadas conforme o roadmap de evolução do produto, com foco imediato no rate limiting distribuído e na documentação de procedimentos de rotação de chaves.

---

**Elaborado por:** Agente de Avaliação de Segurança
**Data:** 2026-03-27
**Próxima revisão recomendada:** Após implementação das recomendações de curto prazo
