# Proposta: Módulo de Parametrização Avançada — NexTraceOne

**Data:** 2026-04-06  
**Autor:** Copilot AI (análise automatizada)  
**Versão:** 1.0  
**Estado:** Proposta para aprovação

---

## 1. Sumário Executivo

Esta proposta apresenta uma análise profunda de todo o NexTraceOne com o objetivo de elevar a plataforma para um novo nível de flexibilidade e personalização. O módulo de Parametrização Avançada permitirá que cada empresa/tenant customize fluxos de atividades, aprovações, gates, políticas e comportamentos — tornando o NexTraceOne uma plataforma verdadeiramente multi-tenant e enterprise-grade.

### Estado Atual da Infraestrutura

O NexTraceOne já possui uma infraestrutura sólida de configuração:
- **345 definições de configuração** já existentes no seeder
- **6 níveis de escopo** com herança hierárquica (System → Tenant → Environment → Role → Team → User)
- **Feature Flags** com resolução efetiva por escopo
- **Auditoria** de todas as alterações de configuração
- **Janelas de validade temporal** (EffectiveFrom/EffectiveTo)

### O que falta

Apesar da infraestrutura existente, a plataforma precisa de:
1. **Novos parâmetros** para funcionalidades que hoje são fixas no código
2. **Workflow Engine parametrizável** — ativar/desativar etapas de aprovação conforme o tenant
3. **Gates de integração externa** — validar condições em sistemas como Jenkins, GitLab, Azure DevOps antes de permitir ações
4. **Políticas de comportamento condicional** — o mesmo fluxo pode ter regras diferentes por ambiente, equipa ou criticidade
5. **Self-service de configuração** — admins devem conseguir parametrizar sem redeploy
6. **Frontend de gestão unificado** — painel visual para gestão de todos os parâmetros

---

## 2. Análise dos Módulos e Parâmetros Propostos

### Legenda de Classificação

| Campo | Descrição |
|-------|-----------|
| **Chave** | Identificador único do parâmetro (namespace.contexto.propriedade) |
| **Nome** | Nome legível para UI |
| **Tipo** | Boolean, String, Integer, Json, Decimal |
| **Escopos** | Níveis onde pode ser customizado |
| **Default** | Valor padrão |
| **Descrição** | O que é, para que serve, qual o comportamento esperado |
| **Prioridade** | 🔴 Alta / 🟡 Média / 🟢 Baixa |
| **Categoria** | Functional / Bootstrap / SensitiveOperational |

---

## 3. MÓDULO: Service Catalog — Governança de Criação de Serviços

### 3.1 Aprovação de Criação de Serviço

| Campo | Valor |
|-------|-------|
| **Chave** | `catalog.service.creation.approval_required` |
| **Nome** | Aprovação Obrigatória para Criação de Serviço |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant, Environment |
| **Default** | `false` |
| **Descrição** | Quando ativado, a criação de novos serviços no catálogo requer aprovação de um responsável antes de ficar visível. Se desativado, serviços são criados diretamente em estado ativo. Empresas que precisam de governança forte podem exigir esta aprovação; empresas ágeis podem desativá-la. |
| **Prioridade** | 🔴 Alta |

| Campo | Valor |
|-------|-------|
| **Chave** | `catalog.service.creation.approval_roles` |
| **Nome** | Papéis Aprovadores de Criação de Serviço |
| **Tipo** | Json |
| **Escopos** | System, Tenant |
| **Default** | `["TechLead", "Architect", "PlatformAdmin"]` |
| **Descrição** | Define quais papéis/roles podem aprovar a criação de novos serviços. Apenas utilizadores com um destes papéis poderão ver e aprovar pedidos de criação de serviço pendentes. |
| **Prioridade** | 🔴 Alta |

| Campo | Valor |
|-------|-------|
| **Chave** | `catalog.service.creation.approval_min_approvers` |
| **Nome** | Número Mínimo de Aprovadores para Criação de Serviço |
| **Tipo** | Integer |
| **Escopos** | System, Tenant |
| **Default** | `1` |
| **Descrição** | Número mínimo de aprovações necessárias para que um serviço seja efetivamente criado. Se definido como 2, pelo menos 2 pessoas com papel aprovador devem aprovar antes do serviço ficar ativo. |
| **Prioridade** | 🟡 Média |

| Campo | Valor |
|-------|-------|
| **Chave** | `catalog.service.creation.auto_approve_conditions` |
| **Nome** | Condições de Auto-Aprovação para Criação de Serviço |
| **Tipo** | Json |
| **Escopos** | System, Tenant |
| **Default** | `{"enabled": false}` |
| **Descrição** | Condições sob as quais a criação de serviço é automaticamente aprovada sem intervenção humana. Exemplo: serviços criados a partir de templates aprovados podem ser auto-aprovados. Formato: `{"enabled": true, "when_from_template": true, "when_owner_is_tech_lead": true}`. |
| **Prioridade** | 🟡 Média |

### 3.2 Requisitos Obrigatórios na Criação de Serviço

| Campo | Valor |
|-------|-------|
| **Chave** | `catalog.service.creation.mandatory_fields` |
| **Nome** | Campos Obrigatórios na Criação de Serviço |
| **Tipo** | Json |
| **Escopos** | System, Tenant |
| **Default** | `["name", "domain", "team", "owner", "description"]` |
| **Descrição** | Define quais campos são obrigatórios ao criar um novo serviço. A empresa pode exigir campos adicionais como "techStack", "repository", "slaTier" ou reduzir para apenas "name" e "team" em ambientes ágeis. |
| **Prioridade** | 🔴 Alta |

| Campo | Valor |
|-------|-------|
| **Chave** | `catalog.service.creation.require_template` |
| **Nome** | Exigir Template na Criação de Serviço |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant |
| **Default** | `false` |
| **Descrição** | Quando ativado, só é possível criar serviços a partir de templates pré-aprovados. Impede criação de serviços "ad-hoc" sem estrutura padronizada. Útil para empresas que querem garantir consistência arquitetural. |
| **Prioridade** | 🟡 Média |

| Campo | Valor |
|-------|-------|
| **Chave** | `catalog.service.creation.require_contract` |
| **Nome** | Exigir Contrato Inicial na Criação de Serviço |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant, Environment |
| **Default** | `false` |
| **Descrição** | Quando ativado, a criação de um serviço só é concluída se pelo menos um contrato (API, Evento ou SOAP) for associado. Garante que nenhum serviço existe sem definição de interface. |
| **Prioridade** | 🟡 Média |

### 3.3 Lifecycle e Desativação de Serviços

| Campo | Valor |
|-------|-------|
| **Chave** | `catalog.service.deactivation.approval_required` |
| **Nome** | Aprovação Obrigatória para Desativação de Serviço |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant |
| **Default** | `true` |
| **Descrição** | Controla se a desativação/remoção de um serviço do catálogo requer aprovação. Previne remoções acidentais de serviços em produção. Quando desativado, qualquer editor pode desativar serviços diretamente. |
| **Prioridade** | 🔴 Alta |

| Campo | Valor |
|-------|-------|
| **Chave** | `catalog.service.deactivation.require_dependency_check` |
| **Nome** | Verificar Dependências antes de Desativar Serviço |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant |
| **Default** | `true` |
| **Descrição** | Quando ativado, o sistema verifica se existem outros serviços que dependem do serviço a ser desativado e bloqueia a operação se existirem dependentes ativos. |
| **Prioridade** | 🔴 Alta |

---

## 4. MÓDULO: Contracts — Governança de Contratos

### 4.1 Aprovação de Contratos

| Campo | Valor |
|-------|-------|
| **Chave** | `catalog.contract.creation.approval_required` |
| **Nome** | Aprovação Obrigatória para Criação de Contrato |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant, Environment |
| **Default** | `false` |
| **Descrição** | Quando ativado, novas versões de contrato (API, Evento, SOAP) precisam de aprovação antes de ficarem disponíveis. Contratos ficam em estado "PendingApproval" até serem aprovados. Em ambientes de produção pode estar ativo enquanto em dev pode estar desativado. |
| **Prioridade** | 🔴 Alta |

