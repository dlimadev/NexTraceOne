# Proposta: Módulo de Parametrização Avançada — NexTraceOne

**Data:** 2026-04-06  
**Autor:** Copilot AI (análise automatizada)  
**Versão:** 2.0 (atualizada com ressalvas do Product Owner)  
**Estado:** ✅ COMPLETO — Todas as 6 fases implementadas (458 seeds, 112 parâmetros, 63+ testes, 7 governance gates, 5 frontend pages, 2760+ translations)

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

## 14. MÓDULO: Knowledge → Operational Knowledge (RECLASSIFICADO)

> ⚠️ **NOTA v2.0**: Este módulo foi reclassificado e proposto para refatoração. 
> Os parâmetros originais foram substituídos por uma análise mais abrangente na **Secção 23** desta proposta.
> A secção 23 inclui: análise de sobreposição com outros módulos, proposta de renomeação para "Operational Knowledge", 
> novas categorias de documento, captura automática de conhecimento, pesquisa federada, e refatoração de frontend.
> **Ver Secção 23 para os parâmetros atualizados deste módulo.**

### 14.1 Parâmetros Mantidos (agora na Secção 23)

| Parâmetro Original | Status | Novo Parâmetro (Secção 23) |
|--------------------|---------|-----------------------------|
| `knowledge.document.review_required` | ✅ Mantido | `knowledge.operational_documents.enabled` |
| `knowledge.document.review_roles` | ✅ Mantido | (Integrado em roles dinâmicos - Secção 24) |
| `knowledge.document.staleness_alert_days` | ✅ Mantido | `knowledge.graph.max_depth` |
| `knowledge.auto_documentation.enabled` | ✅ Mantido | `knowledge.auto_capture.enabled` |
| (NOVO) | ➕ Adicionado | `knowledge.auto_capture.categories` |
| (NOVO) | ➕ Adicionado | `knowledge.search.federated.enabled` |
| (NOVO) | ➕ Adicionado | `knowledge.relations.auto_link.enabled` |

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

> ⚠️ **v2.0**: Tabela atualizada — ver Secção 28 para resumo completo incluindo adendas v2.0.

| Módulo | Parâmetros v1.0 | v2.0 Adicionados | Total | Prioridade Alta | Prioridade Média | Prioridade Baixa |
|--------|----------------|-----------------|-------|-----------------|-------------------|-------------------|
| Service Catalog | 8 | 0 | 8 | 4 | 4 | 0 |
| Contracts | 8 | 0 | 8 | 4 | 4 | 0 |
| Change Governance (Release/Deploy) | 17 | 0 | 17 | 10 | 7 | 0 |
| Promotion Governance | 4 | 0 | 4 | 2 | 2 | 0 |
| Workflow Engine | 5 | 0 | 5 | 0 | 4 | 1 |
| Incidents & Operations | 7 | 0 | 7 | 4 | 2 | 1 |
| AI Hub | 6 | 0 | 6 | 2 | 4 | 0 |
| Identity & Access | 6 | 3 | **9** | 3 | 5 | 1 |
| Audit & Compliance | 3 | 0 | 3 | 2 | 1 | 0 |
| Integrations | 3 | 0 | 3 | 1 | 2 | 0 |
| FinOps | 5 | 0 | 5 | 1 | 2 | 2 |
| ~~Knowledge~~ **Operational Knowledge** | ~~4~~ | 6 | **6** | 2 | 3 | 1 |
| Reliability (SLO/SLA) | 3 | 0 | 3 | 2 | 1 | 0 |
| Governance Transversal | 7 | 0 | 7 | 5 | 2 | 0 |
| Product Analytics | 2 | 0 | 2 | 0 | 1 | 1 |
| Best Practices (DORA/GitOps/OWASP/Chaos/FinOps) | 10 | 0 | 10 | 4 | 6 | 0 |
| **Platform Customization** (NOVO v2.0) | 0 | **9** | **9** | 3 | 3 | 3 |
| **TOTAL** | **98** | **18** | **112** | **49** | **53** | **10** |

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

> ⚠️ **v2.0**: Lista atualizada com itens adicionais. Ver Secção 29 para faseamento detalhado.

1. **Seed dos novos 112 parâmetros** no `ConfigurationDefinitionSeeder` (com chaves i18n)
2. **i18n para todos os parâmetros** — ficheiros de tradução em 4 idiomas (en, pt-BR, pt-PT, es)
3. **Consumo dos parâmetros nos handlers existentes** — cada feature que hoje tem comportamento fixo deve consultar o parâmetro correspondente via `IConfigurationResolutionService` (100% funcional)
4. **Endpoint de validação externa** — novo endpoint `POST /api/v1/releases/{releaseId}/validate` que verifica aprovação interna + externa
5. **Adaptadores de provider externo** — implementação dos adaptadores para Jenkins, GitLab, Azure DevOps, GitHub Actions, Webhook genérico
6. **Gate de deploy** — novo endpoint `GET /api/v1/releases/{releaseId}/deploy-readiness` que o pipeline externo pode consultar
7. **Workflows de aprovação para novas entidades** — estender o WorkflowEngine existente para suportar aprovação de criação de serviço, contrato, conector, agente de IA
8. **CRUD de Roles Customizados** — endpoints para criar, editar e remover roles por tenant (v2.0)
9. **Componente `<RolePicker>`** — multi-select de roles para parâmetros que referenciam roles (v2.0)
10. **User Preferences API** — endpoints para guardar preferências de sidebar, home, widgets por utilizador (v2.0)
11. **Sidebar/Home customizável** — frontend para drag-and-drop de itens do sidebar e widgets do home (v2.0)
12. **Refatoração Knowledge → Operational Knowledge** — reclassificação, novas categorias, pesquisa federada (v2.0)
13. **Frontend de configuração por domínio** — melhorar as páginas existentes com UX orientada ao negócio
14. **Testes** — testes unitários para cada parâmetro (enabled + disabled + scope inheritance)

### 20.3 Faseamento Sugerido

