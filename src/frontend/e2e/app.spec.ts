import { test, expect, type Page } from '@playwright/test';

/**
 * Utilitário para simular uma sessão autenticada nos testes E2E.
 *
 * Segurança: utiliza sessionStorage com as chaves reais do tokenStorage (nxt_at, nxt_tid, nxt_uid),
 * garantindo que os testes reflitam o comportamento real da aplicação.
 * O refresh token NÃO é persistido no storage (apenas em memória), conforme a estratégia de segurança.
 */
async function mockAuthSession(page: Page, roles: string[] = ['Admin']): Promise<void> {
  await page.addInitScript(() => {
    sessionStorage.setItem('nxt_at', 'mock-e2e-token');
    sessionStorage.setItem('nxt_tid', 'tenant-e2e-001');
    sessionStorage.setItem('nxt_uid', 'user-e2e-001');
  });

  // Intercepta a chamada de perfil para retornar roles configuráveis
  await page.route('**/api/v1/identity/users/user-e2e-001', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        id: 'user-e2e-001',
        email: 'admin@acme.com',
        fullName: 'Admin User',
        roles,
        tenantId: 'tenant-e2e-001',
      }),
    })
  );
}

test.describe('Login Page', () => {
  test('exibe o formulário de login na rota /login', async ({ page }) => {
    await page.goto('/login');
    await expect(page.getByText('NexTraceOne')).toBeVisible();
    await expect(page.getByText('Sovereign Change Intelligence Platform')).toBeVisible();
    await expect(page.getByLabel('Tenant ID')).toBeVisible();
    await expect(page.getByLabel('Email')).toBeVisible();
    await expect(page.getByLabel('Password')).toBeVisible();
    await expect(page.getByRole('button', { name: /sign in/i })).toBeVisible();
  });

  test('redireciona para / quando já autenticado', async ({ page }) => {
    await mockAuthSession(page);
    await page.goto('/login');
    // Com autenticação simulada, o AppLayout deve redirecionar para dashboard
    await expect(page).toHaveURL('/');
  });

  test('exibe mensagem de erro com credenciais inválidas', async ({ page }) => {
    await page.goto('/login');

    await page.getByLabel('Tenant ID').fill('tenant-invalid');
    await page.getByLabel('Email').fill('invalid@test.com');
    await page.getByLabel('Password').fill('wrongpass');
    await page.getByRole('button', { name: /sign in/i }).click();

    await expect(
      page.getByText(/invalid credentials or tenant/i)
    ).toBeVisible({ timeout: 5_000 });
  });

  test('desabilita o botão de submit durante o envio', async ({ page }) => {
    await page.goto('/login');

    // Simula uma resposta lenta da API interceptando a requisição
    await page.route('**/api/v1/identity/auth/login', async (route) => {
      await new Promise((r) => setTimeout(r, 500));
      await route.fulfill({ status: 401, body: JSON.stringify({ error: 'Unauthorized' }) });
    });

    await page.getByLabel('Tenant ID').fill('tenant-001');
    await page.getByLabel('Email').fill('user@test.com');
    await page.getByLabel('Password').fill('pass');

    const submitBtn = page.getByRole('button', { name: /sign in/i });
    await submitBtn.click();

    // Durante o envio, o botão deve estar desabilitado
    await expect(submitBtn).toBeDisabled();
  });

  test('exibe o rodapé indicando self-hosted', async ({ page }) => {
    await page.goto('/login');
    await expect(page.getByText(/self-hosted/i)).toBeVisible();
  });
});

