using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using NexTraceOne.BackgroundWorkers;
using NexTraceOne.BackgroundWorkers.Jobs;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Core.Events;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.BuildingBlocks.Infrastructure.EventBus.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Outbox;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;

namespace NexTraceOne.BuildingBlocks.Infrastructure.Tests.Outbox;

/// <summary>
/// Cenários críticos de comunicação cross-module via Outbox Pattern.
///
/// Cenário 1 — Happy path: coberto em OutboxEndToEndFlowTests.
/// Cenário 2 — Retry after transient failure: handler lança exceção na primeira chamada
///             e tem sucesso na segunda chamada (segundo ciclo do processor).
/// Cenário 3 — Exhausted retries / dead-letter: handler sempre falha; após MaxRetryCount
///             tentativas a mensagem permanece com ProcessedAt = null e nunca mais é
///             selecionada pelo processor.
/// </summary>
public sealed class OutboxCrossModuleScenariosTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // Cenário 2 — Retry after transient handler failure
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Cenário 2: o handler de integração falha de forma transiente na primeira tentativa
    /// e tem sucesso na segunda. Após dois ciclos do processor:
    /// - RetryCount deve ser 1 (houve exatamente uma falha antes do sucesso)
    /// - ProcessedAt deve estar preenchido
    /// - LastError deve ser null (limpo ao processar com sucesso)
    /// </summary>
    [Fact]
    public async Task OutboxProcessor_WhenHandlerFailsOnce_ShouldRetryAndSucceed()
    {
        var databaseName = $"outbox-retry-{Guid.NewGuid():N}";
        var handler = new FlakyEventHandler(failFirstNTimes: 1);

        var (provider, processor) = BuildProviderAndProcessor(databaseName, handler);

        // Arrange: criar aggregate que emite um evento de integração.
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CrossModuleTestDbContext>();
            var aggregate = CrossModuleAggregate.Create("service-a", "staging");
            db.Aggregates.Add(aggregate);
            await db.SaveChangesAsync();
        }

        // Act — primeiro ciclo: handler lança exceção.
        await InvokeProcessOutboxAsync(processor);

        // Assert intermediário: mensagem deve ter RetryCount = 1 e ProcessedAt = null.
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CrossModuleTestDbContext>();
            var msg = await db.Set<OutboxMessage>().SingleAsync();
            msg.RetryCount.Should().Be(1, "handler failed once so RetryCount should be 1");
            msg.ProcessedAt.Should().BeNull("message has not been successfully processed yet");
            msg.LastError.Should().NotBeNullOrEmpty("the failed attempt should record an error");
        }

        // Act — segundo ciclo: handler tem sucesso.
        await InvokeProcessOutboxAsync(processor);

        // Assert final: mensagem deve estar processada com sucesso.
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CrossModuleTestDbContext>();
            var msg = await db.Set<OutboxMessage>().SingleAsync();
            msg.ProcessedAt.Should().NotBeNull("message was successfully processed on the second attempt");
            msg.RetryCount.Should().Be(1, "only one failure occurred before success");
            msg.LastError.Should().BeNull("LastError is cleared on successful processing");
        }

        // Assert: handler recebeu exactamente um evento (na tentativa com sucesso).
        handler.SuccessfulDeliveries.Should().Be(1,
            "the integration event should be delivered exactly once");

        provider.Dispose();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Cenário 3 — Exhausted retries (dead-letter behaviour)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Cenário 3: o handler de integração falha em todas as tentativas.
    /// Após MaxRetryCount (5) ciclos do processor, a mensagem deve:
    /// - ter RetryCount >= 5
    /// - ter ProcessedAt = null (nunca foi processada com sucesso)
    /// - ser ignorada em ciclos subsequentes (não é re-selecionada pelo processor)
    /// </summary>
    [Fact]
    public async Task OutboxProcessor_WhenHandlerAlwaysFails_ShouldExhaustRetriesAndStop()
    {
        const int MaxRetryCount = 5; // deve corresponder ao valor em ModuleOutboxProcessorJob

        var databaseName = $"outbox-exhaust-{Guid.NewGuid():N}";
        var handler = new FlakyEventHandler(failFirstNTimes: int.MaxValue); // sempre falha

        var (provider, processor) = BuildProviderAndProcessor(databaseName, handler);

        // Arrange: criar aggregate.
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CrossModuleTestDbContext>();
            var aggregate = CrossModuleAggregate.Create("service-b", "production");
            db.Aggregates.Add(aggregate);
            await db.SaveChangesAsync();
        }

        // Act — executar MaxRetryCount ciclos para esgotar as tentativas.
        for (var i = 0; i < MaxRetryCount; i++)
        {
            await InvokeProcessOutboxAsync(processor);
        }

        // Assert: mensagem deve estar marcada como esgotada.
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CrossModuleTestDbContext>();
            var msg = await db.Set<OutboxMessage>().SingleAsync();
            msg.RetryCount.Should().BeGreaterThanOrEqualTo(MaxRetryCount,
                "all retry attempts should be exhausted");
            msg.ProcessedAt.Should().BeNull(
                "message was never successfully processed");
            msg.LastError.Should().NotBeNullOrEmpty(
                "the last failure reason should be recorded");
        }

        // Act — executar um ciclo adicional após esgotar tentativas.
        await InvokeProcessOutboxAsync(processor);

        // Assert: o handler não deve ter sido chamado com sucesso em nenhuma tentativa.
        handler.SuccessfulDeliveries.Should().Be(0,
            "a permanently failing handler should never deliver the event successfully");

        // Assert: o RetryCount não deve aumentar após atingir MaxRetryCount,
        // porque o processor não seleciona mais mensagens com RetryCount >= MaxRetryCount.
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CrossModuleTestDbContext>();
            var msg = await db.Set<OutboxMessage>().SingleAsync();
            msg.RetryCount.Should().Be(MaxRetryCount,
                "no further processing should occur after max retries are exhausted");
        }

        provider.Dispose();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Test infrastructure
    // ─────────────────────────────────────────────────────────────────────────

    private static (ServiceProvider provider, ModuleOutboxProcessorJob<CrossModuleTestDbContext> processor)
        BuildProviderAndProcessor(string databaseName, FlakyEventHandler handler)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IDateTimeProvider, FixedDateTimeProvider>();
        services.AddSingleton<ICurrentTenant, TestTenantForCrossModule>();
        services.AddSingleton<ICurrentUser, TestUserForCrossModule>();
        services.AddSingleton(handler);
        services.AddSingleton<WorkerJobHealthRegistry>();
        services.AddScoped<IEventBus, BroadcastEventBus>();
        services.AddScoped<IIntegrationEventHandler<CrossModuleIntegrationEvent>>(
            sp => sp.GetRequiredService<FlakyEventHandler>());
        services.AddDbContext<CrossModuleTestDbContext>(
            opts => opts.UseInMemoryDatabase(databaseName));

        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var clock = provider.GetRequiredService<IDateTimeProvider>();
        var registry = provider.GetRequiredService<WorkerJobHealthRegistry>();
        var logger = NullLogger<ModuleOutboxProcessorJob<CrossModuleTestDbContext>>.Instance;

        var processor = new ModuleOutboxProcessorJob<CrossModuleTestDbContext>(
            scopeFactory, clock, registry, logger);

        return (provider, processor);
    }

    private static async Task InvokeProcessOutboxAsync(
        ModuleOutboxProcessorJob<CrossModuleTestDbContext> processor)
    {
        var processMethod = typeof(ModuleOutboxProcessorJob<CrossModuleTestDbContext>)
            .GetMethod("ProcessOutboxAsync",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;

        await (Task)processMethod.Invoke(processor, [CancellationToken.None])!;
    }

    // ── Domain types ────────────────────────────────────────────────────────

    private sealed record CrossModuleIntegrationEvent(
        string SourceService,
        string Environment) : DomainEventBase;

    private sealed class CrossModuleAggregate : AggregateRoot<CrossModuleAggregateId>
    {
        private CrossModuleAggregate() { }

        public string SourceService { get; private set; } = string.Empty;
        public string Environment { get; private set; } = string.Empty;

        public static CrossModuleAggregate Create(string service, string environment)
        {
            var aggregate = new CrossModuleAggregate
            {
                Id = CrossModuleAggregateId.New(),
                SourceService = service,
                Environment = environment
            };
            aggregate.RaiseDomainEvent(
                new CrossModuleIntegrationEvent(service, environment));
            return aggregate;
        }
    }

    private sealed record CrossModuleAggregateId(Guid Value) : TypedIdBase(Value)
    {
        public static CrossModuleAggregateId New() => new(Guid.NewGuid());
        public static CrossModuleAggregateId From(Guid id) => new(id);
    }

    // ── DbContext ────────────────────────────────────────────────────────────

    private sealed class CrossModuleTestDbContext(
        DbContextOptions<CrossModuleTestDbContext> options,
        ICurrentTenant tenant,
        ICurrentUser user,
        IDateTimeProvider clock) : NexTraceDbContextBase(options, tenant, user, clock), IUnitOfWork
    {
        public DbSet<CrossModuleAggregate> Aggregates => Set<CrossModuleAggregate>();

        protected override System.Reflection.Assembly ConfigurationsAssembly =>
            typeof(CrossModuleTestDbContext).Assembly;

        protected override string OutboxTableName => "xmod_outbox_messages";

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CrossModuleAggregate>(b =>
            {
                b.ToTable("xmod_aggregates");
                b.HasKey(x => x.Id);
                b.Property(x => x.Id)
                    .HasConversion(id => id.Value, v => CrossModuleAggregateId.From(v));
                b.Property(x => x.SourceService).HasMaxLength(200).IsRequired();
                b.Property(x => x.Environment).HasMaxLength(100).IsRequired();
            });

            base.OnModelCreating(modelBuilder);
        }

        public Task<int> CommitAsync(CancellationToken cancellationToken = default) =>
            SaveChangesAsync(cancellationToken);
    }

    // ── Handlers & fakes ────────────────────────────────────────────────────

    /// <summary>
    /// Handler que falha nas primeiras <paramref name="failFirstNTimes"/> chamadas
    /// e depois tem sucesso. Permite simular falhas transientes e esgotamento de retries.
    /// </summary>
    private sealed class FlakyEventHandler(int failFirstNTimes)
        : IIntegrationEventHandler<CrossModuleIntegrationEvent>
    {
        private int _callCount;
        public int SuccessfulDeliveries { get; private set; }

        public Task HandleAsync(CrossModuleIntegrationEvent @event, CancellationToken ct = default)
        {
            _callCount++;
            if (_callCount <= failFirstNTimes)
                throw new InvalidOperationException(
                    $"Transient failure on call #{_callCount}.");

            SuccessfulDeliveries++;
            return Task.CompletedTask;
        }
    }

    private sealed class BroadcastEventBus(IServiceProvider serviceProvider) : IEventBus
    {
        public async Task PublishAsync<T>(T integrationEvent, CancellationToken ct = default)
            where T : class
        {
            foreach (var handler in serviceProvider.GetServices<IIntegrationEventHandler<T>>())
                await handler.HandleAsync(integrationEvent, ct);
        }
    }

    private sealed class TestTenantForCrossModule : ICurrentTenant
    {
        public Guid Id { get; } = Guid.Parse("11111111-2222-3333-4444-555555555555");
        public string Slug { get; } = "cross-module-test";
        public string Name { get; } = "Cross Module Test Tenant";
        public bool IsActive { get; } = true;
        public bool HasCapability(string capability) => true;
    }

    private sealed class TestUserForCrossModule : ICurrentUser
    {
        public string Id { get; } = "cross-module-user";
        public string Name { get; } = "Cross Module User";
        public string Email { get; } = "cross.module@nextraceone.local";
        public string? Persona { get; } = null;
        public bool IsAuthenticated { get; } = true;
        public bool HasPermission(string permission) => true;
    }

    private sealed class FixedDateTimeProvider : IDateTimeProvider
    {
        private static readonly DateTimeOffset Fixed =
            new(2026, 04, 04, 10, 00, 00, TimeSpan.Zero);

        public DateTimeOffset UtcNow => Fixed;
        public DateOnly UtcToday => DateOnly.FromDateTime(Fixed.UtcDateTime);
    }
}
