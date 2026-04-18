# NexTraceOne — Gaps e Problemas: Testes
**Data:** 2026-04-18  
**Modo:** Analysis realista — sem minimizar problemas  
**Referência:** [STATE-OF-PRODUCT-2026-04-18.md](./STATE-OF-PRODUCT-2026-04-18.md)

---

## 1. Resumo

O NexTraceOne tem um volume de testes expressivo: ~4.000 testes backend (xUnit) e ~1.700 testes frontend (Vitest + Playwright). Este volume é real e reflecte investimento sério em qualidade. No entanto, o **tipo** de testes é desequilibrado:

- **Demasiados testes unitários que testam mocks** — verificam comportamento de handlers que já não falham porque os seus repositórios são mocks
- **Poucos testes de integração reais** — Testcontainers está configurado mas usado em poucos cenários
- **Testes E2E frontend com MSW** — testam o frontend contra mocks, não contra o backend real
- **Ausência de testes de arquitectura** — nenhuma verificação automática de dependências entre módulos
- **Sem cobertura de segurança automatizada** — os críticos C-01 a C-10 não têm testes de regressão

**A verdade sobre a suite actual:** Os testes passam porque testam implementações isoladas contra mocks bem definidos. Não provam que o sistema funciona como um todo.

---

## 2. Análise da Suite de Testes Backend

### 2.1 Volume por módulo

| Módulo | Testes | Tipo dominante | Cobertura real |
|--------|--------|----------------|----------------|
| Building Blocks | ~400 | Unitário real | Alta |
| Catalog | ~1179 | Unitário com mock | Média |
| Change Governance | ~307 | Unitário com mock | Média |
| Operational Intelligence | ~639 | Unitário com mock | Média |
| AI Knowledge | ~819 | Unitário com mock | Média |
| Identity Access | ~150 | Unitário com mock | Baixa |
| Configuration | ~451 | Unitário com mock | Média |
| Governance | ~233 | Unitário com mock | Média |
| Knowledge | ~60 | Unitário com mock | Baixa |
| Notifications | ~70 | Unitário com mock | Baixa |
| Integrations | ~50 | Unitário com mock | Baixa |
| Product Analytics | ~40 | Unitário com mock | Muito baixa |

**Total estimado:** ~4.398 testes

### 2.2 Problema estrutural: Testes unitários que testam mocks

**Padrão identificado em ~60% dos testes de módulo:**

```csharp
// Handler:
public async Task<Result<ContractDto>> Handle(GetContractQuery query, CancellationToken ct)
{
    var contract = await _repository.GetByIdAsync(query.Id, ct);
    if (contract is null) return Result.Failure<ContractDto>("Contract not found");
    return Result.Success(_mapper.Map<ContractDto>(contract));
}

// Teste:
[Fact]
public async Task Handle_WhenContractExists_ReturnsSuccess()
{
    // Mock do repositório
    _repository.GetByIdAsync(Arg.Any<ContractId>(), Arg.Any<CancellationToken>())
               .Returns(new Contract { ... });
    
    var result = await _handler.Handle(new GetContractQuery(id), CancellationToken.None);
    
    result.IsSuccess.Should().BeTrue();
}
```

**O que este teste prova:** Que o handler chama o repositório e mapeia o resultado. Não prova que:
- A query SQL real funciona
- A migração criou o schema correcto
- O RLS não está a bloquear dados
- A serialização JSON do DTO funciona
- O endpoint retorna o HTTP status code correcto

### 2.3 Cobertura de integração real (Testcontainers)

**Configurado mas subutilizado:**
- `Testcontainers.PostgreSql` está nas dependências
- `Respawn` para reset de base de dados está configurado
- `NexTraceOne.IntegrationTests` existe como projecto

**Lacunas identificadas:**
- Menos de 15% dos fluxos críticos têm testes de integração reais
- Módulos Identity Access, Knowledge, Notifications não têm testes de integração
- Nenhum teste de integração cobre o fluxo C-04 (endpoints em falta)
- Nenhum teste de integração verifica RLS (cross-tenant data leakage)

### 2.4 Cenários críticos sem cobertura de teste

Os problemas mais graves do produto não têm testes de regressão:

| Problema | Tem teste de regressão? |
|----------|------------------------|
| C-01: Colisão de tabela chg_promotion_gates | ❌ Não |
| C-02: JWT hardcoded key | ❌ Não |
| C-04: 6 endpoints em falta | ❌ Não (impossível testar o que não existe) |
| A-01: TenantId.IsRequired() ausente | ❌ Não |
| A-04: Silent exception handling | ❌ Não |
| C-06: OnCall pseudo-random | ⚠️ Passa (mas testa o comportamento errado) |

### 2.5 Ausência de testes de arquitectura

O NexTraceOne tem regras de arquitectura explícitas no CLAUDE.md:
- Módulo X não deve aceder ao DbContext de módulo Y
- Application layer não deve depender de Infrastructure
- Domain não deve conhecer detalhe de persistência

**Problema:** Estas regras são verificadas manualmente em code review. Não há:
- Testes NetArchTest.Rules ou equivalente
- Verificação automática de dependências entre camadas
- Detecção de violações de bounded context

**Consequência:** Uma violação pode entrar no codebase sem que ninguém a detecte.

### 2.6 Testes de performance/carga

**Estado:** `tests/load/` existe com infra Gatling, mas:
- Sem cenários escritos para fluxos críticos (search, change promotion, contract diff)
- Sem baselines documentados (o que é "rápido o suficiente"?)
- Sem integração com CI para detectar regressões de performance

---

## 3. Análise da Suite de Testes Frontend

### 3.1 Volume e estrutura

| Categoria | Ficheiros | Estado |
|-----------|-----------|--------|
| __mocks__ | Múltiplos | MSW handlers bem estruturados |
| components | ~40 ficheiros | Maioritariamente snapshot tests |
| hooks | ~15 ficheiros | Cobertura razoável |
| pages | ~60 ficheiros | Quase todos com MSW mocks |
| utils | ~20 ficheiros | Boa cobertura |
| contexts | ~10 ficheiros | Cobertura parcial |

**Total estimado:** ~1.700 testes

### 3.2 Problema estrutural: Frontend testado contra mocks

**Padrão dominante:**

```typescript
// Teste de página:
it('renders contract list', async () => {
    // MSW intercepta chamada real ao backend
    server.use(
        rest.get('/api/v1/contracts', (req, res, ctx) => {
            return res(ctx.json(mockContracts)); // dados fictícios
        })
    );
    
    render(<ContractListPage />);
    expect(await screen.findByText('My Contract')).toBeInTheDocument();
});
```

**O que prova:** Que o componente React renderiza dados quando o backend responde. Não prova que:
- O backend real retorna aquele formato de dados
- A paginação funciona com dados reais
- Permissões são respeitadas
- Estados de erro são tratados correctamente quando o backend falha de forma inesperada

### 3.3 Testes E2E Playwright — estado real

**Configuração:** `playwright.config.ts` + `playwright.real.config.ts`

**Problema:**
- `playwright.real.config.ts` existe para testes contra ambiente real
- Mas não há evidência de quais testes existem no suite real
- Os testes E2E presentes parecem usar MSW mesmo no config "real"
- Sem cobertura dos fluxos de happy path mais críticos: login → criar serviço → adicionar contrato → criar change → promover

**Fluxos críticos sem E2E:**
- Onboarding completo (convite → activação → login → primeiro serviço)
- Criação de contrato OpenAPI → publicação → compliance gate
- Change criada → aprovação → promoção com blast radius
- Incident correlacionado com change → mitigation playbook

### 3.4 Ausência de testes de acessibilidade

Nenhum ficheiro de teste importa `jest-axe` ou `@axe-core/playwright`. Produto enterprise sem verificação de acessibilidade automatizada é um risco em contextos de procurement enterprise.

### 3.5 Testes de regressão visual

Não há evidência de testes de regressão visual (Chromatic, Percy, ou screenshots Playwright). Mudanças de UI podem introduzir regressões visuais sem detecção automática.

---

## 4. Análise dos Testes de Segurança

**O que existe:**
- `NexTraceOne.BuildingBlocks.Security.Tests` — 8 testes de unidade de auth
- `security.yml` pipeline — scanning de dependências + SAST

