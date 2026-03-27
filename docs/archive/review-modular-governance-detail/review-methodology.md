# Metodologia de Revisão Modular — NexTraceOne

## Objetivo

Este documento define a **metodologia oficial** para a revisão modular do NexTraceOne. O objetivo é garantir que cada módulo do produto é analisado de forma consistente, reprodutível e orientada pela visão oficial do produto.

A revisão modular não é um exercício teórico — é uma ferramenta prática para **alinhar o código real com a visão do produto**.

---

## Princípios Fundamentais

### 1. Código como fonte principal de verdade

O código-fonte é a referência primária para toda a análise. Não se assume que uma funcionalidade existe porque está documentada — ela só é considerada funcional se:

- O código implementa a funcionalidade
- A funcionalidade é acessível pelo utilizador (rota, menu, ação)
- O comportamento corresponde ao esperado
- Existe integração entre camadas (frontend ↔ backend ↔ banco)

### 2. Documentação como referência secundária

A documentação existente (markdown, comments, swagger, etc.) serve como **referência de intenção**, não como prova de implementação. A revisão cruza documentação com código para identificar:

- Funcionalidades documentadas mas não implementadas
- Funcionalidades implementadas mas não documentadas
- Inconsistências entre documentação e comportamento real

### 3. Análise por módulo, página e ação

Cada módulo é analisado em três níveis:

| Nível       | O que é analisado                                                          |
|-------------|----------------------------------------------------------------------------|
| **Módulo**  | Visão geral, papel no produto, pilares associados, dependências            |
| **Página**  | Cada página/rota do frontend, com mapeamento para backend e banco          |
| **Ação**    | Cada ação do utilizador (criar, editar, eliminar, pesquisar, etc.)         |

---

## Cruzamento Entre Camadas

Cada funcionalidade é analisada **transversalmente** entre todas as camadas do produto:

### Matriz de cruzamento

```
Frontend  ←→  Backend  ←→  Banco de Dados
    ↕            ↕              ↕
   IA    ←→  Agents   ←→  Documentação
```

### Perguntas de cruzamento para cada funcionalidade

| Pergunta                                                                     | Camadas envolvidas        |
|------------------------------------------------------------------------------|---------------------------|
| A página/rota existe e é acessível?                                          | Frontend                  |
| Existe endpoint no backend que serve esta funcionalidade?                    | Frontend ↔ Backend        |
| O endpoint retorna dados reais ou mock/stub?                                 | Backend ↔ Banco           |
| As entidades de banco correspondem ao contrato do endpoint?                  | Backend ↔ Banco           |
| A IA tem acesso ao contexto necessário para esta funcionalidade?             | IA ↔ Backend              |
| Existem agents/workers que processam dados deste módulo?                     | Agents ↔ Backend ↔ Banco  |
| A documentação descreve corretamente o comportamento real?                   | Docs ↔ Código             |
| O i18n cobre todos os textos visíveis nesta funcionalidade?                  | Frontend                  |
| A funcionalidade respeita segmentação por persona?                           | Frontend ↔ Backend        |
| Existem testes que cobrem esta funcionalidade?                               | Todas                     |

---

## Classificação de Funcionalidades

Cada funcionalidade identificada recebe uma das seguintes classificações:

### Funcionalidade Funcional ✅

- Implementada em todas as camadas necessárias
- Acessível pelo utilizador
- Comportamento corresponde ao esperado
- Dados reais (não mock/stub)
- i18n aplicado
- Testes existentes

### Funcionalidade Incompleta ⚠️

- Implementação parcial em uma ou mais camadas
- Frontend existe mas backend retorna dados incompletos ou stub
- Endpoint existe mas não tem persistência real
- Falta i18n, testes ou documentação

### Funcionalidade Parcial 🔶

- Funciona para alguns cenários mas não para todos
- Exemplo: listagem funciona, mas criação não
- Exemplo: funciona para um tipo de contrato mas não para outros

### Funcionalidade Órfã 👻

- Código existe mas não é acessível pelo utilizador
- Endpoint existe mas nenhuma página o consome
- Página existe mas não está no menu/navegação
- Componente existe mas não é usado em nenhuma página

### Funcionalidade Escondida 🔍

- Funcionalidade existe e funciona, mas não é descobrível
- Não aparece no menu
- Requer URL direta
- Não está documentada

### Funcionalidade Preview 🧪

- Implementação inicial/experimental
- Pode estar atrás de feature flag
- Não está pronta para produção
- Pode ter dados mock/placeholder

### Funcionalidade Quebrada ❌

- Código existe mas não funciona
- Erro em runtime
- Integração entre camadas quebrada
- Dados corrompidos ou inconsistentes

