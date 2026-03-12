# NexTraceOne — Análise de Cobertura Funcional

> **Data:** Março 2026 — Pós-conclusão dos módulos ChangeIntelligence, RulesetGovernance e Workflow
> **Versão:** 1.0
> **Responsável:** Product Architect

---

## FASE 1 — Inventário Completo de Funcionalidades do Produto

### 1. GESTÃO DE IDENTIDADE E ACESSO

| # | Funcionalidade | O que é | Por que existe | Quem usa |
|---|----------------|---------|----------------|----------|
| 1.1 | Autenticação federada (OIDC) | Login via Azure AD, Okta, Google, Keycloak usando protocolo OIDC | Empresas enterprise já possuem IdP corporativo — integração obrigatória para adoção | Developer, Gestor, Executivo, Auditor |
| 1.2 | Login local (fallback) | Autenticação com e-mail e senha armazenados localmente | Service accounts, pipelines CI/CD e ambientes sem IdP precisam de acesso programático | Pipeline CI/CD, Developer |
| 1.3 | Gestão de sessões com refresh token rotation | Emissão de JWT com refresh token; rotação automática para prevenir roubo | Segurança de sessão: impede uso de tokens roubados por meio de rotação contínua | Developer, Gestor, Executivo |
| 1.4 | RBAC: Admin, Manager, Developer, Viewer, Auditor | Modelo de papéis com permissões hierárquicas por módulo | Regulação bancária e governamental exige controle de quem pode fazer o quê | Todos |
| 1.5 | Permissões granulares por módulo | Permissões como `releases:read`, `workflows:approve` atribuídas por role | Princípio do mínimo privilégio — auditores precisam ler, não escrever | Admin, Auditor |
| 1.6 | Multi-tenancy com isolamento | Cada organização tem seus dados completamente isolados via RLS PostgreSQL | Self-hosted multi-cliente: um único deployment serve múltiplas organizações | Admin |
| 1.7 | Onboarding de tenant com ativação de licença | Fluxo de criação de tenant vinculado à ativação da licença correspondente | Garante que apenas clientes licenciados operam o sistema | Admin |

### 2. LICENCIAMENTO E CAPACIDADES

| # | Funcionalidade | O que é | Por que existe | Quem usa |
|---|----------------|---------|----------------|----------|
| 2.1 | Ativação de licença com hardware fingerprint | Vincula a licença ao hardware físico via hash de identificadores únicos do host | Protege contra cópia não autorizada; sovereign: funciona offline | Admin |
| 2.2 | Verificação de licença no boot | Valida a licença antes de iniciar a API — impede execução se inválida | Garante conformidade contratual sem dependência de servidor externo | Sistema |
| 2.3 | Capabilities por tier | Define quais módulos o tenant pode usar conforme o tier contratado | Monetização: tiers Basic/Standard/Enterprise controlam acesso a funcionalidades | Admin, Gestor |
| 2.4 | Tracking de uso (quotas, métricas) | Mede consumo de APIs, deploys, usuários ativos contra limites contratuais | Detecta uso excessivo antes de gerar surpresas; base para upgrade de tier | Admin |
| 2.5 | Alertas de threshold de quota | Notificação automática quando uso se aproxima de 80%/90%/100% da quota | Evita interrupção de serviço; permite planejamento de capacidade | Admin, Gestor |
| 2.6 | Modo offline com validação local | Licença validada localmente quando sem conectividade | Requisito crítico: bancos e governo operam em redes isoladas | Sistema |

### 3. CATÁLOGO DE SERVIÇOS E GRAFO DE DEPENDÊNCIAS

| # | Funcionalidade | O que é | Por que existe | Quem usa |
|---|----------------|---------|----------------|----------|
| 3.1 | Registro de APIs e serviços | Cadastro de ApiAsset (endpoint) e ServiceAsset (serviço proprietário) | Cria o catálogo central que todos os outros módulos consultam | Developer, Gestor |
| 3.2 | Mapeamento consumidor-provedor | Registra quais serviços consomem quais APIs | Base do blast radius: saber quem é impactado por uma mudança | Developer, Gestor |
| 3.3 | Discovery: contrato estático | Infere dependências a partir de contratos OpenAPI | Automação do mapeamento sem instrumentação manual | Sistema |
| 3.4 | Discovery: runtime OTel | Infere dependências a partir de traces distribuídos | Captura dependências reais de runtime, não apenas declaradas | Sistema |
| 3.5 | Discovery: gateway | Importa rotas e relacionamentos de API gateways (Kong, APIM, etc.) | Gateways são fonte de verdade para tráfego em produção | Sistema |
| 3.6 | Discovery: plataformas | Importa de Backstage, Consul, Kubernetes | Aproveita investimentos existentes em catálogos e service mesh | Platform Engineer |
| 3.7 | Discovery: análise de código | Analisa dependências estáticas no código-fonte | Detecta dependências não instrumentadas | Developer |
| 3.8 | Modelo de confiança (Inferred→Confirmed) | Classifica o nível de certeza de cada relação descoberta | Permite filtrar ruído e focar nas dependências confirmadas | Developer, Gestor |
| 3.9 | Visualização do grafo | Interface gráfica do grafo de dependências | Comunicação visual para gestores e executivos | Gestor, Executivo |
| 3.10 | Detecção de dependências não declaradas | Alerta quando uma dependência detectada não está declarada no catálogo | Governance gap: dependências ocultas são risco operacional | Gestor, Auditor |
| 3.11 | Importação de catálogos externos | Bulk import de Backstage, Kong, Consul, K8s | Reduz onboarding de horas para minutos | Platform Engineer |
| 3.12 | Decomissioning de serviços | Marca serviço como obsoleto e notifica consumidores | Evita dependências em serviços que serão desligados | Gestor |

### 4. PORTAL DO DESENVOLVEDOR

| # | Funcionalidade | O que é | Por que existe | Quem usa |
|---|----------------|---------|----------------|----------|
| 4.1 | Catálogo pesquisável de APIs | Busca full-text em APIs com filtros por tags, tipo, ambiente | Developers precisam descobrir APIs existentes antes de criar novas | Developer |
| 4.2 | Detalhe de API com contrato renderizado | Exibe a spec OpenAPI renderizada (tipo Swagger UI) dentro do portal | One-stop-shop: ver contrato sem sair da plataforma | Developer |
| 4.3 | "Minhas APIs" | Lista de APIs que o usuário autenticado é responsável | Developer precisa saber quais APIs gerencia | Developer |
| 4.4 | "APIs que eu consumo" | Lista de APIs das quais os serviços do usuário dependem | Visibilidade de dependências: saber quando um provedor muda | Developer |
| 4.5 | Timeline de eventos por API | Histórico cronológico de releases, mudanças, incidentes | Contexto histórico para diagnóstico de problemas | Developer, Auditor |
| 4.6 | Health status em tempo real | Indicador de saúde atual baseado em runtime intelligence | Developers saberão instantaneamente se uma API está degradada | Developer |

### 5. GESTÃO DE CONTRATOS