> ⚠️ **v2.0**: Faseamento completo e atualizado na **Secção 29**. A secção abaixo é o faseamento original v1.0 para referência.

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

## 22. Decisão — ✅ APROVADO COM RESSALVAS

> **Estado**: Proposta v1.0 aprovada pelo Product Owner em 2026-04-06 com as seguintes ressalvas, que foram endereçadas na v2.0:

| # | Ressalva | Status | Secção |
|---|----------|--------|--------|
| 1 | Módulo Knowledge está confuso, precisa reclassificação | ✅ Endereçado | Secção 23 |
| 2 | Roles/Perfis devem ser configuráveis pelo utilizador | ✅ Endereçado | Secção 24 |
| 3 | Utilizador deve poder customizar a plataforma (dashboard, sidebar) | ✅ Endereçado | Secção 25 |
| 4 | Seeds devem respeitar i18n (por idioma) | ✅ Endereçado | Secção 26 |
| 5 | Parâmetros devem ser 100% funcionais (enforced no código) | ✅ Endereçado | Secção 27 |

**Próximo passo**: Criar plano de implementação detalhado seguindo o faseamento da Secção 29.

---

# ADENDA v2.0 — Ressalvas do Product Owner (Aprovação Condicional)

> As secções abaixo foram adicionadas após aprovação condicional pelo Product Owner, que identificou as seguintes necessidades adicionais:
> 1. Reclassificação e possível refatoração do módulo Knowledge
> 2. Roles/Perfis devem ser configuráveis pelo utilizador, não hardcoded
> 3. Customização da plataforma pelo utilizador (dashboards, sidebar, layout)
> 4. Seeds com suporte a i18n (por idioma)
> 5. Parâmetros devem ser 100% funcionais (não apenas seedados, mas enforced no código)

---

## 23. ANÁLISE DO MÓDULO KNOWLEDGE — Reclassificação e Refatoração

### 23.1 Diagnóstico Atual

O módulo Knowledge (`NexTraceOne.Knowledge`) é atualmente um módulo isolado que gere:
- **KnowledgeDocument** — Documentos operacionais (runbooks, troubleshooting, post-mortems, procedimentos)
- **OperationalNote** — Notas contextuais ligadas a serviços, incidentes, mudanças
- **KnowledgeRelation** — Relações polimórficas entre documentos/notas e entidades de outros módulos

### 23.2 Problema Identificado

O Product Owner identificou corretamente que **quase tudo que é gerado no NexTraceOne é base de conhecimento**:

| Módulo | Gera conhecimento? | Exemplos |
|--------|-------------------|----------|
| **Catalog** | ✅ Sim | Contratos API, scorecards, topologia, documentação de serviços |
| **ChangeGovernance** | ✅ Sim | Evidence packs, post-release reviews, changelogs, decisões |
| **OperationalIntelligence** | ✅ Sim | Post-incident reviews, root cause analysis, runbooks operacionais |
| **AIKnowledge** | ✅ Sim | Conversas de IA, artefactos gerados, knowledge captures |
| **AuditCompliance** | ✅ Sim | Relatórios de compliance, auditorias, evidências |
| **Governance** | ✅ Sim | Políticas, waivers, compliance gaps, scorecards |
| **Knowledge** | ✅ Sim | Documentos, notas operacionais, relações |

**Conclusão**: O módulo Knowledge **sobrepõe-se conceptualmente** com funcionalidades de outros módulos. A sua razão de existir como módulo independente é questionável.

### 23.3 Análise de Dependências do Módulo Knowledge

**Quem consome o Knowledge:**
1. **Catalog** — `IKnowledgeModule.CountDocumentsByServiceAsync()` (para Service Scorecard)
2. **AIKnowledge** — `IKnowledgeSearchProvider.SearchAsync()` (para grounding de IA)
3. **AIKnowledge** — `DocumentRetrievalService` (recupera documentos como contexto LLM)

**O que é único do Knowledge:**
- Documentos operacionais com lifecycle (Draft → Published → Archived → Deprecated)
- Notas operacionais com severidade e contexto flexível
- Relações polimórficas entre entidades de módulos diferentes
- Full-text search transversal
- Knowledge Graph visualization

### 23.4 Proposta de Reclassificação

**Recomendação: REFATORAR, não eliminar.**

O Knowledge module deve ser **reclassificado e refatorado** da seguinte forma:

#### Opção Recomendada: Transformar em "Operational Knowledge Layer"

Em vez de ser um módulo ao mesmo nível dos outros, o Knowledge passa a ser uma **camada transversal** (cross-cutting concern), similar ao que o módulo Configuration é para configurações.

**Estrutura proposta:**

```
NexTraceOne.Knowledge (renomear para NexTraceOne.OperationalKnowledge)
├── Domain/
│   ├── Entities/
│   │   ├── OperationalDocument.cs      (renomeado de KnowledgeDocument)
│   │   ├── OperationalNote.cs          (mantido)
│   │   └── EntityRelation.cs           (renomeado de KnowledgeRelation, mais genérico)
│   └── Enums/
│       ├── DocumentCategory.cs         (estender: + ApiDocumentation, + ChangeLog, + ComplianceReport)
│       └── RelationType.cs             (estender: + PolicyWaiver, + AuditCampaign, + Budget)
├── Application/
│   ├── Features/                       (manter features existentes)
│   └── Abstractions/
│       └── IOperationalKnowledgeService.cs  (interface enriquecida)
└── Contracts/
    └── OperationalKnowledgeContracts.cs     (contrato público para outros módulos)
```

**Mudanças chave:**
1. **Renomear** `Knowledge` → `OperationalKnowledge` para distinguir de "knowledge" genérico
2. **Enriquecer categorias** — as categorias de documento devem refletir todos os tipos de conhecimento do NexTraceOne
3. **Auto-link** — outros módulos podem automaticamente criar documentos/notas no OperationalKnowledge quando geram conhecimento (ex: post-incident review → cria documento; compliance report → cria documento)
4. **Search Federation** — o Knowledge search deve federar resultados de todos os módulos, não apenas dos seus documentos

