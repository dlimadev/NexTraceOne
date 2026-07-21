/**
 * Handlers MSW das abas do detalhe do serviço e subsistemas ligados.
 *
 * Cobre os endpoints que o catch-all (que devolve `[]`) não serve com a forma
 * correta — as abas Licenças & SBOM, Dependências e Segurança esperam OBJETOS,
 * além do subsistema de Templates + AI-Scaffold e do pipeline de contrato.
 *
 * Registar ANTES do catch-all. Rotas específicas antes das paramétricas.
 */
import { http, HttpResponse } from 'msw';

const API = '/api/v1';

const nowIso = () => new Date().toISOString();

export const serviceTabsHandlers = [
  // ── Aba Licenças & SBOM ─────────────────────────────────────────────
  http.get(`${API}/catalog/dependencies/:id/licenses`, ({ params }) =>
    HttpResponse.json({
      serviceId: String(params.id),
      checkedAt: nowIso(),
      conflicts: [
        {
          packageName: 'Newtonsoft.Json',
          licenseId: 'MIT',
          conflictSeverity: 'Low',
          description: 'Licença permissiva — sem conflito com a política do tenant.',
        },
        {
          packageName: 'SomeGplLib',
          licenseId: 'GPL-3.0',
          conflictSeverity: 'High',
          description: 'Copyleft forte incompatível com distribuição proprietária.',
        },
      ],
    }),
  ),
  http.get(`${API}/catalog/dependencies/:id/upgrades`, ({ params }) =>
    HttpResponse.json({
      serviceId: String(params.id),
      suggestions: [
        {
          packageName: 'Npgsql',
          currentVersion: '8.0.1',
          suggestedVersion: '8.0.3',
          severity: 'Medium',
          reason: 'Correções de segurança e estabilidade.',
        },
      ],
    }),
  ),
  http.post(`${API}/catalog/dependencies/:id/sbom`, ({ params }) =>
    HttpResponse.json({
      profileId: `sbom-${String(params.id)}`,
      serviceId: String(params.id),
      generatedAt: nowIso(),
      format: 'CycloneDX',
    }),
  ),

  // ── Aba Dependências ────────────────────────────────────────────────
  http.post(`${API}/catalog/dependencies/scan`, async ({ request }) => {
    const body = (await request.json().catch(() => ({}))) as { serviceId?: string };
    return HttpResponse.json({
      profileId: `profile-${body.serviceId ?? 'svc'}`,
      healthScore: 82,
      totalDependencies: 47,
      directDependencies: 19,
      vulnerabilityCount: 3,
    });
  }),
  http.get(`${API}/catalog/dependencies/:id/health`, ({ params }) =>
    HttpResponse.json({
      serviceId: String(params.id),
      healthScore: 82,
      lastScanAt: nowIso(),
      totalDeps: 47,
      directDeps: 19,
      transitiveDeps: 28,
      criticalVulnCount: 0,
      highVulnCount: 1,
      mediumVulnCount: 2,
      lowVulnCount: 4,
      outdatedCount: 6,
      deprecatedCount: 1,
      licenseRiskCounts: { High: 1, Low: 12 },
    }),
  ),

  // ── Aba Segurança (SAST) ────────────────────────────────────────────
  http.post(`${API}/governance/security/scan/code`, async ({ request }) => {
    const body = (await request.json().catch(() => ({}))) as { targetId?: string };
    return HttpResponse.json({
      scanId: `scan-${body.targetId ?? 'svc'}`,
      targetType: 'Service',
      scannedAt: nowIso(),
      scanProvider: 'internal',
      overallRisk: 'Medium',
      passedGate: false,
      summary: {
        totalFindings: 2,
        criticalCount: 0,
        highCount: 1,
        mediumCount: 1,
        lowCount: 0,
        infoCount: 0,
        topCategories: ['Injection', 'Secrets'],
      },
      findings: [
        {
          findingId: 'f-1',
          ruleId: 'SEC-SQLI-001',
          category: 'Injection',
          severity: 'High',
          filePath: 'src/PaymentsController.cs',
          lineNumber: 42,
          description: 'Possível SQL injection em query construída por concatenação.',
          remediation: 'Usar parâmetros/consultas preparadas.',
          cweId: 'CWE-89',
          owaspCategory: 'A03:2021',
          isAiGenerated: false,
          status: 'Open',
        },
        {
          findingId: 'f-2',
          ruleId: 'SEC-SECRET-004',
          category: 'Secrets',
          severity: 'Medium',
          filePath: 'src/appsettings.json',
          description: 'Segredo aparente em ficheiro de configuração.',
          remediation: 'Mover para secret store / variável de ambiente.',
          isAiGenerated: false,
          status: 'Open',
        },
      ],
    });
  }),

  // ── Templates + AI-Scaffold ─────────────────────────────────────────
  http.get(`${API}/catalog/templates`, () =>
    HttpResponse.json([
      {
        templateId: 'tpl-rest-dotnet',
        slug: 'rest-api-dotnet',
        displayName: 'REST API (.NET)',
        description: 'Serviço REST em .NET com Clean Architecture.',
        version: '1.2.0',
        serviceType: 'RestApi',
        language: 'DotNet',
        defaultDomain: 'Payments',
        defaultTeam: 'Platform',
        tags: ['ddd', 'clean-architecture'],
        isActive: true,
        usageCount: 12,
        hasBaseContract: true,
        hasScaffoldingManifest: true,
        createdAt: nowIso(),
      },
      {
        templateId: 'tpl-worker-node',
        slug: 'worker-node',
        displayName: 'Background Worker (Node)',
        description: 'Worker assíncrono em Node.js.',
        version: '0.9.0',
        serviceType: 'BackgroundWorker',
        language: 'NodeJs',
        defaultDomain: 'Platform',
        defaultTeam: 'Platform',
        tags: ['queue'],
        isActive: true,
        usageCount: 4,
        hasBaseContract: false,
        hasScaffoldingManifest: true,
        createdAt: nowIso(),
      },
    ]),
  ),
  http.get(`${API}/catalog/templates/slug/:slug`, ({ params }) =>
    HttpResponse.json(buildTemplateDetail(String(params.slug))),
  ),
  http.get(`${API}/catalog/templates/:id`, ({ params }) =>
    HttpResponse.json(buildTemplateDetail(String(params.id))),
  ),
  http.post(`${API}/catalog/templates/:id/scaffold`, async ({ request, params }) => {
    const body = (await request.json().catch(() => ({}))) as { serviceName?: string };
    return HttpResponse.json(buildScaffoldResult(String(params.id), body.serviceName ?? 'new-service'));
  }),
  http.post(`${API}/aiorchestration/generate/scaffold`, async ({ request }) => {
    const body = (await request.json().catch(() => ({}))) as { serviceName?: string };
    return HttpResponse.json({
      scaffoldId: 'ai-scaffold-1',
      serviceName: body.serviceName ?? 'new-service',
      templateId: 'tpl-rest-dotnet',
      templateSlug: 'rest-api-dotnet',
      language: 'DotNet',
      serviceType: 'RestApi',
      domain: 'Payments',
      teamName: 'Platform',
      files: [
        { path: 'README.md', content: '# Serviço gerado por AI' },
        { path: 'src/Program.cs', content: '// entrypoint' },
      ],
      isFallback: false,
      generatedAt: nowIso(),
    });
  }),

  // ── Pipeline de contrato → código ───────────────────────────────────
  http.post(`${API}/catalog/contracts/pipeline/orchestrate`, async ({ request }) => {
    const body = (await request.json().catch(() => ({}))) as {
      serviceName?: string;
      contractVersionId?: string;
      targetLanguage?: string;
    };
    return HttpResponse.json({
      artifacts: [
        {
          artifactType: 'ClientSdk',
          files: [
            { fileName: 'Client.cs', content: '// client sdk', language: 'csharp', description: 'SDK cliente gerado' },
          ],
        },
      ],
      totalArtifacts: 1,
      durationMs: 1234,
      contractVersionId: body.contractVersionId ?? 'cv-1',
      serviceName: body.serviceName ?? 'new-service',
      targetLanguage: body.targetLanguage ?? 'dotnet',
    });
  }),
];

