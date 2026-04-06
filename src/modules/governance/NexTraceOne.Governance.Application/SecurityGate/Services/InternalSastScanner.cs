using System.Text.RegularExpressions;
using NexTraceOne.Governance.Domain.SecurityGate.Entities;
using NexTraceOne.Governance.Domain.SecurityGate.Enums;

namespace NexTraceOne.Governance.Application.SecurityGate.Services;

/// <summary>
/// Scanner SAST interno baseado em regras regex.
/// Analisa código fonte e detecta vulnerabilidades comuns sem dependências externas.
/// Suporta C# (.cs), TypeScript/JavaScript (.ts, .js) e ficheiros de configuração.
/// </summary>
public static class InternalSastScanner
{
    /// <summary>Executa análise SAST num ficheiro e retorna a lista de achados.</summary>
    public static IReadOnlyList<SecurityFinding> Scan(Guid scanResultId, string filePath, string content)
    {
        var findings = new List<SecurityFinding>();
        var lines = content.Split('\n');

        findings.AddRange(DetectHardcodedSecrets(scanResultId, filePath, lines));
        findings.AddRange(DetectInsecureCrypto(scanResultId, filePath, lines));
        findings.AddRange(DetectCorsMisconfiguration(scanResultId, filePath, lines));
        findings.AddRange(DetectInsecureDeserialization(scanResultId, filePath, lines));
        findings.AddRange(DetectSqlInjection(scanResultId, filePath, lines));
        findings.AddRange(DetectXss(scanResultId, filePath, lines));
        findings.AddRange(DetectLoggingSensitiveData(scanResultId, filePath, lines));
        findings.AddRange(DetectMissingAuth(scanResultId, filePath, lines));
        findings.AddRange(DetectPathTraversal(scanResultId, filePath, lines));
        findings.AddRange(DetectMissingInputValidation(scanResultId, filePath, lines));

        return findings;
    }

    // SAST-001: SQL Injection
    private static IEnumerable<SecurityFinding> DetectSqlInjection(
        Guid scanId, string filePath, string[] lines)
    {
        // Detects string concatenation with SQL keywords
        var pattern = new Regex(
            @"(?i)(""SELECT\s|""INSERT\s|""UPDATE\s|""DELETE\s|""WHERE\s).*\+|\.ExecuteSqlRaw\(\s*\$",
            RegexOptions.Compiled);
        return ScanLines(scanId, filePath, lines, pattern,
            "SAST-001", SecurityCategory.Injection, FindingSeverity.High,
            "Possible SQL injection: string concatenation detected in SQL context.",
            "Use parameterized queries or an ORM to prevent SQL injection. Never concatenate user input into SQL strings.",
            "CWE-89", "A03:2021");
    }

    // SAST-002: Hardcoded Secrets
    private static IEnumerable<SecurityFinding> DetectHardcodedSecrets(
        Guid scanId, string filePath, string[] lines)
    {
        var pattern = new Regex(
            @"(?i)(password|passwd|pwd|apikey|api_key|secret|connectionstring|conn_string)\s*=\s*""[^""]{4,}""",
            RegexOptions.Compiled);
        return ScanLines(scanId, filePath, lines, pattern,
            "SAST-002", SecurityCategory.HardcodedSecrets, FindingSeverity.Critical,
            "Hardcoded credential or secret detected in source code.",
            "Move secrets to environment variables, secret managers (Azure Key Vault, AWS Secrets Manager) or configuration providers. Never hardcode secrets in source code.",
            "CWE-798", "A07:2021");
    }

