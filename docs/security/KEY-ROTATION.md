# NexTraceOne — Guia de Rotação de Chaves

> **Classificação:** Operacional — Segurança
> **Última revisão:** Abril 2026
> **Âmbito:** JWT Signing Key + AES-256-GCM Encryption Key

---

## Visão Geral

O NexTraceOne utiliza dois materiais de chave criptográfica em produção:

| Chave | Variável de Ambiente | Algoritmo | Impacto da Rotação |
|-------|---------------------|-----------|-------------------|
| JWT Signing Key | `Jwt__Secret` | HMAC-SHA256 (HS256) | Invalida todos os tokens activos — logout forçado |
| Field Encryption Key | `NEXTRACE_ENCRYPTION_KEY` | AES-256-GCM | Dados cifrados existentes ficam ilegíveis até re-encriptação |

---

## Quando Rodar as Chaves

### Rotação por Calendário (Recomendado)

| Chave | Frequência Mínima | Recomendado |
|-------|------------------|-------------|
| JWT Secret | Anual | Semestral |
| Encryption Key | Bienal | Anual |

### Rotação por Evento (Obrigatório)

Rodar **imediatamente** quando qualquer das seguintes condições se verificar:

- Suspeita ou confirmação de comprometimento da chave
- Saída de um operador com acesso às chaves
- Violação de acesso ao sistema de gestão de secrets (vault, CI/CD)
- Exposição acidental em logs, commits ou sistemas de monitorização
- Após incidente de segurança classificado com severidade ≥ Alta

---

## 1. Rotação do JWT Secret (`Jwt__Secret`)

### 1.1 Impacto

A rotação do JWT Secret **invalida imediatamente todos os access tokens e refresh tokens activos**.
Todos os utilizadores serão forçados a fazer login novamente após o restart da aplicação.

> Planear a rotação durante uma janela de manutenção fora de horas de pico, ou comunicar
> aos utilizadores que será necessário re-autenticação.

### 1.2 Gerar Nova Chave

```bash
# Gerar chave de alta entropia (48 bytes Base64 → 64 caracteres)
openssl rand -base64 48
```

Requisitos mínimos validados pelo `StartupValidation.cs`:
- Comprimento mínimo: **32 caracteres** (material de chave para HMAC-SHA256)
- Recomendado: output de `openssl rand -base64 48` (64 caracteres, alta entropia)

### 1.3 Aplicar Nova Chave

#### Ambiente de Produção / Staging (variável de ambiente)

```bash
# Definir a nova chave no sistema de gestão de secrets / orquestrador
export Jwt__Secret="<nova-chave-gerada>"
```

#### Desenvolvimento Local (dotnet user-secrets)

```bash
cd src/platform/NexTraceOne.ApiHost
dotnet user-secrets set "Jwt:Secret" "<nova-chave-gerada>"
```

#### Docker Compose

```yaml
# docker-compose.override.yml ou via .env
environment:
  - Jwt__Secret=<nova-chave-gerada>
```

### 1.4 Procedimento Completo

1. Gerar nova chave com `openssl rand -base64 48`
2. Guardar a nova chave no sistema de gestão de secrets (vault, GitHub Secrets, Azure Key Vault, etc.)
3. Actualizar a variável de ambiente `Jwt__Secret` no ambiente alvo
4. **Agendar restart** da aplicação — o novo secret só entra em efeito após restart
5. Verificar que o startup completa sem erros (`ValidateJwtSecret` em `StartupValidation.cs`)
6. Confirmar que novos logins emitem tokens com a nova chave
7. Confirmar que tokens antigos são rejeitados (retornam `401 Unauthorized`)
8. Revogar a chave antiga dos sistemas de gestão de secrets
9. Actualizar documentação interna com data de rotação

### 1.5 Rollback

Se o restart falhar ou os tokens não forem validados correctamente:

1. Restaurar o valor anterior de `Jwt__Secret` no sistema de gestão de secrets
2. Fazer restart da aplicação com a chave anterior
3. Investigar e corrigir o problema antes de tentar nova rotação

---

## 2. Rotação da Chave de Encriptação de Campo (`NEXTRACE_ENCRYPTION_KEY`)

### 2.1 Impacto

> ⚠️ **ATENÇÃO:** Esta rotação é mais complexa do que a JWT.
> Dados encriptados com a chave antiga **ficam ilegíveis** com a nova chave.
> É necessário um processo de **re-encriptação** dos dados existentes.

