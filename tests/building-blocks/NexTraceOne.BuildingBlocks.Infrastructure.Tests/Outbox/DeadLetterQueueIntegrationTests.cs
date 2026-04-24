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
/// Testes de integração: ModuleOutboxProcessorJob + DLQ.
/// Verifica que mensagens exaustas são persistidas na DLQ e o outbox é limpo.
/// </summary>
public sealed class DeadLetterQueueIntegrationTests
{
    private const int MaxRetryCount = 5;

    // ── Cenário 1: mensagem persistida na DLQ após esgotar retries ───────────

    [Fact]
    public async Task OutboxProcessor_WhenHandlerAlwaysFails_ShouldPersistMessageToDlq()
    {
        var (provider, processor) = BuildTestEnvironment(alwaysFail: true);

        await CreateAggregateAsync(provider);

        // Executar MaxRetryCount ciclos para esgotar as tentativas.
        for (var i = 0; i < MaxRetryCount; i++)
            await InvokeProcessOutboxAsync(processor);

        // A DLQ deve ter exactamente uma mensagem.
        using var scope = provider.CreateScope();
        var dlqDb = scope.ServiceProvider.GetRequiredService<BuildingBlocksDbContext>();
        var dlqMessages = await dlqDb.DeadLetterMessages.ToListAsync();

        dlqMessages.Should().ContainSingle("exatamente uma mensagem deve ter sido persistida na DLQ");
        var dlqMsg = dlqMessages.Single();
        dlqMsg.Status.Should().Be(DlqMessageStatus.Pending);
        dlqMsg.AttemptCount.Should().Be(MaxRetryCount);
        dlqMsg.ExhaustedAt.Should().NotBe(default);
        dlqMsg.TenantId.Should().Be(TestTenantId);

        provider.Dispose();
    }

    // ── Cenário 2: outbox mantém auditoria após DLQ (ProcessedAt = null) ─────

    [Fact]
    public async Task OutboxProcessor_WhenMessageMovedToDlq_ShouldKeepOutboxMessageForAudit()
    {
        var (provider, processor) = BuildTestEnvironment(alwaysFail: true);

        await CreateAggregateAsync(provider);

        for (var i = 0; i < MaxRetryCount; i++)
            await InvokeProcessOutboxAsync(processor);

        // A mensagem outbox permanece como registo de auditoria (ProcessedAt = null).
        // Não será re-selecionada pois RetryCount >= MaxRetryCount.
        using var scope = provider.CreateScope();
        var outboxDb = scope.ServiceProvider.GetRequiredService<DlqTestDbContext>();
        var outboxMsg = await outboxDb.Set<OutboxMessage>().SingleAsync();

        outboxMsg.ProcessedAt.Should().BeNull(
            "outbox message permanece para auditoria; a DLQ é o registo de falha definitivo");
        outboxMsg.RetryCount.Should().Be(MaxRetryCount,
            "RetryCount deve estar esgotado");

        provider.Dispose();
    }

    // ── Cenário 3: mensagem de sucesso NÃO vai para a DLQ ────────────────────

    [Fact]
    public async Task OutboxProcessor_WhenHandlerSucceeds_ShouldNotCreateDlqEntry()
    {
        var (provider, processor) = BuildTestEnvironment(alwaysFail: false);

        await CreateAggregateAsync(provider);
        await InvokeProcessOutboxAsync(processor);

        using var scope = provider.CreateScope();
        var dlqDb = scope.ServiceProvider.GetRequiredService<BuildingBlocksDbContext>();
        var count = await dlqDb.DeadLetterMessages.CountAsync();

        count.Should().Be(0, "mensagens entregues com sucesso não devem ir para a DLQ");

        provider.Dispose();
    }

    // ── Cenário 4: retry transiente não cria DLQ prematuramente ──────────────

