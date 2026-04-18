# TESTING-STRATEGY.md — NexTraceOne

> **Versão:** 1.0 — Março 2026
> **Escopo:** Estratégia de testes para backend (.NET) e frontend (React/TypeScript).
> **Audiência:** Engineers, Tech Leads e QA.

---

## 1. Pirâmide de Testes Adotada

O NexTraceOne segue a pirâmide clássica com as seguintes camadas, por ordem de volume e velocidade:

```
                    ┌─────────────┐
                    │    Load     │  (k6 — cenários críticos apenas)
                   ┌─────────────────┐
                   │   E2E / Playwright  │  (fluxos críticos do utilizador)
                  ┌─────────────────────────┐
                  │   Integration Tests      │  (Testcontainers, PostgreSQL real)
                 ┌───────────────────────────────┐
                 │        Unit Tests              │  (mais rápidos, maior volume)
                 └───────────────────────────────┘
```

| Camada | Ferramentas Backend | Ferramentas Frontend | Velocidade | Volume |
|--------|-------------------|---------------------|------------|--------|
| Unit | xUnit + NSubstitute + Bogus + FluentAssertions | Vitest + @testing-library/react + MSW | < 1s/teste | Alta |
| Integration | Testcontainers + Respawn + xUnit | Vitest + MSW handlers | 5–30s/teste | Média |
| E2E | — | Playwright | 10–60s/fluxo | Baixa |
| Load | k6 | — | Minutos | Muito baixa |

---

## 2. Quando Escrever Qual Tipo de Teste

### Testes Unitários — escrever quando:

- Implementar ou alterar um Command/Query Handler
- Adicionar lógica de domínio numa entidade
- Implementar algoritmo de negócio (ex.: scoring, blast radius, analytics)
- Implementar behaviors do MediatR pipeline
- Implementar utility/helper com lógica condicional
- Implementar um componente React com comportamento condicional

### Testes de Integração — escrever quando:

- Implementar repositório EF Core (verificar queries e mapeamentos)
- Adicionar migration e precisar validar que o schema está correto
- Implementar fluxo completo de um módulo (Command → Handler → DB → Event)
- Verificar comportamento do Outbox (domain event → outbox → processamento)
- Implementar endpoint de API que depende de estado persistido
- Verificar query TanStack com resposta real de API mockada via MSW

### Testes E2E — escrever quando:

- Fluxo crítico de negócio com múltiplas páginas (ex.: criar serviço → adicionar contrato → publicar)
- Fluxo de autenticação e autorização
- Wizard/multi-step forms
- Fluxo de integração CI/CD (ingestão de evento → change visível na UI)

### Testes de Load — escrever quando:

- Endpoint com volume alto esperado (ex.: `/catalog/services`, analytics summary)
- Endpoint de ingestão que receberá eventos de múltiplos sistemas
- Antes de go-live para validar SLOs

---

## 3. Testes Unitários — Backend

### Stack

| Ferramenta | Papel |
|---|---|
| **xUnit** | Framework de teste — `[Fact]` para testes únicos, `[Theory]` para parametrizados |
| **NSubstitute** | Mocking — substitui interfaces com fakes controláveis |
| **Bogus** | Geração de dados realistas (`Faker<T>`) |
| **FluentAssertions** | Assertions expressivas — `result.Should().BeTrue()` |

### Estrutura de pastas

```
tests/
├── building-blocks/
│   ├── NexTraceOne.BuildingBlocks.Core.Tests/
│   ├── NexTraceOne.BuildingBlocks.Infrastructure.Tests/
│   └── NexTraceOne.BuildingBlocks.Security.Tests/
└── modules/
    ├── integrations/
    │   ├── NexTraceOne.Integrations.Application.Tests/
    │   └── NexTraceOne.Integrations.Integration.Tests/
    ├── knowledge/
    │   └── NexTraceOne.Knowledge.Application.Tests/
    └── productanalytics/
        └── NexTraceOne.ProductAnalytics.Application.Tests/
```