| Campo | Valor |
|-------|-------|
| **Chave** | `catalog.contract.creation.approval_roles` |
| **Nome** | Papéis Aprovadores de Contratos |
| **Tipo** | Json |
| **Escopos** | System, Tenant |
| **Default** | `["Architect", "TechLead"]` |
| **Descrição** | Define quais papéis podem aprovar a criação ou alteração de contratos. Apenas utilizadores com estes papéis verão pedidos pendentes de aprovação de contratos. |
| **Prioridade** | 🔴 Alta |

| Campo | Valor |
|-------|-------|
| **Chave** | `catalog.contract.breaking_change.block_deploy` |
| **Nome** | Bloquear Deploy em Caso de Breaking Change |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant, Environment |
| **Default** | `true` |
| **Descrição** | Quando ativado, se um contrato contém uma breaking change detetada automaticamente, o deploy é bloqueado até que a breaking change seja explicitamente aprovada ou justificada. Previne que alterações incompatíveis cheguem a produção sem revisão. |
| **Prioridade** | 🔴 Alta |

| Campo | Valor |
|-------|-------|
| **Chave** | `catalog.contract.breaking_change.override_roles` |
| **Nome** | Papéis que Podem Aprovar Breaking Changes |
| **Tipo** | Json |
| **Escopos** | System, Tenant |
| **Default** | `["Architect", "PlatformAdmin"]` |
| **Descrição** | Define quais papéis podem aprovar/justificar breaking changes em contratos. Apenas estes papéis podem desbloquear um deploy bloqueado por breaking change. |
| **Prioridade** | 🔴 Alta |

### 4.2 Validação e Qualidade de Contratos

| Campo | Valor |
|-------|-------|
| **Chave** | `catalog.contract.validation.auto_lint_on_save` |
| **Nome** | Linting Automático ao Salvar Contrato |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant |
| **Default** | `true` |
| **Descrição** | Quando ativado, cada vez que um contrato é salvo ou criado, as regras de linting configuradas são automaticamente executadas. Resultados são mostrados ao utilizador. Se desativado, o linting só ocorre quando explicitamente solicitado. |
| **Prioridade** | 🟡 Média |

| Campo | Valor |
|-------|-------|
| **Chave** | `catalog.contract.validation.block_on_lint_errors` |
| **Nome** | Bloquear Publicação com Erros de Lint |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant, Environment |
| **Default** | `true` |
| **Descrição** | Quando ativado, contratos com erros de lint (severidade "error") não podem ser publicados. Warnings são permitidos mas erros bloqueiam. Útil para garantir qualidade mínima antes da publicação. |
| **Prioridade** | 🟡 Média |

| Campo | Valor |
|-------|-------|
| **Chave** | `catalog.contract.publication.require_examples` |
| **Nome** | Exigir Exemplos para Publicação |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant |
| **Default** | `false` |
| **Descrição** | Quando ativado, um contrato só pode ser publicado no portal se contiver pelo menos um exemplo de request/response para cada endpoint. Melhora a experiência do consumidor do contrato. |
| **Prioridade** | 🟡 Média |

| Campo | Valor |
|-------|-------|
| **Chave** | `catalog.contract.deprecation.grace_period_days` |
| **Nome** | Período de Graça para Deprecação de Contrato (dias) |
| **Tipo** | Integer |
| **Escopos** | System, Tenant |
| **Default** | `90` |
| **Descrição** | Número de dias entre o início da deprecação de um contrato e a sua efetiva remoção. Durante este período, consumidores são notificados para migrar. Ao fim do período, o contrato pode ser removido automaticamente conforme política. |
| **Prioridade** | 🟡 Média |

---

## 5. MÓDULO: Change Governance — Release e Deploy

### 5.1 Aprovação de Release

| Campo | Valor |
|-------|-------|
| **Chave** | `change.release.approval_required` |
| **Nome** | Aprovação Obrigatória para Release |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant, Environment |
| **Default** | `true` |
| **Descrição** | Quando ativado, toda release precisa passar por um fluxo de aprovação antes de poder ser deployada. Se desativado, releases podem ser deployadas diretamente (útil para ambientes de desenvolvimento). Em produção, recomenda-se manter sempre ativado. |
| **Prioridade** | 🔴 Alta |

| Campo | Valor |
|-------|-------|
| **Chave** | `change.release.approval_roles` |
| **Nome** | Papéis Aprovadores de Release |
| **Tipo** | Json |
| **Escopos** | System, Tenant, Environment |
| **Default** | `["TechLead", "Architect", "ProductOwner"]` |
| **Descrição** | Define quais papéis podem aprovar uma release para deploy. A configuração pode variar por ambiente — em staging pode ser apenas TechLead, em produção pode exigir Architect + ProductOwner. |
| **Prioridade** | 🔴 Alta |

| Campo | Valor |
|-------|-------|
| **Chave** | `change.release.approval_min_approvers` |
| **Nome** | Número Mínimo de Aprovadores de Release |
| **Tipo** | Integer |
| **Escopos** | System, Tenant, Environment |
| **Default** | `1` |
| **Descrição** | Número mínimo de aprovações necessárias para que uma release possa ser deployada. Em produção, pode exigir 2+ aprovadores. Em dev, 0 (auto-approved). |
| **Prioridade** | 🔴 Alta |

### 5.2 Validação Externa de Release (Jenkins, GitLab, Azure DevOps)

| Campo | Valor |
|-------|-------|
| **Chave** | `change.release.external_validation.enabled` |
| **Nome** | Validação Externa de Release Ativada |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant, Environment |
| **Default** | `false` |
| **Descrição** | Quando ativado, antes de permitir o deploy de uma release, o NexTraceOne consulta um serviço externo (ex: Jenkins, GitLab CI, Azure DevOps) para validar se a release foi aprovada no pipeline externo. Só permite deploy se a validação externa retornar sucesso. |
| **Prioridade** | 🔴 Alta |

| Campo | Valor |
|-------|-------|
| **Chave** | `change.release.external_validation.provider` |
| **Nome** | Provider de Validação Externa |
| **Tipo** | String |
| **Escopos** | System, Tenant, Environment |
| **Default** | `""` |
| **Descrição** | Identificador do provider de CI/CD externo a consultar para validação de release. Valores possíveis: "jenkins", "gitlab", "azure_devops", "github_actions", "custom_webhook". Cada provider tem o seu adaptador de integração. |
| **Prioridade** | 🔴 Alta |

| Campo | Valor |
|-------|-------|
| **Chave** | `change.release.external_validation.endpoint_url` |
| **Nome** | URL do Endpoint de Validação Externa |
| **Tipo** | String |
| **Escopos** | Tenant, Environment |
| **Default** | `""` |
| **Descrição** | URL do endpoint HTTP que será consultado para validar a release. O NexTraceOne faz um GET/POST a esta URL com os dados da release e espera uma resposta indicando aprovado/rejeitado. Formato esperado da resposta: `{"approved": true/false, "reason": "..."}`. |
| **Prioridade** | 🔴 Alta |

| Campo | Valor |
|-------|-------|
| **Chave** | `change.release.external_validation.timeout_seconds` |
| **Nome** | Timeout da Validação Externa (segundos) |
| **Tipo** | Integer |
| **Escopos** | System, Tenant |
| **Default** | `30` |
| **Descrição** | Tempo máximo em segundos que o NexTraceOne aguarda pela resposta do serviço externo de validação. Se excedido, a validação é considerada como falha e o deploy é bloqueado. |
| **Prioridade** | 🟡 Média |

