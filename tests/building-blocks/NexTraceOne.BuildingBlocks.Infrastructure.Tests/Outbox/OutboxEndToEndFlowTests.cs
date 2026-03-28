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
/// Teste end-to-end do fluxo outbox:
/// domain event -> outbox message -> ModuleOutboxProcessorJob -> integration event handler.
/// </summary>
public sealed class OutboxEndToEndFlowTests
{
    [Fact]
    public async Task OutboxProcessor_ShouldDeliverDomainIntegrationEvent_ToRegisteredHandler()
    {
        var databaseName = $"outbox-e2e-{Guid.NewGuid():N}";
        var handler = new RecordingEventHandler();

        var services = new ServiceCollection();
        services.AddSingleton<IDateTimeProvider, TestDateTimeProvider>();
        services.AddSingleton<ICurrentTenant, TestCurrentTenant>();
        services.AddSingleton<ICurrentUser, TestCurrentUser>();
        services.AddSingleton(handler);
        services.AddSingleton<WorkerJobHealthRegistry>();
        services.AddScoped<IEventBus, TestEventBus>();
        services.AddScoped<IIntegrationEventHandler<SampleIntegrationEvent>>(sp => sp.GetRequiredService<RecordingEventHandler>());
        services.AddDbContext<TestOutboxDbContext>(options => options.UseInMemoryDatabase(databaseName));

        using var provider = services.BuildServiceProvider();

        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestOutboxDbContext>();
            var aggregate = TestOutboxAggregate.Create("svc-payments", "production");
            db.Aggregates.Add(aggregate);
            await db.SaveChangesAsync();
        }

        {
            var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
            var clock = provider.GetRequiredService<IDateTimeProvider>();
            var registry = provider.GetRequiredService<WorkerJobHealthRegistry>();
            var logger = NullLogger<ModuleOutboxProcessorJob<TestOutboxDbContext>>.Instance;
            var processor = new ModuleOutboxProcessorJob<TestOutboxDbContext>(scopeFactory, clock, registry, logger);

            var processMethod = typeof(ModuleOutboxProcessorJob<TestOutboxDbContext>)
                .GetMethod("ProcessOutboxAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            processMethod.Should().NotBeNull();
            var task = (Task)processMethod!
                .Invoke(processor, [CancellationToken.None])!;
            await task;
        }

        handler.ReceivedEvents.Should().ContainSingle();
        var received = handler.ReceivedEvents.Single();
        received.ServiceName.Should().Be("svc-payments");
        received.Environment.Should().Be("production");

        using (var verifyScope = provider.CreateScope())
        {
            var db = verifyScope.ServiceProvider.GetRequiredService<TestOutboxDbContext>();
            var outbox = await db.Set<OutboxMessage>().SingleAsync();
            outbox.ProcessedAt.Should().NotBeNull();
            outbox.RetryCount.Should().Be(0);
            outbox.LastError.Should().BeNull();
        }
    }

    private sealed class TestOutboxDbContext(
        DbContextOptions<TestOutboxDbContext> options,
        ICurrentTenant tenant,
        ICurrentUser user,
        IDateTimeProvider clock) : NexTraceDbContextBase(options, tenant, user, clock), IUnitOfWork
    {
        public DbSet<TestOutboxAggregate> Aggregates => Set<TestOutboxAggregate>();

        protected override System.Reflection.Assembly ConfigurationsAssembly => typeof(TestOutboxDbContext).Assembly;
        protected override string OutboxTableName => "tst_outbox_messages";

        public Task<int> CommitAsync(CancellationToken cancellationToken = default) => SaveChangesAsync(cancellationToken);
    }

    private sealed class TestOutboxAggregate : AggregateRoot<TestOutboxAggregateId>
    {
        private TestOutboxAggregate() { }

        public string ServiceName { get; private set; } = string.Empty;
        public string Environment { get; private set; } = string.Empty;

        public static TestOutboxAggregate Create(string serviceName, string environment)
        {
            var aggregate = new TestOutboxAggregate
            {
                Id = TestOutboxAggregateId.New(),
                ServiceName = serviceName,
                Environment = environment
            };

            aggregate.RaiseDomainEvent(new SampleIntegrationEvent(serviceName, environment));
            return aggregate;
        }
    }

    private sealed record TestOutboxAggregateId(Guid Value) : TypedIdBase(Value)
    {
        public static TestOutboxAggregateId New() => new(Guid.NewGuid());
        public static TestOutboxAggregateId From(Guid id) => new(id);
    }

    private sealed record SampleIntegrationEvent(string ServiceName, string Environment) : DomainEventBase;

    private sealed class RecordingEventHandler : IIntegrationEventHandler<SampleIntegrationEvent>
    {
        public List<SampleIntegrationEvent> ReceivedEvents { get; } = [];

        public Task HandleAsync(SampleIntegrationEvent @event, CancellationToken ct = default)
        {
            ReceivedEvents.Add(@event);
            return Task.CompletedTask;
        }
    }

    private sealed class TestEventBus(IServiceProvider serviceProvider) : IEventBus
    {
        public async Task PublishAsync<T>(T integrationEvent, CancellationToken ct = default) where T : class
        {
            var handlers = serviceProvider.GetServices<IIntegrationEventHandler<T>>().ToArray();
            foreach (var handler in handlers)
            {
                await handler.HandleAsync(integrationEvent, ct);
            }
        }
    }

    private sealed class TestCurrentTenant : ICurrentTenant
    {
        public Guid Id { get; } = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        public string Slug { get; } = "test";
        public string Name { get; } = "Test Tenant";
        public bool IsActive { get; } = true;
        public bool HasCapability(string capability) => true;
    }

    private sealed class TestCurrentUser : ICurrentUser
    {
        public string Id { get; } = "outbox-test-user";
        public string Name { get; } = "Outbox Test";
        public string Email { get; } = "outbox.test@nextraceone.local";
        public bool IsAuthenticated { get; } = true;
        public bool HasPermission(string permission) => true;
    }

    private sealed class TestDateTimeProvider : IDateTimeProvider
    {
        private static readonly DateTimeOffset FixedNow = new(2026, 03, 28, 12, 00, 00, TimeSpan.Zero);
        public DateTimeOffset UtcNow => FixedNow;
        public DateOnly UtcToday => DateOnly.FromDateTime(FixedNow.UtcDateTime);
    }
}
