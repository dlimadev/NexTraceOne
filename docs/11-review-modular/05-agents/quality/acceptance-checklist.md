# Agents — Checklist de Aceite

> **Módulo:** Agents  
> **Área:** Qualidade — Acceptance Checklist  
> **Estado:** `NOT_STARTED`  
> **Prioridade:** `CRITICAL`  
> **Última atualização:** [A PREENCHER]

---

## Instruções

Este checklist deve ser preenchido para confirmar que o módulo Agents está pronto para produção. Cada item deve ser verificado e marcado.

---

## 1. Frontend

- [ ] Todas as páginas carregam corretamente
- [ ] Navegação entre páginas funciona sem erros
- [ ] Formulários validam inputs corretamente
- [ ] Mensagens de sucesso exibidas após ações
- [ ] Mensagens de erro claras e informativas
- [ ] Estados de loading implementados
- [ ] Estados vazios (empty state) implementados
- [ ] Estados de erro tratados graciosamente
- [ ] Responsividade testada (desktop, tablet, mobile)
- [ ] Acessibilidade básica verificada (tab navigation, aria labels)
- [ ] Sem erros no console do browser
- [ ] Performance aceitável (First Contentful Paint < 2s)
- [ ] Componentes reutilizáveis onde aplicável

<!-- TODO: validar cada item -->

---

## 2. Backend

- [ ] Todos os endpoints respondem corretamente
- [ ] Validações de input implementadas em todos os endpoints
- [ ] Regras de negócio corretamente aplicadas
- [ ] Tratamento de erros consistente (Result pattern)
- [ ] CancellationToken propagado em todas as operações async
- [ ] Guard clauses no início dos métodos
- [ ] Domain events emitidos corretamente
- [ ] Application services sem lógica de domínio
- [ ] Domain sem referências a infraestrutura
- [ ] Strongly typed IDs utilizados
- [ ] Sem uso de DateTime.Now (usar abstrações)
- [ ] Contratos de API estáveis e documentados

<!-- TODO: validar cada item -->

---

## 3. Base de Dados

- [ ] Schema aderente ao modelo de domínio
- [ ] Índices criados para queries frequentes
- [ ] Foreign keys definidas corretamente
- [ ] Constraints de unicidade onde necessário
- [ ] Tipos de dados adequados
- [ ] Migrations reversíveis
- [ ] Seeds idempotentes e completos
- [ ] RLS configurado para multi-tenancy
- [ ] Audit columns presentes (CreatedAt, UpdatedAt, CreatedBy, UpdatedBy)
- [ ] Sem dados hardcoded em migrations

<!-- TODO: validar cada item -->

---

## 4. i18n

- [ ] Todos os títulos internacionalizados
- [ ] Todos os labels internacionalizados
- [ ] Todos os placeholders internacionalizados
- [ ] Todos os botões internacionalizados
- [ ] Todas as mensagens de erro internacionalizadas
- [ ] Todas as mensagens de sucesso internacionalizadas
- [ ] Todos os tooltips internacionalizados
- [ ] Todos os estados vazios internacionalizados
- [ ] Todas as mensagens de loading internacionalizadas
- [ ] Sem textos hardcoded no frontend
- [ ] Chaves i18n organizadas por módulo/página

<!-- TODO: validar cada item -->

---

## 5. Segurança

- [ ] Autenticação obrigatória em todas as rotas protegidas
- [ ] Autorização verificada em todos os endpoints
- [ ] Permissões granulares implementadas
- [ ] Tokens com expiração adequada
- [ ] Dados sensíveis não expostos em logs
- [ ] CORS configurado corretamente
- [ ] Rate limiting aplicado
- [ ] Input sanitization implementado

<!-- TODO: validar cada item -->

---

## 6. Auditoria

- [ ] Eventos de criação/edição/remoção registados
- [ ] Eventos de alteração de permissões registados
- [ ] Todos os eventos contêm userId, tenantId, timestamp
- [ ] Eventos não contêm dados sensíveis em excesso
- [ ] Auditoria consultável via UI

<!-- TODO: validar cada item -->