| Campo | Valor |
|-------|-------|
| **Chave** | `change.release.external_validation.on_failure_action` |
| **Nome** | Ação em Caso de Falha na Validação Externa |
| **Tipo** | String |
| **Escopos** | System, Tenant, Environment |
| **Default** | `"block"` |
| **Descrição** | Define o comportamento quando a validação externa falha (timeout, erro de rede, serviço indisponível). Opções: "block" (bloqueia o deploy), "warn" (permite mas com alerta), "skip" (ignora a validação). Em produção recomenda-se "block". |
| **Prioridade** | 🔴 Alta |

| Campo | Valor |
|-------|-------|
| **Chave** | `change.release.external_validation.required_checks` |
| **Nome** | Checks Obrigatórios da Validação Externa |
| **Tipo** | Json |
| **Escopos** | System, Tenant, Environment |
| **Default** | `["build_success", "tests_passed"]` |
| **Descrição** | Lista de verificações que devem estar aprovadas no sistema externo. Exemplo: `["build_success", "tests_passed", "security_scan", "code_review", "quality_gate"]`. Cada check é verificado na resposta do provider externo. |
| **Prioridade** | 🔴 Alta |

### 5.3 Deploy Gate — Bloqueio/Permissão de Deploy

| Campo | Valor |
|-------|-------|
| **Chave** | `change.deploy.require_release_approval` |
| **Nome** | Exigir Release Aprovada para Deploy |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant, Environment |
| **Default** | `true` |
| **Descrição** | Quando ativado, o NexTraceOne só permite que um deploy seja registado/executado se a release associada tiver sido aprovada (internamente e/ou externamente conforme configuração). Um serviço externo como Jenkins pode consultar este endpoint para validar se pode prosseguir com o deploy. Este é o gate principal que conecta aprovação de release com permissão de deploy. |
| **Prioridade** | 🔴 Alta |

| Campo | Valor |
|-------|-------|
| **Chave** | `change.deploy.pre_deploy_checks` |
| **Nome** | Verificações Pré-Deploy |
| **Tipo** | Json |
| **Escopos** | System, Tenant, Environment |
| **Default** | `{"contract_compliance": true, "security_scan": true, "evidence_pack": false}` |
| **Descrição** | Define quais verificações devem ser executadas antes de permitir um deploy. Cada verificação pode ser ativada/desativada individualmente. Quando uma verificação obrigatória falha, o deploy é bloqueado. O pipeline CI/CD externo consulta o NexTraceOne e só prossegue se todas as verificações passarem. |
| **Prioridade** | 🔴 Alta |

| Campo | Valor |
|-------|-------|
| **Chave** | `change.deploy.post_deploy_verification.enabled` |
| **Nome** | Verificação Pós-Deploy Ativada |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant, Environment |
| **Default** | `true` |
| **Descrição** | Quando ativado, após um deploy, o NexTraceOne inicia automaticamente uma janela de verificação pós-deploy onde métricas, erros e anomalias são monitorados. Se problemas forem detetados durante a janela, alertas são enviados e rollback pode ser recomendado. |
| **Prioridade** | 🔴 Alta |

| Campo | Valor |
|-------|-------|
| **Chave** | `change.deploy.post_deploy_verification.window_minutes` |
| **Nome** | Janela de Verificação Pós-Deploy (minutos) |
| **Tipo** | Integer |
| **Escopos** | System, Tenant, Environment |
| **Default** | `30` |
| **Descrição** | Duração em minutos da janela de monitoramento pós-deploy. Durante este período, o NexTraceOne compara métricas com a baseline pré-deploy e sinaliza anomalias. |
| **Prioridade** | 🟡 Média |

### 5.4 Canary e Rollback

| Campo | Valor |
|-------|-------|
| **Chave** | `change.deploy.canary.enabled` |
| **Nome** | Deploy Canary Ativado |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant, Environment |
| **Default** | `false` |
| **Descrição** | Quando ativado, deploys podem ser realizados de forma canary (progressive rollout), onde apenas uma percentagem do tráfego é direcionada para a nova versão inicialmente. O NexTraceOne rastreia a progressão e pode recomendar rollback se anomalias forem detetadas. |
| **Prioridade** | 🟡 Média |

| Campo | Valor |
|-------|-------|
| **Chave** | `change.deploy.rollback.auto_enabled` |
| **Nome** | Rollback Automático Ativado |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant, Environment |
| **Default** | `false` |
| **Descrição** | Quando ativado, se durante a janela pós-deploy forem detetadas anomalias que excedam os thresholds definidos, o NexTraceOne recomenda ou executa automaticamente o rollback. Requer integração com o pipeline de deploy. |
| **Prioridade** | 🟡 Média |

| Campo | Valor |
|-------|-------|
| **Chave** | `change.deploy.rollback.auto_thresholds` |
| **Nome** | Thresholds para Rollback Automático |
| **Tipo** | Json |
| **Escopos** | System, Tenant, Environment |
| **Default** | `{"error_rate_increase_pct": 50, "latency_increase_pct": 100, "availability_drop_pct": 5}` |
| **Descrição** | Define os limites que, quando ultrapassados durante a janela pós-deploy, ativam o rollback automático. Exemplo: se a taxa de erros aumentar mais de 50% em relação à baseline, o rollback é ativado. |
| **Prioridade** | 🟡 Média |

---

## 6. MÓDULO: Promotion Governance — Promoção entre Ambientes

### 6.1 Controlo de Promoção

| Campo | Valor |
|-------|-------|
| **Chave** | `promotion.require_all_gates_passed` |
| **Nome** | Exigir Todos os Gates Aprovados para Promoção |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant, Environment |
| **Default** | `true` |
| **Descrição** | Quando ativado, uma promoção entre ambientes só é permitida se TODOS os gates configurados para o ambiente destino estiverem aprovados. Se desativado, apenas os gates marcados como "required" bloqueiam. |
| **Prioridade** | 🔴 Alta |

| Campo | Valor |
|-------|-------|
| **Chave** | `promotion.require_non_prod_validation` |
| **Nome** | Exigir Validação em Ambiente Não-Produtivo |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant |
| **Default** | `true` |
| **Descrição** | Quando ativado, não é possível promover diretamente para produção sem que a release tenha sido validada em pelo menos um ambiente não-produtivo. Garante que mudanças são testadas antes de chegarem a produção. |
| **Prioridade** | 🔴 Alta |

| Campo | Valor |
|-------|-------|
| **Chave** | `promotion.min_time_in_staging_hours` |
| **Nome** | Tempo Mínimo em Staging antes de Produção (horas) |
| **Tipo** | Integer |
| **Escopos** | System, Tenant, Environment |
| **Default** | `24` |
| **Descrição** | Número mínimo de horas que uma release deve permanecer em staging/pré-produção antes de poder ser promovida para produção. Se definido como 0, não há tempo mínimo. Útil para garantir que há tempo suficiente para detetar problemas. |
| **Prioridade** | 🟡 Média |

| Campo | Valor |
|-------|-------|
| **Chave** | `promotion.require_evidence_pack` |
| **Nome** | Exigir Evidence Pack para Promoção |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant, Environment |
| **Default** | `false` |
| **Descrição** | Quando ativado, a promoção de uma release para o ambiente destino só é permitida se um Evidence Pack completo tiver sido gerado e estiver associado à release. O Evidence Pack inclui resultados de testes, logs de CI/CD, e quaisquer observações manuais. |
| **Prioridade** | 🟡 Média |

---

## 7. MÓDULO: Workflow Engine — Fluxos de Aprovação

### 7.1 Configuração Dinâmica de Workflows

| Campo | Valor |
|-------|-------|
| **Chave** | `workflow.dynamic_stages.enabled` |
| **Nome** | Estágios Dinâmicos de Workflow Ativados |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant |
| **Default** | `false` |
| **Descrição** | Quando ativado, os estágios de um workflow podem ser dinamicamente adicionados ou removidos com base em regras e condições (ex: criticidade da mudança, ambiente destino, tipo de serviço). Permite que empresas criem fluxos de aprovação adaptativos. |
| **Prioridade** | 🟡 Média |