test.describe('Navigation (autenticado)', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
  });

  test('redireciona para / quando acessa uma rota desconhecida', async ({ page }) => {
    await page.goto('/rota-inexistente');
    await expect(page).toHaveURL('/');
  });

  test('exibe a sidebar com todos os itens de navegação', async ({ page }) => {
    await page.goto('/');
    await expect(page.getByRole('link', { name: /dashboard/i })).toBeVisible();
    await expect(page.getByRole('link', { name: /releases/i })).toBeVisible();
    await expect(page.getByRole('link', { name: /engineering graph/i })).toBeVisible();
    await expect(page.getByRole('link', { name: /contracts/i })).toBeVisible();
    await expect(page.getByRole('link', { name: /workflow/i })).toBeVisible();
    await expect(page.getByRole('link', { name: /promotion/i })).toBeVisible();
    await expect(page.getByRole('link', { name: /users/i })).toBeVisible();
    await expect(page.getByRole('link', { name: /audit/i })).toBeVisible();
  });

  test('navega para a página de Releases', async ({ page }) => {
    await page.goto('/');
    await page.getByRole('link', { name: /releases/i }).click();
    await expect(page).toHaveURL('/releases');
    await expect(page.getByRole('heading', { name: /releases/i })).toBeVisible();
  });

  test('navega para a página de Contracts', async ({ page }) => {
    await page.goto('/');
    await page.getByRole('link', { name: /contracts/i }).click();
    await expect(page).toHaveURL('/contracts');
    await expect(page.getByRole('heading', { name: /contracts/i })).toBeVisible();
  });

  test('navega para a página de Engineering Graph', async ({ page }) => {
    await page.goto('/');
    await page.getByRole('link', { name: /engineering graph/i }).click();
    await expect(page).toHaveURL('/graph');
    await expect(page.getByRole('heading', { name: /engineering graph/i })).toBeVisible();
  });

  test('navega para a página de Workflow', async ({ page }) => {
    await page.goto('/');
    await page.getByRole('link', { name: /workflow/i }).click();
    await expect(page).toHaveURL('/workflow');
    await expect(page.getByRole('heading', { name: /workflow/i })).toBeVisible();
  });

  test('navega para a página de Promotion', async ({ page }) => {
    await page.goto('/');
    await page.getByRole('link', { name: /promotion/i }).click();
    await expect(page).toHaveURL('/promotion');
    await expect(page.getByRole('heading', { name: /promotion/i })).toBeVisible();
  });

  test('navega para a página de Users', async ({ page }) => {
    await page.goto('/');
    await page.getByRole('link', { name: /users/i }).click();
    await expect(page).toHaveURL('/users');
    await expect(page.getByRole('heading', { name: /users/i })).toBeVisible();
  });

  test('navega para a página de Audit', async ({ page }) => {
    await page.goto('/');
    await page.getByRole('link', { name: /audit/i }).click();
    await expect(page).toHaveURL('/audit');
    await expect(page.getByRole('heading', { name: /audit/i })).toBeVisible();
  });

  test('faz logout ao clicar no botão de sair', async ({ page }) => {
    await page.goto('/');
    await page.getByTitle('Logout').click();
    await expect(page).toHaveURL('/login');
    expect(await page.evaluate(() => sessionStorage.getItem('nxt_at'))).toBeNull();
  });
});

test.describe('Dashboard (autenticado)', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
    // Intercepta chamadas à API do engineering graph para não falhar sem backend
    await page.route('**/api/v1/catalog/graph', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          services: [
            { id: 's1', name: 'payments-service', team: 'Payments', createdAt: '2024-01-01' },
            { id: 's2', name: 'auth-service', team: 'Identity', createdAt: '2024-01-01' },
          ],
          apis: [
            { id: 'a1', name: 'Payments API', baseUrl: '/api/payments', ownerServiceId: 's1', createdAt: '2024-01-01' },
          ],
          relationships: [
            { apiAssetId: 'a1', consumerServiceId: 's2', trustLevel: 'High' },
          ],
        }),
      })
    );
  });

  test('exibe o título do dashboard', async ({ page }) => {
    await page.goto('/');
    await expect(page.getByRole('heading', { name: /dashboard/i })).toBeVisible();
  });

  test('exibe os stat cards', async ({ page }) => {
    await page.goto('/');
    await expect(page.getByText('Active Services')).toBeVisible();
    await expect(page.getByText('Registered APIs')).toBeVisible();
    await expect(page.getByText('Consumer Relations')).toBeVisible();
  });

  test('exibe serviços do grafo', async ({ page }) => {
    await page.goto('/');
    await expect(page.getByText('payments-service')).toBeVisible({ timeout: 5_000 });
    await expect(page.getByText('auth-service')).toBeVisible();
  });
});

