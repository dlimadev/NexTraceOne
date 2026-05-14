using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using SignArtifactFeature = NexTraceOne.Governance.Application.Features.SignArtifact.SignArtifact;
using VerifyArtifactFeature = NexTraceOne.Governance.Application.Features.VerifyArtifact.VerifyArtifact;
using GenerateSbomFeature = NexTraceOne.Governance.Application.Features.GenerateSbom.GenerateSbom;

namespace NexTraceOne.Governance.API.Endpoints;

/// <summary>
/// Endpoints para assinatura digital de artefatos e geração de SBOM.
/// Integra com Cosign para signing/verification e Rekor para transparency log.
/// Essencial para compliance de segurança em pipelines CI/CD.
/// </summary>
public sealed class ArtifactSigningEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/governance/artifact-signing")
            .WithTags("Governance - Artifact Signing & SBOM")
            .RequireRateLimiting("operations");

        // POST /api/v1/governance/artifact-signing/sign — Assina um artefato digitalmente
        group.MapPost("/sign", async (
            SignArtifactFeature.Command request,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var result = await sender.Send(request, ct);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("governance:artifact:write")
        .WithName("SignArtifact")
        .WithSummary("Assina digitalmente um artefato usando Cosign")
        .WithDescription("Gera assinatura digital, SBOM e envia para transparency log (Rekor). Requisito para compliance de supply chain security.");

        // POST /api/v1/governance/artifact-signing/verify — Verifica assinatura de artefato
        group.MapPost("/verify", async (
            VerifyArtifactFeature.Command request,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var result = await sender.Send(request, ct);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("governance:artifact:read")
        .WithName("VerifyArtifact")
        .WithSummary("Verifica a assinatura digital de um artefato")
        .WithDescription("Valida integridade do checksum, autenticidade da assinatura e entrada no transparency log.");

        // POST /api/v1/governance/artifact-signing/sbom/generate — Gera SBOM para projeto
        group.MapPost("/sbom/generate", async (
            GenerateSbomFeature.Command request,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var result = await sender.Send(request, ct);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("governance:sbom:write")
        .WithName("GenerateArtifactSbom")
        .WithSummary("Gera Software Bill of Materials (SBOM) em formato SPDX 2.3")
        .WithDescription("Produz documento SBOM com lista de dependências, licenças e metadados de compliance. Requisito NTIA e Executive Order 14028.");
    }
}