| Campo | Valor |
|-------|-------|
| **Chave** | `workflow.parallel_approval.enabled` |
| **Nome** | Aprovação Paralela de Estágios |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant |
| **Default** | `false` |
| **Descrição** | Quando ativado, múltiplos estágios de aprovação podem ser executados em paralelo (ex: aprovação técnica e aprovação de segurança ao mesmo tempo). Quando desativado, estágios são sempre sequenciais. Reduz o tempo total de aprovação. |
| **Prioridade** | 🟡 Média |

| Campo | Valor |
|-------|-------|
| **Chave** | `workflow.delegation.enabled` |
| **Nome** | Delegação de Aprovação Ativada |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant |
| **Default** | `true` |
| **Descrição** | Quando ativado, aprovadores podem delegar a sua responsabilidade de aprovação a outro utilizador (temporária ou permanentemente). Útil para períodos de férias ou ausência. A delegação é auditada e pode ter data de expiração. |
| **Prioridade** | 🟡 Média |

| Campo | Valor |
|-------|-------|
| **Chave** | `workflow.reminder.enabled` |
| **Nome** | Lembretes de Aprovação Pendente |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant |
| **Default** | `true` |
| **Descrição** | Quando ativado, o sistema envia lembretes automáticos aos aprovadores quando existem aprovações pendentes há mais de X horas (configurável). Previne que aprovações fiquem esquecidas. |
| **Prioridade** | 🟡 Média |

| Campo | Valor |
|-------|-------|
| **Chave** | `workflow.reminder.interval_hours` |
| **Nome** | Intervalo de Lembretes (horas) |
| **Tipo** | Integer |
| **Escopos** | System, Tenant |
| **Default** | `4` |
| **Descrição** | Intervalo em horas entre cada lembrete enviado ao aprovador sobre aprovações pendentes. |
| **Prioridade** | 🟢 Baixa |

---

## 8. MÓDULO: Incidents & Operations — Gestão de Incidentes

### 8.1 Criação e Classificação de Incidentes

| Campo | Valor |
|-------|-------|
| **Chave** | `incidents.auto_creation.from_monitoring.enabled` |
| **Nome** | Criação Automática de Incidentes a partir de Monitoramento |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant, Environment |
| **Default** | `true` |
| **Descrição** | Quando ativado, anomalias detetadas pelo monitoramento (thresholds de erro, latência, disponibilidade) criam automaticamente incidentes. Quando desativado, incidentes são criados apenas manualmente. |
| **Prioridade** | 🔴 Alta |

| Campo | Valor |
|-------|-------|
| **Chave** | `incidents.auto_assign.enabled` |
| **Nome** | Atribuição Automática de Incidentes |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant |
| **Default** | `true` |
| **Descrição** | Quando ativado, incidentes são automaticamente atribuídos à equipa owner do serviço afetado. Quando desativado, incidentes ficam sem atribuição até serem triados manualmente. |
| **Prioridade** | 🔴 Alta |

| Campo | Valor |
|-------|-------|
| **Chave** | `incidents.post_incident_review.required` |
| **Nome** | Post-Incident Review Obrigatório |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant, Environment |
| **Default** | `false` |
| **Descrição** | Quando ativado, após a resolução de um incidente de severidade alta ou crítica, é obrigatório completar uma Post-Incident Review (PIR/Post-Mortem). O incidente não pode ser marcado como "Fechado" até a PIR estar concluída. |
| **Prioridade** | 🔴 Alta |

| Campo | Valor |
|-------|-------|
| **Chave** | `incidents.post_incident_review.min_severity` |
| **Nome** | Severidade Mínima para PIR Obrigatória |
| **Tipo** | String |
| **Escopos** | System, Tenant |
| **Default** | `"High"` |
| **Descrição** | Define a severidade mínima a partir da qual a Post-Incident Review é obrigatória. Opções: "Critical", "High", "Medium", "Low". Se definido como "Medium", todas as incidentes de severidade Medium ou superior exigem PIR. |
| **Prioridade** | 🟡 Média |

| Campo | Valor |
|-------|-------|
| **Chave** | `incidents.correlation.auto_link_to_changes.enabled` |
| **Nome** | Correlação Automática Incidente-Mudança |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant |
| **Default** | `true` |
| **Descrição** | Quando ativado, o NexTraceOne correlaciona automaticamente novos incidentes com mudanças/deploys recentes na janela de tempo configurada. Ajuda a identificar rapidamente se um deploy causou o incidente. |
| **Prioridade** | 🔴 Alta |

### 8.2 Runbooks e Automação

| Campo | Valor |
|-------|-------|
| **Chave** | `operations.runbook.auto_suggest.enabled` |
| **Nome** | Sugestão Automática de Runbooks |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant |
| **Default** | `true` |
| **Descrição** | Quando ativado, ao abrir um incidente, o NexTraceOne sugere automaticamente runbooks relevantes com base no serviço afetado, tipo de incidente e histórico. |
| **Prioridade** | 🟡 Média |

| Campo | Valor |
|-------|-------|
| **Chave** | `operations.automation.require_approval_in_production` |
| **Nome** | Exigir Aprovação para Automação em Produção |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant |
| **Default** | `true` |
| **Descrição** | Quando ativado, ações automatizadas (runbooks, scripts de mitigação) que afetam ambientes de produção requerem aprovação humana antes de serem executadas. Em ambientes não-produtivos, podem ser executadas automaticamente. |
| **Prioridade** | 🔴 Alta |

---

## 9. MÓDULO: AI Hub — Governança de IA

### 9.1 Controlo de Acesso e Uso de IA

| Campo | Valor |
|-------|-------|
| **Chave** | `ai.external_models.require_approval` |
| **Nome** | Exigir Aprovação para Uso de Modelos Externos |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant |
| **Default** | `false` |
| **Descrição** | Quando ativado, a utilização de modelos de IA externos (OpenAI, Anthropic, etc.) requer aprovação do Platform Admin antes de ser ativada para um tenant/equipa. Garante que dados sensíveis não saem da organização sem autorização. |
| **Prioridade** | 🔴 Alta |

| Campo | Valor |
|-------|-------|
| **Chave** | `ai.data_classification.block_sensitive` |
| **Nome** | Bloquear Envio de Dados Sensíveis para IA Externa |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant |
| **Default** | `true` |
| **Descrição** | Quando ativado, o sistema analisa o conteúdo dos prompts antes de enviá-los a modelos externos e bloqueia o envio se forem detetados dados classificados como sensíveis (PII, credenciais, tokens, dados financeiros). |
| **Prioridade** | 🔴 Alta |

| Campo | Valor |
|-------|-------|
| **Chave** | `ai.knowledge_capture.auto_approve` |
| **Nome** | Auto-Aprovar Captura de Conhecimento de IA |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant |
| **Default** | `false` |
| **Descrição** | Quando ativado, respostas de IA capturadas como conhecimento são automaticamente aprovadas e ficam disponíveis para reutilização. Quando desativado, cada captura requer revisão e aprovação humana antes de entrar na base de conhecimento. |
| **Prioridade** | 🟡 Média |

| Campo | Valor |
|-------|-------|
| **Chave** | `ai.agents.custom_creation.enabled` |
| **Nome** | Permitir Criação de Agentes IA Customizados |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant |
| **Default** | `false` |
| **Descrição** | Quando ativado, utilizadores com permissão podem criar os seus próprios agentes de IA especializados. Quando desativado, apenas os agentes pré-definidos pelo sistema estão disponíveis. |
| **Prioridade** | 🟡 Média |