### Exemplo completo — Handler de Command

```csharp
using Bogus;
using FluentAssertions;
using NSubstitute;
using Xunit;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Application.Features.CreateIntegrationConnector;
using NexTraceOne.Integrations.Domain.Entities;

namespace NexTraceOne.Integrations.Application.Tests.Features;

public sealed class CreateIntegrationConnectorHandlerTests
{
    private readonly IIntegrationConnectorRepository _repo = Substitute.For<IIntegrationConnectorRepository>();
    private readonly ICurrentTenant _tenant = Substitute.For<ICurrentTenant>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly Faker _faker = new("pt_BR");

    public CreateIntegrationConnectorHandlerTests()
    {
        _tenant.Id.Returns(Guid.NewGuid());
        _clock.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    private CreateIntegrationConnector.Handler CreateHandler()
        => new(_repo, _tenant, _clock);

    [Fact]
    public async Task Handle_GivenUniqueName_ReturnsSuccessWithNewId()
    {
        // Arrange
        var cmd = new CreateIntegrationConnector.Command(
            _faker.Company.CompanyName(), "GitLab", "https://gitlab.example.com");
        _repo.ExistsByNameAsync(cmd.Name, _tenant.Id, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        await _repo.Received(1).AddAsync(
            Arg.Is<IntegrationConnector>(c => c.Name == cmd.Name),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_GivenDuplicateName_ReturnsConflictError()
    {
        // Arrange
        var cmd = new CreateIntegrationConnector.Command("Duplicate", "Jenkins", null);
        _repo.ExistsByNameAsync(cmd.Name, _tenant.Id, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        await _repo.DidNotReceive().AddAsync(Arg.Any<IntegrationConnector>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("", "GitLab")]
    [InlineData(null, "GitLab")]
    [InlineData("ValidName", "")]
    public void Command_WithInvalidInputs_FailsValidation(string? name, string connectorType)
    {
        // A validação ocorre no ValidationBehavior antes do handler
        var validator = new CreateIntegrationConnector.Validator();
        var result = validator.Validate(
            new CreateIntegrationConnector.Command(name!, connectorType, null));

        result.IsValid.Should().BeFalse();
    }
}
```

### Executar testes de um módulo específico

```bash
# Apenas o módulo integrations
dotnet test tests/modules/integrations/ --no-build --verbosity minimal

# Com relatório de coverage
dotnet test tests/modules/integrations/ --no-build \
  --collect "XPlat Code Coverage" \
  --results-directory ./coverage/integrations

# Todos os módulos
dotnet test tests/ --no-build --verbosity minimal
```

---

## 4. Testes de Integração — Backend

### Stack adicional

| Ferramenta | Papel |
|---|---|
| **Testcontainers** | Levanta PostgreSQL real em Docker para testes |
| **Respawn** | Reseta o estado do DB entre testes sem recriar schema |

### Exemplo — Teste de repositório com PostgreSQL real

```csharp
using DotNet.Testcontainers.Builders;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Respawn;
using Testcontainers.PostgreSql;
using Xunit;

public sealed class IntegrationConnectorRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("nextraceone_integrations_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    private IntegrationsDbContext _context = null!;
    private IntegrationConnectorRepository _repository = null!;
    private Respawner _respawner = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var tenant = Substitute.For<ICurrentTenant>();
        tenant.Id.Returns(Guid.NewGuid());

        var options = new DbContextOptionsBuilder<IntegrationsDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        _context = new IntegrationsDbContext(options, tenant,
            Substitute.For<ICurrentUser>(), Substitute.For<IDateTimeProvider>());

        await _context.Database.MigrateAsync();

        _respawner = await Respawner.CreateAsync(_postgres.GetConnectionString(),
            new RespawnerOptions { DbAdapter = DbAdapter.Postgres });

        _repository = new IntegrationConnectorRepository(_context);
    }

    public async Task DisposeAsync()
    {
        await _postgres.StopAsync();
        await _context.DisposeAsync();
    }

    [Fact]
    public async Task AddAndQuery_ShouldPersistConnector()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var connector = IntegrationConnector.Create("My GitLab", "GitLab", tenantId, new FakeClock());

        // Act
        await _repository.AddAsync(connector, CancellationToken.None);
        await _context.SaveChangesAsync();

        // Assert
        var found = await _repository.GetByIdAsync(connector.Id, CancellationToken.None);
        found.Should().NotBeNull();
        found!.Name.Should().Be("My GitLab");
        found.ConnectorType.Should().Be("GitLab");
    }

    // Limpar estado entre testes
    private async Task ResetAsync() =>
        await _respawner.ResetAsync(_postgres.GetConnectionString());
}
```

