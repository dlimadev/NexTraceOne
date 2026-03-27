# Estratégia de Geração de Prompts de Remediação

**Projeto:** NexTraceOne
**Data da Avaliação:** 2026-03-27
**Escopo:** Documentação da estratégia utilizada para gerar prompts de remediação eficazes e seguros

---

## 1. Introdução

Este documento explica a estratégia utilizada para transformar os problemas identificados na avaliação do NexTraceOne em prompts accionáveis para agentes de IA de desenvolvimento. O objectivo é garantir que cada prompt:

- Resolve um problema concreto e bem delimitado
- Toca apenas nos ficheiros necessários
- Não cria conflitos com outros prompts
- Pode ser executado por um agente sem conhecimento prévio do contexto
- Produz resultados verificáveis

A estratégia segue princípios de engenharia de software: separação de responsabilidades, bounded contexts, e incrementalidade.

---

## 2. Como os Problemas Foram Agrupados

### 2.1 Agrupamento por Bounded Context

O critério primário de agrupamento é o **bounded context** (módulo). Cada prompt deve operar dentro de um único módulo sempre que possível, respeitando as fronteiras definidas pela arquitectura do NexTraceOne.

**Razão:** O NexTraceOne segue DDD com Clean Architecture. Cada módulo tem:
- O seu próprio `DbContext` (ou múltiplos)
- As suas próprias entidades de domínio
- Os seus próprios handlers MediatR
- A sua própria camada de infraestrutura

Misturar módulos num único prompt cria risco de:
- Violar fronteiras de bounded context
- Introduzir acoplamento indevido
- Dificultar revisão e validação

**Exemplo prático:**

| Problema | Módulo | Prompt |
|----------|--------|--------|
| IsSimulated em reliability handlers | `operationalintelligence` | Prompt dedicado para OperationalIntelligence |
| IsSimulated em FinOps handlers | `governance` | Prompt dedicado para Governance |
| Handlers ExternalAI vazios | `aiknowledge` | Prompt dedicado para AIKnowledge |

Mesmo que o padrão `IsSimulated` seja o mesmo problema conceptual, os ficheiros pertencem a módulos diferentes e devem ser tratados em prompts separados.

### 2.2 Agrupamento por Severidade

Dentro do mesmo bounded context, os problemas são agrupados por severidade:

| Severidade | Critério | Exemplo |
|------------|----------|---------|
| **CRÍTICA** | Impede funcionalidade básica | Migrações ausentes, dados fabricados |
| **ESTRUTURAL** | Funcionalidade existe mas incompleta | Handlers vazios, TODOs |
| **ESTRATÉGICA** | Capacidade central não operacional | Knowledge module incompleto |
| **LIMPEZA** | Dívida técnica não urgente | Enum duplicado, docs obsoletos |
| **UX** | Experiência de produto incompleta | Release Calendar UI ausente |

### 2.3 Agrupamento por Tipo de Trabalho

Cada prompt deve envolver um único tipo de trabalho:

| Tipo | Descrição | Exemplo |
|------|-----------|---------|
| **Mecânico** | Alteração repetitiva e previsível | Adicionar CancellationToken |
| **Schema** | Criação/alteração de migrações EF | Migrations para Knowledge module |
| **Lógica** | Substituição de lógica simulada por real | IsSimulated → queries reais |
| **Integração** | Conectar com sistemas externos | ExternalAI provider adapters |
| **Frontend** | Alteração de UI/UX | Substituir mock data, criar páginas |
| **Novo módulo** | Criar módulo completo | Licensing & Entitlements |

**Regra:** Nunca misturar tipos de trabalho num único prompt. Um prompt que pede "adicionar CancellationToken E substituir IsSimulated" no mesmo handler cria complexidade desnecessária e risco de regressão.

---

## 3. Por Que Certos Problemas Foram Divididos

### 3.1 Princípio: 3-12 Ficheiros por Prompt

