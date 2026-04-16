// ITenantSchemaManager foi movida para a camada Application para respeitar a Dependency Rule
// da Clean Architecture: a camada Application não pode depender de Infrastructure.
//
// Localização actual: NexTraceOne.BuildingBlocks.Application.Abstractions.ITenantSchemaManager
//
// Actualizar imports existentes:
//   ANTES: using NexTraceOne.BuildingBlocks.Infrastructure.MultiTenancy;
//   DEPOIS: using NexTraceOne.BuildingBlocks.Application.Abstractions;