---

## 5. Testes Unitários — Frontend

### Stack

| Ferramenta | Papel |
|---|---|
| **Vitest** | Test runner rápido integrado ao Vite |
| **@testing-library/react** | Renderização e interação com componentes |
| **MSW (Mock Service Worker)** | Intercepta chamadas HTTP reais em testes |
| **@testing-library/user-event** | Simula interações de utilizador (clique, digitar) |

### Como usar MSW para mock de API

O MSW é configurado em `src/frontend/src/mocks/` com handlers por módulo:

```typescript
// src/frontend/src/mocks/handlers/integrations.ts
import { http, HttpResponse } from 'msw';

export const integrationHandlers = [
  http.get('/api/v1/integrations/connectors', () => {
    return HttpResponse.json({
      items: [
        { id: 'abc-123', name: 'GitLab CI', connectorType: 'GitLab', status: 'Active' },
        { id: 'def-456', name: 'Jenkins Prod', connectorType: 'Jenkins', status: 'Inactive' },
      ],
      totalCount: 2,
    });
  }),

  http.post('/api/v1/integrations/connectors', async ({ request }) => {
    const body = await request.json() as { name: string };
    return HttpResponse.json({ id: 'new-uuid-here' }, { status: 201 });
  }),
];
```

### Exemplo de teste de componente com providers

```typescript
// src/frontend/src/features/integrations/__tests__/ConnectorListPage.test.tsx
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { server } from '../../../mocks/server';
import { integrationHandlers } from '../../../mocks/handlers/integrations';
import { ConnectorListPage } from '../pages/ConnectorListPage';

// Wrapper com todos os providers necessários
function renderWithProviders(ui: React.ReactElement) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        {ui}
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe('ConnectorListPage', () => {
  beforeAll(() => server.listen());
  afterEach(() => server.resetHandlers());
  afterAll(() => server.close());

  it('should display connector list from API', async () => {
    server.use(...integrationHandlers);
    renderWithProviders(<ConnectorListPage />);

    // Verificar estado de loading
    expect(screen.getByRole('status')).toBeInTheDocument(); // skeleton/spinner

    // Aguardar dados carregarem
    await waitFor(() => {
      expect(screen.getByText('GitLab CI')).toBeInTheDocument();
      expect(screen.getByText('Jenkins Prod')).toBeInTheDocument();
    });
  });

  it('should show empty state when no connectors exist', async () => {
    server.use(
      http.get('/api/v1/integrations/connectors', () =>
        HttpResponse.json({ items: [], totalCount: 0 }))
    );
    renderWithProviders(<ConnectorListPage />);

    await waitFor(() => {
      expect(screen.getByText(/no connectors/i)).toBeInTheDocument();
    });
  });

  it('should show error state when API fails', async () => {
    server.use(
      http.get('/api/v1/integrations/connectors', () =>
        HttpResponse.error())
    );
    renderWithProviders(<ConnectorListPage />);

    await waitFor(() => {
      expect(screen.getByRole('alert')).toBeInTheDocument();
    });
  });
});
```

### Executar testes frontend