**Novas categorias de documento propostas:**

| Categoria | Origem Típica | Descrição |
|-----------|---------------|-----------|
| General | Manual | Documentação geral |
| Runbook | Manual / OperationalIntelligence | Procedimentos operacionais |
| Troubleshooting | Manual / OperationalIntelligence | Guias de resolução de problemas |
| Architecture | Manual / AIKnowledge | Documentação de arquitetura |
| Procedure | Manual | Procedimentos padrão |
| PostMortem | OperationalIntelligence | Análise pós-incidente |
| Reference | Manual | Materiais de referência |
| **ApiDocumentation** | **Catalog** (NOVO) | Documentação gerada de APIs/contratos |
| **ChangeLog** | **ChangeGovernance** (NOVO) | Histórico de mudanças |
| **ComplianceEvidence** | **AuditCompliance** (NOVO) | Evidências de compliance |
| **DecisionRecord** | **AIKnowledge** (NOVO) | Architecture Decision Records |
| **IncidentAnalysis** | **OperationalIntelligence** (NOVO) | Análise de incidentes |
| **OperationalPlaybook** | **OperationalIntelligence** (NOVO) | Playbooks de operação |

### 23.5 Frontend do Knowledge — Refatoração

**Estado atual:** 5 páginas dedicadas em `/features/knowledge/`
- KnowledgeHubPage, KnowledgeDocumentPage, OperationalNotesPage, KnowledgeGraphPage, AutoDocumentationPage

**Proposta de refatoração frontend:**

1. **KnowledgeHubPage** → Mantém como "Centro de Conhecimento Operacional" mas com filtros multi-origem
2. **KnowledgeDocumentPage** → Mantém (visualização de documentos)
3. **OperationalNotesPage** → Move como painel contextual nos módulos de origem (ex: notas de incidente ficam no detalhe do incidente)
4. **KnowledgeGraphPage** → Mantém mas renomeia para "Mapa de Relações" e mostra relações entre TODOS os tipos de entidade
5. **AutoDocumentationPage** → Move para dentro do detalhe de serviço no Catalog

### 23.6 Parâmetros Relacionados ao Knowledge (Substituem Secção 14)

| Campo | Valor |
|-------|-------|
| **Chave** | `knowledge.operational_documents.enabled` |
| **Nome** | Documentos Operacionais Ativados |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant |
| **Default** | `true` |
| **Descrição** | Controla se a funcionalidade de documentos operacionais está ativa. Quando desativado, o módulo de conhecimento operacional fica em modo read-only (documentos existentes são visíveis mas não é possível criar novos). |
| **Prioridade** | 🟡 Média |

| Campo | Valor |
|-------|-------|
| **Chave** | `knowledge.auto_capture.enabled` |
| **Nome** | Captura Automática de Conhecimento |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant |
| **Default** | `true` |
| **Descrição** | Quando ativado, eventos operacionais significativos (post-incident reviews, compliance reports, architecture decisions, changelogs) são automaticamente capturados como documentos operacionais com a categoria correspondente. Permite construir base de conhecimento sem esforço manual. |
| **Prioridade** | 🔴 Alta |

| Campo | Valor |
|-------|-------|
| **Chave** | `knowledge.auto_capture.categories` |
| **Nome** | Categorias de Captura Automática |
| **Tipo** | Json |
| **Escopos** | System, Tenant |
| **Default** | `["PostMortem", "ComplianceEvidence", "DecisionRecord", "ChangeLog"]` |
| **Descrição** | Define quais categorias de documento são automaticamente capturadas. A empresa pode desativar categorias específicas se não quiser que certos tipos de conhecimento sejam auto-capturados. |
| **Prioridade** | 🟡 Média |

| Campo | Valor |
|-------|-------|
| **Chave** | `knowledge.search.federated.enabled` |
| **Nome** | Pesquisa Federada de Conhecimento |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant |
| **Default** | `true` |
| **Descrição** | Quando ativado, a pesquisa no Knowledge Hub retorna resultados de todos os módulos do NexTraceOne (contratos, incidentes, runbooks, policies), não apenas documentos explicitamente criados no Knowledge. Proporciona uma visão unificada de todo o conhecimento da organização. |
| **Prioridade** | 🔴 Alta |

| Campo | Valor |
|-------|-------|
| **Chave** | `knowledge.relations.auto_link.enabled` |
| **Nome** | Linking Automático de Conhecimento |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant |
| **Default** | `true` |
| **Descrição** | Quando ativado, o sistema cria automaticamente relações entre entidades quando contexto relevante é detetado. Exemplo: um runbook que menciona um serviço é automaticamente ligado a esse serviço. |
| **Prioridade** | 🟡 Média |

| Campo | Valor |
|-------|-------|
| **Chave** | `knowledge.graph.max_depth` |
| **Nome** | Profundidade Máxima do Grafo de Conhecimento |
| **Tipo** | Integer |
| **Escopos** | System, Tenant |
| **Default** | `3` |
| **Descrição** | Profundidade máxima de traversal no grafo de conhecimento. Valores maiores mostram mais relações mas podem ser mais lentos. Intervalo: 1-5. |
| **Prioridade** | 🟢 Baixa |

---

## 24. ROLES E PERFIS CONFIGURÁVEIS PELO UTILIZADOR

### 24.1 Problema Identificado

Os perfis/roles hoje são parcialmente hardcoded:
- **Backend**: 7 roles estáticos (`PlatformAdmin`, `TechLead`, `Developer`, `Viewer`, `Auditor`, `SecurityReview`, `ApprovalOnly`)
- **Frontend**: 7 personas estáticas (`Engineer`, `TechLead`, `Architect`, `Product`, `Executive`, `PlatformAdmin`, `Auditor`)
- **Parâmetros**: Usam nomes de roles hardcoded como `["TechLead", "Architect"]`

Nos parâmetros de aprovação (ex: `catalog.service.creation.approval_roles`), os defaults referem roles como `["TechLead", "Architect", "ProductOwner"]` — mas estes devem ser **referências a roles cadastrados** pelo utilizador, não strings fixas.

