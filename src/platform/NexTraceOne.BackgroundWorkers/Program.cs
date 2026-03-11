// ═══════════════════════════════════════════════════════════════════════════════
// NEXTRACEONE — Background Workers
// Processa: Outbox Messages, Quartz Jobs, SLA Escalation, Cost Ingestion
// ═══════════════════════════════════════════════════════════════════════════════

var builder = Host.CreateApplicationBuilder(args);

// TODO: Registrar BuildingBlocks
// TODO: Registrar módulos (Infrastructure layer apenas)
// TODO: Registrar Quartz.NET com jobs agendados

var host = builder.Build();
host.Run();
