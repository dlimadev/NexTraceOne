namespace NexTraceOne.Catalog.Application.Portal.ContractPipeline.Shared;

/// <summary>
/// Standard preview note included in all Contract Pipeline responses.
/// Signals that generated artifacts require developer review before production use.
/// Controlled by feature flag: catalog.contract-pipeline.preview
/// </summary>
public static class PipelinePreviewNote
{
    public const string Text =
        "PREVIEW: This feature is under active development. Generated code contains stubs and TODOs " +
        "that require implementation before use in production. Review all generated files carefully.";
}
