namespace NexTraceOne.IngestionApi.Tests.Security;

/// <summary>
/// Testes unitários para WebhookSignatureValidator.
/// Cobre: extracção de assinatura, verificação HMAC-SHA256, timing-safe comparison,
/// retrocompatibilidade entre formatos GitHub e SonarQube e casos de borda.
/// </summary>
public sealed class WebhookSignatureValidatorTests
{
    private const string Secret = "super-secret-webhook-key-for-tests";

    // ── helpers ──────────────────────────────────────────────────────────────────

    private static byte[] Body(string text) => Encoding.UTF8.GetBytes(text);

    private static string ComputeHmac(byte[] body, string secret)
    {
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), body);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static IHeaderDictionary GitHubHeader(string hex)
    {
        var h = new HeaderDictionary();
        h[WebhookSignatureValidator.GitHubHeader] = $"sha256={hex}";
        return h;
    }

    private static IHeaderDictionary SonarHeader(string hex)
    {
        var h = new HeaderDictionary();
        h[WebhookSignatureValidator.SonarHeader] = hex;
        return h;
    }

    // ── TryExtractSignature ───────────────────────────────────────────────────────

    [Fact]
    public void TryExtractSignature_Should_ReturnFalse_When_NoHeader()
    {
        var headers = new HeaderDictionary();

        var result = WebhookSignatureValidator.TryExtractSignature(headers, out var hex);

        result.Should().BeFalse();
        hex.Should().BeEmpty();
    }

    [Fact]
    public void TryExtractSignature_Should_ExtractHex_FromGitHubHeader()
    {
        var headers = GitHubHeader("abc123def456");

        var result = WebhookSignatureValidator.TryExtractSignature(headers, out var hex);

        result.Should().BeTrue();
        hex.Should().Be("abc123def456");
    }

    [Fact]
    public void TryExtractSignature_Should_ExtractHex_FromSonarHeader()
    {
        var headers = SonarHeader("deadbeef1234");

        var result = WebhookSignatureValidator.TryExtractSignature(headers, out var hex);

        result.Should().BeTrue();
        hex.Should().Be("deadbeef1234");
    }

    [Fact]
    public void TryExtractSignature_Should_ReturnFalse_When_GitHubHeader_MissingPrefix()
    {
        var h = new HeaderDictionary();
        h[WebhookSignatureValidator.GitHubHeader] = "abc123"; // sem "sha256="

        var result = WebhookSignatureValidator.TryExtractSignature(h, out var hex);

        result.Should().BeFalse();
    }

    [Fact]
    public void TryExtractSignature_Should_BeCaseInsensitive_ForSha256Prefix()
    {
        var h = new HeaderDictionary();
        h[WebhookSignatureValidator.GitHubHeader] = "SHA256=abc123def456";

        var result = WebhookSignatureValidator.TryExtractSignature(h, out var hex);

        result.Should().BeTrue();
        hex.Should().Be("abc123def456");
    }

    // ── VerifyHmac ───────────────────────────────────────────────────────────────

    [Fact]
    public void VerifyHmac_Should_ReturnTrue_For_CorrectSignature()
    {
        var body = Body("""{"project":{"key":"my-service"}}""");
        var correctHex = ComputeHmac(body, Secret);

        WebhookSignatureValidator.VerifyHmac(body, Secret, correctHex).Should().BeTrue();
    }

    [Fact]
    public void VerifyHmac_Should_ReturnFalse_For_TamperedBody()
    {
        var originalBody = Body("""{"project":{"key":"my-service"}}""");
        var tamperedBody = Body("""{"project":{"key":"evil-service"}}""");
        var signatureOfOriginal = ComputeHmac(originalBody, Secret);

        WebhookSignatureValidator.VerifyHmac(tamperedBody, Secret, signatureOfOriginal).Should().BeFalse();
    }

    [Fact]
    public void VerifyHmac_Should_ReturnFalse_For_WrongSecret()
    {
        var body = Body("""{"event":"push"}""");
        var hexWithDifferentSecret = ComputeHmac(body, "different-secret");

        WebhookSignatureValidator.VerifyHmac(body, Secret, hexWithDifferentSecret).Should().BeFalse();
    }

    [Fact]
    public void VerifyHmac_Should_ReturnFalse_For_ShortHex()
    {
        var body = Body("""{"event":"push"}""");

        WebhookSignatureValidator.VerifyHmac(body, Secret, "tooshort").Should().BeFalse();
    }

    [Fact]
    public void VerifyHmac_Should_BeCaseInsensitive_ForReceivedHex()
    {
        var body = Body("""{"event":"push"}""");
        var upperHex = ComputeHmac(body, Secret).ToUpperInvariant();

        WebhookSignatureValidator.VerifyHmac(body, Secret, upperHex).Should().BeTrue();
    }

    [Fact]
    public void VerifyHmac_Should_HandleEmptyBody()
    {
        var emptyBody = Array.Empty<byte>();
        var correctHex = ComputeHmac(emptyBody, Secret);

        WebhookSignatureValidator.VerifyHmac(emptyBody, Secret, correctHex).Should().BeTrue();
    }

    // ── Validate (API pública completa) ──────────────────────────────────────────

    [Fact]
    public void Validate_Should_ReturnHasSignatureFalse_When_NoHeader()
    {
        var body = Body("""{"event":"push"}""");
        var headers = new HeaderDictionary();

        var (hasSignature, isValid) = WebhookSignatureValidator.Validate(headers, body, Secret);

        hasSignature.Should().BeFalse();
        isValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Should_ReturnValid_When_GitHubSignatureCorrect()
    {
        var body = Body("""{"project":{"key":"svc"}}""");
        var headers = GitHubHeader(ComputeHmac(body, Secret));

        var (hasSignature, isValid) = WebhookSignatureValidator.Validate(headers, body, Secret);

        hasSignature.Should().BeTrue();
        isValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Should_ReturnInvalid_When_GitHubSignatureWrong()
    {
        var body = Body("""{"project":{"key":"svc"}}""");
        var headers = GitHubHeader("0000000000000000000000000000000000000000000000000000000000000000");

        var (hasSignature, isValid) = WebhookSignatureValidator.Validate(headers, body, Secret);

        hasSignature.Should().BeTrue();
        isValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Should_ReturnValid_When_SonarSignatureCorrect()
    {
        var body = Body("""{"qualityGate":{"status":"OK"}}""");
        var headers = SonarHeader(ComputeHmac(body, Secret));

        var (hasSignature, isValid) = WebhookSignatureValidator.Validate(headers, body, Secret);

        hasSignature.Should().BeTrue();
        isValid.Should().BeTrue();
    }

    // ── WebhookSignatureOptions ───────────────────────────────────────────────────

    [Fact]
    public void WebhookSignatureOptions_TryGetSecret_Returns_True_When_Configured()
    {
        var opts = new WebhookSignatureOptions();
        opts.Secrets["SonarQube"] = "my-secret";

        opts.TryGetSecret("SonarQube", out var secret).Should().BeTrue();
        secret.Should().Be("my-secret");
    }

    [Fact]
    public void WebhookSignatureOptions_TryGetSecret_Returns_False_When_Missing()
    {
        var opts = new WebhookSignatureOptions();

        opts.TryGetSecret("SonarQube", out var secret).Should().BeFalse();
        secret.Should().BeNull();
    }

    [Fact]
    public void WebhookSignatureOptions_TryGetSecret_Returns_False_When_Empty()
    {
        var opts = new WebhookSignatureOptions();
        opts.Secrets["Commits"] = "";

        opts.TryGetSecret("Commits", out _).Should().BeFalse();
    }

    [Fact]
    public void WebhookSignatureOptions_TryGetSecret_IsCaseInsensitive()
    {
        var opts = new WebhookSignatureOptions();
        opts.Secrets["sonarqube"] = "my-secret";

        opts.TryGetSecret("SonarQube", out var secret).Should().BeTrue();
        secret.Should().Be("my-secret");
    }
}
