-- =============================================================================
-- Seed: Planos Comerciais (CommercialCatalog)
-- Módulo: CommercialGovernance
-- Objetivo: Dados de teste representando planos comerciais para diferentes
--           modelos de deployment e modelos comerciais.
-- Idempotente via ON CONFLICT DO NOTHING.
-- APENAS para desenvolvimento/debug — nunca executar em produção.
-- =============================================================================

-- Plano SaaS Professional (Subscription mensal/anual)
INSERT INTO plans (id, code, name, description, commercial_model, deployment_model, is_active, trial_duration_days, grace_period_days, max_activations, price_tag, created_at, created_by)
VALUES
    ('a1b2c3d4-0001-4000-8000-000000000001', 'saas-professional', 'SaaS Professional', 'Plano profissional para equipes SaaS com até 100 APIs e 5 ambientes.', 1, 0, true, 30, 15, 5, 'USD 499/mo', NOW(), 'seed-script'),
    ('a1b2c3d4-0002-4000-8000-000000000002', 'saas-enterprise', 'SaaS Enterprise', 'Plano enterprise SaaS com APIs ilimitadas, 10 ambientes e suporte dedicado.', 1, 0, true, 30, 30, 10, 'USD 1499/mo', NOW(), 'seed-script'),
    ('a1b2c3d4-0003-4000-8000-000000000003', 'selfhosted-professional', 'Self-Hosted Professional', 'Plano profissional para instalação self-hosted com conectividade opcional.', 1, 1, true, 14, 15, 3, 'USD 799/mo', NOW(), 'seed-script'),
    ('a1b2c3d4-0004-4000-8000-000000000004', 'onprem-enterprise', 'On-Premise Enterprise', 'Plano enterprise para ambientes air-gapped com ativação offline.', 0, 2, true, NULL, 30, 2, 'USD 25000/yr', NOW(), 'seed-script'),
    ('a1b2c3d4-0005-4000-8000-000000000005', 'trial-starter', 'Trial Starter', 'Plano trial gratuito para avaliação da plataforma.', 3, 0, true, 30, 0, 1, 'Free', NOW(), 'seed-script')
ON CONFLICT DO NOTHING;