### 24.2 Proposta: Sistema de Roles Dinâmicos

#### Backend

O sistema já suporta parcialmente roles customizados:
- `Role.IsSystem` distingue roles do sistema de roles customizados
- `RolePermission` permite customização de permissões por tenant
- `SeedDefaultRolePermissions` popula roles iniciais no banco

**O que precisa ser construído:**

1. **CRUD de Roles Customizados**
   - `POST /api/v1/identity/roles` — Criar role customizado
   - `PUT /api/v1/identity/roles/{roleId}` — Atualizar role
   - `DELETE /api/v1/identity/roles/{roleId}` — Remover role (se não tiver utilizadores)
   - `GET /api/v1/identity/roles` — Listar roles (com permissões)
   - `POST /api/v1/identity/roles/{roleId}/permissions` — Atribuir permissões

2. **Resolução Dinâmica de Roles nos Parâmetros**
   - Em vez de `["TechLead", "Architect"]`, os parâmetros devem usar `["role:TechLead", "role:Architect"]` ou IDs de role
   - O sistema deve validar que os roles referenciados existem quando o parâmetro é alterado
   - Se um role é removido, parâmetros que o referenciam devem ser atualizados

#### Frontend

1. **Página de Gestão de Roles** (`/admin/roles`)
   - Lista de roles existentes (sistema + customizados)
   - Criação de novos roles com seleção de permissões
   - Edição de permissões de roles existentes (customizados)
   - Indicação visual de quais roles são do sistema (não editáveis na essência) vs customizados
   - Preview de "o que este role pode fazer" com lista de módulos/ações

2. **Seletor de Roles nos Parâmetros**
   - Quando um parâmetro é do tipo "roles" (Json com array de roles), o editor deve mostrar um **multi-select com os roles disponíveis** em vez de um campo de texto JSON livre
   - Validação em tempo real de roles selecionados

### 24.3 Parâmetros Relacionados a Roles

| Campo | Valor |
|-------|-------|
| **Chave** | `identity.roles.custom_creation.enabled` |
| **Nome** | Criação de Roles Customizados Ativada |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant |
| **Default** | `true` |
| **Descrição** | Controla se tenants podem criar roles customizados além dos 7 roles de sistema. Quando desativado, apenas os roles pré-definidos estão disponíveis. |
| **Prioridade** | 🔴 Alta |

| Campo | Valor |
|-------|-------|
| **Chave** | `identity.roles.max_custom_roles` |
| **Nome** | Número Máximo de Roles Customizados |
| **Tipo** | Integer |
| **Escopos** | System, Tenant |
| **Default** | `20` |
| **Descrição** | Limita o número de roles customizados que um tenant pode criar. Previne proliferação excessiva de roles que dificulte a gestão. |
| **Prioridade** | 🟡 Média |

| Campo | Valor |
|-------|-------|
| **Chave** | `identity.roles.require_description` |
| **Nome** | Exigir Descrição ao Criar Role |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant |
| **Default** | `true` |
| **Descrição** | Quando ativado, todos os roles customizados devem ter uma descrição que explique o propósito do role. Melhora a governança de acessos. |
| **Prioridade** | 🟢 Baixa |

### 24.4 Revisão de Todos os Parâmetros que Referenciam Roles

**IMPORTANTE**: Todos os parâmetros propostos na v1.0 que usam roles fixos devem ser atualizados:

| Parâmetro | Default v1.0 | Default v2.0 (corrigido) | Mudança |
|-----------|-------------|-------------------------|---------|
| `catalog.service.creation.approval_roles` | `["TechLead", "Architect", "PlatformAdmin"]` | `["TechLead", "Architect", "PlatformAdmin"]` | Valor é **referência a roles cadastrados**, não strings livres. UI deve mostrar role picker. |
| `catalog.contract.creation.approval_roles` | `["Architect", "TechLead"]` | `["Architect", "TechLead"]` | Idem — role picker no frontend |
| `catalog.contract.breaking_change.override_roles` | `["Architect", "PlatformAdmin"]` | `["Architect", "PlatformAdmin"]` | Idem |
| `change.release.approval_roles` | `["TechLead", "Architect", "ProductOwner"]` | `["TechLead", "Architect"]` | Removido "ProductOwner" (não é role do sistema). Se a empresa quiser, cria role customizado. |
| `governance.four_eyes_principle.actions` | (lista de ações) | (mantido) | Não referencia roles |
| `governance.change_advisory_board.members` | `[]` | `[]` | Formato: `[{"type": "role", "value": "<roleId>"}, ...]` — UI com role/user picker |
| `workflow.approvers.policy` | `"ByOwnership"` | `"ByOwnership"` | Mantido — ownership é dinâmico |
| `workflow.escalation.target_roles` | `["PlatformAdmin", "Architect"]` | `["PlatformAdmin", "Architect"]` | Role picker |
| `security.break_glass.require_dual_approval` | boolean | boolean | Não referencia roles diretamente |
| `knowledge.document.review_roles` | `["TechLead", "Architect"]` | `["TechLead", "Architect"]` | Role picker |

**Regra de implementação**: O frontend deve usar um componente **`<RolePicker>`** sempre que um parâmetro contém um array de roles. Este componente:
- Lista todos os roles disponíveis (sistema + customizados do tenant)
- Permite seleção múltipla
- Valida que roles selecionados existem
- Mostra preview de permissões por role

---

## 25. CUSTOMIZAÇÃO DA PLATAFORMA PELO UTILIZADOR

### 25.1 Problema Identificado

O Product Owner identificou que ter telas diferentes por persona pode não fazer sentido se já temos (ou devemos ter) dashboards customizados. O utilizador deve poder organizar a plataforma como quiser.

### 25.2 Proposta: User Workspace Customization

#### 25.2.1 Sidebar Customizável