```bash
# Todos os testes
cd src/frontend && npm test

# Com coverage
cd src/frontend && npm test -- --coverage

# Modo watch (desenvolvimento)
cd src/frontend && npm test -- --watch

# Apenas um ficheiro
cd src/frontend && npm test -- src/features/integrations/__tests__/ConnectorListPage.test.tsx
```

---

## 6. Testes E2E — Playwright

### Configuração

Os testes E2E estão em `src/frontend/e2e/` com o padrão Page Object.

### Page Object Pattern

```typescript
// src/frontend/e2e/pages/IntegrationsPage.ts
import { Page, Locator } from '@playwright/test';

export class IntegrationsPage {
  readonly page: Page;
  readonly createButton: Locator;
  readonly connectorList: Locator;

  constructor(page: Page) {
    this.page = page;
    this.createButton = page.getByRole('button', { name: /add connector/i });
    this.connectorList = page.getByTestId('connector-list');
  }

  async goto() {
    await this.page.goto('/integrations/connectors');
  }

  async createConnector(name: string, type: string) {
    await this.createButton.click();
    await this.page.getByLabel(/name/i).fill(name);
    await this.page.getByLabel(/connector type/i).selectOption(type);
    await this.page.getByRole('button', { name: /save/i }).click();
  }

  async waitForConnector(name: string) {
    await this.connectorList.getByText(name).waitFor({ state: 'visible' });
  }
}
```

### Exemplo de teste E2E

```typescript
// src/frontend/e2e/integrations/connector-creation.spec.ts
import { test, expect } from '@playwright/test';
import { IntegrationsPage } from '../pages/IntegrationsPage';
import { LoginPage } from '../pages/LoginPage';

test.describe('Integration Connector Creation', () => {
  test.beforeEach(async ({ page }) => {
    const login = new LoginPage(page);
    await login.loginAs('engineer@nextraceone.test', 'testpassword');
  });

  test('should create a new connector and display it in the list', async ({ page }) => {
    const integrationsPage = new IntegrationsPage(page);
    await integrationsPage.goto();

    await integrationsPage.createConnector('My GitLab CI', 'GitLab');
    await integrationsPage.waitForConnector('My GitLab CI');

    await expect(page.getByText('My GitLab CI')).toBeVisible();
  });
});
```

### Executar testes E2E

```bash
# Todos os testes E2E (requer app rodando)
cd src/frontend && npx playwright test

# Com interface visual
cd src/frontend && npx playwright test --ui

# Apenas um ficheiro
cd src/frontend && npx playwright test e2e/integrations/

# Debug mode
cd src/frontend && npx playwright test --debug
```

---

## 7. Testes de Load — k6

