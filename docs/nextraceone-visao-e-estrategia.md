# NexTraceOne — Visão, Estratégia de Mercado e Plano de Evolução

**Data:** 12 de junho de 2026
**Natureza:** documento estratégico de produto — base para pitch, materiais comerciais e conversas com investidores/adquirentes.

---

## 1. O que é o NexTraceOne

**NexTraceOne é uma plataforma de gestão do ciclo de vida de serviços de software** — da criação do serviço à operação em produção — para empresas que não podem (ou não querem) pagar e operar o combo Dynatrace + ServiceNow + PagerDuty + LaunchDarkly.

Em uma frase para o pitch:

> *"Do nascimento do serviço à release governada — e quando der incidente, você sabe em segundos qual mudança causou."*

A plataforma unifica em um único produto multi-tenant SaaS:

| Pilar | O que faz | Equivalente de mercado |
|---|---|---|
| **Catálogo de Serviços & Contratos** | Registro de serviços, contratos de API multiprotocolo (REST/eventos/SOAP/data), SBOM, developer portal, descoberta automática | Backstage, Port, Cortex, OpsLevel |
| **Change Intelligence** (núcleo) | Workflow de promoção com gates e aprovações, blast radius scoring, evidence packs assinados, rulesets/linting, SLA de workflow | ServiceNow Change Management (parcial) |
| **Incidentes & Operação** | Ciclo de vida de incidente, **correlação automática release↔incidente**, postmortems, on-call intelligence, status page pública | PagerDuty/incident.io (parcial) |
| **Observabilidade** | Ingestão OpenTelemetry, armazenamento ClickHouse, dashboards correlacionados a serviços e releases | Dynatrace/Datadog (parcial, sem agentes) |
| **IA aplicada** | Chat e RAG sobre o conhecimento operacional (Ollama local ou OpenAI), sugestões de causa raiz, busca semântica | Camada AI dos concorrentes premium |
| **Governança & Compliance** | Trilha de auditoria, assinaturas digitais, frameworks de conformidade, export PDF/Excel | Módulos GRC enterprise |
| **SaaS completo** | Signup self-service, trial 14 dias, planos com enforcement de capabilities, billing Stripe, multi-tenant com RLS no banco | — |

### Arquitetura (resumo para due diligence técnica)

- **Backend**: .NET 10, monólito modular com 9 bounded contexts (DDD/Clean Architecture/CQRS), PostgreSQL 16 com Row-Level Security por tenant, ClickHouse para analytics, outbox pattern, OpenTelemetry nativo.
- **Frontend**: React 19 + TypeScript, ~300 telas, i18n em 4 idiomas (pt-BR, pt-PT, en, es).
- **Volume**: ~125 mil linhas de código de produção + ~200 mil de testes (taxa 2.6:1).
- **Segurança**: JWT multi-tenant, OIDC/SAML, TOTP, RLS, rate limiting, CSRF, criptografia de campo AES-256-GCM, modo air-gapped.
- **Deploy**: Docker Compose (dev), Helm/Kubernetes (prod), CI/CD completo no GitHub Actions.

---

## 2. Estratégia de mercado

### 2.1 O posicionamento: "bundle bom-o-suficiente" para o mid-market

A tese **não é** competir feature a feature com Dynatrace ou ServiceNow. É atender a empresa que hoje **não tem nenhuma dessas ferramentas** porque:

1. **Preço**: um time médio gasta US$ 5.000–20.000/mês só de Datadog; ServiceNow é projeto de 6 dígitos. No Brasil, tudo cobrado em dólar.
2. **Complexidade**: essas ferramentas exigem equipes dedicadas para operar.
3. **Fragmentação**: mesmo quem compra precisa de 4-5 fornecedores que não conversam entre si.

Esse playbook tem precedentes vencedores: **Better Stack** (observabilidade + incidentes + status page num bundle a partir de US$ 29/mês), **Freshworks** ("ServiceNow para quem não aguenta o ServiceNow" — comprou a FireHydrant em dez/2025), **Zoho** (vs Salesforce).