### Funcionalidade Descontinuada Aparente 🗑️

- Código existe mas parece ter sido abandonado
- Sem commits recentes
- Sem referências ativas
- Pode ser candidata a remoção

### Funcionalidade Não Integrada 🔗

- Funcionalidade implementada isoladamente
- Não conectada ao fluxo principal do módulo
- Não contribui para a experiência integrada do produto

---

## Classificação de Gaps

Os gaps identificados são classificados por tipo e severidade:

### Tipos de gap

| Tipo                     | Descrição                                                                |
|--------------------------|--------------------------------------------------------------------------|
| `GAP_MISSING`            | Funcionalidade esperada que não existe em nenhuma camada                 |
| `GAP_FRONTEND_ONLY`      | Frontend existe mas sem backend correspondente                          |
| `GAP_BACKEND_ONLY`       | Backend existe mas sem frontend correspondente                          |
| `GAP_NO_PERSISTENCE`     | Endpoint existe mas não persiste dados                                  |
| `GAP_NO_I18N`            | Textos hardcoded sem internacionalização                                |
| `GAP_NO_PERSONA`         | Funcionalidade não segmentada por persona                               |
| `GAP_NO_SECURITY`        | Funcionalidade sem controle de acesso adequado                          |
| `GAP_NO_AUDIT`           | Ação relevante sem registo de auditoria                                 |
| `GAP_NO_TESTS`           | Funcionalidade sem cobertura de testes                                  |
| `GAP_NO_DOCS`            | Funcionalidade sem documentação                                         |
| `GAP_INCONSISTENCY`      | Comportamento inconsistente entre camadas                               |
| `GAP_INTEGRATION`        | Funcionalidade não integrada com o fluxo principal                      |

### Severidade do gap

| Severidade   | Descrição                                                                       |
|--------------|---------------------------------------------------------------------------------|
| `CRITICAL`   | Bloqueia uso do produto, compromete segurança ou integridade de dados           |
| `HIGH`       | Funcionalidade core incompleta, impacto direto na experiência do utilizador     |
| `MEDIUM`     | Funcionalidade secundária com gaps, impacto parcial                             |
| `LOW`        | Melhoria cosmética, otimização, documentação complementar                       |

---

## Definição de "Pronto" para Cada Módulo

Um módulo é considerado **DONE** (pronto) quando cumpre todos os critérios abaixo:

### Critérios obrigatórios

- [ ] Todas as funcionalidades esperadas estão implementadas e funcionais
- [ ] Frontend ↔ Backend ↔ Banco de Dados estão integrados e consistentes
- [ ] Todos os textos visíveis ao utilizador usam i18n
- [ ] Segmentação por persona está aplicada onde relevante
- [ ] Controle de acesso e autorização estão implementados
- [ ] Ações relevantes geram registo de auditoria
- [ ] Testes cobrem os cenários principais
- [ ] Documentação técnica está atualizada
- [ ] Logs e exceptions seguem o padrão do produto
- [ ] Nenhum gap classificado como CRITICAL ou HIGH permanece aberto

### Critérios desejáveis

- [ ] Testes cobrem cenários edge e de erro
- [ ] Documentação de utilizador existe
- [ ] Performance validada para volumes esperados
- [ ] IA tem acesso ao contexto deste módulo
- [ ] Observabilidade configurada (métricas, traces, alertas)

### Fluxo de aprovação

```
Revisão completa → Gaps documentados → Correções aplicadas → Re-teste →
→ Checklist satisfeito → Aprovação por Tech Lead/Architect → DONE
```

---

## Ferramentas de Suporte à Revisão

| Ferramenta                                                          | Utilização                                      |
|---------------------------------------------------------------------|-------------------------------------------------|
| [review-status-overview.md](./review-status-overview.md)            | Tracking de status por módulo                   |
| [review-checklist-global.md](./review-checklist-global.md)          | Checklist detalhado por área                     |
| [module-priority-matrix.md](./module-priority-matrix.md)            | Matriz de priorização de módulos                |
| [modular-review-summary.md](./modular-review-summary.md)           | Resumo executivo                                |
| [documentation-vs-code-gap-report.md](./documentation-vs-code-gap-report.md) | Gaps entre docs e código             |

---

## Notas Importantes

1. **Não reescrever módulos inteiros** — a estratégia é refatoração incremental orientada por produto
2. **Preservar contratos públicos** — breaking changes devem ser documentados e justificados
3. **Código em inglês, docs em português** — seguir a convenção do repositório
4. **i18n é obrigatório** — nenhum texto visível ao utilizador deve estar hardcoded
5. **Persona-awareness é obrigatório** — a experiência deve variar por papel do utilizador
