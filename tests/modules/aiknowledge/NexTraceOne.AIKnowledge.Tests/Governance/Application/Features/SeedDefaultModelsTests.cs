using System.Linq;
using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Features.SeedDefaultModels;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Application.Features;

public sealed class SeedDefaultModelsTests
{
    private readonly IAiModelRepository _modelRepository = Substitute.For<IAiModelRepository>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    private SeedDefaultModels.Handler CreateHandler() =>
        new(_modelRepository, _dateTimeProvider);

    [Fact]
    public async Task Handle_EmptyRegistry_SeedsAllCatalogModels()
    {
        // Arrange
        _modelRepository.ListAsync(null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AIModel>());
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new SeedDefaultModels.Command(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ModelsSeeded.Should().Be(DefaultModelCatalog.GetAll().Count);
        result.Value.TotalInCatalog.Should().Be(DefaultModelCatalog.GetAll().Count);

        await _modelRepository.Received(DefaultModelCatalog.GetAll().Count)
            .AddAsync(Arg.Any<AIModel>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AllModelsExist_SeedsNothing()
    {
        // Arrange
        var existingModels = DefaultModelCatalog.GetAll()
            .Select(def => AIModel.Register(
                def.Name, def.DisplayName, def.Provider,
                def.ModelType, def.IsInternal, def.Capabilities,
                def.SensitivityLevel, DateTimeOffset.UtcNow))
            .ToList();

        _modelRepository.ListAsync(null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(existingModels);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new SeedDefaultModels.Command(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ModelsSeeded.Should().Be(0);
        result.Value.TotalInCatalog.Should().Be(DefaultModelCatalog.GetAll().Count);

        await _modelRepository.DidNotReceive()
            .AddAsync(Arg.Any<AIModel>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PartialModelsExist_SeedsOnlyMissing()
    {
        // Arrange — create only the first model from catalog
        var firstDef = DefaultModelCatalog.GetAll()[0];
        var existingModels = new[]
        {
            AIModel.Register(
                firstDef.Name, firstDef.DisplayName, firstDef.Provider,
                firstDef.ModelType, firstDef.IsInternal, firstDef.Capabilities,
                firstDef.SensitivityLevel, DateTimeOffset.UtcNow)
        };

        _modelRepository.ListAsync(null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(existingModels);
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new SeedDefaultModels.Command(), CancellationToken.None);

        // Assert
        var expectedSeeded = DefaultModelCatalog.GetAll().Count - 1;
        result.IsSuccess.Should().BeTrue();
        result.Value.ModelsSeeded.Should().Be(expectedSeeded);

        await _modelRepository.Received(expectedSeeded)
            .AddAsync(Arg.Any<AIModel>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CaseInsensitive_ModelNameMatch()
    {
        // Arrange — create model with different casing
        var firstDef = DefaultModelCatalog.GetAll()[0];
        var existingModels = new[]
        {
            AIModel.Register(
                firstDef.Name.ToUpperInvariant(), firstDef.DisplayName, firstDef.Provider,
                firstDef.ModelType, firstDef.IsInternal, firstDef.Capabilities,
                firstDef.SensitivityLevel, DateTimeOffset.UtcNow)
        };

        _modelRepository.ListAsync(null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(existingModels);
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new SeedDefaultModels.Command(), CancellationToken.None);

        // Assert — should not duplicate the model despite different casing
        var expectedSeeded = DefaultModelCatalog.GetAll().Count - 1;
        result.IsSuccess.Should().BeTrue();
        result.Value.ModelsSeeded.Should().Be(expectedSeeded);
    }

    [Fact]
    public async Task Handle_IsIdempotent_SecondCallSeedsNothing()
    {
        // Arrange
        var capturedModels = new List<AIModel>();
        _modelRepository.ListAsync(null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(
                _ => Task.FromResult<IReadOnlyList<AIModel>>(Array.Empty<AIModel>()),
                _ => Task.FromResult<IReadOnlyList<AIModel>>(capturedModels));

        _modelRepository.When(r => r.AddAsync(Arg.Any<AIModel>(), Arg.Any<CancellationToken>()))
            .Do(callInfo => capturedModels.Add(callInfo.Arg<AIModel>()));

        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);

        var handler = CreateHandler();

        // Act — first call
        var result1 = await handler.Handle(new SeedDefaultModels.Command(), CancellationToken.None);

        // Act — second call
        var result2 = await handler.Handle(new SeedDefaultModels.Command(), CancellationToken.None);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result1.Value.ModelsSeeded.Should().BeGreaterThan(0);

        result2.IsSuccess.Should().BeTrue();
        result2.Value.ModelsSeeded.Should().Be(0, "second call should be idempotent");
    }

    [Fact]
    public async Task Handle_SetsRegisteredAtFromDateTimeProvider()
    {
        // Arrange
        var expectedTime = new DateTimeOffset(2026, 3, 29, 12, 0, 0, TimeSpan.Zero);
        _dateTimeProvider.UtcNow.Returns(expectedTime);
        _modelRepository.ListAsync(null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AIModel>());

        AIModel? capturedModel = null;
        _modelRepository.When(r => r.AddAsync(Arg.Any<AIModel>(), Arg.Any<CancellationToken>()))
            .Do(callInfo => capturedModel ??= callInfo.Arg<AIModel>());

        var handler = CreateHandler();

        // Act
        await handler.Handle(new SeedDefaultModels.Command(), CancellationToken.None);

        // Assert
        capturedModel.Should().NotBeNull();
        capturedModel!.RegisteredAt.Should().Be(expectedTime);
    }
}
