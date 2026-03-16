-- ═══════════════════════════════════════════════════════════════════════════════
-- NEXTRACEONE — Seed data: Catalog Module (nextraceone_catalog)
-- Tabelas: eg_service_assets, eg_api_assets, eg_consumer_assets, eg_consumer_relationships,
--          ct_contract_versions, ct_contract_diffs,
--          dp_subscriptions, dp_portal_analytics_events
-- ═══════════════════════════════════════════════════════════════════════════════

-- ═══ ENGINEERING GRAPH — Services, APIs, Consumer Relationships ═══════════════

-- Services
INSERT INTO eg_service_assets ("Id", "Name", "Domain", "TeamName")
VALUES
  ('c0000000-0000-0000-0000-000000000001', 'Orders Service', 'Commerce', 'Team Alpha'),
  ('c0000000-0000-0000-0000-000000000002', 'Payments Service', 'Finance', 'Team Beta'),
  ('c0000000-0000-0000-0000-000000000003', 'Inventory Service', 'Logistics', 'Team Gamma'),
  ('c0000000-0000-0000-0000-000000000004', 'Notifications Service', 'Platform', 'Team Delta'),
  ('c0000000-0000-0000-0000-000000000005', 'Users Service', 'Identity', 'Team Alpha'),
  ('c0000000-0000-0000-0000-000000000006', 'Shipping Service', 'Logistics', 'Team Gamma'),
  ('c0000000-0000-0000-0000-000000000007', 'Analytics Service', 'Platform', 'Team Delta'),
  ('c0000000-0000-0000-0000-000000000008', 'Gateway Service', 'Platform', 'Team Alpha'),
  ('c0000000-0000-0000-0000-000000000009', 'Search Service', 'Platform', 'Team Beta'),
  ('c0000000-0000-0000-0000-000000000010', 'Pricing Service', 'Commerce', 'Team Beta')
ON CONFLICT DO NOTHING;

-- APIs
INSERT INTO eg_api_assets ("Id", "Name", "RoutePattern", "Version", "Visibility", "OwnerServiceId", "IsDecommissioned")
VALUES
  ('d0000000-0000-0000-0000-000000000001', 'Orders API', '/api/v1/orders', 'v1.3.0', 'Public', 'c0000000-0000-0000-0000-000000000001', false),
  ('d0000000-0000-0000-0000-000000000002', 'Payments API', '/api/v1/payments', 'v2.1.0', 'Public', 'c0000000-0000-0000-0000-000000000002', false),
  ('d0000000-0000-0000-0000-000000000003', 'Inventory API', '/api/v1/inventory', 'v1.0.0', 'Internal', 'c0000000-0000-0000-0000-000000000003', false),
  ('d0000000-0000-0000-0000-000000000004', 'Notifications API', '/api/v1/notifications', 'v1.2.0', 'Internal', 'c0000000-0000-0000-0000-000000000004', false),
  ('d0000000-0000-0000-0000-000000000005', 'Users API', '/api/v1/users', 'v1.1.0', 'Public', 'c0000000-0000-0000-0000-000000000005', false),
  ('d0000000-0000-0000-0000-000000000006', 'Shipping API', '/api/v1/shipping', 'v1.0.0', 'Public', 'c0000000-0000-0000-0000-000000000006', false),
  ('d0000000-0000-0000-0000-000000000007', 'Analytics API', '/api/v1/analytics', 'v1.0.0', 'Internal', 'c0000000-0000-0000-0000-000000000007', false),
  ('d0000000-0000-0000-0000-000000000008', 'Gateway API', '/api/v1/gateway', 'v2.0.0', 'Public', 'c0000000-0000-0000-0000-000000000008', false),
  ('d0000000-0000-0000-0000-000000000009', 'Search API', '/api/v1/search', 'v1.2.0', 'Public', 'c0000000-0000-0000-0000-000000000009', false),
  ('d0000000-0000-0000-0000-000000000010', 'Pricing API', '/api/v1/pricing', 'v1.1.0', 'Internal', 'c0000000-0000-0000-0000-000000000010', false)
ON CONFLICT DO NOTHING;