| Campo | Valor |
|-------|-------|
| **Chave** | `platform.sidebar.user_customization.enabled` |
| **Nome** | Customização de Sidebar pelo Utilizador |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant |
| **Default** | `true` |
| **Descrição** | Quando ativado, cada utilizador pode reordenar, ocultar e fixar itens no sidebar de navegação. A ordem personalizada é salva no perfil do utilizador. Quando desativado, todos os utilizadores veem a mesma ordem (definida pelo persona/role). |
| **Prioridade** | 🔴 Alta |

| Campo | Valor |
|-------|-------|
| **Chave** | `platform.sidebar.pinned_items.max` |
| **Nome** | Número Máximo de Itens Fixados no Sidebar |
| **Tipo** | Integer |
| **Escopos** | System, Tenant |
| **Default** | `10` |
| **Descrição** | Limite de itens que um utilizador pode fixar como favoritos no topo do sidebar. |
| **Prioridade** | 🟢 Baixa |

#### 25.2.2 Home Dashboard Customizável

| Campo | Valor |
|-------|-------|
| **Chave** | `platform.home.user_customization.enabled` |
| **Nome** | Home Dashboard Customizável pelo Utilizador |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant |
| **Default** | `true` |
| **Descrição** | Quando ativado, cada utilizador pode customizar o seu home dashboard: adicionar, remover, reordenar e redimensionar widgets. Quando desativado, o home dashboard é determinado pelo persona/role do utilizador. |
| **Prioridade** | 🔴 Alta |

| Campo | Valor |
|-------|-------|
| **Chave** | `platform.home.default_layout` |
| **Nome** | Layout Padrão do Home Dashboard |
| **Tipo** | String |
| **Escopos** | System, Tenant, Role |
| **Default** | `"two-column"` |
| **Descrição** | Layout padrão para o home dashboard quando o utilizador ainda não customizou. Opções: "single-column", "two-column", "three-column", "grid". Pode ser diferente por role — ex: Executive com "single-column" focado, Engineer com "three-column" rico em dados. |
| **Prioridade** | 🟡 Média |

| Campo | Valor |
|-------|-------|
| **Chave** | `platform.home.available_widgets` |
| **Nome** | Widgets Disponíveis para o Home Dashboard |
| **Tipo** | Json |
| **Escopos** | System, Tenant, Role |
| **Default** | `["team-services", "change-risk", "incident-overview", "slo-status", "pending-approvals", "recent-changes", "contract-health", "dora-metrics", "finops-summary", "ai-insights", "compliance-status", "reliability-trend"]` |
| **Descrição** | Lista de widgets que podem ser adicionados ao home dashboard. A empresa pode restringir quais widgets estão disponíveis por role — ex: widgets de FinOps podem ser restritos a TechLead e acima. Cada widget é identificado por um ID e deve estar registado no catálogo de widgets. |
| **Prioridade** | 🔴 Alta |

| Campo | Valor |
|-------|-------|
| **Chave** | `platform.home.max_widgets` |
| **Nome** | Número Máximo de Widgets no Home |
| **Tipo** | Integer |
| **Escopos** | System, Tenant |
| **Default** | `12` |
| **Descrição** | Limite de widgets que um utilizador pode adicionar ao seu home dashboard. Previne dashboards excessivamente carregados. |
| **Prioridade** | 🟢 Baixa |

#### 25.2.3 Quick Actions Customizáveis

| Campo | Valor |
|-------|-------|
| **Chave** | `platform.quick_actions.user_customization.enabled` |
| **Nome** | Quick Actions Customizáveis pelo Utilizador |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant |
| **Default** | `true` |
| **Descrição** | Quando ativado, cada utilizador pode escolher quais quick actions aparecem no topo do dashboard e na command palette. Quando desativado, quick actions são determinados pelo persona. |
| **Prioridade** | 🟡 Média |

#### 25.2.4 Custom Dashboards (Evolução)

| Campo | Valor |
|-------|-------|
| **Chave** | `platform.custom_dashboards.enabled` |
| **Nome** | Dashboards Customizados Ativados |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant |
| **Default** | `true` |
| **Descrição** | Quando ativado, utilizadores podem criar dashboards completamente customizados além do home dashboard. Podem combinar widgets de diferentes módulos, configurar layouts e partilhar com a equipa. |
| **Prioridade** | 🔴 Alta |

| Campo | Valor |
|-------|-------|
| **Chave** | `platform.custom_dashboards.sharing.enabled` |
| **Nome** | Partilha de Dashboards Customizados |
| **Tipo** | Boolean |
| **Escopos** | System, Tenant |
| **Default** | `true` |
| **Descrição** | Quando ativado, dashboards customizados podem ser partilhados com outros utilizadores, equipas ou toda a organização. Quando desativado, dashboards são privados. |
| **Prioridade** | 🟡 Média |

| Campo | Valor |
|-------|-------|
| **Chave** | `platform.custom_dashboards.max_per_user` |
| **Nome** | Número Máximo de Dashboards por Utilizador |
| **Tipo** | Integer |
| **Escopos** | System, Tenant |
| **Default** | `10` |
| **Descrição** | Limite de dashboards customizados que cada utilizador pode criar. |
| **Prioridade** | 🟢 Baixa |

### 25.3 Implementação Backend: User Preferences

Para suportar a customização, é necessário um novo sub-domínio no IdentityAccess ou Configuration:

**Novo endpoint proposto:**
- `GET /api/v1/identity/me/preferences` — Obter preferências do utilizador atual
- `PUT /api/v1/identity/me/preferences` — Atualizar preferências
- `GET /api/v1/identity/me/preferences/{key}` — Obter preferência específica
- `PUT /api/v1/identity/me/preferences/{key}` — Atualizar preferência específica

**Chaves de preferência do utilizador:**
```json
{
  "sidebar.order": ["home", "services", "changes", "contracts", "operations"],
  "sidebar.pinned": ["services", "changes"],
  "sidebar.hidden": ["analytics"],
  "home.layout": "two-column",
  "home.widgets": [
    {"id": "team-services", "position": {"row": 0, "col": 0, "width": 1, "height": 1}},
    {"id": "change-risk", "position": {"row": 0, "col": 1, "width": 1, "height": 1}}
  ],
  "quick_actions": ["create-service", "review-changes", "check-incidents"],
  "theme": "system",
  "language": "pt-BR"
}
```

