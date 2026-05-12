using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;
using NexTraceOne.AIKnowledge.Application.Governance.Services;
using NexTraceOne.Catalog.Contracts.Graph.ServiceInterfaces;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Services;

/// <summary>
/// Testes unitários para o GroundingEvaluator (W4-04).
/// Cobre: avaliação com entidades encontradas, não encontradas, alucinações e casos limite.
/// </summary>
public class GroundingEvaluatorTests
{
    private readonly ICatalogGraphModule _catalogGraph;
    private readonly GroundingEvaluator _evaluator;

    public GroundingEvaluatorTests()
    {
        _catalogGraph = Substitute.For<ICatalogGraphModule>();
        _evaluator = new GroundingEvaluator(_catalogGraph, NullLogger<GroundingEvaluator>.Instance);
    }

    [Fact]
    public async Task EvaluateAsync_WhenContentHasNoEntities_ShouldReturnNeutralScore()
    {
        // Arrange
        var response = new AiResponse("Esta é uma resposta genérica sem menção a serviços ou contratos.");

        // Act
        var result = await _evaluator.EvaluateAsync(response, CancellationToken.None);

        // Assert
        result.GroundingScore.Should().Be(0.5m);
        result.EntitiesFound.Should().BeEmpty();
        result.EntitiesNotFound.Should().BeEmpty();
        result.HasHallucinations.Should().BeFalse();
        result.ConfidenceLevel.Should().Be("medium");
    }

    [Fact]
    public async Task EvaluateAsync_WhenAllEntitiesExist_ShouldReturnHighScore()
    {
        // Arrange
        var response = new AiResponse("O serviço UserService e o serviço PaymentService estão configurados corretamente.");
        _catalogGraph.ServiceAssetExistsAsync("UserService", Arg.Any<CancellationToken>()).Returns(true);
        _catalogGraph.ServiceAssetExistsAsync("PaymentService", Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _evaluator.EvaluateAsync(response, CancellationToken.None);

        // Assert
        result.GroundingScore.Should().Be(1.0m);
        result.EntitiesFound.Should().HaveCount(2);
        result.EntitiesNotFound.Should().BeEmpty();
        result.HasHallucinations.Should().BeFalse();
        result.ConfidenceLevel.Should().Be("high");
    }

    [Fact]
    public async Task EvaluateAsync_WhenSomeEntitiesNotFound_ShouldReturnMediumScore()
    {
        // Arrange
        var response = new AiResponse("O serviço UserService existe mas o serviço NonExistentService não foi encontrado.");
        _catalogGraph.ServiceAssetExistsAsync("UserService", Arg.Any<CancellationToken>()).Returns(true);
        _catalogGraph.ServiceAssetExistsAsync("NonExistentService", Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await _evaluator.EvaluateAsync(response, CancellationToken.None);

        // Assert
        result.GroundingScore.Should().Be(0.5m);
        result.EntitiesFound.Should().Contain("UserService");
        result.EntitiesNotFound.Should().Contain("NonExistentService");
        result.HasHallucinations.Should().BeFalse();
        result.ConfidenceLevel.Should().Be("medium");
    }

    [Fact]
    public async Task EvaluateAsync_WhenMostEntitiesNotFound_ShouldIndicateHallucinations()
    {
        // Arrange
        var response = new AiResponse("O serviço FakeServiceAlpha e o service FakeServiceBeta e o api FakeServiceGamma foram mencionados.");
        _catalogGraph.ServiceAssetExistsAsync("FakeServiceAlpha", Arg.Any<CancellationToken>()).Returns(false);
        _catalogGraph.ServiceAssetExistsAsync("FakeServiceBeta", Arg.Any<CancellationToken>()).Returns(false);
        _catalogGraph.ServiceAssetExistsAsync("FakeServiceGamma", Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await _evaluator.EvaluateAsync(response, CancellationToken.None);

        // Assert
        result.GroundingScore.Should().Be(0m);
        result.EntitiesFound.Should().BeEmpty();
        result.EntitiesNotFound.Should().HaveCount(3);
        result.HasHallucinations.Should().BeTrue();
        result.ConfidenceLevel.Should().Be("low");
    }

    [Fact]
    public async Task EvaluateAsync_WhenContentIsEmpty_ShouldReturnNeutralScore()
    {
        // Arrange
        var response = new AiResponse(string.Empty);

        // Act
        var result = await _evaluator.EvaluateAsync(response, CancellationToken.None);

        // Assert
        result.GroundingScore.Should().Be(0m);
        result.HasHallucinations.Should().BeFalse();
        result.ConfidenceLevel.Should().Be("low");
    }

    [Fact]
    public async Task EvaluateAsync_WhenContractMentioned_ShouldExtractEntityName()
    {
        // Arrange
        var response = new AiResponse("O contrato UserAPIContract está ativo.");
        _catalogGraph.ServiceAssetExistsAsync("UserAPIContract", Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await _evaluator.EvaluateAsync(response, CancellationToken.None);

        // Assert
        result.EntitiesNotFound.Should().Contain("UserAPIContract");
    }

    [Fact]
    public async Task EvaluateAsync_WhenMixedEntities_ShouldCalculateCorrectScore()
    {
        // Arrange
        var response = new AiResponse(
            "O serviço AuthService e o contract AuthContract estão configurados. " +
            "O serviço InventoryService também está disponível.");

        _catalogGraph.ServiceAssetExistsAsync("AuthService", Arg.Any<CancellationToken>()).Returns(true);
        _catalogGraph.ServiceAssetExistsAsync("AuthContract", Arg.Any<CancellationToken>()).Returns(false);
        _catalogGraph.ServiceAssetExistsAsync("InventoryService", Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _evaluator.EvaluateAsync(response, CancellationToken.None);

        // Assert
        result.GroundingScore.Should().BeApproximately(0.67m, 0.01m); // 2/3 encontrados
        result.EntitiesFound.Should().HaveCount(2);
        result.EntitiesNotFound.Should().HaveCount(1);
        result.ConfidenceLevel.Should().Be("medium");
    }

    [Fact]
    public async Task EvaluateAsync_WhenLowScoreWithMissingEntities_ShouldFlagAsHallucination()
    {
        // Arrange
        var response = new AiResponse("O serviço ImaginaryServiceAlpha e o service ImaginaryServiceBeta estão em produção.");
        _catalogGraph.ServiceAssetExistsAsync("ImaginaryServiceAlpha", Arg.Any<CancellationToken>()).Returns(false);
        _catalogGraph.ServiceAssetExistsAsync("ImaginaryServiceBeta", Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await _evaluator.EvaluateAsync(response, CancellationToken.None);

        // Assert
        result.GroundingScore.Should().Be(0m);
        result.HasHallucinations.Should().BeTrue();
        result.ConfidenceLevel.Should().Be("low");
    }
}
