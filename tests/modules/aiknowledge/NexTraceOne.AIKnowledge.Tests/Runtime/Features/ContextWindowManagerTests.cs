using System.Linq;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

namespace NexTraceOne.AIKnowledge.Tests.Runtime.Features;

/// <summary>
/// Testes unitários para o ContextWindowManager.
/// Valida o comportamento de sliding window em conversas curtas, longas e edge cases.
/// </summary>
public sealed class ContextWindowManagerTests
{
    private static IContextWindowManager CreateManager()
    {
        // Use real TokenCounterService with a simple logger for accurate token counting
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<NexTraceOne.AIKnowledge.Infrastructure.Runtime.Services.TokenCounterService>.Instance;
        var tokenCounter = new NexTraceOne.AIKnowledge.Infrastructure.Runtime.Services.TokenCounterService(logger);
        return new ContextWindowManager(tokenCounter);
    }

    // ── Short conversation — should not be truncated ─────────────────────

    [Fact]
    public void TrimToFit_WhenMessagesAreFewAndShort_ShouldNotTruncate()
    {
        var manager = CreateManager();
        var messages = new List<ChatMessage>
        {
            new("system", "You are a helpful assistant."),
            new("user", "What is the health of service X?"),
            new("assistant", "Service X is healthy."),
        };

        var (result, wasTruncated) = manager.TrimToFit(messages, maxContextTokens: 4096);

        result.Should().HaveCount(3);
        wasTruncated.Should().BeFalse();
    }

    // ── Empty message list ────────────────────────────────────────────────

    [Fact]
    public void TrimToFit_WhenEmpty_ShouldReturnEmpty()
    {
        var manager = CreateManager();
        var (result, wasTruncated) = manager.TrimToFit([], maxContextTokens: 4096);

        result.Should().BeEmpty();
        wasTruncated.Should().BeFalse();
    }

    // ── Long conversation — should truncate older messages ────────────────

    [Fact]
    public void TrimToFit_WhenHistoryExceedsWindow_ShouldTruncateOldMessages()
    {
        var manager = CreateManager();
        var messages = new List<ChatMessage>
        {
            new("system", "You are an AI assistant."),
        };

        // Add 50 pairs of user/assistant messages with long content
        for (var i = 0; i < 50; i++)
        {
            messages.Add(new("user", $"Question number {i}: " + new string('x', 200)));
            messages.Add(new("assistant", $"Answer number {i}: " + new string('y', 200)));
        }
        messages.Add(new("user", "What is the final answer?"));

        var (result, wasTruncated) = manager.TrimToFit(messages, maxContextTokens: 2048);

        wasTruncated.Should().BeTrue();
        result.Should().Contain(m => m.Role == "system"); // system always preserved
        result.Last().Content.Should().Contain("final answer"); // most recent preserved
        result.Count.Should().BeLessThan(messages.Count);
    }

    // ── System prompt is always preserved ────────────────────────────────

    [Fact]
    public void TrimToFit_ShouldAlwaysPreserveSystemPrompt()
    {
        var manager = CreateManager();
        const string systemContent = "Critical system instructions that must be preserved.";
        var messages = new List<ChatMessage>
        {
            new("system", systemContent),
        };

        // Add many long messages
        for (var i = 0; i < 30; i++)
        {
            messages.Add(new("user", new string('a', 500)));
            messages.Add(new("assistant", new string('b', 500)));
        }

        var (result, wasTruncated) = manager.TrimToFit(messages, maxContextTokens: 1024);

        result.Should().Contain(m => m.Role == "system" && m.Content == systemContent);
    }

    // ── Single message with no system ─────────────────────────────────────

    [Fact]
    public void TrimToFit_WithSingleUserMessage_ShouldNotTruncate()
    {
        var manager = CreateManager();
        var messages = new List<ChatMessage>
        {
            new("user", "Hello"),
        };

        var (result, wasTruncated) = manager.TrimToFit(messages, maxContextTokens: 128);

        result.Should().HaveCount(1);
        wasTruncated.Should().BeFalse();
    }

    // ── Most recent messages are kept ────────────────────────────────────

    [Fact]
    public void TrimToFit_ShouldPreserveMostRecentMessages()
    {
        var manager = CreateManager();
        var messages = new List<ChatMessage>
        {
            new("system", "System"),
            new("user", "First old message " + new string('a', 400)),
            new("assistant", "First old reply " + new string('b', 400)),
            new("user", "Second old message " + new string('c', 400)),
            new("assistant", "Second old reply " + new string('d', 400)),
            new("user", "Recent question"),
            new("assistant", "Recent answer"),
        };

        var (result, wasTruncated) = manager.TrimToFit(messages, maxContextTokens: 512);

        // Most recent messages should be preserved
        result.Should().Contain(m => m.Content == "Recent question");
        result.Should().Contain(m => m.Content == "Recent answer");
    }

    // ── Token estimation ─────────────────────────────────────────────────

    [Fact]
    public void EstimateTokens_WithEmptyContent_ShouldReturnZero()
    {
        var manager = CreateManager();
        manager.EstimateTokens("").Should().Be(0);
    }

    [Fact]
    public void EstimateTokens_WithShortText_ShouldReturnAtLeastOne()
    {
        var manager = CreateManager();
        manager.EstimateTokens("hi").Should().BeGreaterThan(0);
    }

    [Fact]
    public void EstimateTokens_WithLongText_ShouldIncreaseWithLength()
    {
        var manager = CreateManager();
        var short1 = manager.EstimateTokens("Hello world.");
        var long1 = manager.EstimateTokens(new string('x', 400));
        long1.Should().BeGreaterThan(short1);
    }

    [Fact]
    public void EstimateTotalTokens_WithMultipleMessages_ShouldSumTokens()
    {
        var manager = CreateManager();
        var messages = new List<ChatMessage>
        {
            new("system", "System prompt."),
            new("user", "User question here."),
            new("assistant", "Assistant response here."),
        };

        var total = manager.EstimateTotalTokens(messages);
        total.Should().BeGreaterThan(0);
    }

    // ── Zero available tokens edge case ──────────────────────────────────

    [Fact]
    public void TrimToFit_WhenMaxTokensVerySmall_ShouldReturnLastUserMessage()
    {
        var manager = CreateManager();
        var messages = new List<ChatMessage>
        {
            new("system", new string('s', 10000)),
            new("user", "Final question?"),
        };

        var (result, wasTruncated) = manager.TrimToFit(messages, maxContextTokens: 10);

        // When system prompt itself is too large, returns last user message
        wasTruncated.Should().BeTrue();
        result.Should().HaveCount(1);
        result[0].Content.Should().Contain("Final question");
    }
}
