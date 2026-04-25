using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Domain.Entities;
using NexTraceOne.Integrations.Domain.Enums;

namespace NexTraceOne.Integrations.Application.Features.CreateLogToMetricRule;

/// <summary>
/// Feature: CreateLogToMetricRule — cria uma nova regra de transformação log → métrica.
/// Ownership: módulo Integrations (Pipeline).
/// </summary>
public static class CreateLogToMetricRule
{
    public sealed record Command(
        string TenantId,
        string Name,
        string Pattern,
        string MetricName,
        MetricType MetricType,
        string? ValueExtractor = null,
        string LabelsJson = "[]",
        bool IsEnabled = true) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
            RuleFor(x => x.Pattern).NotEmpty().MaximumLength(2000);
            RuleFor(x => x.MetricName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.MetricType).IsInEnum();
            RuleFor(x => x.LabelsJson).MaximumLength(1000);
        }
    }

    public sealed class Handler(
        ILogToMetricRuleRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var rule = LogToMetricRule.Create(
                tenantId: request.TenantId,
                name: request.Name,
                pattern: request.Pattern,
                metricName: request.MetricName,
                metricType: request.MetricType,
                valueExtractor: request.ValueExtractor,
                labelsJson: request.LabelsJson,
                isEnabled: request.IsEnabled,
                utcNow: clock.UtcNow);

            await repository.AddAsync(rule, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                RuleId: rule.Id.Value,
                Name: rule.Name,
                MetricName: rule.MetricName,
                MetricType: rule.MetricType,
                IsEnabled: rule.IsEnabled,
                CreatedAt: rule.CreatedAt));
        }
    }

    public sealed record Response(
        Guid RuleId,
        string Name,
        string MetricName,
        MetricType MetricType,
        bool IsEnabled,
        DateTimeOffset CreatedAt);
}
