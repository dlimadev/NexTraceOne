# P-05-02 — Plano de migração de API keys de configuração para armazenamento encriptado em PostgreSQL

## 1. Título

Criar plano de migração para mover API keys de appsettings.json para armazenamento encriptado em PostgreSQL.

## 2. Modo de operação

**Analysis**

## 3. Objetivo

As API keys usadas para autenticação de serviços e integrações estão atualmente armazenadas em configuração (appsettings.json ou variáveis de ambiente). Este prompt produz um plano detalhado para migrar esse armazenamento para PostgreSQL com encriptação, usando a infraestrutura de encriptação já existente no building block de Security.

## 4. Problema atual

- O handler de autenticação por API key em `src/building-blocks/NexTraceOne.BuildingBlocks.Security/Authentication/ApiKeyAuthenticationHandler.cs` valida keys contra configuração.
- As options em `src/building-blocks/NexTraceOne.BuildingBlocks.Security/Authentication/ApiKeyAuthenticationOptions.cs` lêem keys de config.
- Já existe `AesGcmEncryptor.cs` em `src/building-blocks/NexTraceOne.BuildingBlocks.Security/Encryption/` e `EncryptionKeyMaterial.cs` — infraestrutura de encriptação pronta.
- Keys em ficheiros de configuração são difíceis de rotacionar, auditar e governar.
- Em ambiente self-hosted enterprise, administradores precisam gerir keys via UI, não via ficheiros.
- Sem armazenamento em BD, não é possível: rotação automática, expiração, revogação, auditoria de uso.

## 5. Escopo permitido

- Este prompt é **apenas análise** — não produz alterações de código.
- Analisar: `src/building-blocks/NexTraceOne.BuildingBlocks.Security/Authentication/`
- Analisar: `src/building-blocks/NexTraceOne.BuildingBlocks.Security/Encryption/`
- Analisar: `src/platform/NexTraceOne.ApiHost/appsettings.json` (secção de API keys, se existir)
- Analisar: módulos que usam API keys para integrações externas.
- Analisar padrão de entidades e repositórios existentes para modelar ApiKeyEntity.

## 6. Escopo proibido

- **Nenhuma alteração de código neste prompt.**
- Não implementar nada — apenas documentar o plano.
- Não alterar o fluxo de autenticação existente.
- Não remover suporte a API keys via configuração (deve coexistir como fallback).

## 7. Ficheiros principais candidatos a alteração

**0 ficheiros alterados — prompt de análise.**

Ficheiros a analisar:
1. `src/building-blocks/NexTraceOne.BuildingBlocks.Security/Authentication/ApiKeyAuthenticationHandler.cs`
2. `src/building-blocks/NexTraceOne.BuildingBlocks.Security/Authentication/ApiKeyAuthenticationOptions.cs`
3. `src/building-blocks/NexTraceOne.BuildingBlocks.Security/Encryption/AesGcmEncryptor.cs`
4. `src/building-blocks/NexTraceOne.BuildingBlocks.Security/Encryption/EncryptionKeyMaterial.cs`
5. `src/building-blocks/NexTraceOne.BuildingBlocks.Security/DependencyInjection.cs`
6. `src/platform/NexTraceOne.ApiHost/appsettings.json`

## 8. Responsabilidades permitidas

- Analisar como ApiKeyAuthenticationHandler resolve keys atualmente.
- Propor modelo de entidade ApiKey (hash, salt, nome, owner, expiração, scopes, ambiente, estado).
- Propor repositório IApiKeyRepository com métodos de CRUD e validação.
- Propor estratégia de migração: coexistência config + DB → apenas DB.
- Propor uso de AesGcmEncryptor para encriptar metadata sensível (não a key em si — usar hash).
- Propor fluxo de rotação, expiração e revogação.
- Propor endpoints de administração (/api/v1/admin/api-keys).
- Estimar impacto e esforço.

## 9. Responsabilidades proibidas

- Não implementar código.
- Não propor soluções que dependam de key vault externo como requisito obrigatório (deve funcionar self-hosted sem cloud).
- Não propor remoção imediata do suporte a config-based keys.

## 10. Critérios de aceite

- [ ] Documento de análise produzido com: modelo de entidade proposto, fluxo de migração, endpoints necessários, riscos.
- [ ] Estratégia de hashing de API keys definida (ex: SHA-256 + salt, nunca armazenar key em texto).
- [ ] Coexistência config + DB documentada como fase de transição.
- [ ] Estimativa de ficheiros a alterar na implementação futura.
- [ ] Alinhamento com princípios de segurança do NexTraceOne (least privilege, auditoria, self-hosted).

## 11. Validações obrigatórias

- Análise completa dos ficheiros de Authentication e Encryption.
- Verificação de que AesGcmEncryptor é adequado para o caso de uso.
- Verificação de que o modelo proposto suporta multi-tenant.

## 12. Riscos e cuidados

- Hashing de API keys deve ser irreversível (SHA-256, não AES) — a key original nunca deve ser recuperável.
- A migração deve ser backward-compatible — keys existentes em config devem continuar a funcionar.
- Performance: lookup de key por hash deve ser indexado na tabela.
- Key rotation precisa de período de sobreposição onde key antiga e nova coexistem.
- Administradores self-hosted podem não ter acesso a secret managers — a solução deve ser autossuficiente.

## 13. Dependências

- Nenhuma dependência de outros prompts (é análise independente).
- Infraestrutura de encriptação em BuildingBlocks.Security já existe.

## 14. Próximos prompts sugeridos

- **P-XX-XX** — Implementação: criar entidade ApiKey, repositório, migração e endpoints de admin.
- **P-XX-XX** — Implementação: alterar ApiKeyAuthenticationHandler para validar contra DB.
- **P-XX-XX** — UI de gestão de API keys no frontend (módulo Foundation/Platform Admin).
- **P-XX-XX** — Auditoria de uso de API keys (quem usou, quando, endpoint acedido).
