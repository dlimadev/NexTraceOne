using Ardalis.GuardClauses;
using System.Text.Json;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;
using NexTraceOne.Knowledge.Domain.Enums;

namespace NexTraceOne.Knowledge.Application.Features.ValidateDocumentReviewGate;

/// <summary>
/// Feature: ValidateDocumentReviewGate — verifica se um documento de conhecimento
/// precisa de revisão antes da publicação.
/// Consulta:
///   - knowledge.document.review_roles: papéis que devem revisar documentos
///   - knowledge.auto_capture.categories: categorias que ativam auto-captura
/// Pilar: Source of Truth &amp; Operational Knowledge
/// </summary>
public static class ValidateDocumentReviewGate
{
    /// <summary>Query para validar necessidade de revisão de documento.</summary>
    public sealed record Query(
        string DocumentTitle,
        string Category,
        string CreatedBy,
        bool HasReview) : IQuery<Response>;

    /// <summary>Handler que avalia necessidade de revisão.</summary>
    public sealed class Handler(
        IConfigurationResolutionService configService,
        IDateTimeProvider dateTimeProvider) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            // Get review roles
            var reviewRolesConfig = await configService.ResolveEffectiveValueAsync(
                "knowledge.document.review_roles",
                ConfigurationScope.Tenant, null, cancellationToken);

            var reviewRoles = new List<string>();
            if (reviewRolesConfig?.EffectiveValue is not null)
            {
                try
                {
                    reviewRoles = JsonSerializer.Deserialize<List<string>>(reviewRolesConfig.EffectiveValue) ?? [];
                }
                catch
                {
                    reviewRoles = [];
                }
            }

            var requiresReview = reviewRoles.Count > 0;

            // Check auto-capture categories
            var autoCaptureConfig = await configService.ResolveEffectiveValueAsync(
                "knowledge.auto_capture.categories",
                ConfigurationScope.Tenant, null, cancellationToken);

            var autoCapture = false;
            if (autoCaptureConfig?.EffectiveValue is not null)
            {
                try
                {
                    var categories = JsonSerializer.Deserialize<List<string>>(autoCaptureConfig.EffectiveValue) ?? [];
                    autoCapture = categories.Contains(request.Category, StringComparer.OrdinalIgnoreCase);
                }
                catch
                {
                    autoCapture = false;
                }
            }

            if (!requiresReview)
            {
                return new Response(
                    DocumentTitle: request.DocumentTitle,
                    Category: request.Category,
                    ReviewRequired: false,
                    IsReviewed: true,
                    ReviewRoles: [],
                    IsAutoCapturedCategory: autoCapture,
                    Reason: "No review roles configured — document can be published directly",
                    EvaluatedAt: dateTimeProvider.UtcNow);
            }

            return new Response(
                DocumentTitle: request.DocumentTitle,
                Category: request.Category,
                ReviewRequired: true,
                IsReviewed: request.HasReview,
                ReviewRoles: reviewRoles,
                IsAutoCapturedCategory: autoCapture,
                Reason: request.HasReview
                    ? "Document has been reviewed and can be published"
                    : $"Document requires review by: {string.Join(", ", reviewRoles)}",
                EvaluatedAt: dateTimeProvider.UtcNow);
        }
    }

    /// <summary>Resposta da validação de revisão de documento.</summary>
    public sealed record Response(
        string DocumentTitle,
        string Category,
        bool ReviewRequired,
        bool IsReviewed,
        List<string> ReviewRoles,
        bool IsAutoCapturedCategory,
        string Reason,
        DateTimeOffset EvaluatedAt);
}