A granularidade ideal para um prompt é entre **3 e 12 ficheiros**. Esta faixa foi escolhida com base em:

- **Abaixo de 3 ficheiros:** O prompt é demasiado trivial; melhor agrupar com trabalho relacionado
- **Acima de 12 ficheiros:** O contexto do agente fica sobrecarregado; risco de esquecer ficheiros ou introduzir inconsistências
- **Zona ideal (3-12):** O agente consegue manter contexto completo de todos os ficheiros e verificar coerência

### 3.2 Exemplos de Divisão

**CancellationToken (~237 métodos, muitos ficheiros):**

Este problema foi dividido por módulo:
- Prompt 1: `identityaccess` — repositories e handlers
- Prompt 2: `governance` — repositories e handlers
- Prompt 3: `catalog` — repositories e handlers (3 sub-DbContexts)
- Prompt 4: `changegovernance` — repositories e handlers (4 sub-DbContexts)
- Prompt 5: `operationalintelligence` — repositories e handlers (5 sub-DbContexts)
- Prompt 6: `aiknowledge` — repositories e handlers (3 sub-DbContexts)
- Prompt 7: Módulos menores (`audit`, `notifications`, `config`, `integrations`, `knowledge`, `productanalytics`)

**Razão:** Cada módulo tem interfaces de repositório próprias. Alterar a assinatura de um repositório requer actualizar a interface, a implementação, e todos os handlers que o utilizam — tudo dentro do mesmo módulo.

**IsSimulated (31 ocorrências em 11 ficheiros):**

Dividido em dois prompts:
- Prompt A: `operationalintelligence` (reliability, cost, automation — ~7 ficheiros)
- Prompt B: `governance` (FinOps — ~10 ficheiros) + `productanalytics` (~1 ficheiro)

**Razão:** Os DbContexts são diferentes. Os handlers de OperationalIntelligence consultam `ReliabilityDbContext`, `CostIntelligenceDbContext` e `AutomationDbContext`. Os handlers de Governance consultam `GovernanceDbContext`. Misturar requer conhecimento de ambos os schemas.

---

## 4. Por Que Certos Prompts Vêm Primeiro

### 4.1 Princípio de Dependências Fundacionais

Prompts que resolvem **pré-requisitos** devem ser executados antes de prompts que dependem deles.

```
Fase 1: CancellationToken + Migrações
   │
   ├── CancellationToken é fundacional porque:
   │   - Toda operação async futura vai precisar
   │   - Não depende de nenhum outro problema
   │   - Alteração mecânica com risco mínimo
   │
   └── Migrações são fundacionais porque:
       - Sem migrações, Knowledge/Integrations/ProductAnalytics não podem persistir dados
       - Qualquer handler que tente escrever nestes módulos vai falhar
       - Bloqueia trabalho futuro nestes módulos

Fase 2: IsSimulated (depende de Phase 1)
   │
   └── IsSimulated depende de migrações porque:
       - Substituir dados simulados por queries reais requer tabelas reais
       - Se as tabelas não existem, as queries vão falhar

Fase 3: Handlers vazios (depende de Phase 1)
   │
   └── Handlers vazios dependem de CancellationToken porque:
       - Ao implementar handlers reais, devem já ter CancellationToken
       - Evita ter de refazer o mesmo handler duas vezes

Fase 4: Frontend (depende de Phase 2)
   │
   └── Frontend depende de IsSimulated porque:
       - Páginas frontend devem consumir dados reais
       - Se o backend ainda retorna dados simulados, a substituição de mocks não faz sentido
```

### 4.2 Ordem de Execução Recomendada