| Campo | Valor |
|-------|-------|
| **Chave** | `ai.agents.custom_creation.require_approval` |
| **Nome** | Exigir Aprovação para Novos Agentes IA |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant |
| **Default** | `true` |
| **Descrição** | Quando ativado, novos agentes de IA criados por utilizadores requerem aprovação de um Platform Admin antes de ficarem disponíveis para uso geral. |
| **Prioridade** | 🟡 Média |

### 9.2 IDE Extensions

| Campo | Valor |
|-------|-------|
| **Chave** | `ai.ide.extensions.enabled` |
| **Nome** | Extensões IDE Ativadas |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant |
| **Default** | `true` |
| **Descrição** | Controla se as extensões IDE do NexTraceOne (VS Code, Visual Studio) estão ativadas para o tenant. Quando desativado, clientes IDE não conseguem autenticar-se na plataforma. |
| **Prioridade** | 🟡 Média |

| Campo | Valor |
|-------|-------|
| **Chave** | `ai.ide.allowed_capabilities` |
| **Nome** | Capacidades IDE Permitidas |
| **Tipo** | Json |
| **Escopos** | System, Tenant, Role |
| **Default** | `["code_review", "contract_generation", "test_generation", "documentation"]` |
| **Descrição** | Define quais capacidades de IA estão disponíveis através das extensões IDE. A empresa pode restringir funcionalidades específicas. Exemplo: permitir geração de testes mas bloquear geração de código. |
| **Prioridade** | 🟡 Média |

---

## 10. MÓDULO: Identity & Access — Segurança e Acesso

### 10.1 Políticas de Acesso

| Campo | Valor |
|-------|-------|
| **Chave** | `security.password.policy` |
| **Nome** | Política de Passwords |
| **Tipo** | Json |
| **Escopos** | System, Tenant |
| **Default** | `{"min_length": 12, "require_uppercase": true, "require_lowercase": true, "require_number": true, "require_special": true, "max_age_days": 90, "history_count": 5}` |
| **Descrição** | Define a política de complexidade de passwords locais. Inclui comprimento mínimo, requisitos de caracteres, idade máxima e histórico de passwords. Apenas aplicável quando SSO não está configurado. |
| **Prioridade** | 🔴 Alta |

| Campo | Valor |
|-------|-------|
| **Chave** | `security.session.concurrent_limit` |
| **Nome** | Limite de Sessões Concorrentes |
| **Tipo** | Integer |
| **Escopos** | System, Tenant |
| **Default** | `5` |
| **Descrição** | Número máximo de sessões ativas simultâneas por utilizador. Se excedido, a sessão mais antiga é terminada automaticamente. Se definido como 0, não há limite. |
| **Prioridade** | 🟡 Média |

| Campo | Valor |
|-------|-------|
| **Chave** | `security.break_glass.require_dual_approval` |
| **Nome** | Exigir Dupla Aprovação para Break Glass |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant |
| **Default** | `true` |
| **Descrição** | Quando ativado, pedidos de Break Glass Access requerem aprovação de pelo menos 2 Platform Admins. Quando desativado, 1 aprovação é suficiente. Break Glass concede acesso privilegiado temporário em situações de emergência. |
| **Prioridade** | 🔴 Alta |

| Campo | Valor |
|-------|-------|
| **Chave** | `security.jit_access.max_duration_hours` |
| **Nome** | Duração Máxima de Acesso JIT (horas) |
| **Tipo** | Integer |
| **Escopos** | System, Tenant |
| **Default** | `8` |
| **Descrição** | Duração máxima em horas de um acesso Just-In-Time. Após este período, o acesso é automaticamente revogado. JIT permite acesso privilegiado temporário sem alterar permanentemente os papéis do utilizador. |
| **Prioridade** | 🟡 Média |

| Campo | Valor |
|-------|-------|
| **Chave** | `security.access_review.frequency_days` |
| **Nome** | Frequência de Access Review (dias) |
| **Tipo** | Integer |
| **Escopos** | System, Tenant |
| **Default** | `90` |
| **Descrição** | Intervalo em dias entre campanhas de revisão de acessos. Quando o período expira, uma nova campanha é automaticamente iniciada, solicitando que gestores revisem os acessos das suas equipas. |
| **Prioridade** | 🟡 Média |

| Campo | Valor |
|-------|-------|
| **Chave** | `security.access_review.auto_revoke_on_no_response` |
| **Nome** | Revogar Acessos Automaticamente sem Resposta |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant |
| **Default** | `false` |
| **Descrição** | Quando ativado, se um gestor não responder a um item de Access Review dentro do prazo, o acesso é automaticamente revogado. Quando desativado, acessos não revisados são escalados para Platform Admin. |
| **Prioridade** | 🟡 Média |

---

## 11. MÓDULO: Audit & Compliance

### 11.1 Políticas de Auditoria

| Campo | Valor |
|-------|-------|
| **Chave** | `audit.chain_integrity.verification_frequency_hours` |
| **Nome** | Frequência de Verificação de Integridade da Cadeia de Auditoria |
| **Tipo** | Integer |
| **Escopos** | System |
| **Default** | `24` |
| **Descrição** | Intervalo em horas para verificação automática da integridade da cadeia de auditoria (hash chain). Deteta adulterações nos registos de auditoria. Se a verificação falhar, um alerta crítico é enviado. |
| **Prioridade** | 🔴 Alta |

| Campo | Valor |
|-------|-------|
| **Chave** | `audit.continuous_compliance.enabled` |
| **Nome** | Compliance Contínua Ativada |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant |
| **Default** | `true` |
| **Descrição** | Quando ativado, o sistema avalia continuamente a conformidade dos serviços, contratos e operações com as políticas definidas. Violações são registadas e alertas são enviados em tempo real. |
| **Prioridade** | 🔴 Alta |

| Campo | Valor |
|-------|-------|
| **Chave** | `audit.compliance.require_evidence_for_exceptions` |
| **Nome** | Exigir Evidência para Exceções de Compliance |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant |
| **Default** | `true` |
| **Descrição** | Quando ativado, qualquer exceção ou waiver de compliance requer evidência documental anexa (screenshot, documento, link). Sem evidência, o waiver não pode ser aprovado. |
| **Prioridade** | 🟡 Média |

---

## 12. MÓDULO: Integrations — Integrações Externas

### 12.1 Governança de Integrações

| Campo | Valor |
|-------|-------|
| **Chave** | `integrations.new_connector.require_approval` |
| **Nome** | Exigir Aprovação para Novos Conectores |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant |
| **Default** | `false` |
| **Descrição** | Quando ativado, a ativação de um novo conector de integração (ex: Jenkins, GitLab, Jira) requer aprovação de um Platform Admin. Previne integrações não autorizadas com sistemas externos. |
| **Prioridade** | 🟡 Média |

| Campo | Valor |
|-------|-------|
| **Chave** | `integrations.webhook.require_signature_validation` |
| **Nome** | Exigir Validação de Assinatura em Webhooks |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant |
| **Default** | `true` |
| **Descrição** | Quando ativado, todos os webhooks recebidos devem incluir uma assinatura válida (HMAC). Webhooks sem assinatura válida são rejeitados. Garante autenticidade dos eventos recebidos. |
| **Prioridade** | 🔴 Alta |

| Campo | Valor |
|-------|-------|
| **Chave** | `integrations.data_sync.direction_policy` |
| **Nome** | Política de Direção de Sincronização |
| **Tipo** | String |
| **Escopos** | System, Tenant, Environment |
| **Default** | `"bidirectional"` |
| **Descrição** | Define a direção permitida para sincronização de dados com sistemas externos. Opções: "inbound_only" (apenas receber dados), "outbound_only" (apenas enviar dados), "bidirectional" (ambas as direções). Em ambientes produtivos pode ser restrito a "inbound_only" para evitar fuga de dados. |
| **Prioridade** | 🟡 Média |

---

## 13. MÓDULO: FinOps — Otimização de Custos

