-- =============================================================================
-- Script 02: Seed de contratos SOAP/WSDL
-- Módulo: Contracts & Interoperability (Módulo 5)
-- Uso: Apenas desenvolvimento/debug — NÃO executar em produção
-- Pré-requisito: Executar 00-reset.sql e 01-rest-contracts.sql antes
-- =============================================================================

-- Serviço de Pagamentos WSDL — versão 1.0.0 (Draft)
-- Cenário: serviço SOAP legado com duas operações, demonstra parsing de portType
INSERT INTO catalog.ct_contract_versions (
    id, api_asset_id, sem_ver, spec_content, format, protocol, lifecycle_state,
    imported_from, is_locked, created_at, created_by, updated_at, updated_by, is_deleted
) VALUES (
    'c5020001-0000-0000-0000-000000000001'::uuid,
    'c5000001-0000-0000-0000-000000000003'::uuid,
    '1.0.0',
    '<?xml version="1.0"?><definitions name="PaymentService" xmlns="http://schemas.xmlsoap.org/wsdl/" xmlns:xsd="http://www.w3.org/2001/XMLSchema"><types><xsd:schema><xsd:element name="ProcessPaymentRequest" type="xsd:string"/><xsd:element name="ProcessPaymentResponse" type="xsd:string"/><xsd:element name="RefundPaymentRequest" type="xsd:string"/><xsd:element name="RefundPaymentResponse" type="xsd:string"/></xsd:schema></types><message name="ProcessPaymentRequest"><part name="parameters" element="ProcessPaymentRequest"/></message><message name="ProcessPaymentResponse"><part name="result" element="ProcessPaymentResponse"/></message><message name="RefundPaymentRequest"><part name="parameters" element="RefundPaymentRequest"/></message><message name="RefundPaymentResponse"><part name="result" element="RefundPaymentResponse"/></message><portType name="PaymentPortType"><operation name="ProcessPayment"><input message="ProcessPaymentRequest"/><output message="ProcessPaymentResponse"/></operation><operation name="RefundPayment"><input message="RefundPaymentRequest"/><output message="RefundPaymentResponse"/></operation></portType></definitions>',
    'xml', 'Wsdl', 'Draft',
    'migration', false,
    NOW() - INTERVAL '60 days', 'seed-script', NOW() - INTERVAL '60 days', 'seed-script', false
);

-- Serviço de Pagamentos WSDL — versão 1.1.0 (Approved, mudança aditiva)
-- Cenário: nova operação CheckPaymentStatus adicionada — mudança aditiva non-breaking
INSERT INTO catalog.ct_contract_versions (
    id, api_asset_id, sem_ver, spec_content, format, protocol, lifecycle_state,
    imported_from, is_locked, created_at, created_by, updated_at, updated_by, is_deleted
) VALUES (
    'c5020001-0000-0000-0000-000000000002'::uuid,
    'c5000001-0000-0000-0000-000000000003'::uuid,
    '1.1.0',
    '<?xml version="1.0"?><definitions name="PaymentService" xmlns="http://schemas.xmlsoap.org/wsdl/" xmlns:xsd="http://www.w3.org/2001/XMLSchema"><types><xsd:schema><xsd:element name="ProcessPaymentRequest" type="xsd:string"/><xsd:element name="ProcessPaymentResponse" type="xsd:string"/><xsd:element name="RefundPaymentRequest" type="xsd:string"/><xsd:element name="RefundPaymentResponse" type="xsd:string"/><xsd:element name="CheckPaymentStatusRequest" type="xsd:string"/><xsd:element name="CheckPaymentStatusResponse" type="xsd:string"/></xsd:schema></types><message name="ProcessPaymentRequest"><part name="parameters" element="ProcessPaymentRequest"/></message><message name="ProcessPaymentResponse"><part name="result" element="ProcessPaymentResponse"/></message><message name="RefundPaymentRequest"><part name="parameters" element="RefundPaymentRequest"/></message><message name="RefundPaymentResponse"><part name="result" element="RefundPaymentResponse"/></message><message name="CheckPaymentStatusRequest"><part name="paymentId" element="CheckPaymentStatusRequest"/></message><message name="CheckPaymentStatusResponse"><part name="status" element="CheckPaymentStatusResponse"/></message><portType name="PaymentPortType"><operation name="ProcessPayment"><input message="ProcessPaymentRequest"/><output message="ProcessPaymentResponse"/></operation><operation name="RefundPayment"><input message="RefundPaymentRequest"/><output message="RefundPaymentResponse"/></operation><operation name="CheckPaymentStatus"><input message="CheckPaymentStatusRequest"/><output message="CheckPaymentStatusResponse"/></operation></portType></definitions>',
    'xml', 'Wsdl', 'Approved',
    'migration', false,
    NOW() - INTERVAL '45 days', 'seed-script', NOW() - INTERVAL '45 days', 'seed-script', false
);

-- Serviço de Pagamentos WSDL — versão 2.0.0 (InReview, breaking change)
-- Cenário: operação RefundPayment removida — breaking change
INSERT INTO catalog.ct_contract_versions (
    id, api_asset_id, sem_ver, spec_content, format, protocol, lifecycle_state,
    imported_from, is_locked, created_at, created_by, updated_at, updated_by, is_deleted
) VALUES (
    'c5020001-0000-0000-0000-000000000003'::uuid,
    'c5000001-0000-0000-0000-000000000003'::uuid,
    '2.0.0',
    '<?xml version="1.0"?><definitions name="PaymentService" xmlns="http://schemas.xmlsoap.org/wsdl/" xmlns:xsd="http://www.w3.org/2001/XMLSchema"><types><xsd:schema><xsd:element name="ProcessPaymentRequest" type="xsd:string"/><xsd:element name="ProcessPaymentResponse" type="xsd:string"/><xsd:element name="CheckPaymentStatusRequest" type="xsd:string"/><xsd:element name="CheckPaymentStatusResponse" type="xsd:string"/></xsd:schema></types><message name="ProcessPaymentRequest"><part name="parameters" element="ProcessPaymentRequest"/></message><message name="ProcessPaymentResponse"><part name="result" element="ProcessPaymentResponse"/></message><message name="CheckPaymentStatusRequest"><part name="paymentId" element="CheckPaymentStatusRequest"/></message><message name="CheckPaymentStatusResponse"><part name="status" element="CheckPaymentStatusResponse"/></message><portType name="PaymentPortType"><operation name="ProcessPayment"><input message="ProcessPaymentRequest"/><output message="ProcessPaymentResponse"/></operation><operation name="CheckPaymentStatus"><input message="CheckPaymentStatusRequest"/><output message="CheckPaymentStatusResponse"/></operation></portType></definitions>',
    'xml', 'Wsdl', 'InReview',
    'migration', false,
    NOW() - INTERVAL '10 days', 'seed-script', NOW() - INTERVAL '10 days', 'seed-script', false
);