| # | Funcionalidade | O que é | Por que existe | Quem usa |
|---|----------------|---------|----------------|----------|
| 5.1 | Importação de contratos OpenAPI | Upload de arquivo ou pull de URL de spec OpenAPI v2/v3 | Ingesta contratos existentes sem reescrita manual | Developer |
| 5.2 | Versionamento semântico automático | Atribui versão SemVer 2.0 baseada na análise da mudança | Disciplina semântica sem esforço manual | Developer, Pipeline CI/CD |
| 5.3 | Diff semântico entre versões | Compara dois contratos e lista todas as mudanças (campo a campo) | Clareza sobre o que exatamente mudou — reduz surpresas | Developer, Gestor |
| 5.4 | Classificação: breaking vs non-breaking vs additive | Categoriza cada mudança por impacto nos consumidores | Basis para decisão de aprovação: breaking = mais rigor | Gestor, Auditor |
| 5.5 | Sugestão de versão semântica | Sugere MAJOR/MINOR/PATCH baseado no diff | Reduz erros humanos de versionamento | Developer, Pipeline CI/CD |
| 5.6 | Lock de contrato | Congela uma versão para estabilização (bloqueia novas versões) | Janelas de congelamento antes de deadlines regulatórios | Gestor |
| 5.7 | Histórico completo de versões | Lista todas as versões de um contrato com diffs entre elas | Compliance: "o que era o contrato no dia X?" | Auditor, Regulador |

### 6. CHANGE INTELLIGENCE (CORE DO PRODUTO)

| # | Funcionalidade | O que é | Por que existe | Quem usa |
|---|----------------|---------|----------------|----------|
| 6.1 | Recepção de notificações de deploy (webhook) | Endpoint que recebe payload de deploy do CI/CD | Ponto de entrada: toda mudança começa aqui | Pipeline CI/CD |
| 6.2 | Classificação em 5 níveis (N0→N4) | Nível 0=Operacional, N1=Non-Breaking, N2=Additive, N3=Breaking, N4=Publicação | Vocabulário comum entre devs, gestores e reguladores | Sistema, Gestor |
| 6.3 | Cálculo de Blast Radius | Lista todos os consumidores transitivos impactados por uma mudança | Responde: "quem vai quebrar se eu publicar isso?" | Gestor, Developer |
| 6.4 | Score de risco normalizado (0.0–1.0) | Número único que resume o risco da mudança | Executivos e aprovadores precisam de sinais simples | Gestor, Executivo |
| 6.5 | Associação com work items | Vincula um deploy a tickets Jira/Azure DevOps/GitHub Issues/Linear | Rastreabilidade: qual tarefa originou esta mudança? | Auditor, Regulador |
| 6.6 | Tracking de estado do deployment | Ciclo de vida: pending → running → succeeded → failed | Visibilidade em tempo real do status de um deploy | Developer, Gestor |
| 6.7 | Registro de rollbacks | Documenta quando e por que um deploy foi revertido | Compliance: histórico de reversões é exigido por regulação | Auditor |
| 6.8 | Plugins CI/CD | Integrações nativas para Jenkins, GitHub Actions, GitLab CI, Azure DevOps | Adoção zero-atrito: não muda o pipeline existente | Pipeline CI/CD, Developer |

### 7. GOVERNANÇA E REGRAS (RULESET)

| # | Funcionalidade | O que é | Por que existe | Quem usa |
|---|----------------|---------|----------------|----------|
| 7.1 | Upload de rulesets customizáveis (Spectral-compat) | Carrega regras de linting de API (JSON/YAML) no formato Spectral | Organizações têm padrões próprios de API governance | Gestor, Platform Engineer |
| 7.2 | Binding de ruleset por tipo de asset | Associa rulesets específicos a tipos de API (interno, público, parceiro) | Regras diferentes para diferentes audiências | Gestor |
| 7.3 | Execução automática de linting | Roda o ruleset bound automaticamente em cada novo release | Enforcement automático sem intervenção manual | Sistema, Pipeline CI/CD |
| 7.4 | Score de conformidade | Porcentagem de compliance por release e por serviço | KPI mensurável de qualidade de API governance | Gestor, Executivo |
| 7.5 | Rulesets padrão pré-instalados | Conjunto de regras OWASP API Security e melhores práticas de design | Valor imediato no dia 1 sem configuração | Developer, Gestor |
| 7.6 | Findings com severidade | Lista detalhada de violações com severidade (error/warning/info) | Prioriza o que deve ser corrigido antes de aprovar | Developer, Auditor |

### 8. WORKFLOW DE APROVAÇÃO

| # | Funcionalidade | O que é | Por que existe | Quem usa |
|---|----------------|---------|----------------|----------|
| 8.1 | Templates de workflow configuráveis | Define stages de aprovação por ambiente e nível de mudança (ex: N3 em prod = 3 aprovadores) | Políticas organizacionais variam — workflow deve ser flexível | Admin, Gestor |
| 8.2 | Instanciação automática | Cria instância de workflow baseado no ChangeLevel detectado | Sem intervenção manual: mudança detected → workflow iniciado | Sistema |
| 8.3 | Aprovação por stages (multi-step) | Workflow com múltiplos estágios sequenciais com aprovadores distintos | Segregação de funções: tech lead + gestor + compliance | Gestor, Developer |
| 8.4 | Rejeição com motivo obrigatório | Aprovador rejeita informando motivo que fica no registro | Auditabilidade: por que foi rejeitado? | Gestor, Auditor |
| 8.5 | Request Changes | Aprovador pede alterações sem rejeitar formalmente | Workflow colaborativo antes de aprovação final | Gestor, Developer |
| 8.6 | Comentários por stage | Campo de observação em cada etapa do workflow | Comunicação in-context entre aprovadores | Gestor, Auditor |
| 8.7 | Evidence Pack automático | Coletado automaticamente: blast radius + score + diff + work items + linting + health | Aprovadores tomam decisão com todos os dados disponíveis | Gestor, Auditor |
| 8.8 | Exportação de Evidence Pack em PDF | Gera PDF do evidence pack para envio a auditores externos | Reguladores exigem evidência em formato permanente e imprimível | Auditor, Regulador |
| 8.9 | SLA de aprovação com escalação | Define prazo para aprovação; escala automaticamente se expirar | Previne bloqueios de deployment por inação de aprovadores | Gestor, Admin |

### 9. PROMOÇÃO ENTRE AMBIENTES

| # | Funcionalidade | O que é | Por que existe | Quem usa |
|---|----------------|---------|----------------|----------|
| 9.1 | Configuração de ambientes governados | Define a cadeia de ambientes (dev → staging → prod) e suas regras | Formaliza o pipeline de promoção com critérios claros | Admin |
| 9.2 | Criação de pedido de promoção | Developer solicita promoção de uma versão entre ambientes | Governança: toda promoção é rastreada e aprovada | Developer |
| 9.3 | Quality Gates automáticos | Avalia automaticamente: linting, CI/CD, blast radius, workflow, SLA, budget | Prevenção: bloqueia promoção se critérios de qualidade não forem atendidos | Sistema |
| 9.4 | Override de gate com justificativa | Permite bypassar gate em emergências com justificativa obrigatória | Emergências existem — mas devem ser documentadas | Gestor |
| 9.5 | Tracking de estado de promoção | Ciclo de vida da promoção: pendente → gates em avaliação → aprovado → promovido | Visibilidade completa do processo | Developer, Gestor |

### 10. RUNTIME INTELLIGENCE

