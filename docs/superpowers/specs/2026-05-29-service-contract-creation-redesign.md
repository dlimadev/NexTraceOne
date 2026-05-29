# Service & Contract Creation Redesign — Design Spec

**Data:** 2026-05-29  
**Autor:** Diogo Lima  
**Escopo:** Feature — Catalog (service registration, interface creation, contract import)  
**Status:** Aprovado para implementação

---

## 1. Objetivo

Reformular as telas de criação de serviços, interfaces de serviço e contratos do módulo Catalog, substituindo formulários flat e o wizard embutido em Card por **overlays full-screen estilo Grafana** com UX orientada a fluxo, ícones SVG profissionais por categoria e campos adaptativos por tipo.

---

## 2. Contexto — Estado Atual

| Tela | Implementação atual | Problemas |
|---|---|---|
| Registrar Serviço | `ServiceRegistrationWizard` embutido em `<Card>` | Stepper horizontal pequeno, sem ícones por tipo, esmaga o conteúdo |
| Criar Interface | `CreateServiceInterfacePage` — página dedicada flat | Formulário único sem progressão, ícone `Layers` genérico, campos todos visíveis de uma vez |
| Importar Contrato | Form inline em `ContractsPage` com textarea raw | Sem drag-drop, sem auto-detecção de protocolo, sem feedback visual |

---

## 3. Decisões de Design

| Questão | Decisão |
|---|---|
| Padrão de UX | Overlay full-screen (fixed inset-0 z-50), igual ao `PanelEditorOverlay` já existente |
| Stepper | Vertical, na lateral esquerda, com estados done/active/pending e ícones Lucide por passo |
| Ícones de tipo | Cards SVG selecionáveis com cor semântica por categoria (REST=índigo, GraphQL=violeta, Kafka=âmbar, gRPC=ciano, Legacy=slate, Worker=esmeralda, Gateway=vermelho) |
| Arquitetura | `WizardOverlay` base compartilhado + 3 overlays especializados |
| Interface creation | Também vira overlay (mesma família de UX) |
| APIs backend | Sem alteração — reutiliza endpoints existentes |
| Estado de formulário | Local (useState) em cada overlay |
| i18n | 100% via `t()` — sem strings hardcoded |

---

## 4. Arquitetura de Componentes

### 4.1 Novos arquivos

```
src/frontend/src/features/catalog/
├── components/
│   ├── WizardOverlay.tsx              ← base compartilhado
│   ├── ServiceTypeIconPicker.tsx      ← grid de cards SVG por tipo de serviço
│   ├── ServiceRegistrationOverlay.tsx ← substitui ServiceRegistrationWizard
│   ├── ServiceInterfaceOverlay.tsx    ← substitui CreateServiceInterfacePage (como overlay)
│   └── ContractImportOverlay.tsx      ← novo overlay de importação de contrato
```

### 4.2 Arquivos modificados

```
src/frontend/src/features/catalog/
├── pages/
│   ├── ServiceCatalogListPage.tsx     ← controla showRegistrationOverlay (estado booleano)
│   ├── ServiceDetailPage.tsx          ← controla showInterfaceOverlay + showContractOverlay
│   └── CreateServiceInterfacePage.tsx ← mantida como fallback de rota (redireciona para overlay)
├── components/
│   └── ServiceRegistrationWizard.tsx  ← deprecated; não removido mas não referenciado
```

### 4.3 Interface do `WizardOverlay`

```tsx
export interface WizardStep {
  id: string;
  labelKey: string;         // chave i18n
  icon: LucideIcon;         // ícone Lucide para o stepper
}

export interface WizardOverlayProps {
  title: string;            // título no header (já traduzido)
  headerIcon: React.ReactNode;  // ícone do header (JSX)
  steps: WizardStep[];
  currentStep: number;      // 1-based
  onClose: () => void;
  onBack: () => void;
  onNext: () => void;
  onSubmit: () => void;
  isSubmitting?: boolean;
  isNextDisabled?: boolean;
  isLastStep: boolean;
  children: React.ReactNode; // conteúdo do passo atual
}
```

