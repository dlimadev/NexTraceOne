using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.LegacyAssets.Abstractions;
using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;
using NexTraceOne.Catalog.Domain.LegacyAssets.Errors;
using NexTraceOne.Catalog.Domain.LegacyAssets.ValueObjects;

namespace NexTraceOne.Catalog.Application.LegacyAssets.Features.RegisterMainframeSystem;

/// <summary>
/// Feature: RegisterMainframeSystem — regista um novo sistema mainframe no catálogo legacy.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class RegisterMainframeSystem
{
    /// <summary>Comando de registo de um sistema mainframe.</summary>
    public sealed record Command(
        string Name,
        string Domain,
        string TeamName,
        string SysplexName,
        string LparName,
        string? RegionName) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de registo de sistema mainframe.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Domain).NotEmpty().MaximumLength(200);
            RuleFor(x => x.TeamName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.SysplexName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.LparName).NotEmpty().MaximumLength(100);
        }
    }

    /// <summary>Handler que regista um novo sistema mainframe no catálogo legacy.</summary>
    public sealed class Handler(
        IMainframeSystemRepository mainframeSystemRepository,
        ILegacyAssetsUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var existing = await mainframeSystemRepository.GetByNameAsync(request.Name, cancellationToken);
            if (existing is not null)
            {
                return LegacyAssetsErrors.MainframeSystemAlreadyExists(request.Name);
            }

            var lpar = LparReference.Create(request.SysplexName, request.LparName, request.RegionName);
            var system = MainframeSystem.Create(request.Name, request.Domain, request.TeamName, lpar);

            mainframeSystemRepository.Add(system);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                system.Id.Value,
                system.Name,
                system.Domain,
                system.TeamName,
                system.Lpar.SysplexName,
                system.Lpar.LparName);
        }
    }

    /// <summary>Resposta do registo do sistema mainframe.</summary>
    public sealed record Response(
        Guid Id,
        string Name,
        string Domain,
        string TeamName,
        string SysplexName,
        string LparName);
}