### 25.4 Relação Persona vs Customização

A persona **não desaparece** — ela passa a ser o **default inicial** que o utilizador pode depois customizar:

```
┌─────────────────────────────────────────────────────────┐
│ Resolução de UX do Utilizador                            │
│                                                          │
│ 1. Verificar preferências do utilizador (se existirem)   │
│    → Se sim, usar preferências customizadas              │
│ 2. Se não, verificar configuração do Role/Persona        │
│    → Usar defaults do persona config                     │
│ 3. Se nenhum, usar defaults do sistema                   │
│    → Layout padrão genérico                              │
└─────────────────────────────────────────────────────────┘
```

---

## 26. ESTRATÉGIA DE SEED COM i18n (POR IDIOMA)

### 26.1 Problema Atual

Atualmente, os seeds de `ConfigurationDefinition` usam textos em **inglês hardcoded** no `displayName` e `description`. O NexTraceOne suporta 4 idiomas (en, pt-BR, pt-PT, es) mas os labels das configurações aparecem sempre em inglês.

### 26.2 Proposta: Seed Bilíngue com i18n Key Reference

#### Abordagem Recomendada: Chaves i18n no Seed + Ficheiros de Tradução

**No seed (backend):**
```csharp
ConfigurationDefinition.Create(
    key: "catalog.service.creation.approval_required",
    displayName: "config.catalog.service.creation.approval_required.label",  // ← chave i18n
    description: "config.catalog.service.creation.approval_required.description",  // ← chave i18n
    // ...
)
```

**Nos ficheiros de locale (frontend):**

**en.json:**
```json
{
  "config": {
    "catalog": {
      "service": {
        "creation": {
          "approval_required": {
            "label": "Service Creation Approval Required",
            "description": "When enabled, creating new services requires approval before becoming active."
          }
        }
      }
    }
  }
}
```

**pt-BR.json:**
```json
{
  "config": {
    "catalog": {
      "service": {
        "creation": {
          "approval_required": {
            "label": "Aprovação Obrigatória para Criação de Serviço",
            "description": "Quando ativado, a criação de novos serviços requer aprovação antes de ficarem ativos."
          }
        }
      }
    }
  }
}
```

**pt-PT.json:**
```json
{
  "config": {
    "catalog": {
      "service": {
        "creation": {
          "approval_required": {
            "label": "Aprovação Obrigatória para Criação de Serviço",
            "description": "Quando ativado, a criação de novos serviços requer aprovação antes de ficarem ativos."
          }
        }
      }
    }
  }
}
```

**es.json:**
```json
{
  "config": {
    "catalog": {
      "service": {
        "creation": {
          "approval_required": {
            "label": "Aprobación Obligatoria para Creación de Servicio",
            "description": "Cuando está activado, la creación de nuevos servicios requiere aprobación antes de estar activos."
          }
        }
      }
    }
  }
}
```

### 26.3 Resolução no Frontend

```typescript
// Componente de configuração
function ConfigurationLabel({ definition }: { definition: ConfigurationDefinition }) {
  const { t } = useTranslation();
  
  // Tenta resolver como chave i18n; se falhar, usa displayName como fallback
  const label = t(definition.displayName, { defaultValue: definition.displayName });
  const description = t(definition.description, { defaultValue: definition.description });
  
  return (
    <div>
      <h3>{label}</h3>
      <p>{description}</p>
    </div>
  );
}
```

### 26.4 Plano de Migração dos Seeds Existentes

1. **Fase 1**: Manter seeds existentes (345) com texto em inglês (backward compatible)
2. **Fase 2**: Novos seeds (98+) usam chaves i18n desde o início
3. **Fase 3**: Migrar seeds existentes para chaves i18n (batch update)
4. **Fase 4**: Gerar ficheiros de tradução para todos os 443+ seeds em 4 idiomas

### 26.5 Ferramenta de Geração de i18n

Para garantir consistência, propõe-se um script utilitário:

```bash
# Gera ficheiros i18n a partir do seeder
dotnet run --project tools/NexTraceOne.Tools.I18nSeedGenerator -- \
  --seeder-path src/modules/configuration/NexTraceOne.Configuration.Infrastructure/Seed/ConfigurationDefinitionSeeder.cs \
  --output-dir src/frontend/src/locales/ \
  --languages en,pt-BR,pt-PT,es
```

---

## 27. ENFORCEMENT 100% FUNCIONAL — PLANO DE IMPLEMENTAÇÃO

### 27.1 Princípio

**Cada parâmetro deve ser 100% funcional.** Não basta criar o seed — o código que executa a funcionalidade DEVE consultar o parâmetro e aplicar o comportamento configurado.

### 27.2 Padrão de Implementação

Cada parâmetro segue o mesmo padrão de implementação:

```
1. SEED → ConfigurationDefinitionSeeder (definição + default)
2. i18n → Ficheiros de locale (4 idiomas: en, pt-BR, pt-PT, es)
3. HANDLER → Feature handler consulta IConfigurationResolutionService
4. GATE → Lógica condicional baseada no valor do parâmetro
5. FRONTEND → UI reflete o estado do parâmetro
6. TEST → Testes unitários para ambos os estados (ativado/desativado)
```

### 27.3 Checklist de Implementação por Parâmetro

Para CADA um dos 98+ parâmetros, a implementação deve incluir:

#### Backend
- [x] **Seed**: `ConfigurationDefinition.Create(...)` no `ConfigurationDefinitionSeeder`
- [x] **i18n key**: `displayName` e `description` como chaves i18n (não texto hardcoded)
- [x] **Handler modification**: O handler que executa a funcionalidade deve:
  ```csharp
  // Injetar IConfigurationResolutionService
  private readonly IConfigurationResolutionService _configService;
  
  // No handler:
  var approvalRequired = await _configService.ResolveEffectiveValueAsync(
      "catalog.service.creation.approval_required",
      ConfigurationScope.Tenant,
      tenantId.ToString(),
      cancellationToken);
  
  if (approvalRequired?.EffectiveValue == "true")
  {
      // Fluxo com aprovação
  }
  else
  {
      // Fluxo direto (sem aprovação)
  }
  ```
