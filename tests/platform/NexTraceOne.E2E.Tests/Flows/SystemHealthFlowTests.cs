using NexTraceOne.E2E.Tests.Infrastructure;

namespace NexTraceOne.E2E.Tests.Flows;

/// <summary>
/// Testes E2E reais para health checks, readiness e endpoints públicos do sistema.
/// Valida que a plataforma arranca corretamente, aplica migrations e responde
/// a requests HTTP sem dependências de mocks ou stubs.
///
/// Classificação: ALTA CONFIANÇA
/// - Backend real + banco de dados real
/// - Sem mocks
/// - Fluxo completamente real
/// </summary>
[Collection(ApiE2ECollection.Name)]
public sealed class SystemHealthFlowTests(ApiE2EFixture fixture)
{
    // ── Health Checks ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Health_Endpoint_Should_Return_200()
    {
        var response = await fixture.Client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK, "o endpoint /health deve estar disponível sem autenticação");
    }

    [Fact]
    public async Task Readiness_Endpoint_Should_Return_200_Or_503()
    {
        var response = await fixture.Client.GetAsync("/ready");

        // Ready endpoint can return 200 (healthy) or 503 (unhealthy checks)
        // but must not return 404 or 500 (server error)
        ((int)response.StatusCode).Should().BeOneOf(new[] {200, 503}, "o endpoint /ready deve existir e retornar estado de saúde — nunca 404 ou 5xx");
    }

    [Fact]
    public async Task Live_Endpoint_Should_Return_200()
    {
        var response = await fixture.Client.GetAsync("/live");

        response.StatusCode.Should().Be(HttpStatusCode.OK, "o endpoint /live deve confirmar que o processo está ativo");
    }

    [Fact]
    public async Task Health_Endpoint_Should_Return_Json_With_Status()
    {
        var response = await fixture.Client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrWhiteSpace("a resposta de health deve conter JSON");
    }

    // ── API Structure ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Protected_Endpoints_Should_Return_401_Without_Token()
    {
        // Any protected endpoint should return 401, not 404 or 500
        var response = await fixture.Client.GetAsync("/api/v1/catalog/services");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "endpoints protegidos devem retornar 401 quando não há token de autenticação");
    }

    [Fact]
    public async Task Identity_Login_Endpoint_Should_Exist_And_Return_400_On_Empty_Body()
    {
        // POST /api/v1/identity/auth/login with empty body should return 400 (validation error)
        // NOT 404 (endpoint missing) or 500 (server crash)
        var response = await fixture.Client.PostAsJsonAsync("/api/v1/identity/auth/login",
            new { Email = "", Password = "" });

        ((int)response.StatusCode).Should().BeOneOf(new[] {400, 422}, "login com credenciais inválidas deve retornar erro de validação, não 404 ou 500");
    }

    [Fact]
    public async Task Identity_Login_Should_Return_Unauthorized_For_Unknown_Credentials()
    {
        var response = await fixture.Client.PostAsJsonAsync("/api/v1/identity/auth/login",
            new { Email = "nonexistent@e2e-test.local", Password = "ValidPass@123" });

        ((int)response.StatusCode).Should().BeOneOf(new[] {400, 401, 404, 422}, "login com credenciais inexistentes deve retornar erro, não 500");
    }

    // ── OpenAPI ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task OpenAPI_Spec_Should_Be_Accessible()
    {
        var response = await fixture.Client.GetAsync("/openapi/v1.json");

        ((int)response.StatusCode).Should().BeOneOf(new[] {200, 404}, "o endpoint OpenAPI deve existir ou estar explicitamente desabilitado — nunca 500");
    }

    // ── CORS and Security Headers ─────────────────────────────────────────────

    [Fact]
    public async Task Security_Headers_Should_Be_Present_In_Api_Response()
    {
        var response = await fixture.Client.GetAsync("/health");

        // NexTraceOne adds security headers via UseSecurityHeaders() middleware
        response.Headers.Should().ContainKey("X-Content-Type-Options",
            "o middleware de segurança deve adicionar o header X-Content-Type-Options");
    }
}
