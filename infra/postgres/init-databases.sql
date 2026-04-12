-- ═══════════════════════════════════════════════════════════════════════════════
-- NexTraceOne — PostgreSQL Initialization
--
-- Architecture Decision: 1 single physical PostgreSQL database.
-- All modules share the `nextraceone` database, isolated by table prefix per module.
-- Module isolation is enforced by EF Core DbContexts, NOT by separate databases.
--
-- Table prefix assignments:
--   iam_  — Identity & Access
--   env_  — Environment Management
--   cfg_  — Configuration
--   cat_  — Service Catalog (graph)
--   dp_   — Developer Portal
--   ctr_  — Contracts
--   chg_  — Change Governance (all subdomains)
--   ops_  — Operational Intelligence (all subdomains)
--   aud_  — Audit & Compliance
--   gov_  — Governance
--   ntf_  — Notifications
--   int_  — Integrations
--   pan_  — Product Analytics
--   aik_  — AI & Knowledge (all subdomains)
--
-- Executed automatically on first container initialization.
-- Updated: 2026-04-07 | Phase 1 — RLS integration
--
-- POST-MIGRATION STEP:
--   After EF Core migrations have been applied, run apply-rls.sql to enforce
--   Row-Level Security tenant isolation as a defence-in-depth layer:
--     psql -U nextraceone -d nextraceone -f /rls/apply-rls.sql
-- ═══════════════════════════════════════════════════════════════════════════════

-- Single physical database for all modules (architecture: 1 DB, isolation by prefix + DbContext)
CREATE DATABASE nextraceone
    WITH ENCODING = 'UTF8'
    LC_COLLATE = 'en_US.utf8'
    LC_CTYPE = 'en_US.utf8'
    TEMPLATE = template0;