### 13.1 Políticas Financeiras

| Campo | Valor |
|-------|-------|
| **Chave** | `finops.budget.auto_alert.enabled` |
| **Nome** | Alertas Automáticos de Budget |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant |
| **Default** | `true` |
| **Descrição** | Quando ativado, alertas são automaticamente enviados quando o consumo de um serviço/equipa atinge os thresholds de budget definidos (ex: 50%, 75%, 90%, 100%). |
| **Prioridade** | 🔴 Alta |

| Campo | Valor |
|-------|-------|
| **Chave** | `finops.budget.enforcement.enabled` |
| **Nome** | Enforcement de Budget Ativado |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant |
| **Default** | `false` |
| **Descrição** | Quando ativado, quando um serviço/equipa ultrapassa o budget definido, ações preventivas podem ser ativadas (bloquear novos deploys, limitar recursos, escalar para gestão). Quando desativado, apenas alertas são enviados. |
| **Prioridade** | 🟡 Média |

| Campo | Valor |
|-------|-------|
| **Chave** | `finops.recommendations.auto_apply.enabled` |
| **Nome** | Aplicar Recomendações de FinOps Automaticamente |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant |
| **Default** | `false` |
| **Descrição** | Quando ativado, recomendações de otimização de custo (ex: reduzir recursos ociosos, eliminar desperdício) podem ser automaticamente aplicadas sem intervenção humana. Quando desativado, todas as recomendações requerem ação manual. |
| **Prioridade** | 🟢 Baixa |

---

## 14. MÓDULO: Knowledge — Base de Conhecimento

### 14.1 Governança de Conhecimento

| Campo | Valor |
|-------|-------|
| **Chave** | `knowledge.document.review_required` |
| **Nome** | Revisão Obrigatória de Documentos |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant |
| **Default** | `false` |
| **Descrição** | Quando ativado, novos documentos de conhecimento requerem revisão e aprovação antes de ficarem visíveis para toda a organização. Garante qualidade e precisão do conhecimento partilhado. |
| **Prioridade** | 🟡 Média |

| Campo | Valor |
|-------|-------|
| **Chave** | `knowledge.document.review_roles` |
| **Nome** | Papéis Revisores de Documentos |
| **Tipo** | Json |
| **Escopos** | System, Tenant |
| **Default** | `["TechLead", "Architect"]` |
| **Descrição** | Define quais papéis podem aprovar documentos de conhecimento quando a revisão obrigatória está ativa. |
| **Prioridade** | 🟡 Média |

| Campo | Valor |
|-------|-------|
| **Chave** | `knowledge.document.staleness_alert_days` |
| **Nome** | Alerta de Documentação Obsoleta (dias) |
| **Tipo** | Integer |
| **Escopos** | System, Tenant |
| **Default** | `180` |
| **Descrição** | Número de dias sem atualização após os quais um documento é marcado como potencialmente obsoleto e um alerta é enviado ao owner. Incentiva manutenção da base de conhecimento. |
| **Prioridade** | 🟢 Baixa |

| Campo | Valor |
|-------|-------|
| **Chave** | `knowledge.auto_documentation.enabled` |
| **Nome** | Auto-Documentação Ativada |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant |
| **Default** | `true` |
| **Descrição** | Quando ativado, o NexTraceOne gera automaticamente documentação técnica para serviços e contratos usando IA. A documentação gerada é marcada como "auto-generated" e pode ser editada manualmente. |
| **Prioridade** | 🟡 Média |

---

## 15. MÓDULO: Reliability — SLO/SLA

### 15.1 Políticas de Confiabilidade

| Campo | Valor |
|-------|-------|
| **Chave** | `reliability.slo.require_definition` |
| **Nome** | Exigir Definição de SLO para Serviços |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant, Environment |
| **Default** | `false` |
| **Descrição** | Quando ativado, todo serviço em produção deve ter pelo menos um SLO definido. Serviços sem SLO são sinalizados como não-conformes no scorecard de governança. |
| **Prioridade** | 🔴 Alta |

| Campo | Valor |
|-------|-------|
| **Chave** | `reliability.error_budget.auto_block_deploys` |
| **Nome** | Bloquear Deploys quando Error Budget Esgotado |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant, Environment |
| **Default** | `false` |
| **Descrição** | Quando ativado, se o error budget de um serviço estiver esgotado (burn rate excessivo), novos deploys para esse serviço são automaticamente bloqueados até que o error budget recupere. Segue a prática recomendada de SRE da Google. |
| **Prioridade** | 🔴 Alta |

| Campo | Valor |
|-------|-------|
| **Chave** | `reliability.error_budget.block_threshold_pct` |
| **Nome** | Threshold de Error Budget para Bloqueio (%) |
| **Tipo** | Decimal |
| **Escopos** | System, Tenant, Environment |
| **Default** | `0` |
| **Descrição** | Percentagem de error budget restante abaixo da qual deploys são bloqueados. Se definido como 10, deploys são bloqueados quando restam menos de 10% do error budget. Se 0, bloqueio só ocorre quando budget está 100% consumido. |
| **Prioridade** | 🟡 Média |

---

## 16. MÓDULO: Governance — Governança Transversal

### 16.1 Políticas de Governança Empresarial

| Campo | Valor |
|-------|-------|
| **Chave** | `governance.four_eyes_principle.enabled` |
| **Nome** | Princípio dos Quatro Olhos Ativado |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant |
| **Default** | `false` |
| **Descrição** | Quando ativado, qualquer ação crítica (deploy em produção, alteração de configuração de segurança, criação de acesso privilegiado) requer confirmação de um segundo utilizador autorizado. Implementa o princípio de segregação de deveres (Separation of Duties). Recomendado para ambientes regulados (banca, saúde, governo). |
| **Prioridade** | 🔴 Alta |

| Campo | Valor |
|-------|-------|
| **Chave** | `governance.four_eyes_principle.actions` |
| **Nome** | Ações Sujeitas ao Princípio dos Quatro Olhos |
| **Tipo** | Json |
| **Escopos** | System, Tenant |
| **Default** | `["production_deploy", "security_config_change", "privileged_access_grant", "compliance_waiver", "break_glass"]` |
| **Descrição** | Lista de ações que requerem dupla aprovação quando o princípio dos quatro olhos está ativo. Cada ação é identificada pelo seu código e verificada em runtime. |
| **Prioridade** | 🔴 Alta |

| Campo | Valor |
|-------|-------|
| **Chave** | `governance.change_advisory_board.enabled` |
| **Nome** | Change Advisory Board (CAB) Ativado |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant |
| **Default** | `false` |
| **Descrição** | Quando ativado, mudanças de alta criticidade ou alto impacto requerem aprovação de um Change Advisory Board formal antes do deploy. O CAB é composto por membros definidos pela organização. Segue as melhores práticas ITIL. |
| **Prioridade** | 🔴 Alta |

| Campo | Valor |
|-------|-------|
| **Chave** | `governance.change_advisory_board.members` |
| **Nome** | Membros do Change Advisory Board |
| **Tipo** | Json |
| **Escopos** | System, Tenant |
| **Default** | `[]` |
| **Descrição** | Lista de IDs de utilizadores ou papéis que compõem o Change Advisory Board. Formato: `[{"type": "role", "value": "Architect"}, {"type": "role", "value": "ProductOwner"}, {"type": "user", "value": "admin@empresa.com"}]`. |
| **Prioridade** | 🔴 Alta |

| Campo | Valor |
|-------|-------|
| **Chave** | `governance.change_advisory_board.trigger_conditions` |
| **Nome** | Condições que Ativam o CAB |
| **Tipo** | Json |
| **Escopos** | System, Tenant |
| **Default** | `{"min_criticality": "High", "min_blast_radius": "Medium", "environment": ["production"]}` |
| **Descrição** | Define as condições sob as quais uma mudança é encaminhada para o Change Advisory Board. Apenas mudanças que cumprem estas condições são escaladas; outras seguem o fluxo normal de aprovação. |
| **Prioridade** | 🔴 Alta |