**Comportamento:**
- `fixed inset-0 z-50` com backdrop `bg-black/60`
- Header fixo: ícone + título + subtítulo "Passo N de M — {label}" + botão fechar (X)
- Lateral esquerda (220px): stepper com estado visual done (check verde) / active (azul sólido) / pending (border only)
- Área central: scrollável, padding `px-8 py-6`
- Footer fixo: botão Back (secondary) à esquerda, botão Next/Submit (primary) à direita
- Animação de entrada: `animate-fade-in` (já definida no projeto)
- Fecha ao pressionar Escape (useEffect com keydown listener)

---

## 5. ServiceRegistrationOverlay

**Invocado por:** `ServiceCatalogListPage` (botão "Registrar Serviço")  
**Substitui:** `ServiceRegistrationWizard`  
**Passos:** 5

### Passos e ícones do stepper

| # | Label (i18n key) | Ícone Lucide | Campos |
|---|---|---|---|
| 1 | `catalog.registration.step.identity` | `Fingerprint` | name*, domain*, subDomain, capability |
| 2 | `catalog.registration.step.classification` | `LayoutGrid` | serviceType (via `ServiceTypeIconPicker`), criticality, exposureType, dataClassification, regulatoryScope, infrastructureProvider, runtimeLanguage |
| 3 | `catalog.registration.step.ownership` | `Users` | team*, technicalOwner, businessOwner, productOwner, contactChannel |
| 4 | `catalog.registration.step.references` | `Link` | description, documentationUrl, repositoryUrl |
| 5 | `catalog.registration.step.review` | `ClipboardCheck` | Sumário read-only de todos os campos preenchidos |

**Validação por passo:**
- Passo 1: `name` e `domain` obrigatórios
- Passo 3: `team` obrigatório
- Demais: sem bloqueio

**Props:**
```tsx
interface ServiceRegistrationOverlayProps {
  onClose: () => void;
  onSuccess: (serviceId: string) => void;
}
```

**API call:** `serviceCatalogApi.registerService(form)` → sucesso invoca `onSuccess(serviceId)` → `ServiceCatalogListPage` invalida query `['catalog-services']`

---

## 6. ServiceTypeIconPicker

**Usado em:** `ServiceRegistrationOverlay` (passo 2) e `ServiceInterfaceOverlay` (passo 1)

Grid de cards selecionáveis, 4 colunas em desktop, 2 em mobile. Cada card tem:
- SVG inline (22×22) com cor semântica
- Nome do tipo (i18n)
- Borda colorida quando selecionado

### Mapeamento de cores por categoria

| Categoria | Cor | Tipos |
|---|---|---|
| API HTTP | Índigo `#6366f1` | RestApi, GraphqlApi, SoapService, ZosConnectApi |
| Streaming / Eventos | Âmbar `#f59e0b` | KafkaProducer, KafkaConsumer, WebhookProducer, WebhookConsumer, MqQueue |
| RPC | Ciano `#06b6d4` | GrpcService |
| Background | Esmeralda `#10b981` | BackgroundService, ScheduledProcess, ScheduledJob, BackgroundWorker |
| Gateway | Vermelho `#ef4444` | Gateway |
| Platform | Azul slate `#64748b` | IntegrationComponent, SharedPlatformService, Framework, ThirdParty, IntegrationBridge |
| Legacy / Mainframe | Cinza slate `#94a3b8` | LegacySystem, CobolProgram, CicsTransaction, ImsTransaction, BatchJob, MainframeSystem, MqQueueManager |

**Props:**
```tsx
interface ServiceTypeIconPickerProps {
  value: string;
  onChange: (type: string) => void;
  /** 'service' mostra todos os tipos; 'interface' mostra apenas tipos de interface */
  mode: 'service' | 'interface';
}
```

---

## 7. ServiceInterfaceOverlay

**Invocado por:** `ServiceDetailPage` (botão "Nova Interface")  
**Substitui:** navegação para `CreateServiceInterfacePage`  
**Passos:** 3