| # | Funcionalidade | O que é | Por que existe | Quem usa |
|---|----------------|---------|----------------|----------|
| 10.1 | Ingestão de snapshots de runtime | Recebe métricas e traces via OTLP ou webhook | Dados de comportamento real complementam dados de contrato | Sistema |
| 10.2 | Detecção de drift entre versões | Compara comportamento antes e depois do deploy | Detecta regressões silenciosas que testes não pegaram | Developer, Gestor |
| 10.3 | Score de observabilidade | Mede qualidade dos dados de telemetria emitidos por um serviço | Incentiva instrumentação adequada | Developer |
| 10.4 | Comparação de health entre releases | Gráfico comparando latência, taxa de erro entre versões | Decisão data-driven sobre rollback | Gestor, Developer |
| 10.5 | Timeline de health por release | Histórico de health ao longo da vida de um release | Correlação: quando o problema começou? | Developer |
| 10.6 | Detecção de anomalias | Alerta quando métricas saem dos intervalos normais (AIOPS básico) | Detecta degradações antes de virarem incidentes | Developer, Gestor |

### 11. COST INTELLIGENCE

| # | Funcionalidade | O que é | Por que existe | Quem usa |
|---|----------------|---------|----------------|----------|
| 11.1 | Ingestão de dados de custo | Recebe dados de custo de AWS, Azure, GCP | Conecta custos de infraestrutura a serviços e releases | Sistema |
| 11.2 | Atribuição de custo por serviço/rota | Distribui custo de cloud entre serviços responsáveis | Chargeback: cada time sabe quanto gasta | Gestor, Executivo |
| 11.3 | Tendências de custo | Visualiza evolução do custo ao longo do tempo | Identifica tendências antes de virar surpresa no fim do mês | Gestor, Executivo |
| 11.4 | Custo por release | Quanto custou esta mudança em comparação com a versão anterior? | Conecta decisão técnica com impacto financeiro | Gestor, Executivo |
| 11.5 | Delta de custo entre versões | Diferença de custo: ficou mais caro ou mais barato? | KPI de eficiência de engenharia | Gestor, Executivo |
| 11.6 | Alertas de anomalia de custo | Notifica quando custo de um serviço sobe anormalmente | Previne gastos excessivos por configuração errada | Gestor |
| 11.7 | Relatório de custos | Exportação de relatório de custos por período | FinOps: dados para revisão mensal de orçamento | Executivo |

### 12. INTELIGÊNCIA ARTIFICIAL

| # | Funcionalidade | O que é | Por que existe | Quem usa |
|---|----------------|---------|----------------|----------|
| 12.1 | Classificação de mudanças assistida por IA | LLM analisa diff do contrato e classifica o nível de impacto | Reduz erros humanos na classificação de breaking changes | Developer, Sistema |
| 12.2 | Sugestão de versão semântica por IA | LLM sugere MAJOR/MINOR/PATCH baseado no contexto da mudança | Versionamento mais preciso que regras heurísticas simples | Developer |
| 12.3 | Resumo de release para aprovadores | LLM gera explicação não-técnica da mudança para gestores | Gestores não entendem diffs técnicos — precisam de narrativa | Gestor, Executivo |
| 12.4 | Geração de cenários de teste (Robot Framework) | LLM gera casos de teste baseados no contrato OpenAPI | Acelera criação de testes de contrato; reduz gaps de cobertura | Developer, QA |
| 12.5 | Consulta ao catálogo em linguagem natural | "Quais APIs do time de pagamentos têm mudanças breaking este mês?" | Interface conversacional para não-desenvolvedores | Gestor, Executivo |
| 12.6 | Integração com LLMs externos | Configuração de OpenAI, Azure OpenAI, Anthropic como backend de IA | Flexibilidade de escolha de LLM conforme política corporativa | Admin |
| 12.7 | Knowledge capture | Respostas de IA aprovadas por humanos são reutilizadas como base de conhecimento | Reduz custo e latência de IA; acumula conhecimento organizacional | Developer, Gestor |

### 13. AUDITORIA E COMPLIANCE

| # | Funcionalidade | O que é | Por que existe | Quem usa |
|---|----------------|---------|----------------|----------|
| 13.1 | Registro imutável com hash chain SHA-256 | Cada evento registrado com hash encadeado no anterior | Prova criptográfica de que o log não foi adulterado | Auditor, Regulador |
| 13.2 | Busca e filtro no log | Pesquisa por ator, tipo de evento, data, módulo | Resposta a "o que aconteceu no dia X?" em auditoria | Auditor |
| 13.3 | Verificação de integridade da cadeia | Comando que verifica se todos os hashes da cadeia são consistentes | Proof of tamper-evidence: exigido por BACEN, GDPR | Auditor, Regulador |
| 13.4 | Exportação de relatório de auditoria | Exporta log filtrado em formato auditável (CSV, JSON, PDF) | Auditorias externas exigem evidência em formato portátil | Auditor, Regulador |
| 13.5 | Relatório de compliance | Relatório consolidado de conformidade por período | Board e reguladores querem visão executiva de compliance | Executivo, Regulador |
| 13.6 | Política de retenção configurável | Define por quanto tempo eventos são mantidos | Atende requisitos legais de retenção (ex: 5 anos para BACEN) | Admin |

### 14. CLI (ferramenta 'nex')

| # | Funcionalidade | O que é | Por que existe | Quem usa |
|---|----------------|---------|----------------|----------|
| 14.1 | `nex validate` | Valida contrato OpenAPI localmente contra rulesets | Developer valida antes de fazer push — shift-left de governance | Developer, Pipeline CI/CD |
| 14.2 | `nex release` | Consulta status, health e histórico de releases | Visibilidade rápida sem abrir browser | Developer |
| 14.3 | `nex promotion` | Controla e consulta promoções entre ambientes | Gerencia promoções a partir do terminal | Developer, Gestor |
| 14.4 | `nex approval` | Submete e consulta aprovações de workflow | Aprovações sem sair do terminal | Developer, Gestor |
| 14.5 | `nex impact` | Analisa blast radius de uma mudança hipotética | "Se eu publicar esta versão, quem vai quebrar?" — planejamento | Developer, Gestor |
| 14.6 | `nex tests` | Gera cenários de teste Robot Framework a partir do contrato | Automação de QA integrada ao workflow CLI | Developer, QA |
| 14.7 | `nex catalog` | Consulta catálogo de APIs e serviços | Descoberta de APIs no terminal | Developer |

### 15. INTEGRAÇÕES

| # | Funcionalidade | O que é | Por que existe | Quem usa |
|---|----------------|---------|----------------|----------|
| 15.1 | CI/CD: Jenkins, GitHub Actions, GitLab CI, Azure DevOps | Plugins nativos para notificar deploys | Zero-atrito: developer não muda workflow | Pipeline CI/CD |
| 15.2 | Task Management: Jira, Azure DevOps, GitHub Issues, Linear, ClickUp | Lê e vincula work items ao deploy | Rastreabilidade automática | Developer |
| 15.3 | Write-back para task management | Comenta na tarefa quando aprovado; transiciona status; cria bug | Fecha o loop: tarefa sabe que o deploy está aprovado/rejeitado | Developer, Gestor |
| 15.4 | Gateway Discovery: Kong, AWS APIM, Azure APIM, Nginx | Importa rotas e dependências automaticamente | Elimina mapeamento manual para clientes com gateways existentes | Platform Engineer |
| 15.5 | Catálogos: Backstage, Consul, Kubernetes API | Importa catálogos existentes | ROI imediato para clientes que já têm catálogo | Platform Engineer |
| 15.6 | Observabilidade: OTLP endpoint | Recebe traces/métricas no protocolo OpenTelemetry padrão | Reutiliza instrumentação existente sem adaptadores | Developer |
| 15.7 | Cloud Cost: AWS, Azure, GCP | Importa dados de billing dos principais providers | FinOps unificado com rastreabilidade de deploy | Gestor, Executivo |