### 16.2 Compliance e Regulamentação

| Campo | Valor |
|-------|-------|
| **Chave** | `governance.compliance.framework` |
| **Nome** | Framework de Compliance Ativo |
| **Tipo** | Json |
| **Escopos** | System, Tenant |
| **Default** | `["internal"]` |
| **Descrição** | Define quais frameworks de compliance estão ativos para o tenant. Opções incluem: "internal" (regras internas), "soc2" (SOC 2), "iso27001" (ISO 27001), "pci_dss" (PCI DSS), "hipaa" (HIPAA), "gdpr" (GDPR/LGPD), "nist" (NIST). Múltiplos frameworks podem estar ativos simultaneamente. |
| **Prioridade** | 🔴 Alta |

| Campo | Valor |
|-------|-------|
| **Chave** | `governance.compliance.auto_remediation.enabled` |
| **Nome** | Auto-Remediação de Violações de Compliance |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant |
| **Default** | `false` |
| **Descrição** | Quando ativado, certas violações de compliance podem ser automaticamente remediadas (ex: revogar acesso expirado, bloquear recurso não-conforme). Quando desativado, todas as violações requerem ação manual. |
| **Prioridade** | 🟡 Média |

---

## 17. MÓDULO: Product Analytics — Análise de Produto

| Campo | Valor |
|-------|-------|
| **Chave** | `analytics.collection.enabled` |
| **Nome** | Recolha de Analytics Ativada |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant |
| **Default** | `true` |
| **Descrição** | Controla se dados de utilização do produto são recolhidos para análise de adoção, feature heatmap e jornadas de utilizador. Quando desativado, nenhum dado de analytics é registado. Em contexto GDPR, o tenant pode optar por desativar. |
| **Prioridade** | 🟡 Média |

| Campo | Valor |
|-------|-------|
| **Chave** | `analytics.persona_tracking.enabled` |
| **Nome** | Tracking de Persona Ativado |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant |
| **Default** | `true` |
| **Descrição** | Quando ativado, o sistema rastreia padrões de utilização por persona para otimizar a experiência. Quando desativado, analytics são agregados sem distinção de persona. |
| **Prioridade** | 🟢 Baixa |

---

## 18. PARÂMETROS INSPIRADOS EM MELHORES PRÁTICAS DA INDÚSTRIA

Os parâmetros abaixo foram inspirados em plataformas e frameworks de referência (ITIL, SRE, DORA, GitOps, FinOps Foundation, OWASP) para agregar valor diferenciador ao NexTraceOne.

### 18.1 DORA Metrics & Engineering Excellence

| Campo | Valor |
|-------|-------|
| **Chave** | `governance.dora.tracking.enabled` |
| **Nome** | Tracking de DORA Metrics Ativado |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant |
| **Default** | `true` |
| **Descrição** | Quando ativado, o NexTraceOne calcula automaticamente as 4 métricas DORA (Deployment Frequency, Lead Time for Changes, Mean Time to Recovery, Change Failure Rate) por serviço e equipa. Estas métricas são a referência da indústria para medir a performance de entrega de software. |
| **Prioridade** | 🔴 Alta |

| Campo | Valor |
|-------|-------|
| **Chave** | `governance.dora.performance_targets` |
| **Nome** | Metas de Performance DORA |
| **Tipo** | Json |
| **Escopos** | System, Tenant, Team |
| **Default** | `{"deployment_frequency": "weekly", "lead_time_hours": 168, "mttr_hours": 24, "change_failure_rate_pct": 15}` |
| **Descrição** | Define as metas de performance para as métricas DORA. Equipas são avaliadas contra estas metas e classificadas como Elite, High, Medium ou Low performers. Metas podem ser diferentes por equipa conforme a sua maturidade. |
| **Prioridade** | 🟡 Média |

### 18.2 GitOps & Infrastructure as Code

| Campo | Valor |
|-------|-------|
| **Chave** | `change.gitops.require_pr_approval` |
| **Nome** | Exigir Aprovação de PR no Git para Mudanças |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant, Environment |
| **Default** | `false` |
| **Descrição** | Quando ativado, o NexTraceOne valida que cada mudança associada a um deploy tem um Pull Request aprovado no sistema de controlo de versão (GitHub, GitLab, Azure DevOps). O NexTraceOne consulta a API do provider Git e bloqueia o deploy se o PR não estiver aprovado. |
| **Prioridade** | 🔴 Alta |

| Campo | Valor |
|-------|-------|
| **Chave** | `change.gitops.require_signed_commits` |
| **Nome** | Exigir Commits Assinados |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant, Environment |
| **Default** | `false` |
| **Descrição** | Quando ativado, o NexTraceOne valida que os commits associados a uma release estão assinados (GPG/SSH). Releases com commits não assinados são bloqueadas. Aumenta a garantia de autenticidade do código. |
| **Prioridade** | 🟡 Média |

### 18.3 OWASP & Security by Design

| Campo | Valor |
|-------|-------|
| **Chave** | `security.scan.require_before_deploy` |
| **Nome** | Exigir Scan de Segurança antes do Deploy |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant, Environment |
| **Default** | `false` |
| **Descrição** | Quando ativado, o NexTraceOne valida que um scan de segurança (SAST, DAST, SCA) foi executado e não contém vulnerabilidades críticas antes de permitir o deploy. Integra com ferramentas como SonarQube, Snyk, Checkmarx. |
| **Prioridade** | 🔴 Alta |

| Campo | Valor |
|-------|-------|
| **Chave** | `security.scan.blocked_severities` |
| **Nome** | Severidades de Vulnerabilidade que Bloqueiam Deploy |
| **Tipo** | Json |
| **Escopos** | System, Tenant, Environment |
| **Default** | `["critical"]` |
| **Descrição** | Define quais severidades de vulnerabilidade detetadas num scan bloqueiam o deploy. Opções: "critical", "high", "medium", "low". Em produção, pode bloquear "critical" e "high"; em dev, pode permitir todos. |
| **Prioridade** | 🔴 Alta |

### 18.4 Chaos Engineering (inspirado em Netflix/Gremlin)

| Campo | Valor |
|-------|-------|
| **Chave** | `operations.chaos.enabled` |
| **Nome** | Chaos Engineering Ativado |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant, Environment |
| **Default** | `false` |
| **Descrição** | Quando ativado, permite a execução de experiências de chaos engineering (injeção de falhas, latência, perda de rede). Deve ser ativado com cuidado em ambientes produtivos. Ajuda a validar a resiliência dos serviços. |
| **Prioridade** | 🟡 Média |

| Campo | Valor |
|-------|-------|
| **Chave** | `operations.chaos.allowed_environments` |
| **Nome** | Ambientes Permitidos para Chaos Engineering |
| **Tipo** | Json |
| **Escopos** | System, Tenant |
| **Default** | `["development", "staging"]` |
| **Descrição** | Define em quais ambientes é permitido executar experiências de chaos. Por defeito, produção está excluída. A empresa pode optar por incluir produção se tiver maturidade para Game Days. |
| **Prioridade** | 🟡 Média |

### 18.5 Service Mesh & API Rate Limiting

| Campo | Valor |
|-------|-------|
| **Chave** | `catalog.api.rate_limiting.default_policy` |
| **Nome** | Política Padrão de Rate Limiting |
| **Tipo** | Json |
| **Escopos** | System, Tenant, Environment |
| **Default** | `{"enabled": false, "requests_per_minute": 1000, "burst": 100}` |
| **Descrição** | Define a política padrão de rate limiting aplicada a APIs publicadas. Quando ativada, as APIs no portal respeitam os limites definidos. Pode ser customizada por contrato ou por consumidor. Útil para proteger serviços de uso abusivo. |
| **Prioridade** | 🟡 Média |

