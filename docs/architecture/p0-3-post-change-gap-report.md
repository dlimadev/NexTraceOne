# P0.3 — Post-change gap report

## O que foi resolvido

- Fallback AES hardcoded removido do `AesGcmEncryptor`.
- `NEXTRACE_ENCRYPTION_KEY` tornou-se obrigatória na prática:
  - validação em runtime do encryptor;
  - validação de startup no ApiHost;
  - validação no registro de segurança (`AddBuildingBlocksSecurity`).
- Mensagens de erro explícitas para chave ausente ou inválida.
- `.env.example` alinhado com o comportamento real (variável adicionada e documentada).
- Testes ajustados para o novo comportamento sem fallback.

## O que ainda ficou pendente

Fora do escopo desta fase P0.3 (mantido intencionalmente):

- Hardening de JWT fallback/secret (`P0.4+` conforme roadmap de segurança global).
- CORS hardening.
- `NEXTRACE_SKIP_INTEGRITY` em pipelines.
- Hardening adicional de cookies em ambientes não-Development.

## Risco residual

- Operadores que não configurarem `NEXTRACE_ENCRYPTION_KEY` corretamente terão falha de startup (fail-fast esperado).
- Serviços que dependem de dados previamente encriptados com chaves antigas exigem continuidade operacional da chave correta (gestão de rotação continua como tema operacional).

## Próximo prompt recomendado para fase 0

Conforme sequência indicada no plano:

- **P0.4** — remover mock do `AssistantPanel` e ligar o chat ao endpoint real.

