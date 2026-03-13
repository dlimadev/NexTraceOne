# Notas de Privacidade — LGPD / GDPR-RGPD — NexTraceOne

> Documento de referência sobre manipulação de dados pessoais na aplicação.
> Atualizado em Março 2026.

---

## 1. Onde a Aplicação Manipula Dados Pessoais

### 1.1. Módulo Identity

| Dado | Tipo | Onde | Finalidade |
|------|------|------|------------|
| Email | Identificador pessoal | DB `identity.users`, JWT claims, sessionStorage (`nxt_uid` como ID) | Autenticação, identificação no sistema |
| Nome completo | Dado pessoal | DB `identity.users`, resposta `/me`, sidebar/header UI | Identificação visual do usuário |
| Endereço IP | Dado pessoal (LGPD/GDPR) | Sessions (DB), logs de auditoria | Segurança, rastreabilidade de sessão |
| User Agent | Dado técnico com PII potencial | Sessions (DB) | Identificação de dispositivo, segurança |
| Password hash | Credencial | DB `identity.users` | Autenticação |
| Role/Permissões | Dado organizacional | DB, JWT, perfil no frontend | Autorização, controle de acesso |

### 1.2. Módulo Audit

| Dado | Tipo | Onde | Finalidade |
|------|------|------|------------|
| Actor (userId/email) | Identificador pessoal | Audit events (DB) | Rastreabilidade, compliance |
| Timestamps | Metadado | Audit events (DB) | Cronologia de ações |
| Ações realizadas | Dado comportamental | Audit events (DB) | Compliance, investigação |

### 1.3. Frontend (Browser)

| Dado | Tipo | Onde | Finalidade |
|------|------|------|------------|
| Access token | Credencial temporária | sessionStorage (aba) | Autenticação de requests |
| Tenant ID | Identificador organizacional | sessionStorage (aba) | Contexto multi-tenant |
| User ID | Identificador | sessionStorage (aba) | Fallback de perfil |
| Email (primeira letra) | Dado pessoal minimizado | UI (avatar) | Identificação visual |

---

## 2. Riscos Identificados

| # | Risco | Severidade | Status |
|---|-------|------------|--------|
| P-1 | Email exibido na sidebar e header como texto completo | Médio | ℹ️ Necessário para UX — masking parcial pode ser considerado |
| P-2 | Endereço IP persistido em sessions sem política de retenção | Médio | ⚠️ Requer política de retenção para sessions expiradas |
| P-3 | Audit log com actor identificado por tempo indefinido | Médio | ⚠️ Definir política de retenção para dados de auditoria com PII |
| P-4 | Logs do Serilog podem conter dados pessoais inadvertidamente | Baixo | ℹ️ Mitigado por política de logs em inglês sem PII |
| P-5 | Export de auditoria pode conter dados pessoais | Médio | ⚠️ Verificar minimização em funcionalidade de export |

---

## 3. Medidas de Minimização Adotadas

1. **Frontend não persiste dados pessoais além do estritamente necessário.**
   Apenas token (temporário), tenant ID e user ID ficam em sessionStorage.
   Nenhum dado pessoal fica em localStorage.

2. **Avatar exibe apenas a primeira letra do email** — minimização na UI.

3. **Refresh token mantido exclusivamente em memória** — não persistido no browser.

4. **console.log removido em produção** — evita vazamento de dados pessoais em logs do browser.

5. **i18n para mensagens de erro** — mensagens técnicas (que podem conter dados) nunca são
   exibidas diretamente ao usuário.

6. **Security headers impedem cache de respostas** — `Cache-Control: no-store` evita
   persistência de dados sensíveis em cache do browser.

---

## 4. Masking Aplicado ou Recomendado

| Dado | Masking Atual | Recomendação |
|------|--------------|--------------|
| Email na sidebar | Exibido completo | Considerar masking parcial: `u***@empresa.com` |
| Email no header | Exibido completo (quando fullName ausente) | Preferir fullName, masking como fallback |
| IP em sessions | Sem masking | Considerar masking parcial na UI: `192.168.***` |
| User Agent | Sem masking | Considerar truncamento na UI |
| Password | Nunca exibido | ✅ Correto |

---

## 5. Pontos que Dependem de Backend/Infra/Processo

### 5.1. Retenção de Dados (Responsabilidade: Backend + Infra)
- Definir TTL para sessions expiradas no banco de dados
- Definir política de retenção para audit logs com dados pessoais
- Implementar job de limpeza periódica (Quartz.NET)
- Configurar retenção de logs do Serilog (atualmente 30 dias)

### 5.2. Direitos do Titular (Responsabilidade: Backend + Processo)
- **Direito de acesso** — Endpoint para exportar dados pessoais do usuário
- **Direito de retificação** — Funcionalidade de edição de perfil
- **Direito de eliminação** — Pseudonimização nos registros de auditoria
  (não pode apagar audit log, mas pode pseudonimizar o actor)
- **Direito de portabilidade** — Export de dados em formato estruturado

### 5.3. Consentimento e Avisos (Responsabilidade: Processo + Frontend)
- O NexTraceOne é uma ferramenta enterprise interna — o consentimento é tipicamente
  coberto pelo contrato de trabalho ou política interna do cliente
- Se necessário, implementar banner de privacidade na primeira utilização

### 5.4. Pseudonimização (Responsabilidade: Backend)
- Para compliance com direito de eliminação, implementar pseudonimização de PII
  em registros de auditoria sem comprometer a integridade da cadeia de hash

---

## 6. Recomendações para LGPD e GDPR/RGPD

### Curto Prazo (MVP1)
1. Documentar dados pessoais manipulados (✅ este documento)
2. Garantir minimização no frontend (✅ implementado)
3. Garantir que logs não persistem PII desnecessário
4. Definir política de retenção para sessions

### Médio Prazo (Pós-MVP1)
1. Implementar endpoint de export de dados pessoais do titular
2. Implementar pseudonimização para direito de eliminação
3. Implementar masking parcial de email/IP na UI
4. Adicionar registros de processamento de dados (ROPA)

### Longo Prazo
1. Data Protection Impact Assessment (DPIA) formal
2. Privacy by Design review em novas features
3. Mecanismo de consentimento se o produto expandir escopo
4. Integração com ferramentas de DPO do cliente