    [Fact]
    public async Task OutboxProcessor_WhenHandlerFailsOnceThenSucceeds_ShouldNotCreateDlqEntry()
    {
        var (provider, processor) = BuildTestEnvironment(alwaysFail: false, failFirstN: 1);

        await CreateAggregateAsync(provider);

        // Primeiro ciclo: falha.
        await InvokeProcessOutboxAsync(processor);

        // Ainda não deve haver DLQ.
        using (var scope = provider.CreateScope())
        {
            var dlqDb = scope.ServiceProvider.GetRequiredService<BuildingBlocksDbContext>();
            (await dlqDb.DeadLetterMessages.CountAsync())
                .Should().Be(0, "uma única falha não deve criar entrada na DLQ");
        }

        // Segundo ciclo: sucesso.
        await InvokeProcessOutboxAsync(processor);

        // Ainda não deve haver DLQ.
        using (var scope2 = provider.CreateScope())
        {
            var dlqDb = scope2.ServiceProvider.GetRequiredService<BuildingBlocksDbContext>();
            (await dlqDb.DeadLetterMessages.CountAsync())
                .Should().Be(0, "mensagem entregue não deve gerar entrada na DLQ");
        }

        provider.Dispose();
    }

    // ── Cenário 5: DLQ contém dados correctos de MessageType e Payload ───────

    [Fact]
    public async Task OutboxProcessor_WhenMessageExhausted_DlqEntryShouldContainMessageTypeAndPayload()
    {
        var (provider, processor) = BuildTestEnvironment(alwaysFail: true);

        await CreateAggregateAsync(provider);

        for (var i = 0; i < MaxRetryCount; i++)
            await InvokeProcessOutboxAsync(processor);

        using var scope = provider.CreateScope();
        var dlqDb = scope.ServiceProvider.GetRequiredService<BuildingBlocksDbContext>();
        var dlqMsg = await dlqDb.DeadLetterMessages.SingleAsync();

        dlqMsg.MessageType.Should().NotBeNullOrEmpty("MessageType deve ser copiado do OutboxMessage");
        dlqMsg.Payload.Should().NotBeNullOrEmpty("Payload deve ser copiado do OutboxMessage");
        dlqMsg.LastException.Should().NotBeNullOrEmpty("LastException deve conter a mensagem da excepção");
        dlqMsg.FailureReason.Should().NotBeNullOrEmpty("FailureReason deve estar preenchido");

        provider.Dispose();
    }

    // ── Test infrastructure ──────────────────────────────────────────────────

    private static readonly Guid TestTenantId = Guid.Parse("dddddddd-0000-0000-0000-000000000001");

    private static (ServiceProvider provider, ModuleOutboxProcessorJob<DlqTestDbContext> processor)
        BuildTestEnvironment(bool alwaysFail, int failFirstN = int.MaxValue)
    {
        var dbName = $"dlq-test-{Guid.NewGuid():N}";
        var dlqDbName = $"dlq-bb-{Guid.NewGuid():N}";
        var handler = alwaysFail
            ? new DlqTestHandler(failFirstNTimes: int.MaxValue)
            : new DlqTestHandler(failFirstNTimes: failFirstN);

        var services = new ServiceCollection();
        services.AddSingleton<IDateTimeProvider, DlqFixedDateTimeProvider>();
        services.AddSingleton<ICurrentTenant, DlqTestTenant>();
        services.AddSingleton<ICurrentUser, DlqTestUser>();
        services.AddSingleton(handler);
        services.AddSingleton<WorkerJobHealthRegistry>();
        services.AddScoped<IEventBus, DlqBroadcastEventBus>();
        services.AddScoped<IIntegrationEventHandler<DlqIntegrationEvent>>(
            sp => sp.GetRequiredService<DlqTestHandler>());

        services.AddDbContext<DlqTestDbContext>(opts => opts.UseInMemoryDatabase(dbName));
        services.AddDbContext<BuildingBlocksDbContext>(opts => opts.UseInMemoryDatabase(dlqDbName));
        services.AddScoped<IDeadLetterRepository, DeadLetterRepository>();

        var provider = services.BuildServiceProvider();

        var processor = new ModuleOutboxProcessorJob<DlqTestDbContext>(
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<IDateTimeProvider>(),
            provider.GetRequiredService<WorkerJobHealthRegistry>(),
            NullLogger<ModuleOutboxProcessorJob<DlqTestDbContext>>.Instance);

        return (provider, processor);
    }

    private static async Task CreateAggregateAsync(ServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DlqTestDbContext>();
        db.Aggregates.Add(DlqTestAggregate.Create("svc-test", "production"));
        await db.SaveChangesAsync();
    }