| # | Label (i18n key) | Ícone Lucide | Conteúdo |
|---|---|---|---|
| 1 | `catalog.interface.step.type` | `Plug` | `ServiceTypeIconPicker` (mode="interface") + campo `name` + `exposureScope` |
| 2 | `catalog.interface.step.details` | `Settings2` | Campos adaptativos por `interfaceType`: basePath, topicName, wsdlNamespace, grpcServiceName, scheduleCron, documentationUrl, requiresContract |
| 3 | `catalog.interface.step.review` | `ClipboardCheck` | Sumário read-only + badge do tipo selecionado com cor semântica |

**Validação por passo:**
- Passo 1: `name` obrigatório

**Props:**
```tsx
interface ServiceInterfaceOverlayProps {
  serviceId: string;
  serviceName: string;
  onClose: () => void;
  onSuccess: () => void;
}
```

**API call:** `serviceCatalogApi.createServiceInterface({serviceAssetId: serviceId, ...form})` → `onSuccess()` → `ServiceDetailPage` invalida query `['catalog-service-detail', serviceId]`

---

## 8. ContractImportOverlay

**Invocado por:** `ServiceDetailPage` (botão "Importar Contrato") e `ContractsPage` (botão "Importar")  
**Passos:** 3

| # | Label (i18n key) | Ícone Lucide | Conteúdo |
|---|---|---|---|
| 1 | `catalog.contract.step.interface` | `Layers` | Select de interface (lista de interfaces do serviço, ou input manual de `apiAssetId` quando invocado da `ContractsPage`) |
| 2 | `catalog.contract.step.spec` | `FileCode` | 3 abas: Upload (drag-drop zone), URL (input), Editor (Monaco). Badge de protocolo detectado automaticamente. |
| 3 | `catalog.contract.step.version` | `Tag` | Campos: `version` (semver), `protocol` (pre-preenchido da detecção, editável), notas de release (opcional) |

**Detecção automática de protocolo** (no passo 2, cliente-side):
- Lê primeiros 2KB do conteúdo
- Regex `openapi:` ou `"openapi"` → `OpenApi`
- Regex `asyncapi:` → `AsyncApi`
- Regex `syntax = "proto` → `Protobuf`
- Regex `<definitions` ou `<wsdl:` → `Wsdl`
- Regex `"swagger"` → `Swagger`
- Fallback: `GraphQl` se regex `type Query` ou `schema {`
- Não reconhecido: badge laranja "Selecione manualmente"

**Validação por passo:**
- Passo 1: `apiAssetId` obrigatório
- Passo 2: conteúdo da spec não-vazio
- Passo 3: `version` obrigatório (formato semver recomendado mas não bloqueante)

**Props:**
```tsx
interface ContractImportOverlayProps {
  /** Se fornecido, passo 1 é pré-preenchido e pulável */
  preselectedApiAssetId?: string;
  preselectedApiAssetName?: string;
  onClose: () => void;
  onSuccess: () => void;
}
```

**API call:** `contractsApi.importContractVersion({apiAssetId, content, version, protocol})` → `onSuccess()` → página pai invalida query relevante

---

## 9. Integração nas Páginas Pai

### ServiceCatalogListPage

```tsx
const [showRegistration, setShowRegistration] = useState(false);

// No JSX:
<Button onClick={() => setShowRegistration(true)}>
  <Plus size={14} /> {t('serviceCatalog.registerService')}
</Button>

{showRegistration && (
  <ServiceRegistrationOverlay
    onClose={() => setShowRegistration(false)}
    onSuccess={(id) => {
      setShowRegistration(false);
      queryClient.invalidateQueries({ queryKey: ['catalog-services'] });
      navigate(`/services/${id}`);
    }}
  />
)}
```

### ServiceDetailPage

```tsx
const [showInterface, setShowInterface] = useState(false);
const [showContract, setShowContract] = useState(false);

{showInterface && (
  <ServiceInterfaceOverlay
    serviceId={serviceId}
    serviceName={service.displayName}
    onClose={() => setShowInterface(false)}
    onSuccess={() => {
      setShowInterface(false);
      queryClient.invalidateQueries({ queryKey: ['catalog-service-detail', serviceId] });
    }}
  />
)}

{showContract && (
  <ContractImportOverlay
    preselectedApiAssetId={/* apiAssetId da interface selecionada no ServiceDetailPage */}
    preselectedApiAssetName={/* nome da interface */}
    onClose={() => setShowContract(false)}
    onSuccess={() => {
      setShowContract(false);
      queryClient.invalidateQueries({ queryKey: ['contract-governance-list'] });
    }}
  />
)}
```

