using System.CommandLine;
using Spectre.Console;

// ═══════════════════════════════════════════════════════════════════════════════
// NEX — NexTraceOne Command Line Interface
// Uso: nex <command> [options]
// Consome apenas a camada Contracts de cada módulo (consumidor externo)
// ═══════════════════════════════════════════════════════════════════════════════

AnsiConsole.Write(new FigletText("NexTraceOne CLI").Color(Color.Cyan1));

var rootCommand = new RootCommand("NexTraceOne CLI — Sovereign Change Intelligence Platform");

// TODO: nex validate   — valida contrato OpenAPI com ruleset
// TODO: nex release    — gerencia releases (status, health, history)
// TODO: nex promotion  — controla promoção entre ambientes
// TODO: nex approval   — submete e consulta aprovações de workflow
// TODO: nex impact     — analisa blast radius de uma mudança
// TODO: nex tests      — gera cenários de teste em Robot Framework
// TODO: nex catalog    — consulta catálogo de APIs e serviços

return await rootCommand.InvokeAsync(args);