- [x] **Validação**: Se o parâmetro tem validationRules, garantir que são verificadas ao guardar
- [x] **Auditoria**: Alterações ao parâmetro são registadas via `ConfigurationAuditEntry`

#### Frontend
- [x] **i18n**: Chaves de tradução em 4 ficheiros de locale
- [x] **Config page**: Parâmetro visível na página de configuração do domínio correspondente
- [x] **UX reflection**: A UI muda de acordo com o valor do parâmetro (ex: se approval_required=false, o botão "Submit for Approval" não aparece)
- [x] **Role picker**: Se parâmetro contém roles, usar componente `<RolePicker>`

#### Testes
- [x] **Test when enabled**: Teste unitário com parâmetro ativado
- [x] **Test when disabled**: Teste unitário com parâmetro desativado
- [x] **Test scope inheritance**: Teste de resolução hierárquica (Tenant override de System)
- [x] **Test edge cases**: Parâmetro inexistente, valor inválido, etc.

### 27.4 Exemplo Completo: `catalog.service.creation.approval_required`

#### 1. Seed
```csharp
// ConfigurationDefinitionSeeder.cs
ConfigurationDefinition.Create(
    key: "catalog.service.creation.approval_required",
    displayName: "config.catalog.service.creation.approval_required.label",
    category: ConfigurationCategory.Functional,
    valueType: ConfigurationValueType.Boolean,
    allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
    description: "config.catalog.service.creation.approval_required.description",
    defaultValue: "false",
    uiEditorType: "toggle",
    sortOrder: 500)
```

#### 2. i18n (4 idiomas)
```json
// en.json
"config.catalog.service.creation.approval_required.label": "Service Creation Approval Required",
"config.catalog.service.creation.approval_required.description": "When enabled, new services require approval before becoming visible in the catalog. When disabled, services are created directly in active state."

// pt-BR.json  
"config.catalog.service.creation.approval_required.label": "Aprovação Obrigatória para Criação de Serviço",
"config.catalog.service.creation.approval_required.description": "Quando ativado, novos serviços requerem aprovação antes de ficarem visíveis no catálogo. Quando desativado, serviços são criados diretamente em estado ativo."

// pt-PT.json
"config.catalog.service.creation.approval_required.label": "Aprovação Obrigatória para Criação de Serviço",  
"config.catalog.service.creation.approval_required.description": "Quando ativado, novos serviços requerem aprovação antes de ficarem visíveis no catálogo. Quando desativado, serviços são criados diretamente em estado ativo."

// es.json
"config.catalog.service.creation.approval_required.label": "Aprobación Obligatoria para Creación de Servicio",
"config.catalog.service.creation.approval_required.description": "Cuando está activado, los nuevos servicios requieren aprobación antes de ser visibles en el catálogo. Cuando está desactivado, los servicios se crean directamente en estado activo."
```

#### 3. Handler (Backend)
```csharp
// RegisterServiceAsset handler — ANTES (sem parametrização):
public async Task<Result<Response>> Handle(Command request, CancellationToken ct)
{
    var asset = ServiceAsset.Create(request.Name, request.Domain, ...);
    await repository.AddAsync(asset, ct);
    return Result.Success(new Response(asset.Id));
}

// RegisterServiceAsset handler — DEPOIS (com parametrização):
public async Task<Result<Response>> Handle(Command request, CancellationToken ct)
{
    var approvalRequired = await configService.ResolveEffectiveValueAsync(
        "catalog.service.creation.approval_required",
        ConfigurationScope.Tenant,
        request.TenantId.ToString(),
        ct);
    
    var asset = ServiceAsset.Create(request.Name, request.Domain, ...);
    
    if (approvalRequired?.EffectiveValue == "true")
    {
        asset.SetStatus(ServiceStatus.PendingApproval);
        
        // Criar workflow de aprovação
        var approvalRoles = await configService.ResolveEffectiveValueAsync(
            "catalog.service.creation.approval_roles", ...);
        // Iniciar workflow com roles configurados
    }
    else
    {
        asset.SetStatus(ServiceStatus.Active);
    }
    
    await repository.AddAsync(asset, ct);
    return Result.Success(new Response(asset.Id));
}
```

#### 4. Frontend
```typescript
// ServiceCreationForm.tsx — consulta parâmetro para ajustar UX
const { data: approvalRequired } = useConfigValue('catalog.service.creation.approval_required');

return (
  <form>
    {/* ... campos do formulário ... */}
    <Button type="submit">
      {approvalRequired === 'true' 
        ? t('catalog.service.submitForApproval')  // "Submit for Approval"
        : t('catalog.service.create')              // "Create Service"
      }
    </Button>
    {approvalRequired === 'true' && (
      <InfoBanner>{t('catalog.service.approvalInfo')}</InfoBanner>
    )}
  </form>
);
```

#### 5. Testes
```csharp
[Fact]
public async Task RegisterServiceAsset_WhenApprovalRequired_SetsStatusToPendingApproval()
{
    // Arrange: Configure parameter as "true"
    configService.Setup(x => x.ResolveEffectiveValueAsync(
        "catalog.service.creation.approval_required", ...))
        .ReturnsAsync(new EffectiveConfigurationDto { EffectiveValue = "true" });
    
    // Act
    var result = await handler.Handle(command, CancellationToken.None);
    
    // Assert
    result.IsSuccess.Should().BeTrue();
    var asset = await repository.GetByIdAsync(result.Value.Id);
    asset.Status.Should().Be(ServiceStatus.PendingApproval);
}

[Fact]
public async Task RegisterServiceAsset_WhenApprovalNotRequired_SetsStatusToActive()
{
    // Arrange: Configure parameter as "false"
    configService.Setup(x => x.ResolveEffectiveValueAsync(
        "catalog.service.creation.approval_required", ...))
        .ReturnsAsync(new EffectiveConfigurationDto { EffectiveValue = "false" });
    
    // Act
    var result = await handler.Handle(command, CancellationToken.None);
    
    // Assert
    result.IsSuccess.Should().BeTrue();
    var asset = await repository.GetByIdAsync(result.Value.Id);
    asset.Status.Should().Be(ServiceStatus.Active);
}
```