---

## 7. Observabilidade

- [ ] Logs estruturados em operações críticas
- [ ] Correlation ID propagado
- [ ] Métricas expostas
- [ ] Health check implementado
- [ ] Tracing distribuído configurado
- [ ] Alertas definidos para falhas críticas

<!-- TODO: validar cada item -->

---

## 8. IA

- [ ] Capacidades de IA definidas para o módulo
- [ ] Modelos registados no Model Registry
- [ ] Permissões de IA configuradas
- [ ] Quotas de tokens definidas
- [ ] Auditoria de prompts e respostas
- [ ] Human-in-the-loop onde necessário
- [ ] Fallback para quando IA não está disponível

<!-- TODO: validar cada item -->

---

## 9. Agents

- [ ] Agents esperados estão registados
- [ ] Configuração de agents validada
- [ ] Prompts revistos e seguros
- [ ] Execução testada
- [ ] Observabilidade implementada
- [ ] Auditoria de execuções
- [ ] Isolamento de tenant nos agents

<!-- TODO: validar cada item -->

---

## 10. Documentação

- [ ] README do módulo completo
- [ ] Module overview preenchido
- [ ] Páginas documentadas
- [ ] Ações documentadas
- [ ] Endpoints documentados
- [ ] Application services documentados
- [ ] Regras de domínio documentadas
- [ ] Regras de autorização documentadas
- [ ] Validações documentadas
- [ ] Schema documentado
- [ ] Migrations revistas
- [ ] Seed data revisto
- [ ] Capacidades de IA documentadas
- [ ] Agents documentados
- [ ] Bugs e gaps listados
- [ ] Dívida técnica mapeada
- [ ] Cenários de teste definidos
- [ ] Comentários de código adequados
- [ ] Notas de onboarding criadas

<!-- TODO: validar cada item -->

---

## 11. Testes

- [ ] Testes unitários de domínio adequados
- [ ] Testes de application services adequados
- [ ] Testes de integração de endpoints
- [ ] Testes de autorização
- [ ] Testes de multi-tenancy
- [ ] Testes de validação
- [ ] Cobertura de testes aceitável (> 80% domínio)
- [ ] Todos os testes passam

<!-- TODO: validar cada item -->

---

## 12. Pronto para Produção

- [ ] Todas as secções acima validadas
- [ ] Nenhum bug `CRITICAL` em aberto
- [ ] Nenhum gap `CRITICAL` sem mitigação
- [ ] Revisão de código concluída
- [ ] Aprovação do Tech Lead
- [ ] Aprovação do Product Owner
- [ ] Deploy testado em staging
- [ ] Rollback plan definido

<!-- TODO: validar cada item -->

---

## Resultado Final

| Área | Aprovado? | Observações |
|------|----------|-------------|
| Frontend | [A PREENCHER] | <!-- TODO: preencher --> |
| Backend | [A PREENCHER] | <!-- TODO: preencher --> |
| Base de dados | [A PREENCHER] | <!-- TODO: preencher --> |
| i18n | [A PREENCHER] | <!-- TODO: preencher --> |
| Segurança | [A PREENCHER] | <!-- TODO: preencher --> |
| Auditoria | [A PREENCHER] | <!-- TODO: preencher --> |
| Observabilidade | [A PREENCHER] | <!-- TODO: preencher --> |
| IA | [A PREENCHER] | <!-- TODO: preencher --> |
| Agents | [A PREENCHER] | <!-- TODO: preencher --> |
| Documentação | [A PREENCHER] | <!-- TODO: preencher --> |
| Testes | [A PREENCHER] | <!-- TODO: preencher --> |
| **Veredicto final** | **[A PREENCHER]** | <!-- TODO: preencher --> |

---

> **Valores de estado válidos:** `NOT_STARTED` | `IN_ANALYSIS` | `GAP_IDENTIFIED` | `IN_FIX` | `BLOCKED` | `READY_FOR_RETEST` | `APPROVED` | `DONE`
>
> **Valores de prioridade válidos:** `CRITICAL` | `HIGH` | `MEDIUM` | `LOW`