    private static async Task InvokeProcessOutboxAsync(
        ModuleOutboxProcessorJob<DlqTestDbContext> processor)
    {
        var method = typeof(ModuleOutboxProcessorJob<DlqTestDbContext>)
            .GetMethod("ProcessOutboxAsync",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        await (Task)method.Invoke(processor, [CancellationToken.None])!;
    }

    // ── Domain types ─────────────────────────────────────────────────────────

    private sealed record DlqIntegrationEvent(string Service, string Env) : DomainEventBase;

    private sealed class DlqTestAggregate : AggregateRoot<DlqTestAggregateId>
    {
        private DlqTestAggregate() { }

        public string Service { get; private set; } = string.Empty;
        public string Env { get; private set; } = string.Empty;

        public static DlqTestAggregate Create(string service, string env)
        {
            var agg = new DlqTestAggregate
            {
                Id = DlqTestAggregateId.New(),
                Service = service,
                Env = env
            };
            agg.RaiseDomainEvent(new DlqIntegrationEvent(service, env));
            return agg;
        }
    }

    private sealed record DlqTestAggregateId(Guid Value) : TypedIdBase(Value)
    {
        public static DlqTestAggregateId New() => new(Guid.NewGuid());
        public static DlqTestAggregateId From(Guid id) => new(id);
    }

    // ── DbContext ─────────────────────────────────────────────────────────────

    private sealed class DlqTestDbContext(
        DbContextOptions<DlqTestDbContext> options,
        ICurrentTenant tenant,
        ICurrentUser user,
        IDateTimeProvider clock) : NexTraceDbContextBase(options, tenant, user, clock), IUnitOfWork
    {
        public DbSet<DlqTestAggregate> Aggregates => Set<DlqTestAggregate>();

        protected override System.Reflection.Assembly ConfigurationsAssembly =>
            typeof(DlqTestDbContext).Assembly;

        protected override string OutboxTableName => "dlq_test_outbox_messages";

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DlqTestAggregate>(b =>
            {
                b.ToTable("dlq_test_aggregates");
                b.HasKey(x => x.Id);
                b.Property(x => x.Id)
                    .HasConversion(id => id.Value, v => DlqTestAggregateId.From(v));
                b.Property(x => x.Service).HasMaxLength(200).IsRequired();
                b.Property(x => x.Env).HasMaxLength(100).IsRequired();
            });

            base.OnModelCreating(modelBuilder);
        }

        public Task<int> CommitAsync(CancellationToken ct = default) => SaveChangesAsync(ct);
    }

    // ── Handlers & fakes ─────────────────────────────────────────────────────

    private sealed class DlqTestHandler(int failFirstNTimes) : IIntegrationEventHandler<DlqIntegrationEvent>
    {
        private int _calls;

        public Task HandleAsync(DlqIntegrationEvent @event, CancellationToken ct = default)
        {
            _calls++;
            if (_calls <= failFirstNTimes)
                throw new InvalidOperationException($"Simulated handler failure #{_calls}");
            return Task.CompletedTask;
        }
    }

    private sealed class DlqBroadcastEventBus(IServiceProvider sp) : IEventBus
    {
        public async Task PublishAsync<T>(T evt, CancellationToken ct = default) where T : class
        {
            foreach (var h in sp.GetServices<IIntegrationEventHandler<T>>())
                await h.HandleAsync(evt, ct);
        }
    }

    private sealed class DlqTestTenant : ICurrentTenant
    {
        public Guid Id { get; } = TestTenantId;
        public string Slug { get; } = "dlq-test";
        public string Name { get; } = "DLQ Test Tenant";
        public bool IsActive { get; } = true;
        public bool HasCapability(string capability) => true;
    }

    private sealed class DlqTestUser : ICurrentUser
    {
        public string Id { get; } = "dlq-test-user";
        public string Name { get; } = "DLQ Test User";
        public string Email { get; } = "dlq@nextraceone.local";
        public string? Persona { get; } = null;
        public bool IsAuthenticated { get; } = true;
        public bool HasPermission(string permission) => true;
    }

    private sealed class DlqFixedDateTimeProvider : IDateTimeProvider
    {
        private static readonly DateTimeOffset Fixed =
            new(2026, 04, 23, 10, 00, 00, TimeSpan.Zero);

        public DateTimeOffset UtcNow => Fixed;
        public DateOnly UtcToday => DateOnly.FromDateTime(Fixed.UtcDateTime);
    }
}