    // SAST-003: Insecure Crypto
    private static IEnumerable<SecurityFinding> DetectInsecureCrypto(
        Guid scanId, string filePath, string[] lines)
    {
        var pattern = new Regex(
            @"(?i)MD5\.Create\(\)|SHA1\.Create\(\)|new\s+DESCryptoServiceProvider|new\s+TripleDESCryptoServiceProvider|MD5\.HashData\(|SHA1\.HashData\(",
            RegexOptions.Compiled);
        return ScanLines(scanId, filePath, lines, pattern,
            "SAST-003", SecurityCategory.InsecureCrypto, FindingSeverity.High,
            "Weak cryptographic algorithm detected (MD5/SHA1/DES/3DES).",
            "Use SHA-256 or stronger for hashing. Use AES-256-GCM for encryption. Never use MD5 or SHA1 for password hashing — use BCrypt, Argon2, or PBKDF2.",
            "CWE-327", "A02:2021");
    }

    // SAST-004: Missing Auth on endpoints
    private static IEnumerable<SecurityFinding> DetectMissingAuth(
        Guid scanId, string filePath, string[] lines)
    {
        var hasMapEndpoint = lines.Any(l =>
            Regex.IsMatch(l, @"app\.(MapGet|MapPost|MapPut|MapDelete|MapPatch)\(", RegexOptions.IgnoreCase));
        var hasRequireAuth = lines.Any(l =>
            Regex.IsMatch(l, @"\.RequireAuthorization\(|\.RequirePermission\(\[Authorize\]", RegexOptions.IgnoreCase));

        if (hasMapEndpoint && !hasRequireAuth)
        {
            yield return SecurityFinding.Create(
                scanId, "SAST-004", SecurityCategory.BrokenAccessControl, FindingSeverity.High,
                filePath,
                "Endpoint mapping detected without RequireAuthorization() call.",
                "Add .RequireAuthorization() or .RequirePermission() to all sensitive endpoints. Ensure public endpoints are intentionally unauthenticated.",
                lineNumber: null, cweId: "CWE-306", owaspCategory: "A01:2021");
        }
    }

    // SAST-005: CORS Misconfiguration
    private static IEnumerable<SecurityFinding> DetectCorsMisconfiguration(
        Guid scanId, string filePath, string[] lines)
    {
        var pattern = new Regex(@"AllowAnyOrigin\(\)", RegexOptions.Compiled);
        return ScanLines(scanId, filePath, lines, pattern,
            "SAST-005", SecurityCategory.SecurityMisconfiguration, FindingSeverity.Medium,
            "CORS misconfiguration: AllowAnyOrigin() detected.",
            "Restrict CORS to specific origins using WithOrigins(). AllowAnyOrigin() combined with AllowCredentials() is forbidden and causes exceptions.",
            "CWE-942", "A05:2021");
    }

    // SAST-006: Insecure Deserialization
    private static IEnumerable<SecurityFinding> DetectInsecureDeserialization(
        Guid scanId, string filePath, string[] lines)
    {
        var pattern = new Regex(
            @"BinaryFormatter|JavaScriptSerializer|TypeNameHandling\.All|TypeNameHandling\.Objects",
            RegexOptions.Compiled);
        return ScanLines(scanId, filePath, lines, pattern,
            "SAST-006", SecurityCategory.InsecureDeserialization, FindingSeverity.High,
            "Insecure deserialization: BinaryFormatter or unsafe JsonSerializer settings detected.",
            "Replace BinaryFormatter with System.Text.Json or Newtonsoft.Json with safe settings. Avoid TypeNameHandling.All/Objects which allows arbitrary type instantiation.",
            "CWE-502", "A08:2021");
    }

