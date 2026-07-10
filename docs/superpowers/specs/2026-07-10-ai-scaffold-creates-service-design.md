# P5 — Fechar a jornada do AI-Scaffold ("cria serviço") (design)

**Data:** 2026-07-10
**Autor:** Claude Opus 4.8 (autonomia total concedida pelo owner)
**Persona:** developer que faz scaffold de um novo serviço a partir de um
template e quer que ele **exista no catálogo** (registado, pronto para continuar
o setup), não apenas um ZIP no disco.

## Contexto auditado — os 3 candidatos deferidos da J2 Fase 2

- **DevPortal tabs** — a `DeveloperPortalPage` já está redesenhada (6 tabs DS
  Betterstack: catalog/subscriptions/playground/myConsumption/inbox/analytics,
  i18n, subcomponentes `DevPortal*Tab`). **Feito.** Sem gap honesto.
- **Templates internos redesign** — polish visual das páginas de template
  (`TemplateLibraryPage`/`TemplateDetailPage`/`TemplateEditorPage`). Valor baixo.
- **AI-scaffold-cria-serviço** — **o gap real.** O `AiScaffoldWizardPage`
  (`/catalog/templates/:id/scaffold`, wizard de 4 passos template→intent→generate
  →review) termina o passo review em **Download ZIP** apenas. O developer
  forneceu no passo intent tudo o que é preciso para criar o serviço
  (`serviceName`, `serviceDescription`, `teamName`, `domain`) e o template
  fornece `serviceType`/`defaultDomain`/`defaultTeam` — mas **o serviço nunca é
  criado no catálogo**. Beco de jornada, simétrico ao "serviço-criado-cedo" do
  onboarding J2 (ciclo 26).

## Problema

A jornada "template → scaffold → serviço no catálogo" está cortada no fim: o
wizard gera código e para. Não há caminho para registar o serviço nem para
continuar o setup (contratos, interface) a partir do que foi scaffoldado.

## Fator habilitador (honesto)

`serviceCatalogApi.registerService({ name, domain, team, description?,
serviceType?, ... }) => Promise<{ id: string }>` já existe e é a **mesma** API
usada pelo onboarding wizard (`useOnboardWizard.ts`) e pelo `ServiceDetailPage`.
O formulário de intent do scaffold mapeia diretamente:
- `name` ← `serviceName`
- `domain` ← `domain || template.defaultDomain`
- `team` ← `teamName || template.defaultTeam`
- `description` ← `serviceDescription`
- `serviceType` ← `template.serviceType`

## Fatia

### F1 — "Criar serviço no catálogo" no passo de review

No Step 4 (`step === 'review'`) do `AiScaffoldWizardPage`, na zona de ações
(hoje só "Download ZIP"), adicionar a ação **primária** "Criar serviço no
catálogo":

- Nova mutation `createServiceMutation` que chama
  `serviceCatalogApi.registerService({ name: serviceName, domain: domain ||
  template.defaultDomain, team: teamName || template.defaultTeam, description:
  serviceDescription, serviceType: template.serviceType })`.
- Ao sucesso, `navigate('/services/' + res.id)` (detalhe do serviço criado, onde
  o checklist de setup continua). Guard contra duplo-registo: o botão desativa
  enquanto `isPending` e a navegação ao sucesso tira o utilizador da página.
- Ao erro, mostrar mensagem honesta (banner/texto) sem navegar.
- "Download ZIP" passa a ação **secundária** (variante outline/ghost); o código
  continua disponível.
- O botão de criar fica desativado se `!serviceName` (invariante já garantida
  para chegar ao review, mas defensivo).

## i18n

Novas chaves nos 4 locales (`en`, `es`, `pt-BR`, `pt-PT`) via script deep-merge;
`npm run validate:i18n` tem de passar. Chaves previstas:
- `templates.scaffold.review.createService`
- `templates.scaffold.review.createServiceError`

## Testes

Vitest + Testing Library, centralizados em `src/frontend/src/__tests__/**`.
Forward-only. Cobrir: (a) o passo review renderiza o botão "Criar serviço no
catálogo"; (b) clicar chama `registerService` com o payload mapeado correto;
(c) ao sucesso navega para `/services/:id`. Revisão final opus antes do merge
direto em `main` (sem PR).

## Não-objetivos

- Não ligar código gerado → repositório git (sem API; o ZIP é a fronteira).
- Não tocar no DevPortal (já redesenhado) nem no redesign visual de templates.
- Não criar interface/contrato automaticamente no scaffold — o serviço criado
  leva o developer ao detalhe, onde o checklist de setup guia os próximos passos
  (mesma filosofia do onboarding).
