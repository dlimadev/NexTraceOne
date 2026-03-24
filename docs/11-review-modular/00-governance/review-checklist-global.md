# Checklist Global de Revisão — NexTraceOne

## Objetivo

Este checklist deve ser aplicado a **todos os módulos** durante a revisão modular. Cada secção cobre uma área transversal do produto. Ao rever um módulo, copiar este checklist para o ficheiro `module-review.md` do módulo e preencher conforme a análise.

---

## Como Usar

- ✅ = Conforme / Implementado
- ⚠️ = Parcial / Incompleto
- ❌ = Ausente / Não implementado
- ➖ = Não aplicável a este módulo

Substituir `[ ]` por `[x]` quando o item estiver verificado. Adicionar notas quando necessário.

---

## 1. Layout e UX

- [ ] As páginas do módulo seguem o layout padrão do NexTraceOne
- [ ] O header, sidebar e footer são consistentes com o resto do produto
- [ ] Os componentes de UI usam o design system do produto (não componentes ad-hoc)
- [ ] Os estados vazios (empty states) estão tratados com mensagens claras
- [ ] Os estados de loading estão implementados com feedback visual
- [ ] Os estados de erro exibem mensagens úteis ao utilizador
- [ ] A responsividade está adequada para as resoluções suportadas
- [ ] A hierarquia visual (tipografia, espaçamento, cores) é consistente
- [ ] Os formulários seguem o padrão de validação do produto
- [ ] Os modais e dialogs seguem o padrão visual do produto
- [ ] As tabelas têm paginação, ordenação e filtros quando aplicável
- [ ] A experiência varia conforme a persona do utilizador (quando aplicável)

---

## 2. Rotas e Navegação

- [ ] Todas as páginas do módulo têm rotas definidas e acessíveis
- [ ] As rotas seguem a convenção de nomenclatura do produto
- [ ] Não existem rotas órfãs (definidas mas não acessíveis)
- [ ] Não existem rotas duplicadas ou conflitantes
- [ ] A navegação entre páginas do módulo é intuitiva (breadcrumbs, back, etc.)
- [ ] As rotas protegidas validam autenticação e autorização
- [ ] O deep linking funciona (URLs diretas levam à página correta)
- [ ] A navegação preserva estado quando necessário (filtros, paginação, etc.)
- [ ] Rotas com parâmetros dinâmicos (`:id`) funcionam corretamente

---

## 3. Menu e Sidebar

- [ ] O módulo aparece no menu/sidebar na posição correta
- [ ] O ícone e label do menu estão corretos e usam i18n
- [ ] Os sub-itens do menu (se existirem) estão corretos
- [ ] O menu destaca o item ativo corretamente
- [ ] A visibilidade do menu respeita permissões do utilizador
- [ ] A visibilidade do menu respeita a persona do utilizador
- [ ] A ordem dos itens segue a lógica do produto (não ordem alfabética)
- [ ] Não existem itens de menu órfãos (sem página correspondente)
- [ ] Não existem itens de menu escondidos que deveriam ser visíveis

---

## 4. i18n / Traduções

- [ ] Todos os títulos de página usam chaves i18n
- [ ] Todos os labels de formulário usam chaves i18n
- [ ] Todos os botões usam chaves i18n
- [ ] Todos os placeholders usam chaves i18n
- [ ] Todas as mensagens de erro usam chaves i18n
- [ ] Todas as mensagens de sucesso usam chaves i18n
- [ ] Todos os tooltips usam chaves i18n
- [ ] Todos os estados vazios usam chaves i18n
- [ ] Todos os textos de loading usam chaves i18n
- [ ] As chaves i18n seguem a convenção de nomenclatura do produto
- [ ] Os idiomas suportados têm todas as chaves traduzidas
- [ ] Não existem textos hardcoded visíveis ao utilizador
- [ ] As mensagens de validação usam chaves i18n com parâmetros quando necessário

---

## 5. Backend — Endpoints

- [ ] Todos os endpoints do módulo estão implementados
- [ ] Os endpoints seguem a convenção REST do produto
- [ ] Os DTOs de request e response estão definidos e são claros
- [ ] Os endpoints retornam códigos HTTP corretos (200, 201, 400, 401, 403, 404, 500)
- [ ] Os endpoints tratam erros com mensagens estruturadas (code/messageKey/params)
- [ ] Os endpoints suportam paginação onde aplicável
- [ ] Os endpoints suportam filtros onde aplicável
- [ ] Os endpoints validam input (guard clauses, validação de DTOs)
- [ ] Os endpoints usam CancellationToken em operações async
- [ ] Os endpoints não retornam dados sensíveis desnecessariamente
- [ ] Existe documentação Swagger/OpenAPI para os endpoints
- [ ] Os endpoints são consumidos pelo frontend (não são órfãos)

---

## 6. Banco de Dados

- [ ] As entidades do módulo estão definidas e mapeadas corretamente
- [ ] As migrações estão criadas e aplicadas
- [ ] Os relacionamentos (FK, índices) estão corretos
- [ ] Os tipos de dados são adequados (não usar string para tudo)
- [ ] Existem índices para queries frequentes
- [ ] Não existem entidades órfãs (sem uso no código)
- [ ] Não existem campos deprecated sem plano de remoção
- [ ] Os dados de seed/exemplo estão atualizados (quando aplicável)
- [ ] A integridade referencial está garantida
- [ ] As queries não têm problemas de performance óbvios (N+1, full table scan)