### 16. REQUISITOS NÃO-FUNCIONAIS

| # | Funcionalidade | O que é | Por que existe | Quem usa |
|---|----------------|---------|----------------|----------|
| 16.1 | Self-hosted | Deployment on-premises ou cloud privada do cliente | Bancos e governo não podem ter dados em SaaS externo | Admin |
| 16.2 | Multi-tenant com RLS PostgreSQL | Isolamento de dados por tenant na camada de banco | Uma instância serve múltiplos clientes sem vazamento de dados | Admin |
| 16.3 | Encryption at rest (AES-256-GCM) | Dados sensíveis criptografados no banco | LGPD, GDPR, BACEN exigem proteção de dados em repouso | Admin, Auditor |
| 16.4 | Assembly integrity verification | Verificação de assinatura dos assemblies no boot | Impede execução de código adulterado (supply chain attack) | Sistema |
| 16.5 | Hardware fingerprint para license binding | Licença vinculada ao hardware físico | Proteção anti-pirataria e anti-cópia | Sistema |
| 16.6 | Audit trail com hash chain imutável | Log de auditoria com prova criptográfica de integridade | Compliance regulatório (BACEN, GDPR, ISO 27001) | Auditor, Regulador |
| 16.7 | i18n (en-US, pt-BR) | Internacionalização de mensagens de erro e UI | Clientes brasileiros e internacionais na mesma plataforma | Todos |
| 16.8 | OpenAPI spec para todos os endpoints | Documentação automática de toda a API REST | Integrações externas precisam de contrato formal | Developer |
| 16.9 | Health checks (liveness/readiness) | Endpoints padrão para orquestradores (K8s, Docker) | Deployment e monitoring automático | Platform Engineer |
| 16.10 | Structured logging (Serilog) | Logs estruturados em JSON com correlação de trace | Observabilidade e debugging em produção | Developer, Platform Engineer |
| 16.11 | Distributed tracing (OpenTelemetry) | Traces distribuídos com propagação de contexto | Diagnóstico de performance em sistema modular | Developer, Platform Engineer |
| 16.12 | Custom metrics (Prometheus-compatible) | Métricas de negócio exportáveis para Prometheus/Grafana | Dashboards de negócio e alertas operacionais | Platform Engineer |

---

## FASE 2 — Mapeamento: Funcionalidade → Código Existente

### 1. GESTÃO DE IDENTIDADE E ACESSO

| # | Funcionalidade | Módulo | Status | Ficheiros | Notas |
|---|----------------|--------|--------|-----------|-------|
| 1.1 | Autenticação federada (OIDC) | Identity | ✅ IMPLEMENTADO | `FederatedLogin/FederatedLogin.cs`, `IdentityEndpointModule.cs` | JWT gerado com claims de OIDC; `FederatedLoginCommand` completo |
| 1.2 | Login local (fallback) | Identity | ✅ IMPLEMENTADO | `LocalLogin/LocalLogin.cs` | BCrypt para senha; `LocalLoginCommand` com validação |
| 1.3 | Gestão de sessões com refresh token | Identity | ✅ IMPLEMENTADO | `RefreshToken/`, `RevokeSession/`, `Session.cs` | Rotation implementada; `RevokeSession` testado |
| 1.4 | RBAC: 5 papéis | Identity | ✅ IMPLEMENTADO | `Role.cs`, `Permission.cs`, `AssignRole/AssignRole.cs` | 5 roles definidos; permissões granulares por módulo |
| 1.5 | Permissões granulares | Identity | ✅ IMPLEMENTADO | `Permission.cs`, `AssignRole/AssignRole.cs` | Permissões `{module}:{action}` mapeadas |
| 1.6 | Multi-tenancy com RLS | Identity + Infra | ✅ IMPLEMENTADO | `TenantRlsInterceptor.cs`, `TenantMembership.cs`, `TenantResolutionMiddleware.cs` | RLS via interceptor EF Core; `ITenantContext` |
| 1.7 | Onboarding de tenant | Identity | 🟡 PARCIAL | `CreateUser/CreateUser.cs`, `TenantMembership.cs` | Criação de usuário existe; fluxo completo de onboarding de tenant (com licença) falta a integração de módulos |

**O que falta em 1.7:** Integração do onboarding de tenant com ativação automática da licença correspondente (cross-módulo via Integration Event).

### 2. LICENCIAMENTO E CAPACIDADES

| # | Funcionalidade | Módulo | Status | Ficheiros | Notas |
|---|----------------|--------|--------|-----------|-------|
| 2.1 | Ativação com hardware fingerprint | Licensing | ✅ IMPLEMENTADO | `ActivateLicense/ActivateLicense.cs`, `HardwareBinding.cs`, `HardwareFingerprint.cs` | Fingerprint gerado via `IHardwareFingerprintProvider` |
| 2.2 | Verificação no boot | Licensing | ✅ IMPLEMENTADO | `VerifyLicenseOnStartup/VerifyLicenseOnStartup.cs` | Handler completo com `ILicenseRepository` |
| 2.3 | Capabilities por tier | Licensing | ✅ IMPLEMENTADO | `CheckCapability/CheckCapability.cs`, `LicenseCapability.cs` | Tier Basic/Standard/Enterprise com capabilities |
| 2.4 | Tracking de uso | Licensing | ✅ IMPLEMENTADO | `TrackUsageMetric/TrackUsageMetric.cs`, `UsageQuota.cs` | Quotas com tracking incremental |
| 2.5 | Alertas de threshold | Licensing | ✅ IMPLEMENTADO | `AlertLicenseThreshold/AlertLicenseThreshold.cs` | Alerta a 80%/90%/100% via integration event |
| 2.6 | Modo offline | Licensing | ✅ IMPLEMENTADO | `VerifyLicenseOnStartup/VerifyLicenseOnStartup.cs` | Validação local sem dependência de rede |

### 3. CATÁLOGO DE SERVIÇOS E GRAFO DE DEPENDÊNCIAS