-- Consumer Assets
INSERT INTO eg_consumer_assets ("Id", "Name", "Kind", "Environment")
VALUES
  ('c1000000-0000-0000-0000-000000000001', 'Payments Service', 'Service', 'Production'),
  ('c1000000-0000-0000-0000-000000000002', 'Inventory Service', 'Service', 'Production'),
  ('c1000000-0000-0000-0000-000000000003', 'Notifications Service', 'Service', 'Production'),
  ('c1000000-0000-0000-0000-000000000004', 'Orders Service', 'Service', 'Production'),
  ('c1000000-0000-0000-0000-000000000005', 'Mobile App', 'Client', 'Production'),
  ('c1000000-0000-0000-0000-000000000006', 'Shipping Service', 'Service', 'Production'),
  ('c1000000-0000-0000-0000-000000000007', 'Analytics Service', 'Service', 'Production'),
  ('c1000000-0000-0000-0000-000000000008', 'Gateway Service', 'Service', 'Production'),
  ('c1000000-0000-0000-0000-000000000009', 'Search Service', 'Service', 'Production'),
  ('c1000000-0000-0000-0000-000000000010', 'Pricing Service', 'Service', 'Production'),
  ('c1000000-0000-0000-0000-000000000011', 'Admin Dashboard', 'Client', 'Production'),
  ('c1000000-0000-0000-0000-000000000012', 'Partner Portal', 'Client', 'Staging')
ON CONFLICT DO NOTHING;

-- Consumer Relationships
INSERT INTO eg_consumer_relationships ("Id", "ApiAssetId", "ConsumerAssetId", "ConsumerName", "SourceType", "ConfidenceScore", "FirstObservedAt", "LastObservedAt")
VALUES
  ('c2000000-0000-0000-0000-000000000001', 'd0000000-0000-0000-0000-000000000001', 'c1000000-0000-0000-0000-000000000001', 'Payments Service', 'TrafficAnalysis', 0.9500, '2025-03-01T00:00:00Z', '2025-06-01T12:00:00Z'),
  ('c2000000-0000-0000-0000-000000000002', 'd0000000-0000-0000-0000-000000000001', 'c1000000-0000-0000-0000-000000000002', 'Inventory Service', 'TrafficAnalysis', 0.8800, '2025-03-15T00:00:00Z', '2025-06-01T10:00:00Z'),
  ('c2000000-0000-0000-0000-000000000003', 'd0000000-0000-0000-0000-000000000001', 'c1000000-0000-0000-0000-000000000005', 'Mobile App', 'ContractImport', 0.9900, '2025-02-01T00:00:00Z', '2025-06-01T14:00:00Z'),
  ('c2000000-0000-0000-0000-000000000004', 'd0000000-0000-0000-0000-000000000002', 'c1000000-0000-0000-0000-000000000004', 'Orders Service', 'TrafficAnalysis', 0.9200, '2025-04-01T00:00:00Z', '2025-06-01T11:00:00Z'),
  ('c2000000-0000-0000-0000-000000000005', 'd0000000-0000-0000-0000-000000000002', 'c1000000-0000-0000-0000-000000000003', 'Notifications Service', 'TrafficAnalysis', 0.7500, '2025-04-15T00:00:00Z', '2025-06-01T09:00:00Z'),
  ('c2000000-0000-0000-0000-000000000006', 'd0000000-0000-0000-0000-000000000003', 'c1000000-0000-0000-0000-000000000004', 'Orders Service', 'ContractImport', 0.9800, '2025-03-10T00:00:00Z', '2025-06-01T13:00:00Z'),
  ('c2000000-0000-0000-0000-000000000007', 'd0000000-0000-0000-0000-000000000004', 'c1000000-0000-0000-0000-000000000004', 'Orders Service', 'TrafficAnalysis', 0.8200, '2025-05-01T00:00:00Z', '2025-06-01T08:00:00Z'),
  ('c2000000-0000-0000-0000-000000000008', 'd0000000-0000-0000-0000-000000000001', 'c1000000-0000-0000-0000-000000000006', 'Shipping Service', 'TrafficAnalysis', 0.9100, '2025-04-01T00:00:00Z', '2025-06-02T09:00:00Z'),
  ('c2000000-0000-0000-0000-000000000009', 'd0000000-0000-0000-0000-000000000001', 'c1000000-0000-0000-0000-000000000011', 'Admin Dashboard', 'ContractImport', 0.9700, '2025-02-15T00:00:00Z', '2025-06-02T10:00:00Z'),
  ('c2000000-0000-0000-0000-000000000010', 'd0000000-0000-0000-0000-000000000002', 'c1000000-0000-0000-0000-000000000005', 'Mobile App', 'TrafficAnalysis', 0.8900, '2025-04-10T00:00:00Z', '2025-06-02T07:30:00Z'),
  ('c2000000-0000-0000-0000-000000000011', 'd0000000-0000-0000-0000-000000000006', 'c1000000-0000-0000-0000-000000000004', 'Orders Service', 'ContractImport', 0.9600, '2025-04-15T00:00:00Z', '2025-06-02T11:00:00Z'),
  ('c2000000-0000-0000-0000-000000000012', 'd0000000-0000-0000-0000-000000000006', 'c1000000-0000-0000-0000-000000000005', 'Mobile App', 'TrafficAnalysis', 0.8500, '2025-05-01T00:00:00Z', '2025-06-02T08:00:00Z'),
  ('c2000000-0000-0000-0000-000000000013', 'd0000000-0000-0000-0000-000000000010', 'c1000000-0000-0000-0000-000000000004', 'Orders Service', 'TrafficAnalysis', 0.9300, '2025-05-10T00:00:00Z', '2025-06-02T12:00:00Z'),
  ('c2000000-0000-0000-0000-000000000014', 'd0000000-0000-0000-0000-000000000010', 'c1000000-0000-0000-0000-000000000008', 'Gateway Service', 'TrafficAnalysis', 0.8700, '2025-05-15T00:00:00Z', '2025-06-02T13:00:00Z'),
  ('c2000000-0000-0000-0000-000000000015', 'd0000000-0000-0000-0000-000000000009', 'c1000000-0000-0000-0000-000000000005', 'Mobile App', 'ContractImport', 0.9400, '2025-05-20T00:00:00Z', '2025-06-02T14:00:00Z'),
  ('c2000000-0000-0000-0000-000000000016', 'd0000000-0000-0000-0000-000000000009', 'c1000000-0000-0000-0000-000000000011', 'Admin Dashboard', 'TrafficAnalysis', 0.8100, '2025-05-25T00:00:00Z', '2025-06-02T15:00:00Z'),
  ('c2000000-0000-0000-0000-000000000017', 'd0000000-0000-0000-0000-000000000007', 'c1000000-0000-0000-0000-000000000011', 'Admin Dashboard', 'ContractImport', 0.9900, '2025-03-01T00:00:00Z', '2025-06-02T16:00:00Z'),
  ('c2000000-0000-0000-0000-000000000018', 'd0000000-0000-0000-0000-000000000005', 'c1000000-0000-0000-0000-000000000008', 'Gateway Service', 'TrafficAnalysis', 0.9000, '2025-03-15T00:00:00Z', '2025-06-02T09:30:00Z')
