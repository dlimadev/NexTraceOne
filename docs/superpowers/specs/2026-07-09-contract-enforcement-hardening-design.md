# Contract Enforcement & Publication Hardening — Design (P2, fatia 4)

**Data:** 2026-07-09
**Módulo:** catalog / contracts (frontend)
**Persona:** produtor / governança de contratos
**Ciclo:** P2 fatia 4 — endurecimento do loop de autoria/enforcement

---

## 1. Problema

As três telas do loop do produtor — `SpectralRulesetManagerPage` (definir regras),
`CanonicalEntityCatalogPage`/`CanonicalEntityImpactCascadePage` (modelar entidades) e
`PublicationCenterPage` (publicar/retirar) — já estão **ligadas como jornada pelo hub
de governança** (fatia 1: grupos Enforce/Model/Publish). O que resta são becos e
falhas concretas, não falta de ligação:

- **Entradas da Publication são becos:** na tabela, `contractTitle` é renderizado com
  cor de accent (parece um link) mas **não navega** — não há forma de ir da entrada de
  publicação para o contrato correspondente. A entrada já carrega `contractVersionId`
  e `apiAssetId` (`ContractPublicationEntry`).
- **Modal de "withdraw" é cru:** usa `<div className="fixed inset-0 bg-black/50">` com
  markup manual em vez do DS `Modal` — sem focus-trap, sem escape, inconsistente
  (é o último modal cru do módulo de contratos).
- **Spectral elimina sem confirmação:** o `IconButton` de delete chama
  `deleteMutation.mutate(ruleset.id)` diretamente no clique — ação destrutiva sem
  guarda.

## 2. Objetivo (fatia 4)

Endurecer o loop de enforcement/publicação: matar o beco da Publication e alinhar as
duas ações críticas (withdraw, delete) ao DS `Modal` com confirmação e a11y.

Critério de sucesso: (a) da Publication chega-se ao contrato num clique; (b) o withdraw
usa o DS `Modal`; (c) eliminar um ruleset Spectral exige confirmação explícita.

## 3. Não-objetivos (deferidos)

- Novos cross-links entre as 3 ferramentas (o hub da fatia 1 já as liga).
- Redesenho interno mais profundo (ex. `methodColors` com tailwind cru no playground).
- Novos endpoints/backend. Sem fabricação.

## 4. Desenho

### 4.1 Publication — entradas ligam ao contrato

Na `PublicationCenterPage`, o `contractTitle` (célula da tabela) passa a ser um `Link`
para o workspace do contrato:

```tsx
<td className="px-4 py-3 font-medium">
  {entry.contractVersionId ? (
    <Link to={`/contracts/${entry.contractVersionId}`} className="text-accent hover:underline">
      {entry.contractTitle}
    </Link>
  ) : (
    <span className="text-accent">{entry.contractTitle}</span>
  )}
</td>
```

Honest-null: sem `contractVersionId`, mantém o texto sem link. Rota
`/contracts/:contractVersionId` (workspace) já existe; a partir dela a fatia 3 dá um
clique para o portal do consumidor.

### 4.2 Publication — withdraw via DS `Modal`

Substituir o modal cru (`<div className="fixed inset-0 bg-black/50">…</div>`) pelo DS
`Modal` (`open`/`onClose`/`title`/`footer`), preservando a mesma lógica
(`withdrawTarget`, `withdrawReason`, `handleWithdraw`):

```tsx
<Modal
  open={!!withdrawTarget}
  onClose={() => { setWithdrawTarget(null); setWithdrawReason(''); }}
  title={t('contracts.publication.withdrawModal.title', 'Withdraw Publication')}
  size="sm"
  footer={
    <>
      <Button variant="outline" size="sm" onClick={() => { setWithdrawTarget(null); setWithdrawReason(''); }}>
        {t('common.cancel', 'Cancel')}
      </Button>
      <Button variant="danger" size="sm" loading={withdrawMutation.isPending} disabled={withdrawMutation.isPending}
        onClick={() => withdrawTarget && handleWithdraw(withdrawTarget)}>
        {t('contracts.publication.withdrawModal.confirm', 'Withdraw')}
      </Button>
    </>
  }
>
  <p className="text-xs text-muted mb-4">{/* descrição existente com title/version */}</p>
  <TextField label={...} value={withdrawReason} onChange={...} placeholder={...} size="sm" />
</Modal>
```