| # | Funcionalidade | Módulo | Status | Ficheiros | Notas |
|---|----------------|--------|--------|-----------|-------|
| 3.1 | Registro de APIs e serviços | EngineeringGraph | 🟡 PARCIAL | `RegisterApiAsset/`, `RegisterServiceAsset/`, `ApiAsset.cs`, `ServiceAsset.cs` | Domain e Application completos; Infrastructure stub com TODO |
| 3.2 | Mapeamento consumidor-provedor | EngineeringGraph | 🟡 PARCIAL | `MapConsumerRelationship/`, `ConsumerRelationship.cs` | Domain testado; Application stub; Infrastructure stub |
| 3.3 | Discovery: contrato estático | EngineeringGraph | 🟡 PARCIAL | `ValidateDiscoveredDependency/` | Application stub com TODO |
| 3.4 | Discovery: runtime OTel | EngineeringGraph | 🟡 PARCIAL | `InferDependencyFromOtel/`, `InferDependencyFromOtel()` em `ApiAsset.cs` | Domain method implementado e testado; Application stub |
| 3.5 | Discovery: gateway | EngineeringGraph | 🟡 PARCIAL | `ImportFromKongGateway/` | Application stub com TODO |
| 3.6 | Discovery: plataformas | EngineeringGraph | 🔲 FORA DO MVP1 | `ImportFromBackstage/` | Stub existe; fora do MVP1 por complexidade e risco |
| 3.7 | Discovery: análise de código | EngineeringGraph | 🔲 FORA DO MVP1 | — | Não scaffoldado; fora do MVP1 |
| 3.8 | Modelo de confiança | EngineeringGraph | 🟡 PARCIAL | `DiscoverySource.cs` (confiança 0–1), `ConsumerRelationship.cs` | Domain modelado; sem UI/query ainda |
| 3.9 | Visualização do grafo | EngineeringGraph | 🟡 PARCIAL | `GetAssetGraph/` | Application stub; precisa de endpoint funcional |
| 3.10 | Detecção de dependências não declaradas | EngineeringGraph | 🟡 PARCIAL | `ValidateDiscoveredDependency/` | Stub com TODO |
| 3.11 | Importação de catálogos | EngineeringGraph | 🔲 FORA DO MVP1 | `ImportFromBackstage/`, `ImportFromKongGateway/` | Parcialmente scaffoldado; Kong pode entrar no MVP1 |
| 3.12 | Decomissioning | EngineeringGraph | 🟡 PARCIAL | `DecommissionAsset/` | Application stub com TODO |

**O que falta nos itens 🟡 de EngineeringGraph:** Infrastructure layer completa (DbContext, Repositories, Migrations), implementação dos handlers Application.

### 4. PORTAL DO DESENVOLVEDOR

| # | Funcionalidade | Módulo | Status | Ficheiros | Notas |
|---|----------------|--------|--------|-----------|-------|
| 4.1 | Catálogo pesquisável | DeveloperPortal | 🟡 PARCIAL | `SearchCatalog/` | Application stub com TODO |
| 4.2 | Detalhe de API com contrato renderizado | DeveloperPortal | 🟡 PARCIAL | `GetApiDetail/`, `RenderOpenApiContract/` | Application stubs com TODO |
| 4.3 | "Minhas APIs" | DeveloperPortal | 🟡 PARCIAL | `GetMyApis/` | Application stub com TODO |
| 4.4 | "APIs que eu consumo" | DeveloperPortal | 🟡 PARCIAL | `GetApisIConsume/` | Application stub com TODO |
| 4.5 | Timeline de eventos | DeveloperPortal | 🟡 PARCIAL | `GetAssetTimeline/` | Application stub com TODO |
| 4.6 | Health status | DeveloperPortal | 🟡 PARCIAL | `GetApiHealth/` | Application stub; depende de RuntimeIntelligence |

**O que falta nos itens 🟡 de DeveloperPortal:** Todos os handlers são stubs. Depende de EngineeringGraph e Contracts estarem completos (é read-model puro).

### 5. GESTÃO DE CONTRATOS

| # | Funcionalidade | Módulo | Status | Ficheiros | Notas |
|---|----------------|--------|--------|-----------|-------|
| 5.1 | Importação de contratos | Contracts | 🟡 PARCIAL | `ImportContract/`, `ContractVersion.cs` | Application stub com TODO |
| 5.2 | Versionamento semântico | Contracts | 🟡 PARCIAL | `CreateContractVersion/`, `SuggestSemanticVersion/` | Application stubs com TODO |
| 5.3 | Diff semântico | Contracts | 🟡 PARCIAL | `ComputeSemanticDiff/` | Application stub com TODO |
| 5.4 | Classificação breaking/non-breaking | Contracts | 🟡 PARCIAL | `ClassifyBreakingChange/` | Application stub com TODO |
| 5.5 | Sugestão de versão | Contracts | 🟡 PARCIAL | `SuggestSemanticVersion/` | Application stub com TODO |
| 5.6 | Lock de contrato | Contracts | 🟡 PARCIAL | `LockContractVersion/` | Application stub com TODO |
| 5.7 | Histórico de versões | Contracts | 🟡 PARCIAL | `GetContractHistory/` | Application stub com TODO |

**O que falta nos itens 🟡 de Contracts:** Todos os handlers são stubs. Infrastructure layer completa (DbContext, Repositories, Migrations), integração com parser de OpenAPI.

### 6. CHANGE INTELLIGENCE

| # | Funcionalidade | Módulo | Status | Ficheiros | Notas |
|---|----------------|--------|--------|-----------|-------|
| 6.1 | Recepção de notificação de deploy | ChangeIntelligence | ✅ COMPLETO | `NotifyDeployment/NotifyDeployment.cs`, `Release.cs` | Handler funcional com persist + Outbox |
| 6.2 | Classificação em 5 níveis | ChangeIntelligence | ✅ COMPLETO | `ClassifyChangeLevel/`, `ChangeLevel.cs` | Handler funcional; integrado com Contracts diff |
| 6.3 | Cálculo de Blast Radius | ChangeIntelligence | ✅ COMPLETO | `CalculateBlastRadius/`, `BlastRadiusReport.cs` | Handler funcional; consulta EngineeringGraph via IEngineeringGraphModule |
| 6.4 | Score de risco | ChangeIntelligence | ✅ COMPLETO | `ComputeChangeScore/`, `ChangeIntelligenceScore.cs` | Score 0.0–1.0 normalizado |
| 6.5 | Associação com work items | ChangeIntelligence | ✅ COMPLETO | `AttachWorkItemContext/`, `SyncJiraWorkItems/` | Handler funcional |
| 6.6 | Tracking de estado do deployment | ChangeIntelligence | ✅ COMPLETO | `UpdateDeploymentState/`, `DeploymentState.cs` | Handler funcional |
| 6.7 | Registro de rollbacks | ChangeIntelligence | ✅ COMPLETO | `RegisterRollback/` | Handler funcional |
| 6.8 | Plugins CI/CD | ChangeIntelligence | 🟡 PARCIAL | — | Webhook endpoint funcional; plugins específicos (GitHub Actions, Azure DevOps) ficam para MVP2 |

### 7. GOVERNANÇA E REGRAS

| # | Funcionalidade | Módulo | Status | Ficheiros | Notas |
|---|----------------|--------|--------|-----------|-------|
| 7.1 | Upload de rulesets | RulesetGovernance | ✅ COMPLETO | `UploadRuleset/`, `Ruleset.cs` | Handler funcional com persist |
| 7.2 | Binding de ruleset | RulesetGovernance | ✅ COMPLETO | `BindRulesetToAssetType/` | Handler funcional |
| 7.3 | Execução automática de linting | RulesetGovernance | ✅ COMPLETO | `ExecuteLintForRelease/` | Handler funcional com Spectral-compatible runner |
| 7.4 | Score de conformidade | RulesetGovernance | ✅ COMPLETO | `ComputeRulesetScore/`, `GetRulesetScore/` | Score 0.0–1.0 calculado por findings |
| 7.5 | Rulesets padrão | RulesetGovernance | ✅ COMPLETO | `InstallDefaultRulesets/` | Seeding de rulesets OpenAPI padrão |
| 7.6 | Findings com severidade | RulesetGovernance | ✅ COMPLETO | `GetRulesetFindings/`, `LintResult.cs` | Findings por severidade (Error/Warning/Info) |

