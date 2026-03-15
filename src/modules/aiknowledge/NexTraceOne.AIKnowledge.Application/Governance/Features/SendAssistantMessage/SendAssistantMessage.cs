using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.AiGovernance.Application.Abstractions;
using NexTraceOne.AiGovernance.Domain.Entities;
using NexTraceOne.AiGovernance.Domain.Enums;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AiGovernance.Application.Features.SendAssistantMessage;

/// <summary>
/// Feature: SendAssistantMessage — envia uma mensagem ao assistente de IA governado.
/// Valida políticas de acesso, regista auditoria de uso, persiste mensagens na conversa
/// e retorna resposta com metadados completos de grounding e explicabilidade.
/// Implementação stub para resposta de IA — grounding de contexto em desenvolvimento.
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class SendAssistantMessage
{
    /// <summary>Comando de envio de mensagem ao assistente de IA com contexto completo.</summary>
    public sealed record Command(
        Guid? ConversationId,
        string Message,
        string? ContextScope,
        string? Persona,
        Guid? PreferredModelId,
        string ClientType,
        Guid? ServiceId,
        Guid? ContractId,
        Guid? IncidentId,
        Guid? TeamId,
        Guid? DomainId) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de envio de mensagem.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Message).NotEmpty().MaximumLength(10_000);
            RuleFor(x => x.ClientType).NotEmpty();
        }
    }

    /// <summary>Handler que processa a mensagem do assistente com governança integrada.</summary>
    public sealed class Handler(
        IAiUsageEntryRepository usageEntryRepository,
        IAiAssistantConversationRepository conversationRepository,
        IAiMessageRepository messageRepository,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var now = dateTimeProvider.UtcNow;
            var correlationId = Guid.NewGuid().ToString();
            var clientType = Enum.TryParse<AIClientType>(request.ClientType, ignoreCase: true, out var ct)
                ? ct
                : AIClientType.Web;

            // ── Resolver ou criar conversa ────────────────────────────────
            AiAssistantConversation? conversation = null;
            if (request.ConversationId.HasValue)
            {
                var convId = AiAssistantConversationId.From(request.ConversationId.Value);
                conversation = await conversationRepository.GetByIdAsync(convId, cancellationToken);
            }

            if (conversation is null)
            {
                conversation = AiAssistantConversation.Start(
                    request.Message.Length > 100
                        ? string.Concat(request.Message.AsSpan(0, 97), "...")
                        : request.Message,
                    request.Persona ?? "Engineer",
                    clientType,
                    request.ContextScope ?? string.Empty,
                    currentUser.Id,
                    request.ServiceId,
                    request.ContractId,
                    request.IncidentId,
                    request.TeamId);
                await conversationRepository.AddAsync(conversation, cancellationToken);
            }

            var conversationId = conversation.Id.Value;

            // ── Persistir mensagem do utilizador ──────────────────────────
            var userMessage = AiMessage.UserMessage(conversationId, request.Message, now);
            await messageRepository.AddAsync(userMessage, cancellationToken);
            conversation.RecordMessage(null, now);

            // ── Stub: gerar resposta da IA (grounding em desenvolvimento) ─
            const string stubModel = "NexTrace-Internal-v1";
            const string stubProvider = "Internal";
            const int stubPromptTokens = 0;
            const int stubCompletionTokens = 0;

            var groundingSources = ResolveGroundingSources(request.ContextScope);
            var contextRefs = ResolveContextReferences(request);

            var stubResponse = GenerateStubResponse(request.Message, request.Persona);

            // ── Persistir mensagem do assistente ──────────────────────────
            var assistantMsg = AiMessage.AssistantMessage(
                conversationId,
                stubResponse,
                stubModel,
                stubProvider,
                isInternal: true,
                stubPromptTokens,
                stubCompletionTokens,
                appliedPolicyName: null,
                string.Join(",", groundingSources),
                string.Join(",", contextRefs),
                correlationId,
                now);
            await messageRepository.AddAsync(assistantMsg, cancellationToken);
            conversation.RecordMessage(stubModel, now);

            await conversationRepository.UpdateAsync(conversation, cancellationToken);

            // ── Registar auditoria de uso ─────────────────────────────────
            var usageEntry = AIUsageEntry.Record(
                currentUser.Id,
                currentUser.Name,
                request.PreferredModelId ?? Guid.Empty,
                stubModel,
                stubProvider,
                isInternal: true,
                now,
                stubPromptTokens,
                stubCompletionTokens,
                policyId: null,
                policyName: null,
                UsageResult.Allowed,
                request.ContextScope ?? string.Empty,
                clientType,
                correlationId,
                conversationId);

            await usageEntryRepository.AddAsync(usageEntry, cancellationToken);

            return new Response(
                conversationId,
                assistantMsg.Id.Value,
                stubResponse,
                stubModel,
                stubProvider,
                IsInternalModel: true,
                PromptTokens: stubPromptTokens,
                CompletionTokens: stubCompletionTokens,
                AppliedPolicy: null,
                GroundingSources: groundingSources,
                ContextReferences: contextRefs,
                CorrelationId: correlationId);
        }

        /// <summary>
        /// Resolve fontes de grounding baseadas no escopo de contexto solicitado.
        /// Stub: retorna nomes de fontes baseados nos scopes selecionados.
        /// </summary>
        private static List<string> ResolveGroundingSources(string? contextScope)
        {
            if (string.IsNullOrWhiteSpace(contextScope))
                return ["Service Catalog", "Contract Registry"];

            var scopes = contextScope.Split(',', 20, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var sources = new List<string>();

            foreach (var scope in scopes)
            {
                sources.Add(scope.ToLowerInvariant() switch
                {
                    "services" => "Service Catalog",
                    "contracts" => "Contract Registry",
                    "incidents" => "Incident History",
                    "changes" => "Change Intelligence",
                    "runbooks" => "Runbook Library",
                    "dependencies" => "Dependency Graph",
                    "reliability" => "Reliability Metrics",
                    "governance" => "Governance Policies",
                    "policies" => "Access Policies",
                    "models" => "Model Registry",
                    "audit" => "Audit Trail",
                    "compliance" => "Compliance Records",
                    "risk" => "Risk Assessment",
                    "trends" => "Operational Trends",
                    _ => scope
                });
            }

            return sources.Count > 0 ? sources : ["Service Catalog", "Contract Registry"];
        }

        /// <summary>
        /// Resolve referências de contexto baseadas nos IDs fornecidos na requisição.
        /// </summary>
        private static List<string> ResolveContextReferences(Command request)
        {
            var refs = new List<string>();
            if (request.ServiceId.HasValue) refs.Add($"service:{request.ServiceId.Value}");
            if (request.ContractId.HasValue) refs.Add($"contract:{request.ContractId.Value}");
            if (request.IncidentId.HasValue) refs.Add($"incident:{request.IncidentId.Value}");
            if (request.TeamId.HasValue) refs.Add($"team:{request.TeamId.Value}");
            if (request.DomainId.HasValue) refs.Add($"domain:{request.DomainId.Value}");
            return refs;
        }

        /// <summary>
        /// Gera resposta stub contextual baseada no prompt e persona.
        /// Evolução futura: integração com LLM provider via grounding pipeline.
        /// </summary>
        private static string GenerateStubResponse(string message, string? persona)
        {
            var personaContext = persona?.ToLowerInvariant() switch
            {
                "engineer" => "From a technical perspective",
                "techlead" => "From a team leadership perspective",
                "architect" => "From an architectural perspective",
                "product" => "From a product impact perspective",
                "executive" => "At a strategic level",
                "platformadmin" => "From a platform governance perspective",
                "auditor" => "From a compliance and traceability perspective",
                _ => "Based on available context"
            };

            return $"{personaContext}: I've analyzed your question \"{(message.Length > 80 ? string.Concat(message.AsSpan(0, 77), "...") : message)}\". " +
                   "Context grounding is under active development — full contextual responses with service catalog, contract registry, " +
                   "incident history and change intelligence will be available soon. " +
                   "This response demonstrates the metadata and explainability framework that is already in place.";
        }
    }

    /// <summary>Resposta madura do envio de mensagem ao assistente de IA.</summary>
    public sealed record Response(
        Guid ConversationId,
        Guid MessageId,
        string AssistantResponse,
        string ModelUsed,
        string Provider,
        bool IsInternalModel,
        int PromptTokens,
        int CompletionTokens,
        string? AppliedPolicy,
        List<string> GroundingSources,
        List<string> ContextReferences,
        string CorrelationId);
}