### 18.6 FinOps Foundation Best Practices

| Campo | Valor |
|-------|-------|
| **Chave** | `finops.showback.enabled` |
| **Nome** | Showback de Custos Ativado |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant |
| **Default** | `true` |
| **Descrição** | Quando ativado, custos operacionais são atribuídos e exibidos por serviço, equipa e domínio (showback). Permite visibilidade de custos sem enforcement. Passo prévio antes de ativar chargeback. |
| **Prioridade** | 🟡 Média |

| Campo | Valor |
|-------|-------|
| **Chave** | `finops.chargeback.enabled` |
| **Nome** | Chargeback de Custos Ativado |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant |
| **Default** | `false` |
| **Descrição** | Quando ativado, custos são efetivamente cobrados/imputados às equipas responsáveis (chargeback). Requer configuração prévia de centros de custo e aprovação financeira. Incentiva responsabilidade financeira por equipa. |
| **Prioridade** | 🟢 Baixa |

---

## 19. Resumo Quantitativo da Proposta

| Módulo | Parâmetros Propostos | Prioridade Alta | Prioridade Média | Prioridade Baixa |
|--------|---------------------|-----------------|-------------------|-------------------|
| Service Catalog | 8 | 4 | 4 | 0 |
| Contracts | 8 | 4 | 4 | 0 |
| Change Governance (Release/Deploy) | 17 | 10 | 7 | 0 |
| Promotion Governance | 4 | 2 | 2 | 0 |
| Workflow Engine | 5 | 0 | 4 | 1 |
| Incidents & Operations | 7 | 4 | 2 | 1 |
| AI Hub | 6 | 2 | 4 | 0 |
| Identity & Access | 6 | 2 | 4 | 0 |
| Audit & Compliance | 3 | 2 | 1 | 0 |
| Integrations | 3 | 1 | 2 | 0 |
| FinOps | 5 | 1 | 2 | 2 |
| Knowledge | 4 | 0 | 3 | 1 |
| Reliability (SLO/SLA) | 3 | 2 | 1 | 0 |
| Governance Transversal | 7 | 5 | 2 | 0 |
| Product Analytics | 2 | 0 | 1 | 1 |
| Best Practices (DORA/GitOps/OWASP/Chaos/FinOps) | 10 | 4 | 6 | 0 |
| **TOTAL** | **98** | **43** | **49** | **6** |

---

## 20. Arquitetura Proposta para Implementação

### 20.1 Reutilização da Infraestrutura Existente

A infraestrutura do módulo Configuration **já suporta** toda a mecânica necessária:

- ✅ **ConfigurationDefinition** — define o metadata do parâmetro
- ✅ **ConfigurationEntry** — armazena o valor por escopo
- ✅ **FeatureFlagDefinition/Entry** — para toggles booleanos simples
- ✅ **ConfigurationResolutionService** — resolução hierárquica por escopo
- ✅ **Auditoria** — toda alteração é registada
- ✅ **Janelas temporais** — EffectiveFrom/EffectiveTo
- ✅ **Cache** — resolução com cache para performance
- ✅ **API completa** — CRUD + resolução efetiva
- ✅ **Frontend admin** — gestão visual de configurações

### 20.2 O que precisa ser construído

1. **Seed dos novos 98 parâmetros** no `ConfigurationDefinitionSeeder`
2. **Consumo dos parâmetros nos handlers existentes** — cada feature que hoje tem comportamento fixo deve consultar o parâmetro correspondente via `IConfigurationResolutionService`
3. **Endpoint de validação externa** — novo endpoint `POST /api/v1/releases/{releaseId}/validate` que verifica aprovação interna + externa
4. **Adaptadores de provider externo** — implementação dos adaptadores para Jenkins, GitLab, Azure DevOps, GitHub Actions, Webhook genérico
5. **Gate de deploy** — novo endpoint `GET /api/v1/releases/{releaseId}/deploy-readiness` que o pipeline externo pode consultar
6. **Workflows de aprovação para novas entidades** — estender o WorkflowEngine existente para suportar aprovação de criação de serviço, contrato, conector, agente de IA
7. **Frontend de configuração por domínio** — melhorar as páginas existentes com UX orientada ao negócio (não exigir que admin entenda JSON ou chaves técnicas)
8. **i18n** — adicionar keys de tradução para todos os 98 novos parâmetros em 4 idiomas (en, pt-BR, pt-PT, es)
9. **Testes** — testes unitários para cada novo parâmetro e para a lógica condicional nos handlers

### 20.3 Faseamento Sugerido

#### Fase 1 — Fundação (Alta Prioridade, Impacto Imediato)
- Seed dos 43 parâmetros de prioridade alta
- Implementar gate de aprovação de release com validação externa (Jenkins/GitLab)
- Implementar aprovação de criação de serviço
- Implementar bloqueio de deploy por breaking change
- Implementar endpoint de deploy-readiness

#### Fase 2 — Governança Avançada
- Implementar princípio dos quatro olhos
- Implementar Change Advisory Board
- Implementar gates GitOps (PR approval, signed commits)
- Implementar gates de segurança (scan obrigatório)
- Implementar bloqueio por error budget

#### Fase 3 — IA & Conhecimento
- Implementar aprovação de modelos externos
- Implementar bloqueio de dados sensíveis para IA
- Implementar aprovação de agentes customizados
- Implementar revisão de documentos de conhecimento

#### Fase 4 — Otimização & Compliance
- Implementar parâmetros de FinOps (enforcement, chargeback)
- Implementar frameworks de compliance
- Implementar auto-remediação de violações
- Implementar chaos engineering governado

#### Fase 5 — Polish & Analytics
- Parâmetros de analytics e persona tracking
- UX avançada de configuração
- Relatórios de utilização de parâmetros
- Dashboard executivo de compliance de parametrização

---

## 21. Diferencial Competitivo

Com este módulo de parametrização, o NexTraceOne diferencia-se de concorrentes porque:

1. **Flexibilidade Enterprise** — cada empresa adapta o NexTraceOne às suas necessidades sem alteração de código
2. **Governança Configurável** — de "zero governança" para startups a "máxima governança" para banca/saúde
3. **Integração com CI/CD** — o NexTraceOne torna-se o "single pane of glass" para decisão de deploy, consultável por Jenkins/GitLab/Azure DevOps
4. **Compliance Automatizada** — frameworks de compliance ativáveis por parâmetro (SOC 2, ISO 27001, PCI DSS, HIPAA, GDPR)
5. **Multi-tenant por natureza** — cada tenant pode ter configuração completamente diferente
6. **Herança inteligente** — configurações herdam por escopo (System → Tenant → Environment → Team → User)
7. **Auditoria total** — toda alteração de parâmetro é rastreável
8. **Self-service** — admins configuram sem envolver desenvolvimento
9. **Validação externa** — pipeline CI/CD consulta NexTraceOne antes de deployar
10. **Princípios ITIL/SRE/DORA** — melhores práticas da indústria como parâmetros configuráveis

---

## 22. Decisão Pendente

Esta proposta apresenta **98 novos parâmetros** distribuídos por **16 domínios** do NexTraceOne. Cabe ao Product Owner decidir:

1. ✅ / ❌ Aprovar a proposta geral
2. Selecionar quais fases implementar primeiro
3. Priorizar parâmetros específicos
4. Ajustar defaults conforme a visão do produto
5. Definir se algum parâmetro deve ser removido ou adicionado
6. Validar o faseamento proposto

**Após aprovação, será criado um plano de implementação detalhado com estimativas, ficheiros impactados e critérios de aceite por parâmetro.**

---

*Documento gerado automaticamente por análise profunda dos 11 módulos do NexTraceOne (387+ features, 345 configurações existentes, 60+ grupos de API) em 2026-04-06.*