### 8. WORKFLOW DE APROVAÇÃO

| # | Funcionalidade | Módulo | Status | Ficheiros | Notas |
|---|----------------|--------|--------|-----------|-------|
| 8.1 | Templates de workflow | Workflow | ✅ COMPLETO | `CreateWorkflowTemplate/`, `WorkflowTemplate.cs` | Handler funcional + DI registrado |
| 8.2 | Instanciação automática | Workflow | ✅ COMPLETO | `InitiateWorkflow/` | Handler funcional; cria instância + stages |
| 8.3 | Aprovação por stages | Workflow | ✅ COMPLETO | `ApproveStage/`, `WorkflowInstance.cs`, `WorkflowStage.cs` | Handler funcional; auto-advance quando todos completos |
| 8.4 | Rejeição com motivo | Workflow | ✅ COMPLETO | `RejectWorkflow/` | Handler funcional; comentário obrigatório |
| 8.5 | Request Changes | Workflow | ✅ COMPLETO | `RequestChanges/` | Handler funcional; lista de itens obrigatória |
| 8.6 | Comentários | Workflow | ✅ COMPLETO | `AddObservation/` | Handler funcional; observação sem impacto no fluxo |
| 8.7 | Evidence Pack automático | Workflow | ✅ COMPLETO | `GenerateEvidencePack/`, `GetEvidencePack/` | Handler funcional; scores + diff + hash |
| 8.8 | Exportação PDF | Workflow | ✅ COMPLETO | `ExportEvidencePackPdf/` | Dados estruturados para geração externa de PDF |
| 8.9 | SLA com escalação | Workflow | ✅ COMPLETO | `EscalateSlaViolation/` | Handler funcional; SlaPolicy por stage |

### 9. PROMOÇÃO ENTRE AMBIENTES

| # | Funcionalidade | Módulo | Status | Ficheiros | Notas |
|---|----------------|--------|--------|-----------|-------|
| 9.1 | Configuração de ambientes | Promotion | 🟡 PARCIAL | `ConfigureEnvironment/`, `Environment.cs` | Application stub com TODO |
| 9.2 | Criação de pedido de promoção | Promotion | 🟡 PARCIAL | `CreatePromotionRequest/`, `PromotionRequest.cs` | Application stub com TODO |
| 9.3 | Quality Gates | Promotion | 🟡 PARCIAL | `EvaluatePromotionGates/`, `PromotionGate.cs`, `GateEvaluation.cs` | Application stub com TODO |
| 9.4 | Override de gate | Promotion | 🟡 PARCIAL | `OverrideGateWithJustification/` | Application stub com TODO |
| 9.5 | Tracking de estado | Promotion | 🟡 PARCIAL | `GetPromotionStatus/`, `ListPromotionRequests/`, `ApprovePromotion/`, `BlockPromotion/` | Application stubs com TODO |

**O que falta nos itens 🟡 de Promotion:** Infrastructure layer, implementação dos handlers, integração com Workflow para verificar aprovação como gate.

### 10. RUNTIME INTELLIGENCE

| # | Funcionalidade | Módulo | Status | Ficheiros | Notas |
|---|----------------|--------|--------|-----------|-------|
| 10.1 | Ingestão de snapshots | RuntimeIntelligence | 🟡 PARCIAL | `IngestRuntimeSnapshot/`, `RuntimeSnapshot.cs` | Application stub com TODO |
| 10.2 | Detecção de drift | RuntimeIntelligence | 🟡 PARCIAL | `DetectRuntimeDrift/`, `GetDriftFindings/` | Application stubs com TODO |
| 10.3 | Score de observabilidade | RuntimeIntelligence | 🟡 PARCIAL | `GetObservabilityScore/`, `ComputeObservabilityDebt/` | Application stubs com TODO |
| 10.4 | Comparação de health | RuntimeIntelligence | 🟡 PARCIAL | `CompareReleaseRuntime/` | Application stub com TODO |
| 10.5 | Timeline de health | RuntimeIntelligence | 🟡 PARCIAL | `GetReleaseHealthTimeline/` | Application stub com TODO |
| 10.6 | Detecção de anomalias | RuntimeIntelligence | 🔲 FORA DO MVP1 | — | Não scaffoldado; MVP2 |

**O que falta nos itens 🟡 de RuntimeIntelligence:** Infrastructure layer, implementação dos handlers. MVP1 inclui apenas IngestRuntimeSnapshot e GetReleaseHealthTimeline.

### 11. COST INTELLIGENCE

| # | Funcionalidade | Módulo | Status | Ficheiros | Notas |
|---|----------------|--------|--------|-----------|-------|
| 11.1 | Ingestão de dados de custo | CostIntelligence | 🟡 PARCIAL | `IngestCostSnapshot/`, `CostSnapshot.cs` | Application stub com TODO |
| 11.2 | Atribuição de custo | CostIntelligence | 🟡 PARCIAL | `AttributeCostToService/` | Application stub com TODO |
| 11.3 | Tendências | CostIntelligence | 🔲 FORA DO MVP1 | `ComputeCostTrend/` | Application stub; MVP2 |
| 11.4 | Custo por release | CostIntelligence | 🟡 PARCIAL | `GetCostByRelease/` | Application stub com TODO |
| 11.5 | Delta de custo | CostIntelligence | 🔲 FORA DO MVP1 | `GetCostDelta/` | Application stub; MVP2 |
| 11.6 | Alertas de anomalia | CostIntelligence | 🔲 FORA DO MVP1 | `AlertCostAnomaly/` | Application stub; MVP2 |
| 11.7 | Relatório de custos | CostIntelligence | 🔲 FORA DO MVP1 | `GetCostReport/` | Application stub; MVP2 |

**O que falta nos itens 🟡 de CostIntelligence:** Infrastructure layer. MVP1 inclui apenas IngestCostSnapshot e GetCostByRelease.

### 12. INTELIGÊNCIA ARTIFICIAL

| # | Funcionalidade | Módulo | Status | Ficheiros | Notas |
|---|----------------|--------|--------|-----------|-------|
| 12.1 | Classificação por IA | AiOrchestration | 🔲 FORA DO MVP1 | `ClassifyChangeWithAI/` | Application stub; MVP2 |
| 12.2 | Sugestão de versão por IA | AiOrchestration | 🔲 FORA DO MVP1 | `SuggestSemanticVersionWithAI/` | Application stub; MVP2 |
| 12.3 | Resumo para aprovadores | AiOrchestration | 🔲 FORA DO MVP1 | `SummarizeReleaseForApproval/` | Application stub; MVP2 |
| 12.4 | Geração de testes Robot Framework | AiOrchestration | 🔲 FORA DO MVP1 | `GenerateTestScenarios/`, `GenerateRobotFrameworkDraft/` | Application stubs; MVP2 |
| 12.5 | Consulta em linguagem natural | AiOrchestration | 🔲 FORA DO MVP1 | `AskCatalogQuestion/` | Application stub; MVP2 |
| 12.6 | Integração com LLMs externos | ExternalAi | 🔲 FORA DO MVP1 | `QueryExternalAISimple/`, `QueryExternalAIAdvanced/` | Application stubs; MVP2 |
| 12.7 | Knowledge capture | ExternalAi + AiOrchestration | 🔲 FORA DO MVP1 | `CaptureExternalAIResponse/`, `ValidateKnowledgeCapture/` | Application stubs; MVP2 |

