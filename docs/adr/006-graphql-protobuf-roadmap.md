# ADR-006: GraphQL e Protobuf/gRPC — Implementado como Extensão Modular

## Status

Accepted → Implemented

## Data

2026-04-06

## Contexto

Durante o design do módulo de Contract Governance do NexTraceOne, foi avaliada a inclusão de suporte a dois protocolos adicionais de alto valor em ambientes enterprise modernos:

1. **GraphQL** — utilizado amplamente para APIs de consulta flexível, especialmente em frontends complexos e plataformas com múltiplos consumers com requisitos de dados diferentes.
2. **Protobuf / gRPC** — protocolo binário de alta performance utilizado em comunicação inter-serviços, muito presente em arquiteturas de microserviços com requisitos de latência baixa.

Ambos os protocolos têm características distintas dos contratos atualmente suportados (OpenAPI, AsyncAPI, WSDL):

- **GraphQL** utiliza schemas SDL (Schema Definition Language) e introspection queries em vez de especificações estáticas como OpenAPI. O diff semântico requer análise de types, queries, mutations e subscriptions — lógica consideravelmente diferente do diff de OpenAPI/AsyncAPI.
- **Protobuf** utiliza arquivos `.proto` com lógica de versionamento e breaking change fundamentalmente diferente (field numbers, backward/forward compatibility). A análise de compatibilidade exige um parser `.proto` dedicado.

Os fatores considerados foram:

- **Esforço de implementação**: suporte a cada protocolo adicional requer: parser dedicado, lógica de diff semântico específica, validação de compatibilidade, geração de stubs e mocks, e UI no Contract Studio. Estimativa: 3–5 sprints por protocolo.
- **Prioridade de negócio**: os protocolos OpenAPI, AsyncAPI e WSDL cobrem mais de 90% dos casos de uso identificados nos primeiros clientes target.
- **Risco de qualidade**: incluir suporte parcial ou incompleto a GraphQL/gRPC no MVP1 prejudicaria a experiência de product e criaria dívida técnica significativa.
- **Foco estratégico**: o MVP1 deve estabelecer o NexTraceOne como Source of Truth confiável para os protocolos mais prevalentes antes de expandir o escopo.
- **Governança de IA**: os agentes de geração de contratos precisam de templates e validadores específicos por protocolo — adicionar GraphQL/gRPC multiplica a complexidade dos agentes.

## Decisão

**GraphQL e Protobuf/gRPC foram implementados como extensões modulares do Contract Governance em Abril 2026 (Waves G.3 e H.1).**

A implementação seguiu as condições planejadas:

1. ✅ O módulo de Contract Governance está estável para OpenAPI, AsyncAPI e WSDL.
2. ✅ Cada protocolo foi implementado como extensão modular do `ContractProtocol` enum e do pipeline de parsing/diff, sem alterar o núcleo do domínio.
3. ✅ GraphQL foi priorizado e implementado antes de Protobuf/gRPC.
4. ✅ Este ADR foi atualizado para refletir a implementação concluída.

As seguintes entidades e serviços foram adicionados:
- `GraphQlSchemaSnapshot` — snapshot de schema GraphQL com SDL parsing
- `ProtobufSchemaSnapshot` — snapshot de schema Protobuf com `.proto` parsing
- `GraphQlDiffEngine` — diff semântico de types, queries, mutations e subscriptions
- `ProtobufDiffEngine` — diff por field number e backward/forward compatibility
- `VisualDataContractBuilder.tsx` — UI no Contract Studio para edição visual de contratos
- Migrations e testes de integração para ambos os protocolos

## Consequências

### Positivas

- **Foco e qualidade no MVP1**: equipa concentra esforço nos protocolos com maior impacto imediato.
- **Time-to-market reduzido**: entrega mais rápida de valor real para os primeiros clientes.
- **Menor dívida técnica**: evita implementações parciais ou incompletas que gerariam confusão nos utilizadores.
- **Extensibilidade preservada**: a arquitetura modular do Contract Governance permite adicionar protocolos sem alterar o núcleo.
- **Governança clara**: os protocolos suportados são documentados explicitamente, evitando expectativas erradas.

### Negativas

- **Limitação para adoção**: clientes com uso intensivo de GraphQL ou gRPC terão cobertura parcial no MVP1.
- **Roadmap dependency**: features de Change Intelligence e blast radius para gRPC só estarão disponíveis em fase posterior.
- **Agentes de IA limitados**: os agentes de geração de contratos não cobrirão estes protocolos no MVP1.

### Neutras

- Os campos `GraphQl` e `Protobuf` no enum `ContractProtocol` permanecem reservados mas sem suporte de parsing ou diff.
- A UI do Contract Studio exibirá apenas os protocolos suportados. A seleção de GraphQL/gRPC não será disponibilizada na fase MVP1.

## Roadmap previsto

| Fase | Protocolo | Funcionalidades |
|------|-----------|----------------|
| MVP1 (atual) | OpenAPI, AsyncAPI, WSDL | Completo |
| Pós-MVP1 (v1.x) | GraphQL | Parser SDL, diff, validação, Contract Studio |
| v2.x | Protobuf / gRPC | Parser `.proto`, diff por field number, geração de stubs |

## Referências

- [ADR-001: Modular Monolith](./001-modular-monolith.md)
- [Contract Studio Vision](../CONTRACT-STUDIO-VISION.md)
- [Service Contract Governance](../SERVICE-CONTRACT-GOVERNANCE.md)
- [Roadmap](../ROADMAP.md)