ON CONFLICT DO NOTHING;

-- ═══ CONTRACTS — Contract Versions with OpenAPI specs ═════════════════════════

INSERT INTO ct_contract_versions ("Id", "ApiAssetId", "SemVer", "SpecContent", "Format", "Protocol", "LifecycleState", "ImportedFrom", "IsLocked", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted")
VALUES
  -- Orders API (api1) — v1.2.0 → v1.3.0
  ('20000000-0000-0000-0000-000000000001', 'd0000000-0000-0000-0000-000000000001', '1.2.0', '{"openapi":"3.0.0","info":{"title":"Orders API","version":"1.2.0"},"paths":{"/orders":{"get":{"summary":"List orders"}}}}', 'json', 'OpenApi', 'Approved', 'upload', true, '2025-03-01T10:00:00Z', 'admin@nextraceone.dev', '2025-03-01T10:00:00Z', 'admin@nextraceone.dev', false),
  ('20000000-0000-0000-0000-000000000002', 'd0000000-0000-0000-0000-000000000001', '1.3.0', '{"openapi":"3.0.0","info":{"title":"Orders API","version":"1.3.0"},"paths":{"/orders":{"get":{"summary":"List orders"},"post":{"summary":"Create order"}},"/orders/{id}":{"get":{"summary":"Get order"}}}}', 'json', 'OpenApi', 'Approved', 'upload', false, '2025-05-15T14:00:00Z', 'dev@nextraceone.dev', '2025-05-15T14:00:00Z', 'dev@nextraceone.dev', false),
  -- Payments API (api2) — v2.0.0 → v2.1.0
  ('20000000-0000-0000-0000-000000000003', 'd0000000-0000-0000-0000-000000000002', '2.0.0', '{"openapi":"3.0.0","info":{"title":"Payments API","version":"2.0.0"},"paths":{"/payments":{"post":{"summary":"Process payment"}},"/payments/{id}":{"get":{"summary":"Get payment"}}}}', 'json', 'OpenApi', 'Approved', 'upload', true, '2025-04-01T09:00:00Z', 'dev@nextraceone.dev', '2025-04-01T09:00:00Z', 'dev@nextraceone.dev', false),
  ('20000000-0000-0000-0000-000000000004', 'd0000000-0000-0000-0000-000000000002', '2.1.0', '{"openapi":"3.0.0","info":{"title":"Payments API","version":"2.1.0"},"paths":{"/payments":{"post":{"summary":"Process payment"}},"/payments/{id}":{"get":{"summary":"Get payment"},"delete":{"summary":"Refund payment"}}}}', 'json', 'OpenApi', 'InReview', 'upload', false, '2025-05-20T16:00:00Z', 'dev@nextraceone.dev', '2025-05-20T16:00:00Z', 'dev@nextraceone.dev', false),
  -- Inventory API (api3) — v1.0.0
  ('20000000-0000-0000-0000-000000000005', 'd0000000-0000-0000-0000-000000000003', '1.0.0', '{"openapi":"3.0.0","info":{"title":"Inventory API","version":"1.0.0"},"paths":{"/inventory":{"get":{"summary":"List stock"}},"/inventory/{sku}":{"put":{"summary":"Update stock"}}}}', 'json', 'OpenApi', 'Approved', 'upload', true, '2025-03-20T11:00:00Z', 'admin@nextraceone.dev', '2025-03-20T11:00:00Z', 'admin@nextraceone.dev', false),
  -- Notifications API (api4) — v1.2.0 Draft
  ('20000000-0000-0000-0000-000000000006', 'd0000000-0000-0000-0000-000000000004', '1.2.0', '{"openapi":"3.0.0","info":{"title":"Notifications API","version":"1.2.0"},"paths":{"/notifications":{"post":{"summary":"Send notification"}},"/notifications/templates":{"get":{"summary":"List templates"}}}}', 'json', 'OpenApi', 'Draft', 'upload', false, '2025-06-01T08:00:00Z', 'dev@nextraceone.dev', '2025-06-01T08:00:00Z', 'dev@nextraceone.dev', false),
  -- Users API (api5) — v1.0.0 → v1.1.0
  ('20000000-0000-0000-0000-000000000007', 'd0000000-0000-0000-0000-000000000005', '1.0.0', '{"openapi":"3.0.0","info":{"title":"Users API","version":"1.0.0"},"paths":{"/users":{"get":{"summary":"List users"}},"/users/{id}":{"get":{"summary":"Get user"}}}}', 'json', 'OpenApi', 'Approved', 'upload', true, '2025-02-10T10:00:00Z', 'admin@nextraceone.dev', '2025-02-10T10:00:00Z', 'admin@nextraceone.dev', false),
  ('20000000-0000-0000-0000-000000000008', 'd0000000-0000-0000-0000-000000000005', '1.1.0', '{"openapi":"3.0.0","info":{"title":"Users API","version":"1.1.0"},"paths":{"/users":{"get":{"summary":"List users"},"post":{"summary":"Create user"}},"/users/{id}":{"get":{"summary":"Get user"},"put":{"summary":"Update user"},"delete":{"summary":"Delete user"}}}}', 'json', 'OpenApi', 'Approved', 'upload', false, '2025-05-28T11:00:00Z', 'ana.costa@nextraceone.dev', '2025-05-28T11:00:00Z', 'ana.costa@nextraceone.dev', false),
  -- Shipping API (api6) — v1.0.0
  ('20000000-0000-0000-0000-000000000009', 'd0000000-0000-0000-0000-000000000006', '1.0.0', '{"openapi":"3.0.0","info":{"title":"Shipping API","version":"1.0.0"},"paths":{"/shipments":{"get":{"summary":"List shipments"},"post":{"summary":"Create shipment"}},"/shipments/{id}/track":{"get":{"summary":"Track shipment"}}}}', 'json', 'OpenApi', 'Approved', 'upload', true, '2025-04-15T09:00:00Z', 'lucia.ferreira@nextraceone.dev', '2025-04-15T09:00:00Z', 'lucia.ferreira@nextraceone.dev', false),
  -- Analytics API (api7) — v1.0.0
  ('20000000-0000-0000-0000-000000000010', 'd0000000-0000-0000-0000-000000000007', '1.0.0', '{"openapi":"3.0.0","info":{"title":"Analytics API","version":"1.0.0"},"paths":{"/analytics/events":{"post":{"summary":"Track event"}},"/analytics/reports":{"get":{"summary":"Get reports"}},"/analytics/dashboards":{"get":{"summary":"List dashboards"}}}}', 'json', 'OpenApi', 'Approved', 'upload', true, '2025-04-20T14:00:00Z', 'rafael.lima@nextraceone.dev', '2025-04-20T14:00:00Z', 'rafael.lima@nextraceone.dev', false),
  -- Gateway API (api8) — v1.0.0 → v2.0.0 (breaking)
  ('20000000-0000-0000-0000-000000000011', 'd0000000-0000-0000-0000-000000000008', '1.0.0', '{"openapi":"3.0.0","info":{"title":"Gateway API","version":"1.0.0"},"paths":{"/gateway/routes":{"get":{"summary":"List routes"}},"/gateway/health":{"get":{"summary":"Health check"}}}}', 'json', 'OpenApi', 'Deprecated', 'upload', true, '2025-03-05T08:00:00Z', 'admin@nextraceone.dev', '2025-03-05T08:00:00Z', 'admin@nextraceone.dev', false),
  ('20000000-0000-0000-0000-000000000012', 'd0000000-0000-0000-0000-000000000008', '2.0.0', '{"openapi":"3.0.0","info":{"title":"Gateway API","version":"2.0.0"},"paths":{"/v2/gateway/routes":{"get":{"summary":"List routes v2"},"post":{"summary":"Register route"}},"/v2/gateway/health":{"get":{"summary":"Health check v2"}},"/v2/gateway/metrics":{"get":{"summary":"Gateway metrics"}}}}', 'json', 'OpenApi', 'InReview', 'upload', false, '2025-06-01T10:00:00Z', 'pedro.alves@nextraceone.dev', '2025-06-01T10:00:00Z', 'pedro.alves@nextraceone.dev', false),
  -- Search API (api9) — v1.0.0 → v1.2.0
  ('20000000-0000-0000-0000-000000000013', 'd0000000-0000-0000-0000-000000000009', '1.0.0', '{"openapi":"3.0.0","info":{"title":"Search API","version":"1.0.0"},"paths":{"/search":{"get":{"summary":"Full-text search"}}}}', 'json', 'OpenApi', 'Approved', 'upload', true, '2025-03-25T09:00:00Z', 'admin@nextraceone.dev', '2025-03-25T09:00:00Z', 'admin@nextraceone.dev', false),
  ('20000000-0000-0000-0000-000000000014', 'd0000000-0000-0000-0000-000000000009', '1.2.0', '{"openapi":"3.0.0","info":{"title":"Search API","version":"1.2.0"},"paths":{"/search":{"get":{"summary":"Full-text search"},"post":{"summary":"Advanced search"}},"/search/suggest":{"get":{"summary":"Search suggestions"}},"/search/facets":{"get":{"summary":"Search facets"}}}}', 'json', 'OpenApi', 'Approved', 'upload', false, '2025-05-25T11:00:00Z', 'ana.costa@nextraceone.dev', '2025-05-25T11:00:00Z', 'ana.costa@nextraceone.dev', false),
  -- Pricing API (api10) — v1.0.0 → v1.1.0
  ('20000000-0000-0000-0000-000000000015', 'd0000000-0000-0000-0000-000000000010', '1.0.0', '{"openapi":"3.0.0","info":{"title":"Pricing API","version":"1.0.0"},"paths":{"/pricing/rules":{"get":{"summary":"List pricing rules"}},"/pricing/calculate":{"post":{"summary":"Calculate price"}}}}', 'json', 'OpenApi', 'Approved', 'upload', true, '2025-04-10T10:00:00Z', 'dev@nextraceone.dev', '2025-04-10T10:00:00Z', 'dev@nextraceone.dev', false),
  ('20000000-0000-0000-0000-000000000016', 'd0000000-0000-0000-0000-000000000010', '1.1.0', '{"openapi":"3.0.0","info":{"title":"Pricing API","version":"1.1.0"},"paths":{"/pricing/rules":{"get":{"summary":"List pricing rules"},"post":{"summary":"Create pricing rule"}},"/pricing/calculate":{"post":{"summary":"Calculate price"}},"/pricing/history":{"get":{"summary":"Price history"}}}}', 'json', 'OpenApi', 'Draft', 'upload', false, '2025-06-02T09:00:00Z', 'rafael.lima@nextraceone.dev', '2025-06-02T09:00:00Z', 'rafael.lima@nextraceone.dev', false)
ON CONFLICT DO NOTHING;

-- Contract Diffs
INSERT INTO ct_contract_diffs ("Id", "ContractVersionId", "BaseVersionId", "TargetVersionId", "ApiAssetId", "Protocol", "ChangeLevel", "BreakingChanges", "NonBreakingChanges", "AdditiveChanges", "SuggestedSemVer", "Confidence", "ComputedAt")
VALUES
  -- Orders API v1.2.0 → v1.3.0 (Minor — additive)
  ('21000000-0000-0000-0000-000000000001', '20000000-0000-0000-0000-000000000002', '20000000-0000-0000-0000-000000000001', '20000000-0000-0000-0000-000000000002', 'd0000000-0000-0000-0000-000000000001', 'OpenApi', 2, '[]', '[]', '[{"path":"/orders","changeType":"Added","isBreaking":false,"description":"Added POST /orders endpoint"},{"path":"/orders/{id}","changeType":"Added","isBreaking":false,"description":"Added GET /orders/{id} endpoint"}]', '1.3.0', 0.9500, '2025-05-15T14:30:00Z'),
  -- Payments API v2.0.0 → v2.1.0 (Minor — additive)
  ('21000000-0000-0000-0000-000000000002', '20000000-0000-0000-0000-000000000004', '20000000-0000-0000-0000-000000000003', '20000000-0000-0000-0000-000000000004', 'd0000000-0000-0000-0000-000000000002', 'OpenApi', 2, '[]', '[]', '[{"path":"/payments/{id}","changeType":"Added","isBreaking":false,"description":"Added DELETE /payments/{id} for refund"}]', '2.1.0', 0.9200, '2025-05-20T16:30:00Z'),
  -- Users API v1.0.0 → v1.1.0 (Minor — additive with modifications)
  ('21000000-0000-0000-0000-000000000003', '20000000-0000-0000-0000-000000000008', '20000000-0000-0000-0000-000000000007', '20000000-0000-0000-0000-000000000008', 'd0000000-0000-0000-0000-000000000005', 'OpenApi', 2, '[]', '[{"path":"/users/{id}","changeType":"Modified","isBreaking":false,"description":"Added PUT and DELETE methods"}]', '[{"path":"/users","changeType":"Added","isBreaking":false,"description":"Added POST /users endpoint"}]', '1.1.0', 0.9400, '2025-05-28T11:30:00Z'),
  -- Gateway API v1.0.0 → v2.0.0 (Breaking — path restructure)
  ('21000000-0000-0000-0000-000000000004', '20000000-0000-0000-0000-000000000012', '20000000-0000-0000-0000-000000000011', '20000000-0000-0000-0000-000000000012', 'd0000000-0000-0000-0000-000000000008', 'OpenApi', 4, '[{"path":"/gateway/routes","changeType":"Removed","isBreaking":true,"description":"Removed /gateway/routes — replaced by /v2/gateway/routes"},{"path":"/gateway/health","changeType":"Removed","isBreaking":true,"description":"Removed /gateway/health — replaced by /v2/gateway/health"}]', '[]', '[{"path":"/v2/gateway/routes","changeType":"Added","isBreaking":false,"description":"New v2 routes endpoint with POST"},{"path":"/v2/gateway/metrics","changeType":"Added","isBreaking":false,"description":"Added metrics endpoint"}]', '2.0.0', 0.9800, '2025-06-01T10:30:00Z'),
  -- Search API v1.0.0 → v1.2.0 (Minor — additive)
  ('21000000-0000-0000-0000-000000000005', '20000000-0000-0000-0000-000000000014', '20000000-0000-0000-0000-000000000013', '20000000-0000-0000-0000-000000000014', 'd0000000-0000-0000-0000-000000000009', 'OpenApi', 2, '[]', '[]', '[{"path":"/search","changeType":"Added","isBreaking":false,"description":"Added POST /search for advanced search"},{"path":"/search/suggest","changeType":"Added","isBreaking":false,"description":"Added search suggestions"},{"path":"/search/facets","changeType":"Added","isBreaking":false,"description":"Added search facets"}]', '1.2.0', 0.9600, '2025-05-25T11:30:00Z'),
  -- Pricing API v1.0.0 → v1.1.0 (Minor — additive)
  ('21000000-0000-0000-0000-000000000006', '20000000-0000-0000-0000-000000000016', '20000000-0000-0000-0000-000000000015', '20000000-0000-0000-0000-000000000016', 'd0000000-0000-0000-0000-000000000010', 'OpenApi', 2, '[]', '[]', '[{"path":"/pricing/rules","changeType":"Added","isBreaking":false,"description":"Added POST /pricing/rules"},{"path":"/pricing/history","changeType":"Added","isBreaking":false,"description":"Added price history endpoint"}]', '1.1.0', 0.9100, '2025-06-02T09:30:00Z')
ON CONFLICT DO NOTHING;

-- ═══ DEVELOPER PORTAL — Subscriptions, Analytics ══════════════════════════════

-- Subscriptions (Level: 0=BreakingChangesOnly, 1=AllChanges, 2=DeprecationNotices, 3=SecurityAdvisories)
-- Channel: 0=Email, 1=Webhook
INSERT INTO dp_subscriptions ("Id", "ApiAssetId", "ApiName", "SubscriberId", "SubscriberEmail", "ConsumerServiceName", "ConsumerServiceVersion", "Level", "Channel", "IsActive", "CreatedAt")
VALUES
  ('80000000-0000-0000-0000-000000000001', 'd0000000-0000-0000-0000-000000000001', 'Orders API', 'b0000000-0000-0000-0000-000000000002', 'techlead@nextraceone.dev', 'Payments Service', '2.0.0', 0, 0, true, '2025-03-01T00:00:00Z'),
  ('80000000-0000-0000-0000-000000000002', 'd0000000-0000-0000-0000-000000000002', 'Payments API', 'b0000000-0000-0000-0000-000000000003', 'dev@nextraceone.dev', 'Orders Service', '1.3.0', 1, 0, true, '2025-04-01T00:00:00Z'),
  ('80000000-0000-0000-0000-000000000003', 'd0000000-0000-0000-0000-000000000001', 'Orders API', 'b0000000-0000-0000-0000-000000000003', 'dev@nextraceone.dev', 'Mobile App', '3.0.0', 1, 0, true, '2025-03-15T00:00:00Z'),
  ('80000000-0000-0000-0000-000000000004', 'd0000000-0000-0000-0000-000000000006', 'Shipping API', 'b0000000-0000-0000-0000-000000000007', 'lucia.ferreira@nextraceone.dev', 'Orders Service', '1.3.0', 1, 0, true, '2025-04-20T00:00:00Z'),
  ('80000000-0000-0000-0000-000000000005', 'd0000000-0000-0000-0000-000000000008', 'Gateway API', 'b0000000-0000-0000-0000-000000000006', 'pedro.alves@nextraceone.dev', 'Users Service', '1.1.0', 0, 0, true, '2025-03-10T00:00:00Z'),
  ('80000000-0000-0000-0000-000000000006', 'd0000000-0000-0000-0000-000000000009', 'Search API', 'b0000000-0000-0000-0000-000000000005', 'ana.costa@nextraceone.dev', 'Admin Dashboard', '1.0.0', 1, 0, true, '2025-05-20T00:00:00Z'),
  ('80000000-0000-0000-0000-000000000007', 'd0000000-0000-0000-0000-000000000010', 'Pricing API', 'b0000000-0000-0000-0000-000000000008', 'rafael.lima@nextraceone.dev', 'Orders Service', '1.3.0', 1, 1, true, '2025-05-15T00:00:00Z'),
  ('80000000-0000-0000-0000-000000000008', 'd0000000-0000-0000-0000-000000000005', 'Users API', 'b0000000-0000-0000-0000-000000000006', 'pedro.alves@nextraceone.dev', 'Gateway Service', '2.0.0', 0, 0, true, '2025-03-20T00:00:00Z'),
  ('80000000-0000-0000-0000-000000000009', 'd0000000-0000-0000-0000-000000000003', 'Inventory API', 'b0000000-0000-0000-0000-000000000003', 'dev@nextraceone.dev', 'Orders Service', '1.3.0', 3, 0, true, '2025-03-25T00:00:00Z'),
  ('80000000-0000-0000-0000-000000000010', 'd0000000-0000-0000-0000-000000000007', 'Analytics API', 'b0000000-0000-0000-0000-000000000001', 'admin@nextraceone.dev', 'Admin Dashboard', '1.0.0', 2, 1, true, '2025-04-25T00:00:00Z'),
  ('80000000-0000-0000-0000-000000000011', 'd0000000-0000-0000-0000-000000000001', 'Orders API', 'b0000000-0000-0000-0000-000000000005', 'ana.costa@nextraceone.dev', 'Shipping Service', '1.0.0', 0, 0, true, '2025-05-01T00:00:00Z'),
  ('80000000-0000-0000-0000-000000000012', 'd0000000-0000-0000-0000-000000000002', 'Payments API', 'b0000000-0000-0000-0000-000000000006', 'pedro.alves@nextraceone.dev', 'Mobile App', '3.0.0', 1, 0, false, '2025-04-10T00:00:00Z')
ON CONFLICT DO NOTHING;

-- Portal Analytics Events
INSERT INTO dp_portal_analytics_events ("Id", "UserId", "EventType", "EntityId", "EntityType", "SearchQuery", "OccurredAt")
VALUES
  ('81000000-0000-0000-0000-000000000001', 'b0000000-0000-0000-0000-000000000003', 'api_view', 'd0000000-0000-0000-0000-000000000001', 'ApiAsset', NULL, '2025-06-01T08:00:00Z'),
  ('81000000-0000-0000-0000-000000000002', 'b0000000-0000-0000-0000-000000000003', 'search', NULL, NULL, 'orders', '2025-06-01T07:55:00Z'),
  ('81000000-0000-0000-0000-000000000003', 'b0000000-0000-0000-0000-000000000002', 'api_view', 'd0000000-0000-0000-0000-000000000002', 'ApiAsset', NULL, '2025-05-31T14:00:00Z'),
  ('81000000-0000-0000-0000-000000000004', 'b0000000-0000-0000-0000-000000000002', 'search', NULL, NULL, 'payments', '2025-05-31T13:55:00Z'),
  ('81000000-0000-0000-0000-000000000005', 'b0000000-0000-0000-0000-000000000005', 'api_view', 'd0000000-0000-0000-0000-000000000009', 'ApiAsset', NULL, '2025-06-01T09:00:00Z'),
  ('81000000-0000-0000-0000-000000000006', 'b0000000-0000-0000-0000-000000000005', 'search', NULL, NULL, 'search', '2025-06-01T08:55:00Z'),
  ('81000000-0000-0000-0000-000000000007', 'b0000000-0000-0000-0000-000000000006', 'api_view', 'd0000000-0000-0000-0000-000000000008', 'ApiAsset', NULL, '2025-06-01T10:30:00Z'),
  ('81000000-0000-0000-0000-000000000008', 'b0000000-0000-0000-0000-000000000006', 'api_view', 'd0000000-0000-0000-0000-000000000005', 'ApiAsset', NULL, '2025-06-01T10:35:00Z'),
  ('81000000-0000-0000-0000-000000000009', 'b0000000-0000-0000-0000-000000000007', 'api_view', 'd0000000-0000-0000-0000-000000000006', 'ApiAsset', NULL, '2025-05-30T11:00:00Z'),
  ('81000000-0000-0000-0000-000000000010', 'b0000000-0000-0000-0000-000000000008', 'search', NULL, NULL, 'pricing rules', '2025-06-02T09:10:00Z'),
  ('81000000-0000-0000-0000-000000000011', 'b0000000-0000-0000-0000-000000000008', 'api_view', 'd0000000-0000-0000-0000-000000000010', 'ApiAsset', NULL, '2025-06-02T09:15:00Z'),
  ('81000000-0000-0000-0000-000000000012', 'b0000000-0000-0000-0000-000000000001', 'api_view', 'd0000000-0000-0000-0000-000000000007', 'ApiAsset', NULL, '2025-06-01T11:00:00Z'),
  ('81000000-0000-0000-0000-000000000013', 'b0000000-0000-0000-0000-000000000003', 'api_view', 'd0000000-0000-0000-0000-000000000003', 'ApiAsset', NULL, '2025-06-01T08:15:00Z'),
  ('81000000-0000-0000-0000-000000000014', 'b0000000-0000-0000-0000-000000000005', 'search', NULL, NULL, 'inventory stock', '2025-06-02T07:30:00Z'),
  ('81000000-0000-0000-0000-000000000015', 'b0000000-0000-0000-0000-000000000003', 'search', NULL, NULL, 'shipping tracking', '2025-06-02T08:00:00Z'),
  ('81000000-0000-0000-0000-000000000016', 'b0000000-0000-0000-0000-000000000001', 'search', NULL, NULL, 'gateway', '2025-06-01T10:45:00Z'),
  ('81000000-0000-0000-0000-000000000017', 'b0000000-0000-0000-0000-000000000006', 'api_view', 'd0000000-0000-0000-0000-000000000001', 'ApiAsset', NULL, '2025-06-02T09:00:00Z'),
  ('81000000-0000-0000-0000-000000000018', 'b0000000-0000-0000-0000-000000000004', 'search', NULL, NULL, 'audit trail', '2025-06-01T14:30:00Z'),
  ('81000000-0000-0000-0000-000000000019', 'b0000000-0000-0000-0000-000000000004', 'api_view', 'd0000000-0000-0000-0000-000000000004', 'ApiAsset', NULL, '2025-06-01T14:35:00Z'),
  ('81000000-0000-0000-0000-000000000020', 'b0000000-0000-0000-0000-000000000007', 'search', NULL, NULL, 'notifications templates', '2025-05-30T11:10:00Z')
ON CONFLICT DO NOTHING;
