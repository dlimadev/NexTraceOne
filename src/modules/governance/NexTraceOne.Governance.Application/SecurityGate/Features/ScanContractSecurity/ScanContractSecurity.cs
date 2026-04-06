using System.Text.Json;
using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.SecurityGate.Ports;
using NexTraceOne.Governance.Domain.SecurityGate.Entities;
using NexTraceOne.Governance.Domain.SecurityGate.Enums;

namespace NexTraceOne.Governance.Application.SecurityGate.Features.ScanContractSecurity;

/// <summary>Analisa um contrato OpenAPI para detectar problemas de segurança.</summary>
public static class ScanContractSecurity
{
    /// <summary>Comando para scan de segurança de contrato.</summary>
    public sealed record Command(
        Guid ContractVersionId,
        string ContractJson) : ICommand<Response>;

    /// <summary>Validação do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ContractVersionId).NotEmpty();
            RuleFor(x => x.ContractJson).NotEmpty().MaximumLength(2_000_000);
        }
    }

    /// <summary>Handler que analisa o contrato e persiste o resultado.</summary>
    public sealed class Handler(
        ISecurityScanRepository repository,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var scanResult = SecurityScanResult.Create(ScanTarget.Contract, request.ContractVersionId, ScanProvider.Internal);
            var findings = AnalyzeContract(scanResult.Id.Value, request.ContractJson);

            scanResult.AddFindings(findings);
            await repository.AddAsync(scanResult, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                ScanId: scanResult.Id.Value,
                PassedGate: scanResult.PassedGate,
                TotalFindings: findings.Count,
                OverallRisk: scanResult.OverallRisk));
        }

        private static IReadOnlyList<SecurityFinding> AnalyzeContract(Guid scanId, string contractJson)
        {
            var findings = new List<SecurityFinding>();
            try
            {
                using var doc = JsonDocument.Parse(contractJson);
                var root = doc.RootElement;

                // Check for missing security schemes
                if (!root.TryGetProperty("components", out _) ||
                    !root.TryGetProperty("components", out var components) ||
                    !components.TryGetProperty("securitySchemes", out _))
                {
                    findings.Add(SecurityFinding.Create(
                        scanId, "CONTRACT-001", SecurityCategory.BrokenAuth, FindingSeverity.Medium,
                        "contract.json",
                        "No security schemes defined in the OpenAPI contract.",
                        "Define at least one security scheme (Bearer, OAuth2, API Key) in components.securitySchemes and apply it globally or per-endpoint.",
                        cweId: "CWE-306", owaspCategory: "A07:2021"));
                }

                // Check for paths without security
                if (root.TryGetProperty("paths", out var paths))
                {
                    foreach (var path in paths.EnumerateObject())
                    {
                        foreach (var method in path.Value.EnumerateObject())
                        {
                            if (!method.Value.TryGetProperty("security", out _) &&
                                !root.TryGetProperty("security", out _))
                            {
                                findings.Add(SecurityFinding.Create(
                                    scanId, "CONTRACT-002", SecurityCategory.BrokenAccessControl, FindingSeverity.High,
                                    $"paths.{path.Name}.{method.Name}",
                                    $"Endpoint {method.Name.ToUpperInvariant()} {path.Name} has no security requirement defined.",
                                    "Add a security requirement to the endpoint or define a global security requirement in the root of the OpenAPI document.",
                                    cweId: "CWE-306", owaspCategory: "A01:2021"));
                                break; // one finding per path
                            }
                        }
                    }
                }
            }
            catch (JsonException)
            {
                findings.Add(SecurityFinding.Create(
                    scanId, "CONTRACT-000", SecurityCategory.SecurityMisconfiguration, FindingSeverity.Low,
                    "contract.json",
                    "Contract JSON could not be parsed for security analysis.",
                    "Ensure the contract is valid JSON/YAML before security scanning."));
            }

            return findings;
        }
    }

    /// <summary>Resposta do scan de segurança de contrato.</summary>
    public sealed record Response(
        Guid ScanId,
        bool PassedGate,
        int TotalFindings,
        SecurityRiskLevel OverallRisk);
}