| Ordem | Prompt | Justificação |
|-------|--------|--------------|
| 1 | CancellationToken (por módulo) | Sem dependências; mecânico; fundacional |
| 2 | Migrações (Knowledge, Integrations, ProductAnalytics) | Sem dependências entre si; desbloqueia módulos |
| 3 | IsSimulated OperationalIntelligence | Depende de migrações estarem estáveis |
| 4 | IsSimulated Governance FinOps | Mesmo padrão, módulo diferente |
| 5 | ExternalAI handlers vazios | Depende de CancellationToken estar feito |
| 6 | Orchestration handlers vazios | Depende de ExternalAI |
| 7 | TODO Governance handlers | Independente, pode parallelizar com 5-6 |
| 8 | Frontend mock data replacement | Depende de IsSimulated estar resolvido |
| 9+ | Lacunas estratégicas | Depende de fundações estarem sólidas |

---

## 5. Quais Prompts Podem Rodar em Paralelo

### 5.1 Regra de Paralelismo

Dois prompts podem rodar em paralelo se e somente se:

1. **Tocam em ficheiros diferentes** — Nenhum ficheiro é editado por ambos
2. **Estão em módulos diferentes** — Fronteiras de bounded context protegem contra interferência
3. **Não têm dependência de dados** — Um não precisa do resultado do outro

### 5.2 Mapa de Paralelismo

```
═══════════════════════════════════════════════════════════
FASE 1 — Pode tudo em paralelo (módulos diferentes)
═══════════════════════════════════════════════════════════

Paralelo A: CancellationToken identityaccess
Paralelo B: CancellationToken governance
Paralelo C: CancellationToken catalog
Paralelo D: CancellationToken changegovernance
Paralelo E: CancellationToken operationalintelligence
Paralelo F: CancellationToken aiknowledge
Paralelo G: CancellationToken módulos menores
Paralelo H: Migration Knowledge
Paralelo I: Migration Integrations
Paralelo J: Migration ProductAnalytics

Todos podem rodar simultaneamente — ficheiros completamente disjuntos.

═══════════════════════════════════════════════════════════
FASE 2 — Paralelo parcial
═══════════════════════════════════════════════════════════

Paralelo A: IsSimulated OperationalIntelligence
Paralelo B: IsSimulated Governance FinOps
Paralelo C: TODO Governance handlers (se ficheiros não sobrepõem com B)

A e B podem rodar em paralelo (módulos diferentes).
B e C devem ser verificados — ambos tocam no módulo Governance.
Se os handlers são diferentes, podem paralelizar.

═══════════════════════════════════════════════════════════
FASE 3 — Paralelo por módulo
═══════════════════════════════════════════════════════════

Paralelo A: ExternalAI handlers (aiknowledge)
Paralelo B: Frontend mock data (frontend)
Paralelo C: ProductAnalytics Contracts (productanalytics)

Módulos completamente diferentes — seguro paralelizar.
```

### 5.3 Anti-Padrão: Dois Prompts no Mesmo Módulo em Paralelo

**Nunca paralelizar** dois prompts que tocam no mesmo módulo, mesmo que sejam ficheiros diferentes. Razões:

- Imports e referências partilhadas podem criar conflitos
- Alterações em interfaces afectam múltiplos ficheiros
- Migrações EF no mesmo DbContext podem conflitar
- O agente B pode não ver as alterações do agente A

**Excepção:** Módulos com sub-DbContexts claramente separados (ex: `operationalintelligence` com 5 DbContexts) podem ser paralelizados se cada prompt toca apenas num sub-contexto.

---

## 6. Quais Prompts Têm Maior Risco

### 6.1 Matriz de Risco

