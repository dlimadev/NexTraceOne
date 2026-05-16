using Microsoft.Extensions.Logging.Abstractions;
using NexTraceOne.AIKnowledge.Infrastructure.Runtime.Services;

namespace NexTraceOne.AIKnowledge.Tests.Runtime.Services;

public sealed class PiiRedactionServiceTests
{
    private readonly PiiRedactionService _sut = new(NullLogger<PiiRedactionService>.Instance);

    [Theory]
    [InlineData("Server=myServer;Database=myDB;User Id=admin;Password=secret123", "[REDACTED-CONNECTION-STRING]")]
    [InlineData("Host=localhost;Port=5432;Password=pwd", "[REDACTED-CONNECTION-STRING]")]
    public void Redact_ShouldRemoveConnectionStrings(string input, string expectedPlaceholder)
    {
        var result = _sut.Redact(input);
        result.Should().Contain(expectedPlaceholder);
        result.Should().NotContain("secret123");
        result.Should().NotContain("pwd");
    }

    [Theory]
    [InlineData("Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.test.signature")]
    [InlineData("Bearer abc.def.ghi")]
    public void Redact_ShouldRemoveBearerTokens(string input)
    {
        var result = _sut.Redact(input);
        result.Should().Contain("[REDACTED-BEARER-TOKEN]");
        result.Should().NotContain("eyJhbGci");
    }

    [Fact]
    public void Redact_ShouldRemovePrivateKeys()
    {
        var input = "-----BEGIN PRIVATE KEY-----\nMIIEvQIBADANBgkqhkiG9w0BAQEFAASCBKcwggSjAgEAAoIBAQC...\n-----END PRIVATE KEY-----";
        var result = _sut.Redact(input);
        result.Should().Contain("[REDACTED-PRIVATE-KEY]");
        result.Should().NotContain("MIIEvQIBADANBgkqhkiG9w0BAQEFAASCBKcwggSjAgEAAoIBAQC");
    }

    [Theory]
    [InlineData("Contact us at support@nextraceone.com")]
    [InlineData("Email: user.name@example.co.uk")]
    public void Redact_ShouldRemoveEmails(string input)
    {
        var result = _sut.Redact(input);
        result.Should().Contain("[REDACTED-EMAIL]");
        result.Should().NotContain("@");
    }

    [Theory]
    [InlineData("Server IP is 192.168.1.1")]
    [InlineData("IPv6: 2001:0db8:85a3:0000:0000:8a2e:0370:7334")]
    public void Redact_ShouldRemoveIpAddresses(string input)
    {
        var result = _sut.Redact(input);
        result.Should().Contain("[REDACTED-IP]");
    }

    [Theory]
    [InlineData("api-key: sk-abc123456789abcdef")]
    [InlineData("secret=supersecretkey123456")]
    public void Redact_ShouldRemoveApiKeys(string input)
    {
        var result = _sut.Redact(input);
        result.Should().Contain("[REDACTED-API-KEY]");
    }

    [Fact]
    public void Redact_ShouldPreserveSafeText()
    {
        var input = "Service Payment API is running normally. Version 2.5.0. Team: Finance.";
        var result = _sut.Redact(input);
        result.Should().Be(input);
    }

    [Fact]
    public void ContainsSensitiveData_ShouldReturnTrue_ForSensitiveInput()
    {
        _sut.ContainsSensitiveData("Server=localhost;Password=secret").Should().BeTrue();
        _sut.ContainsSensitiveData("Bearer eyJhbGciOiJIUzI1NiIs.x.y").Should().BeTrue();
    }

    [Fact]
    public void ContainsSensitiveData_ShouldReturnFalse_ForSafeInput()
    {
        _sut.ContainsSensitiveData("Service is healthy").Should().BeFalse();
    }

    [Fact]
    public void Redact_ShouldHandleNullAndEmpty()
    {
        _sut.Redact(null!).Should().BeNull();
        _sut.Redact("").Should().BeEmpty();
    }
}
