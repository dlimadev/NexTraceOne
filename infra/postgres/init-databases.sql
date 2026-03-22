-- ═══════════════════════════════════════════════════════════════════════════════
-- NexTraceOne — PostgreSQL Initialization
-- Cria os 4 bancos lógicos da plataforma em uma única instância PostgreSQL.
-- Executado automaticamente na primeira inicialização do container.
-- ═══════════════════════════════════════════════════════════════════════════════

-- Identity database: IdentityDbContext, AuditDbContext
CREATE DATABASE nextraceone_identity
    WITH ENCODING = 'UTF8'
    LC_COLLATE = 'en_US.utf8'
    LC_CTYPE = 'en_US.utf8'
    TEMPLATE = template0;

-- Catalog database: CatalogGraphDbContext, ContractsDbContext, DeveloperPortalDbContext
CREATE DATABASE nextraceone_catalog
    WITH ENCODING = 'UTF8'
    LC_COLLATE = 'en_US.utf8'
    LC_CTYPE = 'en_US.utf8'
    TEMPLATE = template0;

-- Operations database: ChangeIntelligenceDbContext, RulesetGovernanceDbContext,
--                      WorkflowDbContext, PromotionDbContext, IncidentDbContext,
--                      RuntimeIntelligenceDbContext, CostIntelligenceDbContext,
--                      GovernanceDbContext
CREATE DATABASE nextraceone_operations
    WITH ENCODING = 'UTF8'
    LC_COLLATE = 'en_US.utf8'
    LC_CTYPE = 'en_US.utf8'
    TEMPLATE = template0;

-- AI database: AiGovernanceDbContext, ExternalAiDbContext, AiOrchestrationDbContext
CREATE DATABASE nextraceone_ai
    WITH ENCODING = 'UTF8'
    LC_COLLATE = 'en_US.utf8'
    LC_CTYPE = 'en_US.utf8'
    TEMPLATE = template0;