Campos actualmente encriptados com AES-256-GCM:
- `AuditEvent.Payload` (futuramente — ver roadmap Phase 1)
- Campos marcados com `[Encrypted]` via `EncryptedStringConverter`
- Dados em `ConfigurationSecurityService`

### 2.2 Gerar Nova Chave

```bash
# Opção A: Base64 de 32 bytes (recomendado — mais seguro e explícito)
openssl rand -base64 32

# Opção B: String UTF-8 de exactamente 32 caracteres (legacy)
# Não recomendado para novas instalações
```

O `EncryptionKeyMaterial` aceita:
- String Base64 que decodifica para exactamente 32 bytes
- String UTF-8 com exactamente 32 bytes

### 2.3 Procedimento de Re-encriptação

> Executar **antes** de substituir a chave antiga na produção.

```bash
# 1. Exportar chave atual (antes de rodar)
OLD_KEY="$(vault read -field=value secret/nextraceone/encryption-key)"
NEW_KEY="$(openssl rand -base64 32)"

# 2. Executar migration tool (quando disponível)
# dotnet run --project tools/NexTraceOne.KeyRotation -- \
#   --old-key "$OLD_KEY" \
#   --new-key "$NEW_KEY" \
#   --dry-run   # validar primeiro sem alterar dados

# 3. Verificar output do dry-run
# 4. Executar sem --dry-run após validação
```

> **Nota:** A ferramenta de migration de chaves está prevista na Fase 1 (Hardening).
> Enquanto não existir, a re-encriptação deve ser feita através de script de base de dados
> com acesso directo às chaves, mantendo a aplicação em modo de manutenção.

### 2.4 Procedimento Completo

1. Agendar janela de manutenção (downtime pode ser necessário)
2. Criar backup completo da base de dados antes de qualquer alteração
3. Gerar nova chave com `openssl rand -base64 32`
4. Executar processo de re-encriptação dos dados existentes (ver secção 2.3)
5. Verificar integridade dos dados re-encriptados (amostragem ou full scan)
6. Actualizar `NEXTRACE_ENCRYPTION_KEY` no sistema de gestão de secrets
7. Fazer restart da aplicação com a nova chave
8. Verificar que a aplicação arranca sem erros
9. Executar smoke tests nos fluxos que lêem dados encriptados
10. Revogar a chave antiga
11. Actualizar documentação interna com data de rotação

### 2.5 Rollback

Se algo correr mal após a rotação:

1. Restaurar backup da base de dados (dados encriptados com chave antiga)
2. Restaurar `NEXTRACE_ENCRYPTION_KEY` com valor anterior
3. Fazer restart da aplicação
4. Investigar causa do problema

---

## 3. Verificação Pós-Rotação

### Checklist JWT

- [ ] Aplicação arrancou sem erros em `ValidateJwtSecret`
- [ ] `POST /api/auth/login` retorna novo token com `exp` correcto
- [ ] Token anterior retorna `401 Unauthorized`
- [ ] Refresh token flow funciona correctamente
- [ ] MFA challenge tokens são emitidos e validados correctamente

### Checklist Encryption Key

- [ ] Aplicação arrancou sem erros em `ValidateEncryptionKey`
- [ ] Campos encriptados são lidos correctamente após rotação
- [ ] Novos dados são encriptados com a nova chave
- [ ] Audit trail regista a rotação de chave

---

## 4. Registo de Rotações

Manter registo de todas as rotações em sistema de ticketing/ITSM:

| Campo | Valor |
|-------|-------|
| Data | YYYY-MM-DD |
| Chave | JWT Secret / Encryption Key |
| Motivo | Calendário / Evento |
| Executado por | Nome/ID do operador |
| Aprovado por | Nome/ID do aprovador |
| Janela de manutenção | HH:MM–HH:MM UTC |
| Verificação pós-rotação | Passou / Falhou |
| Rollback necessário | Sim / Não |

---

## 5. Referências

- `src/platform/NexTraceOne.ApiHost/StartupValidation.cs` — validação de chaves no startup
- `src/building-blocks/NexTraceOne.BuildingBlocks.Security/Encryption/EncryptionKeyMaterial.cs` — resolução da chave de encriptação
- `src/building-blocks/NexTraceOne.BuildingBlocks.Security/Encryption/AesGcmEncryptor.cs` — implementação AES-256-GCM
- `docs/security/PHASE-1-SECRETS-BASELINE.md` — política de secrets e variáveis de ambiente
- `.env.example` — exemplo de configuração de ambiente
