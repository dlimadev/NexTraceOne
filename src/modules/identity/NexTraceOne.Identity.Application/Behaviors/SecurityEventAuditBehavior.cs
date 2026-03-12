using MediatR;
using Microsoft.Extensions.Logging;
using NexTraceOne.Identity.Application.Abstractions;

namespace NexTraceOne.Identity.Application.Behaviors;

/// <summary>
/// Behavior do MediatR que propaga eventos de segurança para o módulo Audit central
/// após a execução bem-sucedida de qualquer handler do módulo Identity.
///
/// Fluxo:
/// 1. O handler executa e, ao criar <see cref="Domain.Entities.SecurityEvent"/>s,
///    registra-os no <see cref="ISecurityEventTracker"/>.
/// 2. Este behavior intercepta a resposta — se o handler retornou sucesso e existem
///    eventos pendentes, propaga cada um via <see cref="ISecurityAuditBridge"/>.
/// 3. Os eventos são limpos do tracker para evitar reprocessamento.
///
/// Estratégia de resiliência:
/// - Propagação é best-effort (fire-and-forget com logging).
/// - Falha na propagação NÃO afeta a resposta do handler — o SecurityEvent já está
///   persistido no módulo Identity e pode ser reconciliado posteriormente.
/// - Cada evento é propagado independentemente: falha em um não impede os demais.
///
/// Posição no pipeline: deve executar APÓS o handler (é um wrapper em torno de next()).
/// Funciona com qualquer IRequest do módulo Identity — commands, queries e notifications.
/// </summary>
public sealed class SecurityEventAuditBehavior<TRequest, TResponse>(
    ISecurityEventTracker securityEventTracker,
    ISecurityAuditBridge securityAuditBridge,
    ILogger<SecurityEventAuditBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next();

        if (!securityEventTracker.HasPendingEvents)
        {
            return response;
        }

        // Propaga eventos de segurança para o Audit central de forma resiliente.
        // Captura a lista antes de limpar para garantir atomicidade.
        var pendingEvents = securityEventTracker.GetTrackedEvents().ToList();
        securityEventTracker.Clear();

        foreach (var securityEvent in pendingEvents)
        {
            try
            {
                await securityAuditBridge.PropagateAsync(securityEvent, cancellationToken);

                logger.LogDebug(
                    "SecurityEvent '{EventType}' for user '{UserId}' propagated to central audit successfully.",
                    securityEvent.EventType,
                    securityEvent.UserId?.Value);
            }
            catch (Exception ex)
            {
                // Falha na propagação é logada mas não propagada — o evento já foi persistido
                // no módulo Identity. A reconciliação pode ocorrer via job batch posterior.
                logger.LogWarning(
                    ex,
                    "Failed to propagate SecurityEvent '{EventType}' for user '{UserId}' to central audit. " +
                    "Event is persisted in Identity module and can be reconciled later.",
                    securityEvent.EventType,
                    securityEvent.UserId?.Value);
            }
        }

        return response;
    }
}
