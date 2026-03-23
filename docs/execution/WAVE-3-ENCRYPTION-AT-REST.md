# Wave 3 — Encryption At-Rest

## Abordagem Escolhida

**Opção B — Field-level encryption com convention automática via EF Core ValueConverter.**

Em vez de criar um `SaveChangesInterceptor` (Opção A), a abordagem escolhida aplica encriptação diretamente via `EncryptedStringConverter` que usa `AesGcmEncryptor.Encrypt()/Decrypt()`. A convenção é aplicada automaticamente pelo `NexTraceDbContextBase.OnModelCreating` a qualquer propriedade `string` marcada com `[EncryptedField]`.

### Justificação
- Reutiliza a infraestrutura existente (`AesGcmEncryptor`, `EncryptedStringConverter`)
- Transparente para o código de domínio e aplicação
- Não interfere com queries (encriptação/decriptação acontece no ValueConverter)
- Centralized e testável
- Funciona com todos os módulos que herdam de `NexTraceDbContextBase`

## Campos Cobertos

| Entidade | Campo | Módulo | Justificação |
|----------|-------|--------|-------------|
| `EnvironmentIntegrationBinding` | `BindingConfigJson` | IdentityAccess | Contém credenciais de integração (endpoints, API keys, secrets) |

### Campos não encriptados (intencionalmente)
- `User.PasswordHash` — já é hash (não necessita encriptação at-rest adicional)
- `User.Email` — necessário para queries de lookup (encriptação impediria busca)
- Dados de incidentes — não contêm segredos, são dados operacionais

## Key Management

- **Variável**: `NEXTRACE_ENCRYPTION_KEY`
- **Formato**: Base64-encoded 32-byte key ou string UTF-8 de 32 caracteres
- **Fallback**: SHA-256 derivation para chaves com tamanho diferente
- **Desenvolvimento**: fallback automático para chave derivada (apenas quando `ASPNETCORE_ENVIRONMENT=Development`)
- **Produção**: `InvalidOperationException` se a chave não estiver configurada

## Limitações Conhecidas

1. **Queries em campos encriptados**: campos com `[EncryptedField]` não podem ser usados em `WHERE` clauses (valores encriptados são não-determinísticos devido ao nonce aleatório)
2. **Migração de dados existentes**: dados já persistidos em plaintext precisam de migração manual para formato encriptado
3. **Key rotation**: não há suporte automático para rotação de chaves nesta fase — requer migração de dados
4. **Performance**: overhead mínimo de encriptação/decriptação por operação (~microsegundos)
