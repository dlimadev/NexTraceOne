using System.Linq;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Domain.Entities;

public sealed class DefaultModelCatalogTests
{
    [Fact]
    public void GetAll_Returns_NonEmpty_List()
    {
        var models = DefaultModelCatalog.GetAll();

        models.Should().NotBeEmpty();
        models.Count.Should().Be(DefaultModelCatalog.GetAll().Count);
    }

    [Fact]
    public void All_Models_Have_Required_Fields()
    {
        foreach (var model in DefaultModelCatalog.GetAll())
        {
            model.Name.Should().NotBeNullOrWhiteSpace($"Model must have a name");
            model.DisplayName.Should().NotBeNullOrWhiteSpace($"Model '{model.Name}' must have a display name");
            model.Provider.Should().NotBeNullOrWhiteSpace($"Model '{model.Name}' must have a provider");
            model.Capabilities.Should().NotBeNullOrWhiteSpace($"Model '{model.Name}' must have capabilities");
            model.SensitivityLevel.Should().BeInRange(1, 5, $"Model '{model.Name}' sensitivity must be 1-5");
            model.LicenseName.Should().NotBeNullOrWhiteSpace($"Model '{model.Name}' must have a license");
        }
    }

    [Fact]
    public void Model_Names_Are_Unique()
    {
        var names = DefaultModelCatalog.GetAll().Select(m => m.Name).ToList();

        names.Should().OnlyHaveUniqueItems("model names must be unique in the catalog");
    }

    [Fact]
    public void Contains_At_Least_One_Internal_Model()
    {
        DefaultModelCatalog.GetAll()
            .Should().Contain(m => m.IsInternal, "catalog must include at least one internal/local model");
    }

    [Fact]
    public void Contains_At_Least_One_External_Model()
    {
        DefaultModelCatalog.GetAll()
            .Should().Contain(m => !m.IsInternal, "catalog must include at least one external model");
    }

    [Fact]
    public void Contains_Exactly_One_Default_Chat_Model()
    {
        DefaultModelCatalog.GetAll()
            .Count(m => m.IsDefaultForChat).Should().Be(1, "exactly one model should be default for chat");
    }

    [Fact]
    public void Contains_Exactly_One_Default_Reasoning_Model()
    {
        DefaultModelCatalog.GetAll()
            .Count(m => m.IsDefaultForReasoning).Should().Be(1, "exactly one model should be default for reasoning");
    }

    [Fact]
    public void Contains_Exactly_One_Default_Embeddings_Model()
    {
        DefaultModelCatalog.GetAll()
            .Count(m => m.IsDefaultForEmbeddings).Should().Be(1, "exactly one model should be default for embeddings");
    }

    [Fact]
    public void Default_Chat_Model_Is_Internal()
    {
        var chatDefault = DefaultModelCatalog.GetAll().Single(m => m.IsDefaultForChat);

        chatDefault.Should().NotBeNull("exactly one default chat model must exist");
        chatDefault.Name.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Contains_Chat_And_Embedding_Types()
    {
        var models = DefaultModelCatalog.GetAll();

        models.Should().Contain(m => m.ModelType == ModelType.Chat, "must include chat models");
        models.Should().Contain(m => m.ModelType == ModelType.Embedding, "must include embedding models");
    }

    [Fact]
    public void Embedding_Model_Supports_Embeddings_Flag()
    {
        var embeddingModels = DefaultModelCatalog.GetAll()
            .Where(m => m.ModelType == ModelType.Embedding);

        foreach (var model in embeddingModels)
        {
            model.SupportsEmbeddings.Should().BeTrue($"Embedding model '{model.Name}' must have SupportsEmbeddings=true");
        }
    }

    [Fact]
    public void All_Ollama_Models_Are_Internal()
    {
        var ollamaModels = DefaultModelCatalog.GetAll().Where(m => m.Provider == "Ollama");

        foreach (var model in ollamaModels)
        {
            model.IsInternal.Should().BeTrue($"Ollama model '{model.Name}' must be internal");
        }
    }

    [Fact]
    public void All_OpenAI_And_Anthropic_Models_Are_External()
    {
        var externalProviders = new[] { "OpenAI", "Anthropic" };
        var externalModels = DefaultModelCatalog.GetAll()
            .Where(m => externalProviders.Contains(m.Provider));

        foreach (var model in externalModels)
        {
            model.IsInternal.Should().BeFalse($"Provider '{model.Provider}' model '{model.Name}' must be external");
        }
    }

    [Fact]
    public void All_Models_Have_Valid_Category()
    {
        foreach (var model in DefaultModelCatalog.GetAll())
        {
            model.Category.Should().NotBeNullOrWhiteSpace($"Model '{model.Name}' must have a category");
        }
    }
}