### 13. AUDITORIA E COMPLIANCE

| # | Funcionalidade | Módulo | Status | Ficheiros | Notas |
|---|----------------|--------|--------|-----------|-------|
| 13.1 | Registro imutável com hash chain | Audit | 🟡 PARCIAL | `RecordAuditEvent/`, `AuditEvent.cs`, `AuditChainLink.cs` | Domain modelado com hash; Application stub com TODO |
| 13.2 | Busca e filtro | Audit | 🟡 PARCIAL | `SearchAuditLog/` | Application stub com TODO |
| 13.3 | Verificação de integridade | Audit | 🟡 PARCIAL | `VerifyChainIntegrity/` | Application stub com TODO |
| 13.4 | Exportação de relatório | Audit | 🟡 PARCIAL | `ExportAuditReport/` | Application stub com TODO |
| 13.5 | Relatório de compliance | Audit | 🟡 PARCIAL | `GetComplianceReport/` | Application stub com TODO |
| 13.6 | Política de retenção | Audit | 🟡 PARCIAL | `ConfigureRetention/`, `RetentionPolicy.cs` | Application stub com TODO |

**O que falta nos itens 🟡 de Audit:** Infrastructure layer, implementação dos handlers, AuditInterceptor já produz eventos que precisam ser persistidos.

### 14. CLI

| # | Funcionalidade | Módulo | Status | Ficheiros | Notas |
|---|----------------|--------|--------|-----------|-------|
| 14.1 | `nex validate` | CLI | 🟡 PARCIAL | `tools/NexTraceOne.CLI/` | Scaffold existe; comandos são stubs |
| 14.2 | `nex release` | CLI | 🟡 PARCIAL | `tools/NexTraceOne.CLI/` | Scaffold existe; comandos são stubs |
| 14.3 | `nex promotion` | CLI | 🟡 PARCIAL | `tools/NexTraceOne.CLI/` | Scaffold existe; comandos são stubs |
| 14.4 | `nex approval` | CLI | 🟡 PARCIAL | `tools/NexTraceOne.CLI/` | Scaffold existe; comandos são stubs |
| 14.5 | `nex impact` | CLI | 🟡 PARCIAL | `tools/NexTraceOne.CLI/` | Scaffold existe; comandos são stubs |
| 14.6 | `nex tests` | CLI | 🔲 FORA DO MVP1 | — | Depende de AiOrchestration; MVP2 |
| 14.7 | `nex catalog` | CLI | 🟡 PARCIAL | `tools/NexTraceOne.CLI/` | Scaffold existe; comandos são stubs |

### 15. INTEGRAÇÕES

| # | Funcionalidade | Módulo | Status | Ficheiros | Notas |
|---|----------------|--------|--------|-----------|-------|
| 15.1 | CI/CD plugins | ChangeIntelligence.API | 🟡 PARCIAL | `ChangeIntelligenceEndpointModule.cs` | Endpoint webhook scaffoldado; plugins externos não implementados |
| 15.2 | Task Management: leitura | ChangeIntelligence | 🟡 PARCIAL | `SyncJiraWorkItems/`, `AttachWorkItemContext/` | Application stubs com TODO |
| 15.3 | Write-back task management | ChangeIntelligence | 🔲 FORA DO MVP1 | — | Não scaffoldado; MVP2 |
| 15.4 | Gateway Discovery | EngineeringGraph | 🟡 PARCIAL | `ImportFromKongGateway/` | Application stub; pode entrar no MVP1 |
| 15.5 | Catálogos externos | EngineeringGraph | 🔲 FORA DO MVP1 | `ImportFromBackstage/` | Stub existe; fora do MVP1 |
| 15.6 | OTLP endpoint | RuntimeIntelligence | 🟡 PARCIAL | `IngestRuntimeSnapshot/` | Application stub; precisa de endpoint OTLP dedicado |
| 15.7 | Cloud Cost | CostIntelligence | 🟡 PARCIAL | `IngestCostSnapshot/` | Application stub com TODO |

### 16. REQUISITOS NÃO-FUNCIONAIS

| # | Funcionalidade | Módulo | Status | Ficheiros | Notas |
|---|----------------|--------|--------|-----------|-------|
| 16.1 | Self-hosted | Platform | ✅ IMPLEMENTADO | `ApiHost/Program.cs`, `BackgroundWorkers/` | Deployment standalone sem dependências externas pagas |
| 16.2 | Multi-tenant RLS | BuildingBlocks.Infrastructure | ✅ IMPLEMENTADO | `TenantRlsInterceptor.cs`, `NexTraceDbContextBase.cs` | RLS automático via interceptor EF Core |
| 16.3 | Encryption at rest AES-256-GCM | BuildingBlocks.Security | ✅ IMPLEMENTADO | `EncryptedStringConverter.cs` | Converter automático em EF Core |
| 16.4 | Assembly integrity | BuildingBlocks.Security | ✅ IMPLEMENTADO | `AssemblyIntegrityChecker.cs` | Verificação SHA-256 no boot |
| 16.5 | Hardware fingerprint | BuildingBlocks.Security | ✅ IMPLEMENTADO | `HardwareFingerprint.cs` | Fingerprint via identificadores do host |
| 16.6 | Audit trail com hash chain | BuildingBlocks.Infrastructure | ✅ IMPLEMENTADO | `AuditInterceptor.cs` (gera eventos) | Interceptor captura; módulo Audit persiste |
| 16.7 | i18n (en-US, pt-BR) | BuildingBlocks.Application | ✅ IMPLEMENTADO | `SharedMessages.resx`, `SharedMessages.pt-BR.resx` | ErrorLocalizer + IStringLocalizer |
| 16.8 | OpenAPI spec | Platform | 🟡 PARCIAL | `ApiHost/Program.cs` | `AddOpenApi()` registrado; endpoints ainda stubs |
| 16.9 | Health checks | BuildingBlocks.Observability | ✅ IMPLEMENTADO | `HealthChecks/` | Liveness e readiness configurados |
| 16.10 | Structured logging | BuildingBlocks.Observability | ✅ IMPLEMENTADO | `SerilogConfiguration.cs` | Serilog estruturado com enrichers |
| 16.11 | Distributed tracing | BuildingBlocks.Observability | ✅ IMPLEMENTADO | `NexTraceActivitySources.cs` | OpenTelemetry com OTLP exporter |
| 16.12 | Custom metrics | BuildingBlocks.Observability | ✅ IMPLEMENTADO | `NexTraceMeters.cs` | Prometheus-compatible via OTel Meters |

---

### Totais

