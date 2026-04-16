# SERVICE-CONTRACT-GOVERNANCE.md

## Objetivo

Garantir que todos os serviços têm contratos bem definidos, interfaces de exposição governadas e um estado de saúde monitorizado continuamente.

---

## Módulo de Serviços (Service Catalog)

### Entidade: ServiceAsset

Campos de domínio implementados:

- `ServiceAssetId`, `Name`, `DisplayName`, `Description`
- `TeamName`, `TechnicalOwner`, `BusinessOwner`, `ProductOwner`
- `Domain`, `SubDomain`, `Capability`
- `SystemArea`, `ServiceType`, `Criticality`, `LifecycleStatus`
- `ExposureType`, `SloTarget`, `DataClassification`, `RegulatoryScope`
- `DocumentationUrl`, `RepositoryUrl`, `ContactChannel`

### Páginas Frontend

- **Service Catalog** (`/catalog/services`) — visão geral com tabs: Overview, Graph, Services, APIs, Impact, Temporal
- **Service Catalog List** (`/catalog/service-list`) — listagem tabelar com registo de novos serviços e APIs
- **Service Detail** (`/catalog/services/:id`) — detalhe com tabs: Overview, APIs, Contracts, **Interfaces**
- **Create Service Interface** (`/catalog/services/:id/interfaces/new`) — criação de nova interface de exposição
- **Ownership Transfer** — transferência de ownership entre equipas
- **Orphan Services** — serviços sem dono identificado
- **Service Changelog** — histórico de mudanças por serviço

---

## Módulo de Contratos (Contract Governance)

### Tipos de Contratos Suportados

- REST (OpenAPI/JSON Schema)
- SOAP (WSDL/XSD)
- Kafka / AsyncAPI (contratos de eventos)
- Background services e jobs agendados

### Entidade: ServiceInterface

Campos de domínio implementados:

- `InterfaceId`, `ServiceAssetId`, `Name`, `Description`
- `InterfaceType` — `RestApi`, `SoapService`, `KafkaProducer`, `KafkaConsumer`, `GraphQL`, `gRPC`, `WebSocket`, `ScheduledJob`, `Webhook`, `DatabaseView`
- `Status` — `Active`, `Deprecated`, `Sunset`, `Retired`
- `ExposureScope` — `Internal`, `External`, `Partner`
- `BasePath`, `TopicName`, `WsdlNamespace`, `GrpcServiceName`, `ScheduleCron`
- `SloTarget`, `RequiresContract`, `AuthScheme`, `RateLimitPolicy`
- `DocumentationUrl`, `IsDeprecated`

### Entidade: ContractBinding

Liga uma `ServiceInterface` a uma `ContractVersion` específica:

- `BindingId`, `InterfaceId`, `ContractVersionId`
- `Status` — `Active`, `Deprecated`, `Sunset`
- `BoundAt`

### Capacidades Implementadas

- Criação manual, importação e exportação de contratos
- Versionamento semântico (SemVer)
- Diff semântico entre versões
- Validação de compatibilidade
- Workflow de aprovação (Draft → Review → Approved → Locked)
- Exemplos e schemas canónicos
- Ownership por serviço
- Publication workflow
- Políticas e linting via Spectral
- Documentação viva
- Geração assistida por IA

---

## Contract Health Dashboard

Monitorização do estado de saúde dos contratos com:

- **Health Score** — pontuação global (0-100)
- Indicadores: `text-success` (≥ 80), `text-warning` (50–79), `text-critical` (< 50)
- Cobertura de exemplos e entidades canónicas
- Top violações por contrato (via Spectral)
- Versões deprecated e sua proporção

---

## Spectral Rulesets

Gestão de rulesets de linting para contratos REST:

- Listagem, criação, activação/desactivação e remoção de rulesets
- Integração com validação automática de contratos ao submeter versão

---

## Regras de Governança

Nenhum serviço pode estar em produção sem:

- **Owner** (equipa técnica e/ou negócio)
- **Contract** associado (via ServiceInterface + ContractBinding)
- **Version** semântica publicada
- **Documentation** URL válida
- **Lifecycle Status** definido

---

## Integração com Outros Módulos

| Módulo | Integração |
|---|---|
| **Change Intelligence** | Correlação mudança ↔ contrato ↔ serviço |
| **AI Knowledge** | Grounding com contexto de serviço e contrato para assistente IA |
| **Developer Portal** | Publicação de contratos para consumidores externos |
| **Observability** | SLO target por interface vinculado a alertas |
| **Governance** | Relatórios de conformidade por equipa/domínio |

---

## Estado de Implementação

- [x] Backend: `ServiceAsset` com 15+ campos de domínio
- [x] Backend: `ServiceInterface` com todos os tipos de exposição
- [x] Backend: `ContractBinding` para vincular interface a versão de contrato
- [x] Backend: Migrações EF Core correspondentes
- [x] Frontend: `ServiceInterfacesTab` no `ServiceDetailPage`
- [x] Frontend: `CreateServiceInterfacePage`
- [x] Frontend: API client com `listServiceInterfaces`, `createServiceInterface`, `bindContractToInterface`
- [x] Frontend: i18n completo (pt-BR, pt-PT, es)
- [x] Frontend: Testes unitários (202 ficheiros, 1318 testes, 0 falhas)
- [x] AI Grounding: `ServiceGroundingContext` atualizado com campos `SubDomain`, `Capability`, `DataClassification`, etc.
- [x] Configuration Seeder: chaves `catalog.service_interface.*` adicionadas