### ContractsPage

Quando invocado da `ContractsPage` (sem interface pré-selecionada), o passo 1 exibe um `<select>` ou `<input>` para `apiAssetId` manual — comportamento já coberto pelos props opcionais.

```tsx
const [showContract, setShowContract] = useState(false);

// No header da ContractsPage, substituir o form inline atual:
<Button onClick={() => setShowContract(true)}>
  <FilePlus size={14} /> {t('contractGov.actions.import')}
</Button>

{showContract && (
  <ContractImportOverlay
    // sem preselectedApiAssetId → passo 1 fica editável
    onClose={() => setShowContract(false)}
    onSuccess={() => {
      setShowContract(false);
      queryClient.invalidateQueries({ queryKey: ['contract-governance-list'] });
      queryClient.invalidateQueries({ queryKey: ['contract-governance-summary'] });
    }}
  />
)}
```

---

## 10. Tratamento de Erros

- **Validação de campo:** mensagem inline abaixo do input, botão Next desabilitado enquanto inválido
- **Erro de API:** `toast.error(t('common.errorSaving'))` via Sonner + mensagem no footer do overlay acima dos botões
- **Arquivo inválido (upload):** badge vermelho "Formato não suportado" na drop zone
- **Spec malformada (detecção):** badge laranja com prompt para seleção manual de protocolo

---

## 11. i18n — Novas Chaves

Prefixos para adicionar em `en.json`, `pt-BR.json`, `es.json`, `pt-PT.json` (e inferir `fr.json`):

```
catalog.registration.step.identity
catalog.registration.step.classification
catalog.registration.step.ownership
catalog.registration.step.references
catalog.registration.step.review
catalog.interface.step.type
catalog.interface.step.details
catalog.interface.step.review
catalog.contract.step.interface
catalog.contract.step.spec
catalog.contract.step.version
catalog.wizard.close
catalog.wizard.back
catalog.wizard.next
catalog.wizard.submit
catalog.wizard.submitting
catalog.contract.protocolDetected
catalog.contract.protocolUnknown
catalog.contract.dropZone.hint
catalog.contract.dropZone.formats
catalog.contract.tab.upload
catalog.contract.tab.url
catalog.contract.tab.editor
```

---

## 12. Testes

| Arquivo | Casos principais |
|---|---|
| `WizardOverlay.test.tsx` | Renderiza passo 1, avança para passo 2, Back desabilitado no passo 1, Escape fecha, Next desabilitado quando `isNextDisabled` |
| `ServiceTypeIconPicker.test.tsx` | Renderiza todos os tipos, clique seleciona tipo, tipo selecionado tem borda destacada |
| `ServiceRegistrationOverlay.test.tsx` | Passo 1 valida `name`/`domain`, avança 5 passos, submit chama API, erro de API mostra toast |
| `ServiceInterfaceOverlay.test.tsx` | Seleção de tipo muda campos visíveis no passo 2, submit chama API |
| `ContractImportOverlay.test.tsx` | Drop de arquivo dispara detecção de protocolo, badge "OpenAPI detectado", submit chama API |

Framework: Vitest + Testing Library. APIs mockadas com `vi.mock('../api')`.

---

## 13. Fora de Escopo

- Nenhuma alteração no backend (endpoints, DTOs, schemas)
- Sem mudança nas regras de validação existentes (apenas UX da apresentação)
- `ContractDetailPage` e `ContractListPage` não são alteradas (apenas ponto de entrada do overlay)
- Sem testes E2E novos neste redesign (cobertos pelos testes unitários + mocks)
- `ServiceRegistrationWizard.tsx` não é deletado — apenas deixa de ser referenciado (remoção em PR separado após estabilização)