### 2.2 Mercado endereçável

- IDP/portais de desenvolvedor: Gartner projeta **75% das organizações com platform engineering usando IDP até 2026** (45% em 2023). Port levantou US$ 100M a um valuation de US$ 800M (dez/2025).
- Change management: mercado de ~US$ 4,2 bi (2025); DevOps total caminhando para US$ 47 bi até 2030 (CAGR 25,8%).
- Gestão de incidentes: incident.io a US$ 400M de valuation; FireHydrant adquirida pela Freshworks — **o segmento está em fase ativa de consolidação por aquisição** (relevante para a tese de exit).

### 2.3 Segmento-alvo e go-to-market

**ICP (perfil de cliente ideal) inicial:**
- Empresas de 50–500 desenvolvedores no Brasil/LATAM;
- Setores com pressão regulatória de governança de mudanças: **fintechs, bancos médios, seguradoras, healthtechs** (BACEN, SOX, LGPD — auditoria de mudança é obrigação, não luxo);
- Sem ferramenta consolidada hoje (planilha + Slack + Grafana solto).

**Motores de aquisição (em ordem de implantação):**
1. **Self-service / PLG**: signup → trial 14 dias → upgrade via Stripe (já implementado). Conteúdo em português sobre governança de mudanças como canal orgânico.
2. **Venda assistida mid-market**: demo da jornada completa em 30 minutos; pricing em reais com previsibilidade (diferencial direto contra cobrança em dólar por consumo).
3. **Parcerias**: consultorias de cloud/DevOps brasileiras como canal de implantação (elas ganham o serviço, NexTraceOne ganha a licença).

**Pricing sugerido (validar com pilotos):**
- **Trial** 14 dias (sem cartão) → **Starter** (gratuito ou ~R$ 990/mês, catálogo + incidentes básicos) → **Professional** (~R$ 4.990/mês, governança completa + IA) → **Enterprise** (sob consulta: SSO/SAML, air-gapped, multi-region, auditoria avançada).
- Observabilidade cobrada por volume de ingestão (é o único custo que escala com uso do cliente — nunca dar de graça ilimitado).

### 2.4 Regras estratégicas que protegem o produto

1. **Reimplementar fluxo de trabalho, nunca infraestrutura pesada**: receber OpenTelemetry (nunca construir agentes), governar feature flags (nunca construir runtime de flags), referenciar segredos (nunca ser cofre).
2. **A cola é o produto**: nenhum módulo isolado vence o especialista; a correlação serviço→contrato→release→incidente→telemetria é o que ninguém no preço oferece.
3. **Integração como porta de entrada híbrida**: quem já tem Datadog/PagerDuty entra pela camada de governança; quem não tem nada entra pelo bundle.

---

## 3. Funcionalidades (estado real, junho/2026)

### Prontas e demonstráveis
- Registro/ciclo de vida de serviços com wizard; contratos multiprotocolo com versões, diff semântico, scorecards e verificação de breaking changes
- Promoção entre ambientes com gates, aprovações (ator autenticado anti-spoofing), override com justificativa e **notificações automáticas** (e-mail/Teams/Slack/webhook)
- Blast radius com consumidores diretos+transitivos; evidence packs com assinatura digital e export PDF
- Incidentes: criar → correlacionar a releases (motor real: janela temporal + interseção de serviço + blast radius) → resolver; timeline; postmortems
- **Status page pública** por tenant (anônima, auto-refresh)
- **Webhook inbound do GitHub**: deployment vira release automaticamente (HMAC + API key)
- Ingestão OpenTelemetry → ClickHouse com isolamento por tenant; dashboards de requests/erros/saúde
- IA: chat com RAG (Qdrant), Ollama local por padrão (LGPD-friendly: o dado não sai de casa), quotas de token por tenant
- **Funil comercial completo**: signup público → trial → ativação por e-mail → enforcement de capabilities por plano → upgrade via Stripe Checkout → downgrade automático em cancelamento
- SBOM e registro de governança de feature flags (inventário, flags obsoletas, risco)
- Developer portal, descoberta de serviços, ativos legados (mainframe/COBOL — nicho raro e valioso em bancos brasileiros)