Os testes de load estão em `tests/load/` e utilizam a ferramenta [k6](https://k6.io/).

### Estrutura

```
tests/load/
├── scenarios/
│   ├── catalog-services-list.js   # Lista de serviços sob carga
│   ├── analytics-summary.js       # Resumo de analytics
│   └── ingestion-events.js        # Ingestão de eventos de deploy
└── config/
    └── thresholds.js               # SLOs de referência
```

### Exemplo de cenário k6

```javascript
// tests/load/scenarios/catalog-services-list.js
import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '30s', target: 20 },  // ramp-up
    { duration: '1m', target: 20 },   // sustentado
    { duration: '10s', target: 0 },   // ramp-down
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'],  // 95% das requests < 500ms
    http_req_failed: ['rate<0.01'],    // < 1% de falhas
  },
};

const BASE_URL = __ENV.API_URL || 'http://localhost:8080';
const TOKEN = __ENV.API_TOKEN;

export default function () {
  const res = http.get(`${BASE_URL}/api/v1/catalog/services`, {
    headers: { Authorization: `Bearer ${TOKEN}` },
  });

  check(res, {
    'status is 200': (r) => r.status === 200,
    'response time < 500ms': (r) => r.timings.duration < 500,
  });

  sleep(1);
}
```

### Executar testes de load

```bash
# Requer k6 instalado (https://k6.io/docs/get-started/installation/)
k6 run tests/load/scenarios/catalog-services-list.js \
  -e API_URL=http://localhost:8080 \
  -e API_TOKEN=<jwt_token>
```

---

## 8. Metas de Cobertura

| Camada | Meta | Justificativa |
|---|---|---|
| Backend — Handlers e Domínio | 80%+ | Lógica de negócio crítica |
| Backend — Infrastructure | 40%+ | Testado via integration tests |
| Frontend — Componentes | 30% mínimo → 50% alvo | Componentes com lógica condicional |
| Frontend — Hooks customizados | 60%+ | Lógica de data fetching e estado |
| E2E | Fluxos críticos cobertos | Autenticação, criação de serviço, publicação de contrato |

### Gerar relatório de cobertura — Backend

```bash
# Gerar coverage para todos os módulos
dotnet test tests/ --collect "XPlat Code Coverage" --results-directory ./coverage

# Converter para relatório HTML com ReportGenerator
dotnet tool install -g dotnet-reportgenerator-globaltool  # instalar se necessário
reportgenerator -reports:"coverage/**/*.xml" -targetdir:"coverage/report" -reporttypes:Html

# Abrir relatório
open coverage/report/index.html
```

### Gerar relatório de cobertura — Frontend

```bash
cd src/frontend
npm test -- --coverage --reporter=html

# Abrir relatório
open coverage/index.html
```

---

## 9. Integração com CI

### Workflows GitHub Actions

| Workflow | Testes Executados | Trigger |
|---|---|---|
| `ci.yml` | Unit + Integration (backend) + Unit (frontend) | Push / PR |
| `e2e.yml` | Playwright E2E | PR para `main` |
| `load.yml` | k6 cenários críticos | Release / Manual |

### Paralelismo no CI

Os testes de integração backend são paralelizados por módulo para reduzir o tempo total de CI. Cada runner de CI pode executar um subset de módulos em paralelo:

```yaml
# .github/workflows/ci.yml (exemplo de matrix)
strategy:
  matrix:
    module: [building-blocks, integrations, knowledge, productanalytics, changegovernance]
```

---

## 10. Padrões e Boas Práticas

### Backend

- **Arrange / Act / Assert** — estrutura clara em todos os testes
- **Nomes descritivos**: `Handle_GivenDuplicateName_ReturnsConflictError`
- **Um assert por teste** como regra geral; múltiplos quando relacionados
- **NSubstitute**: usar `Arg.Any<T>()` apenas quando o valor não importa; ser específico quando importa
- **Bogus**: preferir `Faker<T>` tipado para entidades complexas; `new Faker()` para strings simples
- **Isolamento**: cada teste deve ser independente — sem estado partilhado entre testes

### Frontend

- **Queries pelo papel/texto** (`getByRole`, `getByText`) em vez de `getByTestId` quando possível
- **Aguardar assincronicidade** com `waitFor` / `findBy*` — nunca testes síncronos em componentes com fetch
- **MSW** como mock padrão para API — não mockar `axios` ou `fetch` directamente
- **Providers wrapper** reutilizável para evitar repetição em cada teste
- **Testar comportamento**, não implementação — não testar props internas, testar o que o utilizador vê

---

## 11. Referências

| Tópico | Documento / Link |
|---|---|
| Estrutura de módulos | `docs/BACKEND-MODULE-GUIDELINES.md` |
| Arquitetura geral | `docs/ARCHITECTURE-OVERVIEW.md` |
| Frontend | `docs/FRONTEND-ARCHITECTURE.md` |
| MSW | https://mswjs.io/docs/ |
| Testcontainers .NET | https://dotnet.testcontainers.org/ |
| Playwright | https://playwright.dev/docs/intro |
| k6 | https://k6.io/docs/ |

---

*Última atualização: Março 2026.*