| Prompt | Risco | Razão | Mitigação |
|--------|-------|-------|-----------|
| **IsSimulated → queries reais** | ALTO | Muda lógica de negócio; queries podem falhar se schema não corresponde; pode revelar bugs latentes | Testar queries em ambiente de dev; manter contratos de resposta estáveis |
| **Criação de migrações EF** | ALTO | Migrações incorrectas podem corromper schema; conflito com migrações existentes | Gerar em BD limpa; verificar SQL gerado antes de aplicar |
| **ExternalAI handlers** | MÉDIO | Integração com APIs externas; tratamento de erros; segurança de credenciais | Implementar com retry, circuit breaker; nunca logar credenciais |
| **CancellationToken** | BAIXO | Alteração mecânica; não muda lógica | Verificar que testes existentes passam |
| **Frontend mock replacement** | BAIXO | Apenas frontend; não afecta persistência | Testar com e sem dados no backend |
| **TODO handlers** | BAIXO | Completar lógica existente | Verificar contratos de resposta |

### 6.2 Estratégia de Mitigação por Risco

**Risco ALTO — IsSimulated:**
1. Para cada handler, primeiro verificar que o DbContext e tabelas correspondentes existem
2. Escrever a query real e verificar que compila
3. Quando não há dados, retornar conjuntos vazios (não null, não excepção)
4. Manter o contrato de resposta (DTO) estável — não alterar campos publicados
5. Remover flag `IsSimulated` ou defini-la como `false`
6. Executar testes existentes após cada handler alterado

**Risco ALTO — Migrações:**
1. Verificar entity configurations no DbContext antes de gerar
2. Gerar migração e inspecionar o SQL produzido
3. Verificar naming conventions (prefixos `knw_`, `int_`, `pan_`)
4. Aplicar em base de dados de desenvolvimento
5. Verificar que up e down migration funcionam
6. Não alterar migrações de outros módulos

---

## 7. Como Garantir que o Agente Não Se Perca

### 7.1 Princípio: Single Bounded Context por Prompt

Cada prompt deve:

1. **Nomear explicitamente o módulo** — Ex: "Módulo: operationalintelligence"
2. **Listar todos os ficheiros a alterar** — Lista exhaustiva, não "e outros"
3. **Definir o objectivo numa frase** — Ex: "Substituir dados simulados por queries reais"
4. **Fornecer padrão de antes/depois** — Ex: "De `IsSimulated = true` para `query ao DbContext`"
5. **Definir critério de conclusão** — Ex: "Zero handlers com IsSimulated, build passa"

### 7.2 Informação Contextual Obrigatória no Prompt

Cada prompt deve incluir:

```
## Contexto
- Módulo: [nome do módulo]
- Bounded Context: [descrição curta]
- DbContext(s) relevante(s): [lista]
- Ficheiros a alterar: [lista com caminhos completos]

## Objectivo
[Uma frase clara]

## Padrão Actual (antes)
[Código de exemplo do que existe]

## Padrão Desejado (depois)
[Código de exemplo do que deve existir]

## Restrições
- Não alterar contratos de resposta (DTOs)
- Não alterar ficheiros fora do módulo
- Adicionar CancellationToken se ainda não existir
- Manter i18n em qualquer texto novo

## Critério de Conclusão
- [ ] Build sem erros
- [ ] Testes existentes passam
- [ ] [Critérios específicos]
```

### 7.3 Anti-Padrões a Evitar no Prompt

| Anti-Padrão | Problema | Solução |
|-------------|----------|---------|
| "Corrija todos os problemas do módulo X" | Demasiado vago; agente não sabe por onde começar | Listar problemas específicos e ficheiros |
| "Faça como achar melhor" | Agente pode tomar decisões arquitecturais incorrectas | Dar padrão explícito de antes/depois |
| "Altere o que for necessário" | Escopo ilimitado; pode tocar em ficheiros inesperados | Listar ficheiros explicitamente |
| Prompt com >15 ficheiros | Agente perde contexto; esquece ficheiros | Dividir em prompts menores |
| Prompt com múltiplos tipos de trabalho | Agente confunde objectivos | Um tipo por prompt |

---

## 8. Como a Granularidade Foi Escolhida

### 8.1 Critério Primário: Contagem de Handlers/Ficheiros

