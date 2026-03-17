using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Application.Abstractions;

/// <summary>
/// Rastreador de eventos de segurança criados durante uma requisição.
///
/// Permite que handlers registrem <see cref="SecurityEvent"/>s para propagação
/// ao módulo Audit central após a conclusão bem-sucedida da operação.
///
/// Ciclo de vida:
/// 1. Handler cria o <see cref="SecurityEvent"/> e persiste via <see cref="ISecurityEventRepository"/>.
/// 2. Handler chama <see cref="Track"/> para sinalizar que o evento deve ir para o Audit central.
/// 3. O <see cref="Behaviors.SecurityEventAuditBehavior{TRequest,TResponse}"/> coleta os eventos
///    rastreados e propaga via <see cref="ISecurityAuditBridge"/> após a execução bem-sucedida.
/// 4. Os eventos são limpos após a propagação para evitar duplicatas.
///
/// Escopo: registrado como Scoped no DI — cada requisição HTTP tem sua própria instância.
/// </summary>
public interface ISecurityEventTracker
{
    /// <summary>
    /// Registra um evento de segurança para propagação posterior ao módulo Audit central.
    /// Deve ser chamado após persistir o evento no <see cref="ISecurityEventRepository"/>.
    /// </summary>
    /// <param name="securityEvent">Evento de segurança a ser propagado.</param>
    void Track(SecurityEvent securityEvent);

    /// <summary>
    /// Retorna todos os eventos rastreados na requisição atual que ainda não foram propagados.
    /// </summary>
    IReadOnlyList<SecurityEvent> GetTrackedEvents();

    /// <summary>
    /// Indica se existem eventos pendentes de propagação.
    /// </summary>
    bool HasPendingEvents { get; }

    /// <summary>
    /// Limpa os eventos rastreados após propagação bem-sucedida.
    /// Evita reprocessamento duplicado caso múltiplos behaviors executem na mesma requisição.
    /// </summary>
    void Clear();
}