**O que falta:**
- Testes de penetração automatizados (OWASP ZAP ou similar)
- Testes de tenant isolation (verificar que tenant A não consegue dados de tenant B)
- Testes de rate limiting (verificar que as 6 políticas funcionam como esperado)
- Testes de privilege escalation (JIT, Break Glass com aprovações inválidas)
- Testes de input validation/SQL injection nas camadas de API

---

## 5. Cobertura de código — lacunas identificadas

Sem execução de `dotnet test --collect:"XPlat Code Coverage"`, os números exactos não estão disponíveis, mas a análise estrutural indica:

| Módulo | Cobertura estimada | Áreas não cobertas |
|--------|-------------------|-------------------|
| Identity Access | ~55% | Fluxos de convite, MFA, Break Glass completo |
| Change Governance | ~70% | Collision path, freeze windows em produção |
| Operational Intelligence | ~65% | OnCall real, correlação avançada |
| Knowledge | ~45% | Knowledge Graph, relações transitivas |
| Notifications | ~50% | Delivery channels, templates dinâmicos |
| Product Analytics | ~35% | Maioria não testado |
| Integrations | ~40% | Webhooks de CI/CD, multi-cluster |

---

## 6. Qualidade dos testes existentes

### 6.1 O que está bem feito

- **Building Blocks**: Testes genuinamente úteis que verificam comportamento real (Result<T>, guards, strongly typed IDs)
- **Catalog**: Volume alto com cenários de borda (compatibilidade semântica, drift detection)
- **AI Knowledge**: Testes de guardrails com strings de injecção real — coverage de casos reais
- **Audit Compliance**: Testes de integridade da hash chain — verificação funcional real

### 6.2 Padrões de teste problemáticos

```csharp
// [ANTI-PADRÃO 1] Teste que não testa nada real:
[Fact]
public void Constructor_ShouldSetProperties()
{
    var entity = new Contract { Name = "Test" };
    entity.Name.Should().Be("Test"); // óbvio
}

// [ANTI-PADRÃO 2] Teste que sempre passa:
[Fact]
public async Task Handle_ShouldReturnResult()
{
    var result = await _handler.Handle(query, CancellationToken.None);
    result.Should().NotBeNull(); // qualquer resultado passa
}

// [ANTI-PADRÃO 3] Dados de teste sem Bogus/faker:
var contract = new Contract { Id = Guid.NewGuid(), Name = "Test Contract", Version = "1.0" };
// Hardcoded — não testa variações de input
```

---

## 7. Recomendações prioritárias

### Curto prazo (Sprint 1)

1. **Testes de integração para fluxos críticos** — Identity (login, activation), Change promotion, Contract creation
2. **Testes de RLS** — Verificar que tenant A não vê dados de tenant B
3. **Testes de arquitectura** com NetArchTest.Rules — 1 dia de setup, protege para sempre
4. **Teste E2E de smoke** — happy path do produto em 10 minutos de execução

### Médio prazo (Sprint 2-3)

5. **Expandir integração tests** para cobrir todos os módulos críticos
6. **Substituir testes que testam o óbvio** por testes de comportamento real
7. **Adicionar axe-core** nos testes de componentes principais
8. **Implementar cenários de carga Gatling** para change promotion e catalog search

### Longo prazo

9. **Contract testing** (Pact.io ou equivalente) entre frontend e backend — detecta DTO mismatches antes de chegar a produção
10. **Mutation testing** (Stryker.NET) em módulos críticos — detecta testes que passam mesmo com bugs

---

## 8. Veredicto final sobre testes

**O volume de testes cria uma falsa sensação de segurança.**

4.000 testes que verificam que mocks retornam o que foram programados para retornar não são equivalentes a 400 testes de integração que verificam que o sistema real funciona.

**Antes de considerar o produto production-ready:**
- Pelo menos 30% dos testes de módulo devem ser de integração com BD real
- Os 6 fluxos E2E críticos devem existir e passar
- Testes de tenant isolation devem existir e passar

---

*Para análise de documentação ver [GAPS-DOCUMENTATION-2026-04-18.md](./GAPS-DOCUMENTATION-2026-04-18.md)*