---

## 7. Segurança e Autorização

- [ ] Todas as rotas protegidas exigem autenticação
- [ ] As ações do módulo respeitam o modelo de autorização (roles, permissions)
- [ ] Não existem endpoints públicos que deveriam ser protegidos
- [ ] Os dados são filtrados por tenant/organização quando aplicável
- [ ] Input do utilizador é sanitizado (proteção contra XSS, injection)
- [ ] Tokens e segredos não estão expostos no frontend
- [ ] As ações destrutivas (eliminar, alterar) exigem confirmação
- [ ] O princípio de menor privilégio está aplicado
- [ ] As políticas de acesso estão documentadas
- [ ] Rate limiting está configurado onde necessário

---

## 8. Auditoria

- [ ] Ações de criação geram registo de auditoria
- [ ] Ações de alteração geram registo de auditoria
- [ ] Ações de eliminação geram registo de auditoria
- [ ] Ações de acesso a dados sensíveis geram registo de auditoria
- [ ] Os registos de auditoria incluem: quem, o quê, quando, de onde
- [ ] Os registos de auditoria são imutáveis
- [ ] Existe forma de consultar o histórico de auditoria do módulo
- [ ] Ações de IA (prompts, respostas) são auditadas quando aplicável
- [ ] Alterações de configuração são auditadas

---

## 9. Observabilidade

- [ ] O módulo emite logs estruturados para operações relevantes
- [ ] Os logs incluem correlation ID para rastreabilidade
- [ ] Métricas de negócio estão definidas (quando aplicável)
- [ ] Métricas técnicas estão definidas (latência, erros, throughput)
- [ ] Traces distribuídos estão configurados para fluxos críticos
- [ ] Alertas estão definidos para cenários de falha
- [ ] Health checks incluem dependências do módulo
- [ ] Os dashboards de observabilidade estão contextualizados por serviço/equipa

---

## 10. IA

- [ ] A IA tem acesso ao contexto deste módulo (quando aplicável)
- [ ] As interações de IA são auditadas
- [ ] As políticas de acesso a modelos são respeitadas
- [ ] Os prompts usam contexto seguro (sem dados sensíveis desnecessários)
- [ ] As respostas da IA são validadas antes de serem apresentadas
- [ ] O controle de tokens/budget está aplicado
- [ ] A IA contextual (não genérica) está integrada no fluxo do módulo
- [ ] O fallback para ausência de IA está tratado (graceful degradation)

---

## 11. Agents e Background Services

- [ ] Os workers/agents do módulo estão identificados
- [ ] Os agents processam dados corretamente
- [ ] Os agents têm tratamento de erros adequado (retry, dead letter, logging)
- [ ] Os agents suportam CancellationToken para graceful shutdown
- [ ] Os agents não bloqueiam outras operações
- [ ] Os event handlers estão registados e funcionais
- [ ] Os scheduled jobs têm configuração de intervalo adequada
- [ ] Os agents são monitorizados (health check, métricas)

---

## 12. Documentação

- [ ] O módulo tem documentação técnica atualizada
- [ ] A documentação descreve a arquitetura do módulo
- [ ] A documentação descreve os endpoints e contratos
- [ ] A documentação descreve os fluxos principais
- [ ] A documentação inclui exemplos de uso
- [ ] A documentação está acessível a partir do knowledge hub (quando existir)
- [ ] Os comments no código são úteis e atualizados
- [ ] Não existe documentação desatualizada que possa confundir

---

## 13. Onboarding Técnico

- [ ] Um novo developer consegue entender o módulo lendo a documentação
- [ ] A estrutura de pastas do módulo é intuitiva
- [ ] As dependências do módulo estão claras
- [ ] Os testes existentes servem como documentação viva
- [ ] O módulo pode ser executado/testado isoladamente (quando possível)
- [ ] Existem exemplos de como estender o módulo
- [ ] Os padrões de código são consistentes com o resto do produto
- [ ] O README do módulo (se existir) está atualizado

---

## Resumo da Verificação

| Área                          | Status | Notas |
|-------------------------------|--------|-------|
| Layout e UX                   |        |       |
| Rotas e Navegação             |        |       |
| Menu e Sidebar                |        |       |
| i18n / Traduções              |        |       |
| Backend — Endpoints           |        |       |
| Banco de Dados                |        |       |
| Segurança e Autorização       |        |       |
| Auditoria                     |        |       |
| Observabilidade               |        |       |
| IA                            |        |       |
| Agents e Background Services  |        |       |
| Documentação                  |        |       |
| Onboarding Técnico            |        |       |

---

## Instruções Finais

1. Copiar as secções relevantes para o `module-review.md` de cada módulo
2. Preencher cada item durante a análise
3. Marcar itens não aplicáveis com ➖
4. Adicionar notas para itens parciais ou com ressalvas
5. Usar este checklist como base para a definição de "pronto" do módulo
