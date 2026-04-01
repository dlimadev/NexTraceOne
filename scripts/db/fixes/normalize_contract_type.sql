-- Normalizes legacy ContractType string values to the current enum names
-- Back up data before running in non-dev environments.
BEGIN;

-- Rest / Api variants -> RestApi
UPDATE "ctr_contract_drafts"
SET "ContractType" = 'RestApi'
WHERE "ContractType" IS NOT NULL
  AND lower("ContractType") IN ('api', 'rest', 'restapi');

-- Soap
UPDATE "ctr_contract_drafts"
SET "ContractType" = 'Soap'
WHERE "ContractType" IS NOT NULL
  AND lower("ContractType") = 'soap';

-- Event
UPDATE "ctr_contract_drafts"
SET "ContractType" = 'Event'
WHERE "ContractType" IS NOT NULL
  AND lower("ContractType") IN ('event', 'eventapi');

-- Background service
UPDATE "ctr_contract_drafts"
SET "ContractType" = 'BackgroundService'
WHERE "ContractType" IS NOT NULL
  AND lower("ContractType") IN ('background', 'backgroundservice');

-- Shared schema / schema
UPDATE "ctr_contract_drafts"
SET "ContractType" = 'SharedSchema'
WHERE "ContractType" IS NOT NULL
  AND lower("ContractType") IN ('sharedschema', 'schema');

-- Copybook
UPDATE "ctr_contract_drafts"
SET "ContractType" = 'Copybook'
WHERE "ContractType" IS NOT NULL
  AND lower("ContractType") = 'copybook';

-- MQ message variants
UPDATE "ctr_contract_drafts"
SET "ContractType" = 'MqMessage'
WHERE "ContractType" IS NOT NULL
  AND lower("ContractType") IN ('mqmessage', 'mq');

-- Fixed layout
UPDATE "ctr_contract_drafts"
SET "ContractType" = 'FixedLayout'
WHERE "ContractType" IS NOT NULL
  AND lower("ContractType") = 'fixedlayout';

-- CICS COMMAREA
UPDATE "ctr_contract_drafts"
SET "ContractType" = 'CicsCommarea'
WHERE "ContractType" IS NOT NULL
  AND lower("ContractType") IN ('cicscommarea', 'cicscomarea');

-- Webhook
UPDATE "ctr_contract_drafts"
SET "ContractType" = 'Webhook'
WHERE "ContractType" IS NOT NULL
  AND lower("ContractType") = 'webhook';

COMMIT;

-- Optional: show normalized counts
SELECT "ContractType", count(*) FROM "ctr_contract_drafts" GROUP BY "ContractType" ORDER BY count DESC;