    // SAST-007: Path Traversal
    private static IEnumerable<SecurityFinding> DetectPathTraversal(
        Guid scanId, string filePath, string[] lines)
    {
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (Regex.IsMatch(line, @"File\.(ReadAll|Open|Create|Delete)|Directory\.(Get|Create|Delete)",
                    RegexOptions.IgnoreCase))
            {
                // Check nearby lines for user input usage
                var context = string.Join(" ", lines.Skip(Math.Max(0, i - 2)).Take(5));
                if (Regex.IsMatch(context, @"Request\.(Query|Form|Body|Path)|HttpContext", RegexOptions.IgnoreCase))
                {
                    yield return SecurityFinding.Create(
                        scanId, "SAST-007", SecurityCategory.BrokenAccessControl, FindingSeverity.High,
                        filePath,
                        "Possible path traversal: file system operation near user-controlled input.",
                        "Validate and sanitize all file paths. Use Path.GetFullPath() and verify the result starts with the expected base directory.",
                        lineNumber: i + 1, cweId: "CWE-22", owaspCategory: "A01:2021");
                }
            }
        }
    }

    // SAST-008: XSS
    private static IEnumerable<SecurityFinding> DetectXss(
        Guid scanId, string filePath, string[] lines)
    {
        var pattern = new Regex(
            @"dangerouslySetInnerHTML|Html\.Raw\((?!.*HtmlEncoder|.*HtmlEncode)",
            RegexOptions.Compiled);
        return ScanLines(scanId, filePath, lines, pattern,
            "SAST-008", SecurityCategory.Xss, FindingSeverity.High,
            "Cross-Site Scripting (XSS): unencoded HTML output detected.",
            "Always encode output using HtmlEncoder.Default.Encode(). In React, avoid dangerouslySetInnerHTML with unsanitized content.",
            "CWE-79", "A03:2021");
    }

    // SAST-009: Logging Sensitive Data
    private static IEnumerable<SecurityFinding> DetectLoggingSensitiveData(
        Guid scanId, string filePath, string[] lines)
    {
        var pattern = new Regex(
            @"(?i)(Log\.(Information|Warning|Error|Debug|Trace|Critical)|logger\.(Log|Information|Warning|Error))\s*\([^)]*\b(password|token|secret|apikey|ssn|creditcard|cvv)\b",
            RegexOptions.Compiled);
        return ScanLines(scanId, filePath, lines, pattern,
            "SAST-009", SecurityCategory.LoggingSensitiveData, FindingSeverity.Medium,
            "Sensitive data logging detected: PII or credentials may be logged.",
            "Remove sensitive fields from log messages. Use structured logging with field masking for PII fields.",
            "CWE-532", "A09:2021");
    }

    // SAST-010: Missing Input Validation
    private static IEnumerable<SecurityFinding> DetectMissingInputValidation(
        Guid scanId, string filePath, string[] lines)
    {
        var hasCommand = lines.Any(l =>
            Regex.IsMatch(l, @":\s*I(Command|Query|Request)\b", RegexOptions.IgnoreCase));
        var hasValidator = lines.Any(l =>
            Regex.IsMatch(l, @"AbstractValidator|FluentValidation|DataAnnotations|Required\]|MaxLength\]",
                RegexOptions.IgnoreCase));

        if (hasCommand && !hasValidator)
        {
            yield return SecurityFinding.Create(
                scanId, "SAST-010", SecurityCategory.MissingInputValidation, FindingSeverity.Medium,
                filePath,
                "Command/Query handler without input validation detected.",
                "Add a FluentValidation AbstractValidator<T> for every Command and Query to validate inputs before processing.",
                cweId: "CWE-20", owaspCategory: "A03:2021");
        }
    }

    private static IEnumerable<SecurityFinding> ScanLines(
        Guid scanId,
        string filePath,
        string[] lines,
        Regex pattern,
        string ruleId,
        SecurityCategory category,
        FindingSeverity severity,
        string description,
        string remediation,
        string? cweId = null,
        string? owaspCategory = null)
    {
        for (var i = 0; i < lines.Length; i++)
        {
            if (pattern.IsMatch(lines[i]))
            {
                yield return SecurityFinding.Create(
                    scanId, ruleId, category, severity,
                    filePath, description, remediation,
                    lineNumber: i + 1, cweId: cweId, owaspCategory: owaspCategory);
            }
        }
    }
}