| Contagem de Ficheiros | Decisão |
|----------------------|---------|
| 1-2 ficheiros | Agrupar com trabalho relacionado do mesmo módulo |
| 3-8 ficheiros | Prompt individual |
| 9-12 ficheiros | Prompt individual, verificar se pode dividir |
| 13+ ficheiros | Obrigatório dividir em múltiplos prompts |

### 8.2 Critério Secundário: Complexidade da Alteração

| Complexidade | Ficheiros por Prompt | Razão |
|-------------|---------------------|-------|
| Mecânica (CancellationToken) | 10-12 | Alteração repetitiva, baixo risco |
| Lógica simples (TODO completion) | 6-8 | Requer entendimento do handler |
| Lógica complexa (IsSimulated) | 4-6 | Requer entendimento do schema |
| Integração (ExternalAI) | 3-4 | Requer entendimento de APIs externas |
| Schema (migrações) | 1-3 | Alto impacto por ficheiro |

### 8.3 Exemplo de Calibração

**Módulo operationalintelligence com IsSimulated:**

- Total de handlers afectados: ~13
- Ficheiros de handler: ~7 (alguns handlers partilham ficheiro)
- DbContexts relevantes: 3 (Reliability, Cost, Automation)
- Ficheiros de repository: ~5

**Opções:**
1. Um prompt com todos os 12 ficheiros → ❌ Demasiado, DbContexts diferentes
2. Três prompts por sub-DbContext (4 ficheiros cada) → ✅ Ideal
3. Treze prompts individuais (1 handler cada) → ❌ Demasiado granular

**Decisão:** 2-3 prompts, agrupados por sub-DbContext:
- Prompt A: Reliability handlers + ReliabilityDbContext queries (~5 ficheiros)
- Prompt B: Cost handlers + CostIntelligenceDbContext queries (~4 ficheiros)
- Prompt C: Automation handlers + AutomationDbContext queries (~3 ficheiros)

---

## 9. Como Evitar Prompts Grandes

### 9.1 Regra dos 12 Ficheiros

**Nenhum prompt deve tocar em mais de 12 ficheiros.** Esta regra é baseada em:

1. **Limites de contexto do agente** — Com mais de 12 ficheiros, o agente tende a esquecer detalhes dos primeiros ficheiros quando chega aos últimos
2. **Verificabilidade** — Um revisor humano consegue verificar até 12 ficheiros alterados de forma razoável; acima disso, a revisão torna-se superficial
3. **Reversibilidade** — Se algo correr mal, reverter 12 ficheiros é gerível; reverter 30 é problemático

### 9.2 Técnicas de Redução

**Dividir por camada:**
Se um problema toca em handlers (Application) e repositories (Infrastructure):
- Prompt 1: Repositories — alterar interfaces e implementações
- Prompt 2: Handlers — alterar para usar CancellationToken (depende de Prompt 1)

**Dividir por sub-domínio:**
Se um módulo tem múltiplos DbContexts:
- Um prompt por DbContext

**Dividir por operação:**
Se o problema é "completar CRUD":
- Prompt 1: Create + Read
- Prompt 2: Update + Delete

### 9.3 Excepção Permitida

CancellationToken em módulos pequenos (ex: `configuration` com 4 repositories) pode agrupar mais ficheiros porque a alteração é mecânica e repetitiva. O risco é proporcional à complexidade, não ao número de ficheiros.

---

## 10. Como Evitar Muitas Responsabilidades por Prompt

### 10.1 Regra: Um Tipo de Trabalho por Prompt

Cada prompt deve ter **exactamente um objectivo**:

| ✅ Bom | ❌ Mau |
|--------|--------|
| "Adicionar CancellationToken ao módulo X" | "Adicionar CancellationToken e substituir IsSimulated no módulo X" |
| "Substituir IsSimulated em handlers de Reliability" | "Substituir IsSimulated e criar testes e actualizar documentação" |
| "Criar migração para Knowledge" | "Criar migração e implementar operações CRUD e criar UI" |

