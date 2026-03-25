# P0.3 — AES key hardening report

## Objetivo

Eliminar fallback AES hardcoded e tornar `NEXTRACE_ENCRYPTION_KEY` obrigatório via configuração externa, com validação explícita no startup.

## Ficheiros alterados

- `/home/runner/work/NexTraceOne/NexTraceOne/src/building-blocks/NexTraceOne.BuildingBlocks.Security/Encryption/AesGcmEncryptor.cs`
- `/home/runner/work/NexTraceOne/NexTraceOne/src/building-blocks/NexTraceOne.BuildingBlocks.Security/DependencyInjection.cs`
- `/home/runner/work/NexTraceOne/NexTraceOne/src/platform/NexTraceOne.ApiHost/StartupValidation.cs`
- `/home/runner/work/NexTraceOne/NexTraceOne/.env.example`
- `/home/runner/work/NexTraceOne/NexTraceOne/tests/building-blocks/NexTraceOne.BuildingBlocks.Security.Tests/Encryption/AesGcmEncryptorTests.cs`
- `/home/runner/work/NexTraceOne/NexTraceOne/tests/building-blocks/NexTraceOne.BuildingBlocks.Security.Tests/DependencyInjection/SecurityDependencyInjectionTests.cs`
- `/home/runner/work/NexTraceOne/NexTraceOne/tests/building-blocks/NexTraceOne.BuildingBlocks.Infrastructure.Tests/Encryption/EncryptionAtRestTests.cs`
- `/home/runner/work/NexTraceOne/NexTraceOne/tests/building-blocks/NexTraceOne.BuildingBlocks.Infrastructure.Tests/Configuration/StartupValidationTests.cs`

## Ponto onde o fallback AES foi removido

- Remoção direta do fallback hardcoded em:
  - `AesGcmEncryptor.cs` (bloco que retornava hash de `"NexTraceOne-Development-Only-Key-Not-For-Production"`).
- Resultado:
  - Não existe mais chave AES embutida no código.
  - Quando `NEXTRACE_ENCRYPTION_KEY` está ausente/vazia, `AesGcmEncryptor` lança `InvalidOperationException`.
  - Quando `NEXTRACE_ENCRYPTION_KEY` está inválida, também lança `InvalidOperationException` (sem derivação automática).

## Estratégia adotada para resolução de `NEXTRACE_ENCRYPTION_KEY`

Fonte única obrigatória: variável de ambiente `NEXTRACE_ENCRYPTION_KEY`.

Formato aceito:
- Base64 que decode para 32 bytes; **ou**
- String UTF-8 com 32 bytes.

Formato não aceito:
- Valor ausente/vazio;
- Base64 com tamanho diferente de 32 bytes;
- String UTF-8 com tamanho diferente de 32 bytes.

## Validação de startup implementada

Foram adicionados pontos explícitos de enforcement:

1. `StartupValidation.ValidateEncryptionKey` (ApiHost)
   - Executado no boot via `app.ValidateStartupConfiguration()`.
   - Faz fail-fast com mensagem clara quando `NEXTRACE_ENCRYPTION_KEY` não está configurada corretamente.

2. `DependencyInjection.ValidateEncryptionKey` (BuildingBlocks.Security)
   - Executado durante `AddBuildingBlocksSecurity(...)`.
   - Garante que qualquer host que use o building block de segurança não inicializa sem chave de encriptação válida.

## Alinhamento com `.env.example`

- `.env.example` foi atualizado para incluir explicitamente:
  - `NEXTRACE_ENCRYPTION_KEY=REPLACE-WITH-BASE64-32-BYTE-KEY`
  - descrição de obrigatoriedade, formato esperado e comando de geração (`openssl rand -base64 32`).

## Validação funcional realizada

Antes da alteração:
- execução baseline de testes dos projetos relevantes:
  - `NexTraceOne.BuildingBlocks.Security.Tests`
  - `NexTraceOne.BuildingBlocks.Infrastructure.Tests`

Depois da alteração:
- atualização de testes para o novo comportamento (sem fallback):
  - `AesGcmEncryptorTests` agora valida falha quando chave ausente e quando chave inválida.
  - `SecurityDependencyInjectionTests` valida falha quando `NEXTRACE_ENCRYPTION_KEY` não está definida.
  - `EncryptionAtRestTests` deixa de depender de fallback de Development e passa a definir chave válida explícita.
  - `StartupValidationTests` valida presença da regra de startup para `NEXTRACE_ENCRYPTION_KEY`.