function buildTemplateDetail(idOrSlug: string) {
  return {
    templateId: idOrSlug,
    slug: idOrSlug,
    displayName: 'REST API (.NET)',
    description: 'Serviço REST em .NET com Clean Architecture.',
    version: '1.2.0',
    serviceType: 'RestApi',
    language: 'DotNet',
    defaultDomain: 'Payments',
    defaultTeam: 'Platform',
    tags: ['ddd', 'clean-architecture'],
    isActive: true,
    usageCount: 12,
    hasBaseContract: true,
    hasScaffoldingManifest: true,
    createdAt: nowIso(),
    governancePolicyIds: [],
    baseContractSpec: 'openapi: 3.0.0',
    repositoryTemplateUrl: 'https://github.com/org/template',
    repositoryTemplateBranch: 'main',
  };
}

function buildScaffoldResult(templateId: string, serviceName: string) {
  return {
    scaffoldingId: `scaffold-${templateId}`,
    serviceName,
    templateId,
    templateSlug: templateId,
    templateVersion: '1.2.0',
    serviceType: 'RestApi',
    language: 'DotNet',
    domain: 'Payments',
    teamName: 'Platform',
    governancePolicyIds: [],
    baseContractSpec: 'openapi: 3.0.0',
    files: [
      { path: 'README.md', content: '# ' + serviceName },
      { path: 'src/Program.cs', content: '// entrypoint' },
    ],
    variables: { ServiceName: serviceName },
  };
}