### Em evolução (gaps conhecidos e mapeados)
- UIs de rulesets e configuração de SLA (backend pronto)
- On-call com escala de plantão (hoje: só inteligência retrospectiva)
- Webhook inbound GitLab/Azure DevOps (GitHub pronto; padrão estabelecido)
- Eventos Stripe de falha de pagamento/dunning
- Canary com lógica de decisão automática
- Alguns relatórios analíticos cross-module ainda servidos por leitores placeholder (padrão honest-null documentado)

---

## 4. Diferenciais defensáveis

1. **Correlação mudança→incidente como cidadã de primeira classe.** Dynatrace detecta o sintoma; ServiceNow registra o processo; PagerDuty acorda alguém. Nenhum deles responde nativamente "qual release causou isso?" cruzando contrato, blast radius e janela de deploy. O NexTraceOne nasceu para essa pergunta.
2. **Evidence packs assinados digitalmente.** Para empresas reguladas, transformar cada release em um pacote de evidências auditável (gates passados, aprovador, score de risco, assinatura) é exatamente o que auditoria interna/BACEN/SOX pede — e hoje é feito em planilha.
3. **IA local-first.** Ollama por padrão significa que prompts e contexto operacional nunca saem da infraestrutura do cliente — argumento decisivo para o ICP regulado, impossível de replicar por concorrentes SaaS-only.
4. **Suporte a ativos legados (mainframe/COBOL/CICS).** Nenhum IDP moderno mapeia z/OS. Bancos brasileiros vivem disso. É um cunho de entrada quase sem concorrência.
5. **Custo total e moeda.** Bundle em reais com preço previsível vs 4 fornecedores cobrando em dólar por consumo.
6. **Air-gapped por design.** Enforcement de rede em todos os HttpClients permite vender para ambientes desconectados (governo, defesa, infra crítica) — mercado que os SaaS globais ignoram.

---

## 5. Plano de evolução

### Horizonte 1 — Validação (0–6 meses) · objetivo: 5 clientes pagantes

A engenharia está à frente do negócio; este horizonte é quase todo comercial.

| # | Ação | Critério de sucesso |
|---|---|---|
| 1 | **3 pilotos design partners** (fintech/banco médio) — gratuitos por 90 dias em troca de feedback semanal e case público | 3 pilotos ativos usando a jornada completa |
| 2 | Hardening do caminho do piloto: onboarding em <1h (collector OTel + webhook GitHub + 1º serviço no catálogo), docs de cliente em português | Piloto se ativa sozinho sem chamada de suporte |
| 3 | Completar gaps que pilotos sentirem primeiro (provável: UI de rulesets, GitLab webhook, on-call básico) | Backlog dirigido por uso real, não por roadmap interno |
| 4 | Iniciar **SOC 2 Type 1** (escopo mínimo) + página de segurança/trust center | Relatório Type 1 emitido (destrava deals e valoriza o ativo) |
| 5 | Pricing público + conversão dos pilotos | ≥2 pilotos convertidos em pagantes |

**Não fazer neste horizonte:** módulos novos, multi-region, marketplace, mobile.

### Horizonte 2 — Tração (6–18 meses) · objetivo: R$ 1–3M ARR

