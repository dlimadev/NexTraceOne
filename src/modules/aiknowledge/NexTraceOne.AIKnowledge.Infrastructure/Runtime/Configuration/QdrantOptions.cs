namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Configuration;

/// <summary>
/// Opções de configuração do Qdrant vector database.
/// </summary>
public sealed class QdrantOptions
{
    public const string SectionName = "Qdrant";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 6334; // gRPC port
    public int HttpPort { get; set; } = 6333;
    public bool Enabled { get; set; } = true;
}