### 10.2 Teste de Responsabilidade Única

Antes de finalizar um prompt, aplicar este teste:

1. **O prompt pode ser descrito numa única frase?** Se precisa de "e", "também" ou "além disso", provavelmente tem responsabilidades demais.
2. **O critério de conclusão tem mais de 3 itens?** Se sim, considerar dividir.
3. **Os ficheiros pertencem a camadas diferentes?** Se toca em API + Application + Infrastructure + Frontend, provavelmente é grande demais.
4. **Um segundo revisor entenderia o escopo em 30 segundos?** Se não, simplificar.

### 10.3 Exemplo de Decomposição

**Problema original:** "Completar módulo Knowledge"

**Decomposição em prompts de responsabilidade única:**

| # | Prompt | Tipo | Ficheiros |
|---|--------|------|-----------|
| 1 | Criar migração EF para KnowledgeDbContext | Schema | 2-3 |
| 2 | Implementar operações Create e Read de artigos | Lógica | 5-6 |
| 3 | Implementar operações Update e Delete de artigos | Lógica | 4-5 |
| 4 | Implementar FTS search dentro de artigos | Lógica | 3-4 |
| 5 | Implementar relações cross-module | Lógica | 4-6 |
| 6 | Criar páginas frontend de Knowledge Hub | Frontend | 6-8 |

Total: 6 prompts em vez de 1 prompt gigante.

---

## 11. Como Evitar Conflitos Entre Prompts

### 11.1 Regra de Disjunção de Ficheiros

**Dois prompts nunca devem alterar o mesmo ficheiro.** Isto é garantido por:

1. **Agrupamento por módulo** — Cada prompt opera num único bounded context
2. **Lista explícita de ficheiros** — Cada prompt lista os ficheiros que vai tocar
3. **Verificação cruzada** — Antes de finalizar o conjunto de prompts, verificar que nenhum ficheiro aparece em dois prompts

### 11.2 Fases Sequenciais

Quando dois prompts do mesmo módulo precisam alterar ficheiros que poderiam sobrepor-se, são colocados em fases sequenciais:

```
Fase 1: CancellationToken no módulo X
   Toca em: interfaces de repositório, implementações, handlers
   
Fase 2: IsSimulated no módulo X
   Toca em: handlers (os mesmos da Fase 1, mas já com CancellationToken)
```

A Fase 2 só executa após a Fase 1 estar concluída e integrada (merge).

### 11.3 Ficheiros Partilhados

Alguns ficheiros são partilhados entre funcionalidades (ex: `GovernanceRepositories.cs` contém múltiplos repositórios). Estratégia:

- **Se possível:** Dividir o ficheiro antes (refactoring prévio)
- **Se não possível:** Atribuir a um único prompt e marcar como ficheiro exclusivo
- **Nunca:** Dois prompts a alterar o mesmo ficheiro em paralelo

### 11.4 Ficheiros de Configuração (*.csproj, .sln)

Ficheiros como `NexTraceOne.sln` ou `.csproj` partilhados devem ser alterados apenas por um prompt por fase. Se múltiplos prompts precisam de alterar o `.sln`:
- Consolidar as alterações ao `.sln` num prompt de setup
- Ou sequenciar estritamente

---

## 12. Checklist de Validação de Prompt

Antes de submeter qualquer prompt para execução, verificar:

### 12.1 Escopo

- [ ] O prompt opera num único bounded context (módulo)?
- [ ] O prompt tem um único tipo de trabalho?
- [ ] O prompt toca em 3-12 ficheiros?
- [ ] Todos os ficheiros estão listados com caminhos completos?
- [ ] Nenhum ficheiro é partilhado com outro prompt da mesma fase?

### 12.2 Contexto

- [ ] O módulo está identificado?
- [ ] Os DbContexts relevantes estão listados?
- [ ] O padrão actual (antes) está exemplificado?
- [ ] O padrão desejado (depois) está exemplificado?
- [ ] As restrições estão explícitas?