test.describe('Releases Page (autenticado)', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
  });

  test('exibe o botão de "Notify Deployment"', async ({ page }) => {
    await page.goto('/releases');
    await expect(page.getByRole('button', { name: /notify deployment/i })).toBeVisible();
  });

  test('exibe o formulário ao clicar em "Notify Deployment"', async ({ page }) => {
    await page.goto('/releases');
    await page.getByRole('button', { name: /notify deployment/i }).click();
    await expect(page.getByText('Notify New Deployment')).toBeVisible();
    await expect(page.getByPlaceholder(/uuid of the api asset/i)).toBeVisible();
  });

  test('exibe instrução para inserir API Asset ID', async ({ page }) => {
    await page.goto('/releases');
    await expect(
      page.getByText(/enter an api asset id above/i)
    ).toBeVisible();
  });
});

test.describe('Controle de Acesso (RBAC)', () => {
  test('Developer não vê link de Users na sidebar', async ({ page }) => {
    await mockAuthSession(page, ['Developer']);
    await page.goto('/');
    // Aguarda perfil ser carregado (sidebar renderizar sem Users)
    await expect(page.getByRole('link', { name: /releases/i })).toBeVisible({ timeout: 5_000 });
    await expect(page.getByRole('link', { name: /users/i })).not.toBeVisible();
  });

  test('Admin vê o link de Users na sidebar', async ({ page }) => {
    await mockAuthSession(page, ['Admin']);
    await page.goto('/');
    await expect(page.getByRole('link', { name: /users/i })).toBeVisible({ timeout: 5_000 });
  });

  test('Manager vê o link de Users na sidebar', async ({ page }) => {
    await mockAuthSession(page, ['Manager']);
    await page.goto('/');
    await expect(page.getByRole('link', { name: /users/i })).toBeVisible({ timeout: 5_000 });
  });

  test('Developer é redirecionado ao tentar acessar /users diretamente', async ({ page }) => {
    await mockAuthSession(page, ['Developer']);
    await page.goto('/users');
    // Deve redirecionar para /unauthorized
    await expect(page).toHaveURL('/unauthorized', { timeout: 5_000 });
    await expect(page.getByText(/access denied/i)).toBeVisible();
  });

  test('Viewer é redirecionado ao tentar acessar /users diretamente', async ({ page }) => {
    await mockAuthSession(page, ['Viewer']);
    await page.goto('/users');
    await expect(page).toHaveURL('/unauthorized', { timeout: 5_000 });
  });

  test('Auditor pode acessar /audit', async ({ page }) => {
    await mockAuthSession(page, ['Auditor']);
    await page.goto('/audit');
    await expect(page.getByRole('heading', { name: /audit log/i })).toBeVisible({ timeout: 5_000 });
  });

  test('página /unauthorized exibe mensagem e botão de retorno', async ({ page }) => {
    await mockAuthSession(page, ['Developer']);
    await page.goto('/users');
    await expect(page.getByText(/access denied/i)).toBeVisible({ timeout: 5_000 });
    await expect(page.getByRole('button', { name: /go to dashboard/i })).toBeVisible();
  });

  test('botão Go to Dashboard redireciona para /', async ({ page }) => {
    await mockAuthSession(page, ['Developer']);
    await page.goto('/users');
    await page.getByRole('button', { name: /go to dashboard/i }).click();
    await expect(page).toHaveURL('/');
  });
});
