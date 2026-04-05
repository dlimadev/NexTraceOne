using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.Catalog.Application.DeveloperExperience.Features.ComputeDeveloperExperienceScore;
using NexTraceOne.Catalog.Application.DeveloperExperience.Features.GetDeveloperExperienceScore;
using NexTraceOne.Catalog.Application.DeveloperExperience.Features.ListDeveloperExperienceScores;
using NexTraceOne.Catalog.Application.DeveloperExperience.Features.RecordProductivitySnapshot;

namespace NexTraceOne.Catalog.Application.DeveloperExperience;

/// <summary>
/// Registra serviços da camada Application do subdomínio DeveloperExperience do módulo Catalog.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddDeveloperExperienceApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddTransient<IValidator<ComputeDeveloperExperienceScore.Command>, ComputeDeveloperExperienceScore.Validator>();
        services.AddTransient<IValidator<GetDeveloperExperienceScore.Query>, GetDeveloperExperienceScore.Validator>();
        services.AddTransient<IValidator<RecordProductivitySnapshot.Command>, RecordProductivitySnapshot.Validator>();
        services.AddTransient<IValidator<ListDeveloperExperienceScores.Query>, ListDeveloperExperienceScores.Validator>();
        return services;
    }
}
