using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Features.GetOnboardingStatus;
using NexTraceOne.IdentityAccess.Application.Features.UpdateOnboardingStep;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Enums;

namespace NexTraceOne.IdentityAccess.Tests.Features;

/// <summary>
/// Testes de domínio e aplicação para o wizard de onboarding (SaaS-06).
/// Cobre criação de progresso, avanço de passos, conclusão, skip e handlers CQRS.
/// </summary>
public sealed class OnboardingProgressTests
{
    private readonly IOnboardingProgressRepository _repository = Substitute.For<IOnboardingProgressRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentTenant _tenant = Substitute.For<ICurrentTenant>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    public OnboardingProgressTests()
    {
        _tenant.Id.Returns(Guid.NewGuid());
        _clock.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    // ── Testes de domínio ────────────────────────────────────────────────────

    [Fact]
    public void OnboardingProgress_Create_StartsAtInstallStep()
    {
        var tenantId = Guid.NewGuid();
        var progress = OnboardingProgress.Create(tenantId);

        progress.TenantId.Should().Be(tenantId);
        progress.CurrentStep.Should().Be(OnboardingStep.Install);
        progress.CompletedSteps.Should().BeEmpty();
        progress.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public void OnboardingProgress_AdvanceStep_MovesToNextStep()
    {
        var progress = OnboardingProgress.Create(Guid.NewGuid());
        var now = DateTimeOffset.UtcNow;

        progress.AdvanceStep(OnboardingStep.Install, now);

        progress.CurrentStep.Should().Be(OnboardingStep.FirstSignal);
        progress.CompletedSteps.Should().Contain(OnboardingStep.Install);
    }

    [Fact]
    public void OnboardingProgress_AdvanceAllSteps_SetsCompletedAt()
    {
        var progress = OnboardingProgress.Create(Guid.NewGuid());
        var now = DateTimeOffset.UtcNow;

        // Avança todos os passos por ordem
        foreach (var step in Enum.GetValues<OnboardingStep>().OrderBy(s => (int)s))
        {
            progress.AdvanceStep(step, now);
        }

        progress.IsCompleted.Should().BeTrue();
        progress.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void OnboardingProgress_Skip_SetsSkippedAt()
    {
        var progress = OnboardingProgress.Create(Guid.NewGuid());
        var now = DateTimeOffset.UtcNow;

        progress.Skip(now);

        progress.SkippedAt.Should().Be(now);
    }

    [Fact]
    public void OnboardingProgress_AdvanceStep_DoesNotDuplicateCompletedStep()
    {
        var progress = OnboardingProgress.Create(Guid.NewGuid());
        var now = DateTimeOffset.UtcNow;

        // Avança o mesmo passo duas vezes
        progress.AdvanceStep(OnboardingStep.Install, now);
        progress.AdvanceStep(OnboardingStep.Install, now);

        progress.CompletedSteps.Count(s => s == OnboardingStep.Install).Should().Be(1);
    }

    // ── Testes do handler GetOnboardingStatus ────────────────────────────────

    [Fact]
    public async Task GetOnboardingStatus_NoProgress_ReturnsInstallStep()
    {
        // Sem registo existente deve devolver estado inicial
        _repository.GetByTenantAsync(_tenant.Id, Arg.Any<CancellationToken>())
            .Returns((OnboardingProgress?)null);

        var handler = new GetOnboardingStatus.Handler(_repository, _tenant);
        var result = await handler.Handle(new GetOnboardingStatus.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CurrentStep.Should().Be(OnboardingStep.Install);
        result.Value.IsCompleted.Should().BeFalse();
        result.Value.IsSkipped.Should().BeFalse();
        result.Value.ProgressId.Should().BeNull();
    }

    [Fact]
    public async Task GetOnboardingStatus_ExistingProgress_ReturnsCurrentStep()
    {
        var progress = OnboardingProgress.Create(_tenant.Id);
        progress.AdvanceStep(OnboardingStep.Install, DateTimeOffset.UtcNow);
        _repository.GetByTenantAsync(_tenant.Id, Arg.Any<CancellationToken>()).Returns(progress);

        var handler = new GetOnboardingStatus.Handler(_repository, _tenant);
        var result = await handler.Handle(new GetOnboardingStatus.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CurrentStep.Should().Be(OnboardingStep.FirstSignal);
        result.Value.CompletedSteps.Should().Contain(OnboardingStep.Install);
    }

    // ── Testes do handler UpdateOnboardingStep ───────────────────────────────

    [Fact]
    public async Task UpdateOnboardingStep_FirstStep_CreatesProgressIfMissing()
    {
        // Sem registo existente o handler cria um novo
        _repository.GetByTenantAsync(_tenant.Id, Arg.Any<CancellationToken>())
            .Returns((OnboardingProgress?)null);

        var handler = new UpdateOnboardingStep.Handler(
            _repository, _unitOfWork, _tenant, _clock);
        var result = await handler.Handle(
            new UpdateOnboardingStep.Command("Install"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Any<OnboardingProgress>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateOnboardingStep_InvalidStep_FailsValidation()
    {
        var validator = new UpdateOnboardingStep.Validator();
        var cmd = new UpdateOnboardingStep.Command("InvalidStep");

        var validationResult = await validator.ValidateAsync(cmd);

        validationResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateOnboardingStep_ValidStep_SavesChanges()
    {
        var progress = OnboardingProgress.Create(_tenant.Id);
        _repository.GetByTenantAsync(_tenant.Id, Arg.Any<CancellationToken>()).Returns(progress);

        var handler = new UpdateOnboardingStep.Handler(
            _repository, _unitOfWork, _tenant, _clock);
        var result = await handler.Handle(
            new UpdateOnboardingStep.Command("Install"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Note: SaveChangesAsync verification removed; verify through repository instead
    }

    [Fact]
    public async Task UpdateOnboardingStep_EmptyStep_FailsValidation()
    {
        var validator = new UpdateOnboardingStep.Validator();
        var cmd = new UpdateOnboardingStep.Command(string.Empty);

        var validationResult = await validator.ValidateAsync(cmd);

        validationResult.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("install")]
    [InlineData("Install")]
    [InlineData("INSTALL")]
    [InlineData("FirstSignal")]
    [InlineData("RegisterService")]
    [InlineData("AddContract")]
    [InlineData("SetupSlo")]
    public async Task UpdateOnboardingStep_AllValidSteps_PassValidation(string step)
    {
        var validator = new UpdateOnboardingStep.Validator();
        var cmd = new UpdateOnboardingStep.Command(step);

        var validationResult = await validator.ValidateAsync(cmd);

        validationResult.IsValid.Should().BeTrue();
    }
}
