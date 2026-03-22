using Microsoft.Extensions.Logging;
using NexTraceOne.BuildingBlocks.Observability.Alerting.Abstractions;
using NexTraceOne.BuildingBlocks.Observability.Alerting.Models;

namespace NexTraceOne.BuildingBlocks.Observability.Alerting;

/// <summary>
/// Gateway central de alertas da plataforma.
/// Faz fan-out para todos os canais registados, recolhe resultados individuais
/// e isola falhas de canal (um canal falhado não bloqueia os restantes).
/// </summary>
public sealed class AlertGateway : IAlertGateway
{
    private readonly IEnumerable<IAlertChannel> _channels;
    private readonly ILogger<AlertGateway> _logger;

    public AlertGateway(
        IEnumerable<IAlertChannel> channels,
        ILogger<AlertGateway> logger)
    {
        _channels = channels ?? throw new ArgumentNullException(nameof(channels));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<AlertDispatchResult> DispatchAsync(
        AlertPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);

        var channelList = _channels.ToList();

        if (channelList.Count == 0)
        {
            _logger.LogWarning("No alert channels registered; alert '{Title}' was not dispatched", payload.Title);
            return new AlertDispatchResult();
        }

        _logger.LogInformation(
            "Dispatching alert '{Title}' [{Severity}] to {ChannelCount} channel(s)",
            payload.Title,
            payload.Severity,
            channelList.Count);

        var tasks = channelList.Select(channel =>
            SendToChannelAsync(channel, payload, cancellationToken));

        var results = await Task.WhenAll(tasks);

        var channelResults = results.ToDictionary(r => r.ChannelName, r => r.Success);

        var result = new AlertDispatchResult { ChannelResults = channelResults };

        LogDispatchSummary(payload, result);

        return result;
    }

    /// <inheritdoc />
    public async Task<AlertDispatchResult> DispatchAsync(
        AlertPayload payload,
        string channelName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentException.ThrowIfNullOrWhiteSpace(channelName);

        var channel = _channels.FirstOrDefault(c =>
            string.Equals(c.ChannelName, channelName, StringComparison.OrdinalIgnoreCase));

        if (channel is null)
        {
            _logger.LogWarning(
                "Alert channel '{ChannelName}' not found; alert '{Title}' was not dispatched",
                channelName,
                payload.Title);

            return new AlertDispatchResult
            {
                ChannelResults = new Dictionary<string, bool> { [channelName] = false }
            };
        }

        _logger.LogInformation(
            "Dispatching alert '{Title}' [{Severity}] to channel '{ChannelName}'",
            payload.Title,
            payload.Severity,
            channelName);

        var sendResult = await SendToChannelAsync(channel, payload, cancellationToken);

        var result = new AlertDispatchResult
        {
            ChannelResults = new Dictionary<string, bool> { [sendResult.ChannelName] = sendResult.Success }
        };

        LogDispatchSummary(payload, result);

        return result;
    }

    private async Task<(string ChannelName, bool Success)> SendToChannelAsync(
        IAlertChannel channel,
        AlertPayload payload,
        CancellationToken cancellationToken)
    {
        try
        {
            var success = await channel.SendAsync(payload, cancellationToken);
            return (channel.ChannelName, success);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "Alert dispatch to channel '{ChannelName}' was cancelled",
                channel.ChannelName);
            return (channel.ChannelName, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled error dispatching alert to channel '{ChannelName}'",
                channel.ChannelName);
            return (channel.ChannelName, false);
        }
    }

    private void LogDispatchSummary(AlertPayload payload, AlertDispatchResult result)
    {
        if (result.AllSucceeded)
        {
            _logger.LogInformation(
                "Alert '{Title}' dispatched successfully to {Total} channel(s)",
                payload.Title,
                result.TotalChannels);
        }
        else if (result.AnySucceeded)
        {
            _logger.LogWarning(
                "Alert '{Title}' partially dispatched: {Failed}/{Total} channel(s) failed",
                payload.Title,
                result.FailedChannels,
                result.TotalChannels);
        }
        else
        {
            _logger.LogError(
                "Alert '{Title}' dispatch failed on all {Total} channel(s)",
                payload.Title,
                result.TotalChannels);
        }
    }
}
