# Licensing & Self-Hosted Readiness — Gaps, Erros e Pendências

## 1. Estado resumido do módulo
**Zero implementação.** Nenhum ficheiro, nenhum projecto, nenhuma entidade, nenhuma migração. Licensing é requisito estratégico do produto (§17 das Copilot Instructions) mas não tem qualquer código.

## 2. Gaps críticos

### 2.1 Módulo de Licensing Inexistente
- **Severidade:** HIGH
- **Classificação:** NOT_DEPLOYABLE
- **Descrição:** O produto define licensing como requisito estratégico central: license online/offline, entitlements por capacidade, trial/freemium, enforcement no backend, ativação, validação recorrente, heartbeat, revogação, machine fingerprinting, assembly integrity verification, anti-tampering. **Nada disto existe.**
- **Impacto:** O produto não é deployável comercialmente sem licensing. Self-hosted/on-premises readiness é zero.
- **Evidência:** Zero ficheiros com pattern `licens|entitlement|fingerprint|tamper` em `src/`. Zero directórios com pattern `licens|selfhost`.

## 3. Gaps altos

### 3.1 Sem Assembly Integrity Verification runtime
- **Severidade:** HIGH
- **Classificação:** NOT_DEPLOYABLE
- **Descrição:** `AssemblyIntegrityChecker` existe em `BuildingBlocks.Security` como verificador de integridade de assemblies. Porém não existe enforcement runtime ligado a licensing.
- **Impacto:** Self-hosted deployment sem protecção contra modificação de binários.
- **Evidência:** `src/building-blocks/NexTraceOne.BuildingBlocks.Security/`

### 3.2 Sem entitlements por capacidade
- **Severidade:** HIGH
- **Classificação:** NOT_DEPLOYABLE
- **Descrição:** O modelo de entitlements (quais módulos/capacidades estão licenciados por tenant) não existe. Todo o acesso é controlado apenas por RBAC.
- **Impacto:** Impossível vender o produto com tiers diferenciados.

## 4. Gaps médios

### 4.1 Sem machine fingerprinting
- **Severidade:** MEDIUM
- **Classificação:** INCOMPLETE
- **Descrição:** Não existe mecanismo para gerar fingerprint da máquina host para validação de licença offline.

### 4.2 Sem heartbeat de licença
- **Severidade:** MEDIUM
- **Classificação:** INCOMPLETE
- **Descrição:** Não existe serviço de validação periódica de licença (heartbeat).

## 5. Itens mock / stub / placeholder
Nenhum — o módulo não existe.

## 6. Erros de desenho / implementação incorreta
N/A — não há implementação.

## 7-12. Gaps de frontend / backend / banco / configuração / documentação / seed
N/A — módulo inexistente.

## 13. Ações corretivas obrigatórias
1. Criar módulo `NexTraceOne.Licensing` com Domain, Application, Infrastructure, API, Contracts
2. Definir entidades: License, Entitlement, LicenseActivation, MachineFingerprint
3. Implementar enforcement middleware no backend
4. Implementar validação de licença no arranque
5. Implementar heartbeat service
6. Implementar entitlements por módulo/capacidade
7. Documentar estratégia de licensing