| # | Ação | Racional |
|---|---|---|
| 1 | PLG completo: free tier do catálogo, telemetria de ativação, e-mails de ciclo de vida do trial | Funil mensurável → CAC baixo |
| 2 | Programa de parceiros (consultorias cloud/DevOps) com margem de revenda | Escala comercial sem time grande de vendas |
| 3 | Profundidade no diferencial: RCA assistido por IA usando a correlação como contexto; score de risco preditivo de release | Aumenta o moat onde já se ganha |
| 4 | Integrações de entrada: GitLab, Azure DevOps, Jira bidirecional, Datadog/Grafana como fontes | Reduz fricção de adoção em quem já tem stack |
| 5 | SOC 2 Type 2 + ISO 27001 roadmap | Pré-requisito para mid-market grande e para due diligence |
| 6 | 1ª contratação comercial sênior + 1-2 devs | Fundador sai do gargalo |

### Horizonte 3 — Escala ou Exit (18–36 meses)

**Caminho A — Crescer:** expansão LATAM (es já suportado no produto), vertical packs (fintech compliance pack com templates BACEN), receita de plataforma (API pública + SDK já existem). Meta: R$ 10M+ ARR, série A com tese "Backstage comercial da América Latina".

**Caminho B — Venda estratégica.** O segmento está consolidando por aquisição (Freshworks↔FireHydrant, Datadog↔Eppo). Compradores naturais e o porquê:

| Comprador potencial | Tese de aquisição |
|---|---|
| **TOTVS / Softplan / Sankhya** | Gigantes brasileiros de software B2B sem oferta de engineering platform; compram presença + produto pronto multi-tenant |
| **Freshworks / Zoho** | Estratégia explícita de bundle mid-market; NexTraceOne adiciona governança+catálogo que eles não têm |
| **Stefanini / CI&T / Globant** | Integradoras que ganham um produto proprietário para ancorar contratos de transformação (o suporte a mainframe é ouro aqui) |
| **Datadog / Dynatrace / ServiceNow** | Aquisição de entrada no mid-market LATAM ou acqui-hire da camada de change intelligence |

**O que maximiza o valor de venda (preparar desde já):**
1. **ARR recorrente com churn baixo** — nada vale mais; até R$ 1M ARR com 10-20 logos já muda a conversa de "código" para "negócio".
2. **Propriedade intelectual limpa**: CLA/registro de autoria, dependências auditadas (CI já assina artefatos), nenhum código de terceiros sem licença clara.
3. **Compliance**: SOC 2 reduz semanas de due diligence.
4. **Documentação de arquitetura** (ADRs já existem) + baixa dependência do fundador (runbooks, onboarding de eng documentado).
5. **Casos com marcas reconhecíveis** no segmento regulado.

### Riscos honestos e mitigação

| Risco | Mitigação |
|---|---|
| Fundador único (bus factor, velocidade comercial) | Horizonte 2 prioriza 1ª contratação; documentação e testes já mitigam o lado técnico |
| Gigantes descerem ao mid-market com bundle barato | Velocidade + foco regulatório local (compliance BR é defensável) + IA local-first |
| Custo de infra da observabilidade comer a margem | Pricing por volume de ingestão desde o dia 1; retenção hot/warm/cold já implementada |
| Amplitude do produto diluir o foco | Regra de ouro: toda sprint precisa servir à correlação mudança↔incidente ou ao funil; o resto espera |
| Zero validação de mercado até hoje | Horizonte 1 inteiro existe para resolver isso antes de qualquer outro investimento |

---

## 6. Resumo executivo (versão de 30 segundos)

O NexTraceOne é a plataforma que dá a empresas de 50–500 desenvolvedores — começando pelo mercado regulado brasileiro — o que hoje só existe somando 4 ferramentas americanas cobradas em dólar: catálogo de serviços, governança de mudanças com evidência auditável, incidentes correlacionados automaticamente à release que os causou, observabilidade OpenTelemetry e IA que roda dentro de casa. O produto está tecnicamente pronto para pilotos, com funil comercial self-service completo (trial → Stripe). O plano: 5 clientes pagantes em 6 meses validando o diferencial de correlação+compliance, R$ 1–3M ARR em 18 meses via PLG+parceiros, e a partir daí escolher entre escalar na LATAM ou vender para um consolidador — num segmento que está comprando ativamente.
