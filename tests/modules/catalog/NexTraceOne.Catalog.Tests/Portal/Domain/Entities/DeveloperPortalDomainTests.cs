using FluentAssertions;
using NexTraceOne.DeveloperPortal.Domain.Entities;
using NexTraceOne.DeveloperPortal.Domain.Enums;

namespace NexTraceOne.DeveloperPortal.Tests.Domain.Entities;

/// <summary>
/// Testes de domínio complementares para as entidades do Developer Portal.
/// Foca nos cenários de falha por guard clauses (dados obrigatórios vazios ou nulos)
/// e na cobertura da entidade PortalAnalyticsEvent, que não possuía testes dedicados.
/// Complementa os testes existentes em SubscriptionTests, PlaygroundSessionTests,
/// CodeGenerationRecordTests e SavedSearchTests que cobrem os cenários de sucesso.
/// </summary>
public sealed class DeveloperPortalDomainTests
{
    private static readonly DateTimeOffset Now = new(2025, 06, 15, 10, 0, 0, TimeSpan.Zero);

    #region Subscription — Guard Clause Failures

    /// <summary>
    /// Verifica que a criação de subscrição com nome de API vazio lança exceção.
    /// O nome da API é obrigatório para identificação legível em notificações.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Subscription_Create_Should_Throw_When_ApiNameIsNullOrWhiteSpace(string? apiName)
    {
        var act = () => Subscription.Create(
            Guid.NewGuid(),
            apiName!,
            Guid.NewGuid(),
            "dev@acme.com",
            "OrderService",
            "1.0.0",
            SubscriptionLevel.AllChanges,
            NotificationChannel.Email,
            webhookUrl: null,
            Now);

        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Verifica que a criação de subscrição com e-mail do subscritor vazio lança exceção.
    /// O e-mail é necessário para entrega de notificações por canal Email.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Subscription_Create_Should_Throw_When_SubscriberEmailIsNullOrWhiteSpace(string? email)
    {
        var act = () => Subscription.Create(
            Guid.NewGuid(),
            "Payments API",
            Guid.NewGuid(),
            email!,
            "OrderService",
            "1.0.0",
            SubscriptionLevel.AllChanges,
            NotificationChannel.Email,
            webhookUrl: null,
            Now);

        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Verifica que a criação de subscrição com nome do serviço consumidor vazio lança exceção.
    /// O serviço consumidor é fundamental para rastreabilidade de dependências.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Subscription_Create_Should_Throw_When_ConsumerServiceNameIsNullOrWhiteSpace(string? serviceName)
    {
        var act = () => Subscription.Create(
            Guid.NewGuid(),
            "Payments API",
            Guid.NewGuid(),
            "dev@acme.com",
            serviceName!,
            "1.0.0",
            SubscriptionLevel.AllChanges,
            NotificationChannel.Email,
            webhookUrl: null,
            Now);

        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Verifica que a criação de subscrição com versão do serviço consumidor vazia lança exceção.
    /// A versão é necessária para contextualizar a dependência no momento da subscrição.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Subscription_Create_Should_Throw_When_ConsumerServiceVersionIsNullOrWhiteSpace(string? version)
    {
        var act = () => Subscription.Create(
            Guid.NewGuid(),
            "Payments API",
            Guid.NewGuid(),
            "dev@acme.com",
            "OrderService",
            version!,
            SubscriptionLevel.AllChanges,
            NotificationChannel.Email,
            webhookUrl: null,
            Now);

        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Verifica que a criação de subscrição com ApiAssetId default (Guid.Empty) lança exceção.
    /// O identificador da API é obrigatório para vincular a subscrição ao ativo correto.
    /// </summary>
    [Fact]
    public void Subscription_Create_Should_Throw_When_ApiAssetIdIsDefault()
    {
        var act = () => Subscription.Create(
            Guid.Empty,
            "Payments API",
            Guid.NewGuid(),
            "dev@acme.com",
            "OrderService",
            "1.0.0",
            SubscriptionLevel.AllChanges,
            NotificationChannel.Email,
            webhookUrl: null,
            Now);

        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Verifica que a criação de subscrição com SubscriberId default (Guid.Empty) lança exceção.
    /// O identificador do subscritor é obrigatório para vincular a subscrição ao utilizador.
    /// </summary>
    [Fact]
    public void Subscription_Create_Should_Throw_When_SubscriberIdIsDefault()
    {
        var act = () => Subscription.Create(
            Guid.NewGuid(),
            "Payments API",
            Guid.Empty,
            "dev@acme.com",
            "OrderService",
            "1.0.0",
            SubscriptionLevel.AllChanges,
            NotificationChannel.Email,
            webhookUrl: null,
            Now);

        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Verifica que a desativação seguida de reativação restaura o estado ativo.
    /// Garante a consistência do ciclo de vida da subscrição.
    /// </summary>
    [Fact]
    public void Subscription_DeactivateAndReactivate_Should_RestoreActiveState()
    {
        var subscription = CreateActiveSubscription();

        subscription.Deactivate();
        subscription.IsActive.Should().BeFalse();

        subscription.Reactivate();
        subscription.IsActive.Should().BeTrue();
    }

    /// <summary>
    /// Verifica que UpdatePreferences altera o canal de Email para Webhook com URL válida.
    /// Cenário de migração de canal frequente em integrações enterprise.
    /// </summary>
    [Fact]
    public void Subscription_UpdatePreferences_Should_SwitchFromEmailToWebhook()
    {
        var subscription = CreateActiveSubscription();
        subscription.Channel.Should().Be(NotificationChannel.Email);

        var result = subscription.UpdatePreferences(
            SubscriptionLevel.SecurityAdvisories,
            NotificationChannel.Webhook,
            "https://hooks.acme.com/alerts");

        result.IsSuccess.Should().BeTrue();
        subscription.Channel.Should().Be(NotificationChannel.Webhook);
        subscription.Level.Should().Be(SubscriptionLevel.SecurityAdvisories);
        subscription.WebhookUrl.Should().Be("https://hooks.acme.com/alerts");
    }

    #endregion

    #region PlaygroundSession — Guard Clause Failures

    /// <summary>
    /// Verifica que a criação de sessão de playground com método HTTP vazio lança exceção.
    /// O método HTTP é essencial para registo correto da chamada executada.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void PlaygroundSession_Create_Should_Throw_When_HttpMethodIsNullOrWhiteSpace(string? method)
    {
        var act = () => PlaygroundSession.Create(
            Guid.NewGuid(),
            "Payments API",
            Guid.NewGuid(),
            method!,
            "/api/payments",
            null,
            null,
            200,
            null,
            durationMs: 100,
            Now);

        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Verifica que a criação de sessão de playground com caminho do request vazio lança exceção.
    /// O caminho identifica o endpoint testado e é obrigatório para auditoria.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void PlaygroundSession_Create_Should_Throw_When_RequestPathIsNullOrWhiteSpace(string? path)
    {
        var act = () => PlaygroundSession.Create(
            Guid.NewGuid(),
            "Payments API",
            Guid.NewGuid(),
            "GET",
            path!,
            null,
            null,
            200,
            null,
            durationMs: 100,
            Now);

        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Verifica que a criação de sessão de playground com ApiAssetId default lança exceção.
    /// O identificador do ativo vincula a sessão à API correta no catálogo.
    /// </summary>
    [Fact]
    public void PlaygroundSession_Create_Should_Throw_When_ApiAssetIdIsDefault()
    {
        var act = () => PlaygroundSession.Create(
            Guid.Empty,
            "Payments API",
            Guid.NewGuid(),
            "GET",
            "/api/payments",
            null,
            null,
            200,
            null,
            durationMs: 100,
            Now);

        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Verifica que a criação de sessão de playground com UserId default lança exceção.
    /// O utilizador é obrigatório para trilha de auditoria do playground.
    /// </summary>
    [Fact]
    public void PlaygroundSession_Create_Should_Throw_When_UserIdIsDefault()
    {
        var act = () => PlaygroundSession.Create(
            Guid.NewGuid(),
            "Payments API",
            Guid.Empty,
            "GET",
            "/api/payments",
            null,
            null,
            200,
            null,
            durationMs: 100,
            Now);

        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Verifica que a criação de sessão com nome da API vazio lança exceção.
    /// O nome legível é obrigatório para apresentação em dashboards de analytics.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void PlaygroundSession_Create_Should_Throw_When_ApiNameIsNullOrWhiteSpace(string? apiName)
    {
        var act = () => PlaygroundSession.Create(
            Guid.NewGuid(),
            apiName!,
            Guid.NewGuid(),
            "GET",
            "/api/payments",
            null,
            null,
            200,
            null,
            durationMs: 100,
            Now);

        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Verifica que o Id é gerado automaticamente na criação da sessão.
    /// O Id fortemente tipado deve ser não-vazio para garantir unicidade no repositório.
    /// </summary>
    [Fact]
    public void PlaygroundSession_Create_Should_GenerateNonEmptyId()
    {
        var session = PlaygroundSession.Create(
            Guid.NewGuid(),
            "Payments API",
            Guid.NewGuid(),
            "GET",
            "/api/payments",
            null,
            null,
            200,
            null,
            durationMs: 50,
            Now);

        session.Id.Should().NotBeNull();
        session.Id.Value.Should().NotBeEmpty();
    }

    #endregion

    #region CodeGenerationRecord — Guard Clause Failures

    /// <summary>
    /// Verifica que a criação de registo de geração com linguagem vazia lança exceção.
    /// A linguagem alvo é obrigatória para selecionar o gerador correto.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CodeGenerationRecord_Create_Should_Throw_When_LanguageIsNullOrWhiteSpace(string? language)
    {
        var act = () => CodeGenerationRecord.Create(
            Guid.NewGuid(),
            "Payments API",
            "2.0.0",
            Guid.NewGuid(),
            language!,
            "SdkClient",
            "public class Client { }",
            isAiGenerated: false,
            templateId: null,
            Now);

        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Verifica que a criação de registo de geração com tipo vazio lança exceção.
    /// O tipo de geração (SdkClient, IntegrationExample, etc.) é obrigatório para categorização.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CodeGenerationRecord_Create_Should_Throw_When_GenerationTypeIsNullOrWhiteSpace(string? generationType)
    {
        var act = () => CodeGenerationRecord.Create(
            Guid.NewGuid(),
            "Payments API",
            "2.0.0",
            Guid.NewGuid(),
            "CSharp",
            generationType!,
            "public class Client { }",
            isAiGenerated: false,
            templateId: null,
            Now);

        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Verifica que a criação de registo de geração com código gerado vazio lança exceção.
    /// O código é o artefacto principal da geração e deve conter conteúdo válido.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CodeGenerationRecord_Create_Should_Throw_When_GeneratedCodeIsNullOrWhiteSpace(string? code)
    {
        var act = () => CodeGenerationRecord.Create(
            Guid.NewGuid(),
            "Payments API",
            "2.0.0",
            Guid.NewGuid(),
            "CSharp",
            "SdkClient",
            code!,
            isAiGenerated: false,
            templateId: null,
            Now);

        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Verifica que a criação de registo com nome da API vazio lança exceção.
    /// O nome da API é obrigatório para rastreabilidade do artefacto gerado.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CodeGenerationRecord_Create_Should_Throw_When_ApiNameIsNullOrWhiteSpace(string? apiName)
    {
        var act = () => CodeGenerationRecord.Create(
            Guid.NewGuid(),
            apiName!,
            "2.0.0",
            Guid.NewGuid(),
            "CSharp",
            "SdkClient",
            "public class Client { }",
            isAiGenerated: false,
            templateId: null,
            Now);

        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Verifica que a criação de registo com versão de contrato vazia lança exceção.
    /// A versão identifica o contrato exato utilizado na geração.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CodeGenerationRecord_Create_Should_Throw_When_ContractVersionIsNullOrWhiteSpace(string? version)
    {
        var act = () => CodeGenerationRecord.Create(
            Guid.NewGuid(),
            "Payments API",
            version!,
            Guid.NewGuid(),
            "CSharp",
            "SdkClient",
            "public class Client { }",
            isAiGenerated: false,
            templateId: null,
            Now);

        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Verifica que a criação com ApiAssetId default lança exceção.
    /// O ativo é obrigatório para vincular a geração ao contrato correto.
    /// </summary>
    [Fact]
    public void CodeGenerationRecord_Create_Should_Throw_When_ApiAssetIdIsDefault()
    {
        var act = () => CodeGenerationRecord.Create(
            Guid.Empty,
            "Payments API",
            "2.0.0",
            Guid.NewGuid(),
            "CSharp",
            "SdkClient",
            "public class Client { }",
            isAiGenerated: false,
            templateId: null,
            Now);

        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Verifica que a criação com RequestedById default lança exceção.
    /// O solicitante é obrigatório para auditoria e controlo de acesso.
    /// </summary>
    [Fact]
    public void CodeGenerationRecord_Create_Should_Throw_When_RequestedByIdIsDefault()
    {
        var act = () => CodeGenerationRecord.Create(
            Guid.NewGuid(),
            "Payments API",
            "2.0.0",
            Guid.Empty,
            "CSharp",
            "SdkClient",
            "public class Client { }",
            isAiGenerated: false,
            templateId: null,
            Now);

        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region PortalAnalyticsEvent — Cobertura Completa

    /// <summary>
    /// Verifica que a criação de evento de analytics com dados válidos produz entidade correta.
    /// Cenário típico: registo de pesquisa no catálogo com query e resultados.
    /// </summary>
    [Fact]
    public void PortalAnalyticsEvent_Create_Should_ReturnEvent_When_InputIsValid()
    {
        var userId = Guid.NewGuid();

        var analyticsEvent = PortalAnalyticsEvent.Create(
            userId,
            "Search",
            entityId: null,
            entityType: null,
            searchQuery: "payment gateway",
            zeroResults: false,
            durationMs: 120,
            metadata: """{"filters":["rest"]}""",
            Now);

        analyticsEvent.UserId.Should().Be(userId);
        analyticsEvent.EventType.Should().Be("Search");
        analyticsEvent.SearchQuery.Should().Be("payment gateway");
        analyticsEvent.ZeroResults.Should().BeFalse();
        analyticsEvent.DurationMs.Should().Be(120);
        analyticsEvent.Metadata.Should().Contain("rest");
        analyticsEvent.OccurredAt.Should().Be(Now);
    }

    /// <summary>
    /// Verifica que a criação de evento com tipo vazio lança exceção.
    /// O tipo do evento é o classificador principal e deve estar sempre presente.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void PortalAnalyticsEvent_Create_Should_Throw_When_EventTypeIsNullOrWhiteSpace(string? eventType)
    {
        var act = () => PortalAnalyticsEvent.Create(
            Guid.NewGuid(),
            eventType!,
            entityId: null,
            entityType: null,
            searchQuery: null,
            zeroResults: null,
            durationMs: null,
            metadata: null,
            Now);

        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Verifica que a criação aceita UserId nulo para eventos anónimos.
    /// Eventos de pesquisa podem ocorrer antes do login do utilizador.
    /// </summary>
    [Fact]
    public void PortalAnalyticsEvent_Create_Should_AcceptNullUserId()
    {
        var analyticsEvent = PortalAnalyticsEvent.Create(
            userId: null,
            "Search",
            entityId: null,
            entityType: null,
            searchQuery: "rest apis",
            zeroResults: true,
            durationMs: null,
            metadata: null,
            Now);

        analyticsEvent.UserId.Should().BeNull();
        analyticsEvent.EventType.Should().Be("Search");
        analyticsEvent.ZeroResults.Should().BeTrue();
    }

    /// <summary>
    /// Verifica criação de evento de visualização de API com entidade associada.
    /// Cenário típico: desenvolvedor visualiza detalhes de uma API no catálogo.
    /// </summary>
    [Fact]
    public void PortalAnalyticsEvent_Create_Should_SetEntityFields_ForApiViewEvent()
    {
        var apiAssetId = Guid.NewGuid();

        var analyticsEvent = PortalAnalyticsEvent.Create(
            Guid.NewGuid(),
            "ApiView",
            entityId: apiAssetId.ToString(),
            entityType: "ApiAsset",
            searchQuery: null,
            zeroResults: null,
            durationMs: 3500,
            metadata: """{"source":"catalog-browse"}""",
            Now);

        analyticsEvent.EventType.Should().Be("ApiView");
        analyticsEvent.EntityId.Should().Be(apiAssetId.ToString());
        analyticsEvent.EntityType.Should().Be("ApiAsset");
        analyticsEvent.SearchQuery.Should().BeNull();
        analyticsEvent.ZeroResults.Should().BeNull();
        analyticsEvent.DurationMs.Should().Be(3500);
    }

    /// <summary>
    /// Verifica que todos os campos opcionais podem ser nulos simultaneamente.
    /// Cenário mínimo: apenas o tipo do evento é obrigatório.
    /// </summary>
    [Fact]
    public void PortalAnalyticsEvent_Create_Should_AcceptAllNullableFieldsAsNull()
    {
        var analyticsEvent = PortalAnalyticsEvent.Create(
            userId: null,
            "OnboardingStarted",
            entityId: null,
            entityType: null,
            searchQuery: null,
            zeroResults: null,
            durationMs: null,
            metadata: null,
            Now);

        analyticsEvent.UserId.Should().BeNull();
        analyticsEvent.EntityId.Should().BeNull();
        analyticsEvent.EntityType.Should().BeNull();
        analyticsEvent.SearchQuery.Should().BeNull();
        analyticsEvent.ZeroResults.Should().BeNull();
        analyticsEvent.DurationMs.Should().BeNull();
        analyticsEvent.Metadata.Should().BeNull();
        analyticsEvent.OccurredAt.Should().Be(Now);
    }

    /// <summary>
    /// Verifica que o Id fortemente tipado é gerado automaticamente na criação.
    /// Garante unicidade para persistência e consultas posteriores.
    /// </summary>
    [Fact]
    public void PortalAnalyticsEvent_Create_Should_GenerateNonEmptyId()
    {
        var analyticsEvent = PortalAnalyticsEvent.Create(
            Guid.NewGuid(),
            "PlaygroundExecution",
            entityId: null,
            entityType: null,
            searchQuery: null,
            zeroResults: null,
            durationMs: 250,
            metadata: null,
            Now);

        analyticsEvent.Id.Should().NotBeNull();
        analyticsEvent.Id.Value.Should().NotBeEmpty();
    }

    /// <summary>
    /// Verifica criação de evento de pesquisa com zero resultados.
    /// Cenário importante para identificar lacunas no catálogo de APIs.
    /// </summary>
    [Fact]
    public void PortalAnalyticsEvent_Create_Should_TrackZeroResultSearches()
    {
        var analyticsEvent = PortalAnalyticsEvent.Create(
            Guid.NewGuid(),
            "Search",
            entityId: null,
            entityType: null,
            searchQuery: "blockchain api",
            zeroResults: true,
            durationMs: 80,
            metadata: null,
            Now);

        analyticsEvent.SearchQuery.Should().Be("blockchain api");
        analyticsEvent.ZeroResults.Should().BeTrue();
    }

    #endregion

    #region SavedSearch — Guard Clause Failures

    /// <summary>
    /// Verifica que a criação de pesquisa salva com nome vazio lança exceção.
    /// O nome é obrigatório para identificação da pesquisa na lista do utilizador.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SavedSearch_Create_Should_Throw_When_NameIsNullOrWhiteSpace(string? name)
    {
        var act = () => SavedSearch.Create(
            Guid.NewGuid(),
            name!,
            "payment gateway",
            filters: null,
            Now);

        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Verifica que a criação de pesquisa salva com query vazia lança exceção.
    /// A query de pesquisa é o critério principal e deve conter conteúdo válido.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SavedSearch_Create_Should_Throw_When_SearchQueryIsNullOrWhiteSpace(string? query)
    {
        var act = () => SavedSearch.Create(
            Guid.NewGuid(),
            "APIs de Pagamento",
            query!,
            filters: null,
            Now);

        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Verifica que a criação de pesquisa salva com UserId default lança exceção.
    /// O utilizador é obrigatório para associar a pesquisa ao perfil correto.
    /// </summary>
    [Fact]
    public void SavedSearch_Create_Should_Throw_When_UserIdIsDefault()
    {
        var act = () => SavedSearch.Create(
            Guid.Empty,
            "APIs REST",
            "rest openapi",
            filters: null,
            Now);

        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Verifica que UpdateQuery com nome vazio lança exceção.
    /// Protege a invariante de que toda pesquisa salva deve ter nome legível.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SavedSearch_UpdateQuery_Should_Throw_When_NameIsNullOrWhiteSpace(string? name)
    {
        var search = SavedSearch.Create(
            Guid.NewGuid(),
            "APIs REST",
            "rest openapi",
            filters: null,
            Now);

        var act = () => search.UpdateQuery(name!, "new query", filters: null);

        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Verifica que UpdateQuery com query vazia lança exceção.
    /// A query é obrigatória para que a pesquisa salva tenha critérios válidos.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SavedSearch_UpdateQuery_Should_Throw_When_SearchQueryIsNullOrWhiteSpace(string? query)
    {
        var search = SavedSearch.Create(
            Guid.NewGuid(),
            "APIs REST",
            "rest openapi",
            filters: null,
            Now);

        var act = () => search.UpdateQuery("Nova Pesquisa", query!, filters: null);

        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Verifica que LastUsedAt é inicializado com a data de criação.
    /// Garante ordenação correta por relevância desde o primeiro momento.
    /// </summary>
    [Fact]
    public void SavedSearch_Create_Should_InitializeLastUsedAtWithCreatedAt()
    {
        var search = SavedSearch.Create(
            Guid.NewGuid(),
            "APIs gRPC",
            "grpc protobuf",
            filters: null,
            Now);

        search.LastUsedAt.Should().Be(search.CreatedAt);
    }

    #endregion

    #region Helpers

    /// <summary>Cria uma subscrição ativa válida para cenários que requerem estado pré-existente.</summary>
    private static Subscription CreateActiveSubscription()
    {
        var result = Subscription.Create(
            Guid.NewGuid(),
            "Payments API",
            Guid.NewGuid(),
            "dev@acme.com",
            "OrderService",
            "2.1.0",
            SubscriptionLevel.AllChanges,
            NotificationChannel.Email,
            webhookUrl: null,
            Now);

        return result.Value;
    }

    #endregion
}
