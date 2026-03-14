using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.Licensing.Application.Abstractions;

namespace NexTraceOne.Licensing.Application.Features.ListLicenses;

/// <summary>
/// Feature: ListLicenses — query de vendor ops para listar todas as licenças.
///
/// Usada pelo backoffice interno da NexTraceOne para gestão de licenças.
/// Suporta paginação para cenários com grande volume de licenças.
///
/// Permissão requerida: licensing:vendor:license:read
/// </summary>
public static class ListLicenses
{
    /// <summary>Query paginada para listar licenças.</summary>
    public sealed record Query(int Page = 1, int PageSize = 20) : IQuery<Response>;

    /// <summary>Valida os parâmetros de paginação.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Page).GreaterThan(0);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    /// <summary>
    /// Handler que retorna uma lista paginada de licenças.
    /// Projeção leve para listagem — detalhes completos via GetLicenseStatus.
    /// </summary>
    public sealed class Handler(
        ILicenseRepository licenseRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var (items, totalCount) = await licenseRepository.ListAsync(
                request.Page,
                request.PageSize,
                cancellationToken);

            var licenseItems = items.Select(l => new LicenseItem(
                l.Id.Value,
                l.LicenseKey,
                l.CustomerName,
                l.IsActive,
                l.Type.ToString(),
                l.Edition.ToString(),
                l.DeploymentModel.ToString(),
                l.Status.ToString(),
                l.IssuedAt,
                l.ExpiresAt,
                l.Activations.Count,
                l.IsTrial,
                l.TrialConverted)).ToList();

            return new Response(licenseItems, totalCount, request.Page, request.PageSize);
        }
    }

    /// <summary>Item resumido de licença para listagem.</summary>
    public sealed record LicenseItem(
        Guid LicenseId,
        string LicenseKey,
        string CustomerName,
        bool IsActive,
        string LicenseType,
        string Edition,
        string DeploymentModel,
        string Status,
        DateTimeOffset IssuedAt,
        DateTimeOffset ExpiresAt,
        int ActivationCount,
        bool IsTrial,
        bool TrialConverted);

    /// <summary>Resposta paginada da listagem de licenças.</summary>
    public sealed record Response(
        IReadOnlyList<LicenseItem> Items,
        int TotalCount,
        int Page,
        int PageSize);
}