| Status | Quantidade |
|--------|-----------|
| ✅ IMPLEMENTADO | **38** |
| 🟡 PARCIAL | **79** |
| ❌ NÃO EXISTE (MVP1) | **1** (Plugins CI/CD nativos) |
| 🔲 FORA DO MVP1 (MVP2) | **25** |
| **Total de funcionalidades** | **143** |

**Percentagem de cobertura funcional atual:** ~27% (38/143 totalmente implementadas)
**Cobertura do fluxo crítico MVP1:** 100% scaffoldado, 20% implementado funcionalmente

---

## FASE 3 — Definição do MVP1 Funcional Mínimo Viável

### 3.1 Fluxo End-to-End do MVP1

O MVP1 DEVE entregar este fluxo completo e funcional:

```
Pipeline CI/CD → POST /api/deployments/notify
  ↓
ChangeIntelligence.NotifyDeployment
  → ClassifyChangeLevel (compara com contrato anterior via Contracts)
  → CalculateBlastRadius (consulta grafo via EngineeringGraph)
  → ComputeChangeScore (score 0.0–1.0)
  → ExecuteLintForRelease (RulesetGovernance)
  ↓
Workflow.InitiateWorkflow (baseado no ChangeLevel)
  → GenerateEvidencePack (blast radius + score + diff + linting + work items)
  → ApproveStage (aprovador recebe notificação, aprova/rejeita)
  ↓
Promotion.CreatePromotionRequest
  → EvaluatePromotionGates (linting ✓, workflow aprovado ✓, blast radius aceitável ✓)
  → ApprovePromotion
  ↓
Audit.RecordAuditEvent (em CADA passo acima)
  → VerifyChainIntegrity (hash chain SHA-256 imutável)
```

### 3.2 Módulos MVP1 Obrigatórios

#### 🔴 CRÍTICOS (MVP1 sem exceção)

| Módulo | Escopo MVP1 | Estado Atual | Esforço Restante |
|--------|-------------|-------------|-----------------|
| **Identity** | 100% completo | ✅ 100% | ✅ Zero |
| **Licensing** | Ativação + Verificação no boot + Capabilities + Tracking | ✅ 100% | ✅ Zero |
| **EngineeringGraph** | RegisterApiAsset, RegisterServiceAsset, MapConsumerRelationship, GetAssetGraph, InferDependencyFromOtel | ✅ 100% | ✅ Zero |
| **Contracts** | ImportContract, ComputeSemanticDiff, ClassifyBreakingChange, SuggestSemanticVersion, GetContractHistory | ✅ 100% | ✅ Zero |
| **ChangeIntelligence** | NotifyDeployment, ClassifyChangeLevel, CalculateBlastRadius, ComputeChangeScore, UpdateDeploymentState, RegisterRollback, GetRelease | ✅ 100% | ✅ Zero |
| **RulesetGovernance** | UploadRuleset, InstallDefaultRulesets, BindRulesetToAssetType, ExecuteLintForRelease, GetRulesetFindings | ✅ 100% | ✅ Zero |
| **Workflow** | CreateWorkflowTemplate, InitiateWorkflow, ApproveStage, RejectWorkflow, RequestChanges, AddObservation, GetWorkflowStatus, ListPendingApprovals, GenerateEvidencePack, GetEvidencePack, ExportEvidencePackPdf, EscalateSlaViolation | ✅ 100% | ✅ Zero |
| **Promotion** | ConfigureEnvironment, CreatePromotionRequest, EvaluatePromotionGates, ApprovePromotion, OverrideGateWithJustification | 🟡 Domain+App scaffold | Infrastructure + Handlers |
| **Audit** | RecordAuditEvent, SearchAuditLog, VerifyChainIntegrity, ExportAuditReport | 🟡 Domain+App scaffold | Infrastructure + Handlers |
| **CLI** | `nex validate`, `nex release`, `nex impact`, `nex approval` | 🟡 Scaffold | Implementação dos comandos |

#### 🟡 OPCIONAIS MVP1 (se a base já permitir)

| Módulo | Escopo Parcial MVP1 | Dependência |
|--------|--------------------|----|
| **DeveloperPortal** | SearchCatalog, GetApiDetail, GetMyApis, GetApisIConsume | EngineeringGraph + Contracts |
| **RuntimeIntelligence** | IngestRuntimeSnapshot, GetReleaseHealthTimeline | EngineeringGraph |
| **CostIntelligence** | IngestCostSnapshot, GetCostByRelease | ChangeIntelligence |

#### ❌ EXCLUÍDOS DO MVP1 (→ MVP2)

- AiOrchestration (todos os features)
- ExternalAi (todos os features)
- Write-back para task management (escrita em Jira/ADO)
- Gateway discovery automático (Kong, APIM)
- Importação de Backstage/Consul/K8s
- `nex tests` (depende de AI)
- Robot Framework generation
- Cost trends, cost delta, cost anomaly alerts
- Runtime anomaly detection

### 3.3 Cronograma MVP1 Revisado

| Semana | Módulo | Deliverable |
|--------|--------|-------------|
| **5** _(concluída)_ | Licensing | ✅ Infrastructure completa, Migrations, Repositories |
| **6–7** _(concluída)_ | EngineeringGraph | ✅ Infrastructure + Handlers funcionais + Testes |
| **8–9** _(concluída)_ | Contracts | ✅ Infrastructure + OpenAPI parser + Diff semântico + Testes |
| **10–13** _(concluída)_ | ChangeIntelligence | ✅ Infrastructure + Blast Radius end-to-end + Testes |
| **14–15** _(concluída)_ | RulesetGovernance | ✅ Infrastructure + Spectral runner + Linting + Testes |
| **16–18** _(concluída)_ | Workflow | ✅ Infrastructure + Evidence Pack + PDF export + IWorkflowModule + Testes |
| **19–20** | Promotion | Infrastructure + Gates + Aprovação + Testes |
| **21–22** | Audit | Infrastructure + Hash Chain + Export + Testes |
| **23** | DeveloperPortal | Read-model handlers + Testes |
| **24** | RuntimeIntelligence (parcial) | IngestSnapshot + HealthTimeline + Testes |
| **25** | CostIntelligence (parcial) | IngestSnapshot + GetCostByRelease + Testes |
| **26** | CLI | Comandos essenciais + Integração com Contracts API |

### 3.4 Critérios de Conclusão do MVP1

O MVP1 está **COMPLETO** quando:

1. ✅ Um pipeline CI/CD consegue notificar um deploy via webhook
2. ✅ O sistema classifica automaticamente o nível de mudança (N0–N4)
3. ✅ O blast radius mostra quais serviços são impactados
4. ✅ Um workflow de aprovação é iniciado automaticamente para mudanças N2+
5. ✅ Um aprovador consegue ver o evidence pack e aprovar/rejeitar
6. ✅ Uma promoção de ambiente é bloqueada se os gates não passarem
7. ✅ Todos os eventos acima estão registrados no audit trail com hash chain
8. ✅ `nex validate` roda localmente antes do push
9. ✅ `nex impact` mostra o blast radius de uma mudança hipotética
10. ✅ A licença é verificada no boot e capabilities controlam acesso

---

*Documento gerado em Março 2026 · Ver também: `docs/ROADMAP.md`, `docs/MVP1-EXPANDED-PLAN.md`*