### 4.3 Spectral — confirmação de eliminação via DS `Modal`

Adicionar estado `deleteTarget: SpectralRuleset | null`. O `IconButton` de delete passa
a `setDeleteTarget(ruleset)` (não elimina). Um DS `Modal` de confirmação executa a
eliminação:

```tsx
const [deleteTarget, setDeleteTarget] = useState<SpectralRuleset | null>(null);
// ...
<IconButton ... onClick={() => setDeleteTarget(ruleset)} />
// ...
<Modal
  open={!!deleteTarget}
  onClose={() => setDeleteTarget(null)}
  title={t('contracts.spectral.manager.deleteTitle', 'Delete ruleset')}
  size="sm"
  footer={
    <>
      <Button variant="outline" size="sm" onClick={() => setDeleteTarget(null)}>{t('common.cancel', 'Cancel')}</Button>
      <Button variant="danger" size="sm" loading={deleteMutation.isPending} disabled={deleteMutation.isPending}
        onClick={() => { if (deleteTarget) { deleteMutation.mutate(deleteTarget.id, { onSuccess: () => setDeleteTarget(null) }); } }}>
        {t('common.delete', 'Delete')}
      </Button>
    </>
  }
>
  <p className="text-xs text-muted">
    {t('contracts.spectral.manager.deleteConfirm', 'Delete ruleset "{{name}}"? This cannot be undone.', { name: deleteTarget?.name ?? '' })}
  </p>
</Modal>
```

## 5. Componentes / ficheiros

- **Modificar:** `features/contracts/publication/PublicationCenterPage.tsx` — link no
  título + withdraw via DS `Modal`.
- **Modificar:** `features/contracts/spectral/SpectralRulesetManagerPage.tsx` —
  confirmação de eliminação via DS `Modal`.
- **Modificar:** `locales/{en,es,pt-BR,pt-PT}.json` — chaves novas (§7).

## 6. Fluxo de dados

- Sem query nova. Publication usa `entry.contractVersionId` já carregado; withdraw e
  delete usam as mutations existentes. Zero fetch novo, zero fabricação.

## 7. i18n (4 locales: en, es, pt-BR, pt-PT)

Chaves novas (com fallback inglês via `t('key','fallback')`):

- `contracts.spectral.manager.deleteTitle` — en `Delete ruleset`
- `contracts.spectral.manager.deleteConfirm` — en `Delete ruleset "{{name}}"? This cannot be undone.`

(As chaves do withdraw modal já existem: `contracts.publication.withdrawModal.*`.)

`validate:i18n` tem de passar.

## 8. Testes

- **`PublicationCenterPage`**: uma entrada publicada renderiza o `contractTitle` como
  `link` com `href` `/contracts/<contractVersionId>`; abrir o withdraw mostra o DS
  `Modal` (role `dialog`) com o título.
- **`SpectralRulesetManagerPage`**: clicar em delete **não** chama a mutation
  imediatamente; abre um `dialog` de confirmação; confirmar chama
  `deleteMutation.mutate` com o id.
- **e2e (`contract-enforcement.spec.ts`):** ir à Publication com uma entrada mockada →
  o título é um link para `/contracts/<id>`.

Gates: `npm run test` (suite completa verde), `validate:i18n` PASS, `npm run build`
exit 0, `eslint` 0 erros nos ficheiros alterados, e2e verde.

## 9. Constraints globais

- DS de `../../../shared/ui` (`Modal`, `Button`, `TextField`, `IconButton`);
  componentes de `components/*`; ícones `lucide-react`; `Link` de `react-router-dom`.
- Honest-null: link do título só com `contractVersionId`; nunca fabricar.
- i18n: nenhuma string de UI hardcoded; chaves nos 4 locales (NÃO há `fr`); ficheiros
  FLAT `src/frontend/src/locales/<l>.json`.
- Mudanças cirúrgicas: não alterar a lógica das mutations; preservar
  `withdrawTarget`/`withdrawReason`/`handleWithdraw` e o comportamento de toggle.
- Testes centralizados em `src/frontend/src/__tests__/**`; e2e em
  `src/frontend/e2e/**` (globs de URL Playwright usam `**`).
- Tooling: `npm run test` (não `npx vitest`); gate final `npm run build` (`tsc -b`).
- Rota verbatim: workspace `/contracts/:contractVersionId`.