### 12.3 Critérios

- [ ] O critério de conclusão é verificável?
- [ ] O build sem erros é critério obrigatório?
- [ ] Os testes existentes são citados?
- [ ] A alteração preserva contratos publicados?

### 12.4 Riscos

- [ ] O nível de risco está identificado?
- [ ] A mitigação está descrita?
- [ ] Existe fallback se o prompt falhar?
- [ ] Dependências de outros prompts estão documentadas?

---

## 13. Resumo da Estratégia

### 13.1 Princípios Resumidos

| # | Princípio | Regra |
|---|-----------|-------|
| 1 | Um bounded context por prompt | Não misturar módulos |
| 2 | Um tipo de trabalho por prompt | Mecânico OU lógico OU schema OU frontend |
| 3 | 3-12 ficheiros por prompt | Nunca mais de 12 |
| 4 | Fundacional primeiro | CancellationToken e migrações antes de tudo |
| 5 | Paralelo entre módulos | Módulos diferentes podem rodar em paralelo |
| 6 | Sequencial dentro do módulo | Mesmo módulo, fases sequenciais |
| 7 | Ficheiros disjuntos | Nenhum ficheiro em dois prompts da mesma fase |
| 8 | Contexto completo | Cada prompt é auto-suficiente |
| 9 | Critério verificável | Build + testes + critérios específicos |
| 10 | Risco documentado | Mitigação explícita para cada nível |

### 13.2 Resultado Esperado

Seguindo esta estratégia, cada prompt produz:
- Uma alteração coesa e verificável
- Sem conflitos com outros prompts
- Com contexto suficiente para um agente executar sem ambiguidade
- Com critérios claros de sucesso
- Com risco identificado e mitigado

O conjunto total de prompts cobre todas as lacunas identificadas na avaliação, respeitando a ordem de dependências e permitindo paralelismo onde seguro.

---

## 14. Exemplo Completo de Prompt Gerado

Para ilustrar a aplicação da estratégia, segue um exemplo completo:

```markdown
## Prompt: Substituir IsSimulated em Handlers de Reliability

### Contexto
- Módulo: operationalintelligence
- Bounded Context: Service Reliability
- DbContext: ReliabilityDbContext
- Localização: src/modules/operationalintelligence/

### Ficheiros a Alterar
1. src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Application/Features/GetReliabilitySnapshot/GetReliabilitySnapshot.cs
2. src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Application/Features/GetBurnRate/GetBurnRate.cs
3. src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Application/Features/GetErrorBudget/GetErrorBudget.cs
4. src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Application/Features/GetServiceHealth/GetServiceHealth.cs

### Objectivo
Substituir dados fabricados (IsSimulated = true) por queries reais ao ReliabilityDbContext.

### Padrão Actual
```csharp
return new Response { IsSimulated = true, Data = FabricatedData() };
```

### Padrão Desejado
```csharp
var data = await _dbContext.ReliabilitySnapshots
    .Where(s => s.ServiceId == request.ServiceId)
    .OrderByDescending(s => s.CreatedAt)
    .FirstOrDefaultAsync(cancellationToken);
return new Response { IsSimulated = false, Data = data ?? EmptyResult() };
```

### Restrições
- Não alterar DTOs de resposta
- Retornar conjuntos vazios quando não há dados (não null)
- Usar CancellationToken em todas as queries
- Manter logging estruturado

### Critério de Conclusão
- [ ] Zero handlers com IsSimulated = true nestes 4 ficheiros
- [ ] Build sem erros
- [ ] Testes existentes passam
```

Este formato garante que o agente tem toda a informação necessária para executar o trabalho sem ambiguidade, dentro das fronteiras correctas do bounded context.

---

*Documento gerado como parte da avaliação de estado do projecto NexTraceOne em 2026-03-27.*
