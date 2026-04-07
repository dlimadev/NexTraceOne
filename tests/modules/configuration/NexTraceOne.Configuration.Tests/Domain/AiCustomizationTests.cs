using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Tests.Domain;

/// <summary>
/// Testes de unidade para as entidades de customização de IA: SavedPrompt.
/// Valida criação, invariantes e partilha de prompts guardados pelo utilizador.
/// </summary>
public sealed class AiCustomizationTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    // ── SavedPrompt — Create happy path ──────────────────────────────────────

    [Fact]
    public void SavedPrompt_Create_WithValidData_ShouldReturn()
    {
        var prompt = SavedPrompt.Create(
            "user1", "tenant1", "My Incident Prompt",
            "Summarize the incident timeline for {{serviceName}}",
            "incident", "ops,ai", false, Now);

        Assert.NotNull(prompt);
        Assert.Equal("My Incident Prompt", prompt.Name);
        Assert.Equal("incident", prompt.ContextType);
        Assert.Equal("ops,ai", prompt.TagsCsv);
        Assert.False(prompt.IsShared);
        Assert.Equal("user1", prompt.UserId);
        Assert.NotEqual(Guid.Empty, prompt.Id.Value);
    }

    // ── SavedPrompt — Name validation ─────────────────────────────────────────

    [Fact]
    public void SavedPrompt_Create_WithEmptyName_ShouldThrow()
    {
        var act = () => SavedPrompt.Create(
            "user1", "tenant1", "",
            "Some prompt text", "general", null, false, Now);
        Assert.ThrowsAny<Exception>(act);
    }

    [Fact]
    public void SavedPrompt_Create_WithNameExceeding100Chars_ShouldThrow()
    {
        var longName = new string('x', 101);
        var act = () => SavedPrompt.Create(
            "user1", "tenant1", longName,
            "Some prompt text", "general", null, false, Now);
        Assert.ThrowsAny<Exception>(act);
    }

    // ── SavedPrompt — PromptText validation ──────────────────────────────────

    [Fact]
    public void SavedPrompt_Create_WithEmptyPromptText_ShouldThrow()
    {
        var act = () => SavedPrompt.Create(
            "user1", "tenant1", "Valid Name",
            "", "general", null, false, Now);
        Assert.ThrowsAny<Exception>(act);
    }

    // ── SavedPrompt — ContextType normalisation ──────────────────────────────

    [Theory]
    [InlineData("general")]
    [InlineData("incident")]
    [InlineData("contract")]
    [InlineData("change")]
    [InlineData("service")]
    public void SavedPrompt_Create_WithValidContextType_ShouldNormalise(string contextType)
    {
        var prompt = SavedPrompt.Create(
            "user1", "tenant1", "Prompt",
            "Some text", contextType, null, false, Now);
        Assert.Equal(contextType, prompt.ContextType);
    }

    [Fact]
    public void SavedPrompt_Create_WithUnknownContextType_ShouldDefaultToGeneral()
    {
        var prompt = SavedPrompt.Create(
            "user1", "tenant1", "Prompt",
            "Some text", "unknown_type", null, false, Now);
        Assert.Equal("general", prompt.ContextType);
    }

    // ── SavedPrompt — SetShared ───────────────────────────────────────────────

    [Fact]
    public void SavedPrompt_SetShared_True_ShouldUpdateFlag()
    {
        var prompt = SavedPrompt.Create(
            "user1", "tenant1", "Prompt",
            "Some text", "general", null, false, Now);

        prompt.SetShared(true);

        Assert.True(prompt.IsShared);
    }

    [Fact]
    public void SavedPrompt_SetShared_FalseAfterTrue_ShouldUpdateFlag()
    {
        var prompt = SavedPrompt.Create(
            "user1", "tenant1", "Prompt",
            "Some text", "general", null, true, Now);

        prompt.SetShared(false);

        Assert.False(prompt.IsShared);
    }

    // ── SavedPrompt — TagsCsv optional ────────────────────────────────────────

    [Fact]
    public void SavedPrompt_Create_WithNullTags_ShouldStoreNull()
    {
        var prompt = SavedPrompt.Create(
            "user1", "tenant1", "Prompt",
            "Some text", "general", null, false, Now);
        Assert.Null(prompt.TagsCsv);
    }
}