---

## 28. RESUMO QUANTITATIVO ATUALIZADO (v2.0)

| Módulo | Parâmetros v1.0 | Parâmetros Adicionados v2.0 | Total |
|--------|----------------|----------------------------|-------|
| Service Catalog | 8 | 0 | 8 |
| Contracts | 8 | 0 | 8 |
| Change Governance (Release/Deploy) | 17 | 0 | 17 |
| Promotion Governance | 4 | 0 | 4 |
| Workflow Engine | 5 | 0 | 5 |
| Incidents & Operations | 7 | 0 | 7 |
| AI Hub | 6 | 0 | 6 |
| Identity & Access | 6 | 3 (roles dinâmicos) | 9 |
| Audit & Compliance | 3 | 0 | 3 |
| Integrations | 3 | 0 | 3 |
| FinOps | 5 | 0 | 5 |
| ~~Knowledge~~ Operational Knowledge | ~~4~~ | 6 (refatoração) | 6 |
| Reliability (SLO/SLA) | 3 | 0 | 3 |
| Governance Transversal | 7 | 0 | 7 |
| Product Analytics | 2 | 0 | 2 |
| Best Practices (DORA/GitOps/OWASP/Chaos/FinOps) | 10 | 0 | 10 |
| **Platform Customization** (NOVO) | 0 | **9** | **9** |
| **TOTAL** | **98** | **18** | **112** |

---

## 29. FASEAMENTO ATUALIZADO (v2.0)

### Fase 0 — Infraestrutura de Parametrização (PRÉ-REQUISITO) ✅
- [x] Migrar seed para usar chaves i18n (displayName e description)
- [x] Criar ficheiros de tradução para 4 idiomas (en, pt-BR, pt-PT, es) para TODOS os parâmetros
- [x] Implementar hook `useConfigValue()` no frontend para consulta de parâmetros
- [x] Implementar componente `<RolePicker>` para parâmetros que referenciam roles
- [x] Implementar componente `<ConfigToggle>` para parâmetros booleanos simples
- [x] Criar interface `IParameterizedHandler<T>` para padronizar handlers parametrizados
- [x] Script de geração de i18n a partir do seeder

### Fase 1 — Fundação (Alta Prioridade, Impacto Imediato) ✅
- [x] Seed dos 43 parâmetros de prioridade alta + 18 novos da v2.0
- [x] Implementar CRUD de roles customizados (backend + frontend)
- [x] Implementar gate de aprovação de release com validação externa (Jenkins/GitLab)
- [x] Implementar aprovação de criação de serviço (100% funcional com handler modificado)
- [x] Implementar bloqueio de deploy por breaking change
- [x] Implementar endpoint de deploy-readiness
- [x] Testes unitários para cada parâmetro (enabled/disabled)

### Fase 2 — Governança Avançada + Customização de Plataforma ✅
- [x] Implementar preferências de utilizador (sidebar, home, widgets)
- [x] Implementar sidebar customizável
- [x] Implementar home dashboard customizável com widgets drag-and-drop
- [x] Implementar princípio dos quatro olhos
- [x] Implementar Change Advisory Board
- [x] Implementar gates GitOps (PR approval, signed commits)
- [x] Implementar gates de segurança (scan obrigatório)
- [x] Implementar bloqueio por error budget

### Fase 3 — IA, Knowledge & Conhecimento Operacional ✅
- [x] Refatorar módulo Knowledge → OperationalKnowledge
- [x] Implementar captura automática de conhecimento (auto_capture)
- [x] Implementar pesquisa federada de conhecimento
- [x] Implementar aprovação de modelos externos
- [x] Implementar bloqueio de dados sensíveis para IA
- [x] Implementar aprovação de agentes customizados

### Fase 4 — Otimização & Compliance ✅
- [x] Implementar parâmetros de FinOps (enforcement, chargeback)
- [x] Implementar frameworks de compliance
- [x] Implementar auto-remediação de violações
- [x] Implementar chaos engineering governado

### Fase 5 — Polish, Migração i18n & Analytics ✅
- [x] Migrar seeds existentes (345) para chaves i18n
- [x] Parâmetros de analytics e persona tracking
- [x] UX avançada de configuração
- [x] Relatórios de utilização de parâmetros
- [x] Dashboard executivo de compliance de parametrização

---

## 30. ENTREGÁVEIS POR FASE

### Fase 0 — Para cada parâmetro:
| Entregável | Descrição |
|------------|-----------|
| `ConfigurationDefinitionSeeder.cs` | Seed com chave i18n |
| `en.json` | Label + description em inglês |
| `pt-BR.json` | Label + description em pt-BR |
| `pt-PT.json` | Label + description em pt-PT |
| `es.json` | Label + description em espanhol |

### Fases 1-4 — Para cada parâmetro:
| Entregável | Descrição |
|------------|-----------|
| Seed | ✅ Já criado na Fase 0 |
| i18n | ✅ Já criado na Fase 0 |
| Handler | Modificação do handler para consultar parâmetro |
| Frontend | Ajuste da UI para refletir estado do parâmetro |
| Testes (enabled) | Teste com parâmetro ativado |
| Testes (disabled) | Teste com parâmetro desativado |
| Testes (inheritance) | Teste de resolução por escopo |

---

*Documento atualizado em 2026-04-06 v2.0 — Inclui ressalvas do Product Owner sobre Knowledge module, roles configuráveis, customização de plataforma, i18n nos seeds e enforcement funcional.*
