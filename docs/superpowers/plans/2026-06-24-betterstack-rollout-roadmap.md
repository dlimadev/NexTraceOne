# Betterstack v5 — Roadmap dos módulos restantes (controlos→DS)

**Data:** 2026-06-24
**Contexto:** continuação de `project_betterstack_redesign` (ciclos 14–16). Já concluídos ao nível dos
controlos: **contratos** (ciclos 9–15) e **catalog/serviços** (ciclo 16). Tema/tokens já aplicados app-wide.

## Objetivo

Levar os restantes módulos do frontend ao mesmo padrão de jornada v5: **zero controlos HTML crus**
(`<button>/<input>/<select>/<textarea>` → DS de `shared/ui`) e **zero cores cruas/`dark:`** (→ tokens
semânticos), preservando comportamento e mantendo os testes verdes. NÃO é redesign de layout — é
substituição de controlos + sweep de cores, cirúrgico, página a página.

## Auditoria (2026-06-24) — ficheiros com controlos crus, por módulo

| Prioridade | Módulo | Páginas | Componentes | Ocorrências | Notas |
|---|---|---:|---:|---:|---|
| P1 | **operations** | 28 | 0 | 117 | uso diário; só páginas; alto impacto |
| P1 | **change-governance** | 19 | 4 | 135 | core de mudanças; GraphQL |
| P1 | **identity-access** | 8 | 0 | 35 | login/RBAC/sessões; sensível |
| P2 | **governance** | 41 | 28 | 275 | **o maior** — partir por subdomínio |
| P2 | **ai-hub** | 15 | 8 | 109 | AI hub/agents/models |
| P2 | **platform-admin** | 22 | 0 | 84 | admin; muitas páginas |
| P3 | **configuration** | 11 | 5 | 78 | flags/settings |
| P3 | **product-analytics** | 10 | 0 | 33 | telemetria de produto |
| P3 | **knowledge** | 6 | 0 | 17 | runbooks/docs |
| P3 | **integrations** | 4 | 0 | 19 | CI/CD/webhooks |
| P4 | **notifications** | 2 | 2 | 18 | pequeno |
| P4 | **saas** | 1 | 0 | 6 | pequeno |
| P4 | **operational-intelligence** | 1 | 0 | 13 | 1 página densa |
| P4 | **observability** | 1 | 1 | 2 | quase nada (TraceExplorer deferido) |
| P4 | **legacy-assets** | 1 | 0 | 4 | pequeno |
| P4 | **audit-compliance** | 1 | 0 | 7 | pequeno |

**Total ≈ 171 páginas + 56 componentes ≈ 227 ficheiros.** As ocorrências são um **limite superior** do
trabalho — nem todo `<button>` é defeito (cards clicáveis, tabs por role já corretas); exige juízo por
página. A maioria das páginas tem teste dedicado (gate forte, como em catalog).

## Faseamento recomendado (cada fase = 1 sub-projeto/ciclo, processo dos ciclos 14–16)

- **Fase 1 — operations** (28 páginas). Maior impacto diário. Partir em 2–3 vagas por subdomínio
  (incidentes/SLO, runbooks/oncall, custo/reliability, etc.). Cuidado: `TraceExplorerPage` está
  **deferido** (refatoração própria [[project_trace_refactor]]) — excluir.
- **Fase 2 — change-governance** (19 pág + 4 comp). Core de mudanças; verificar GraphQL/subscriptions
  intactos. 1–2 vagas.
- **Fase 3 — identity-access** (8 páginas). Sensível (auth/RBAC/sessões/break-glass) — testes
  rigorosos; não tocar lógica de segurança. 1 vaga.
- **Fase 4 — governance** (41 pág + 28 comp). **O maior bloco** — partir por subdomínio (SLO/SLA,
  incidentes/RCA, policy/risk, compliance/reports, alertas) em 4–6 vagas. Atenção a `NotebookEditorPage`
  (editor — deferido) e a quaisquer editores/charts.
- **Fase 5 — ai-hub** (15 pág + 8 comp). Hub/agents/models/marketplace. 2 vagas. Não tocar runtime LLM.
- **Fase 6 — platform-admin** (22 páginas). Admin (users/health/integrations). 2 vagas. `BrandingAdmin`
  tem color pickers — preservar.
- **Fase 7 — configuration + product-analytics + knowledge + integrations** (≈31 ficheiros). Agrupar;
  2–3 vagas.
- **Fase 8 — tails** (notifications, saas, operational-intelligence, observability, legacy-assets,
  audit-compliance ≈ 8 ficheiros). Quick wins — 1 vaga.

## Processo por fase (igual ciclos 14–16)

1. Auditar o módulo (controlos + cores full-palette), separar páginas vs componentes, listar testes.
2. Spec curto + plano (uma tarefa por ficheiro, vagas por subdomínio). Branch `redesign/betterstack-<modulo>`.
3. Subagent-driven: 1 página/commit; gate por página = teste da página (se existir) + `npm run lint`+`build` +
   grep de confirmação. Review por vaga + final review opus. Fixes. Merge local + push.
4. Atualizar memória (`project_betterstack_redesign`) com o ciclo.

## Convenções/lições a aplicar (de [[reference_button_aschild_not_implemented]] e ciclos 14–16)

- DS `Button` faz `disabled={disabled||loading}` interno → `disabled={X}`+`loading={Y}` ≡ `disabled={X||Y}`.
- `Button.asChild` **não implementado** → link-como-botão via `useNavigate`, nunca `<Link><Button>`.
- `Select` sem `<optgroup>` (achatar, sem perder opções); placeholder = opção **disabled** → para opção
  vazia selecionável pôr `{value:'',label}` em `options`.
- `SearchInput` → role ARIA `searchbox` (ajustar testes `textbox`→`searchbox` quando preciso).
- `IconButton` exige `label`; `Toggle` tem `label`→aria-label; `TextField`/`TextArea` **não** renderizam
  asterisco para `required` (acrescentar ` *` ao label se for campo obrigatório visível).
- **Taxonomias intencionais ficam raw** quando não há token DS: severidade CVE "High" = `orange-500`;
  cores por linguagem/tipo (`purple`/`orange`). Documentar com comentário inline.
- **Editores/charts não são troca cega:** Monaco, `TraceExplorerPage`, `NotebookEditorPage`, color
  pickers de branding → excluir ou tratamento dedicado.
- Diff hygiene: `git add` só paths explícitos; verificar `git diff --name-only` por vaga.

## Critério de "módulo concluído"

`grep -rlE "<(button|input|select|textarea)\b" src/frontend/src/features/<modulo>` → vazio (salvo
exclusões deferidas documentadas), cores cruas só nas taxonomias documentadas, suíte verde, build+lint OK.

## Não-objetivos

Redesign de layout/novas features; tocar lógica de negócio/segurança/LLM; converter editores Monaco/charts;
módulos `shared`/`auth` (infra, não telas de jornada).
