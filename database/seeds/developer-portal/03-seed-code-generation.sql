-- ============================================================================
-- NexTraceOne — Developer Portal — Registos de geração de código de teste
-- Cria 6 registos cobrindo diferentes linguagens de programação e tipos de
-- artefacto gerado. Inclui cenários com e sem IA, com e sem template.
-- ============================================================================
-- GenerationType (armazenado como string no EF — ver configuração):
--   SdkClient, IntegrationExample, ContractTest, DataModels
-- Linguagens: CSharp, TypeScript, Python, Java, Go
-- ============================================================================

INSERT INTO dp_code_generation_records (
    "Id", "ApiAssetId", "ApiName", "ContractVersion", "RequestedById",
    "Language", "GenerationType", "GeneratedCode",
    "IsAiGenerated", "TemplateId", "GeneratedAt"
)
VALUES
    -- ── Registo 1: Cliente SDK em C# para Payments API — gerado por template ─
    (
        'd3000000-0000-0000-0000-000000000001',
        'e2000000-0000-0000-0000-000000000001',
        'Payments API',
        '2.1.0',
        'u1000000-0000-0000-0000-000000000003',
        'CSharp',
        'SdkClient',
        '// Auto-generated SDK Client for Payments API v2.1.0
using System.Net.Http;
using System.Text.Json;

namespace Acme.Payments.Client;

public sealed class PaymentsApiClient
{
    private readonly HttpClient _http;

    public PaymentsApiClient(HttpClient http) => _http = http;

    public async Task<PaymentResponse> CreatePaymentAsync(CreatePaymentRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("/api/v2/payments", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PaymentResponse>(ct);
    }
}',
        false,
        'sdk-csharp-httpclient-v2',
        '2025-03-08T10:00:00Z'
    ),

    -- ── Registo 2: Exemplo de integração em TypeScript para Payments API — gerado por IA
    (
        'd3000000-0000-0000-0000-000000000002',
        'e2000000-0000-0000-0000-000000000001',
        'Payments API',
        '2.1.0',
        'u1000000-0000-0000-0000-000000000002',
        'TypeScript',
        'IntegrationExample',
        '// AI-generated integration example for Payments API v2.1.0
import axios from "axios";

const API_BASE = "https://api.acme-corp.test/api/v2";

export async function createPayment(amount: number, currency: string): Promise<PaymentResponse> {
    const { data } = await axios.post<PaymentResponse>(`${API_BASE}/payments`, {
        amount,
        currency,
        description: "Integration example",
    });
    return data;
}

interface PaymentResponse {
    id: string;
    status: string;
    createdAt: string;
}',
        true,
        NULL,
        '2025-03-08T11:30:00Z'
    ),

    -- ── Registo 3: Testes de contrato em Python para Refunds API — gerado por template
    (
        'd3000000-0000-0000-0000-000000000003',
        'e2000000-0000-0000-0000-000000000002',
        'Refunds API',
        '1.0.0',
        'u1000000-0000-0000-0000-000000000003',
        'Python',
        'ContractTest',
        '# Auto-generated contract tests for Refunds API v1.0.0
import pytest
import requests

BASE_URL = "https://api.acme-corp.test/api/v1"

class TestRefundsContract:
    def test_create_refund_returns_201(self):
        response = requests.post(f"{BASE_URL}/refunds", json={
            "paymentId": "pay-001",
            "amount": 50.00,
            "reason": "Customer request"
        })
        assert response.status_code == 201

    def test_create_refund_negative_amount_returns_422(self):
        response = requests.post(f"{BASE_URL}/refunds", json={
            "paymentId": "pay-001",
            "amount": -10.00
        })
        assert response.status_code == 422',
        false,
        'contract-test-python-pytest-v1',
        '2025-03-09T14:00:00Z'
    ),

    -- ── Registo 4: Modelos de dados em Java para Processing API — gerado por IA
    (
        'd3000000-0000-0000-0000-000000000004',
        'e2000000-0000-0000-0000-000000000003',
        'Processing API',
        '3.0.0',
        'u1000000-0000-0000-0000-000000000007',
        'Java',
        'DataModels',
        '// AI-generated data models for Processing API v3.0.0
package com.globex.processing.models;

public record ProcessingRequest(
    String merchantId,
    BigDecimal amount,
    String currency,
    String description
) {}

public record ProcessingResponse(
    String id,
    String status,
    Instant createdAt,
    Instant updatedAt
) {}',
        true,
        NULL,
        '2025-03-09T16:45:00Z'
    ),

    -- ── Registo 5: Cliente SDK em Go para Settlements API — gerado por template
    (
        'd3000000-0000-0000-0000-000000000005',
        'e2000000-0000-0000-0000-000000000004',
        'Settlements API',
        '1.2.0',
        'u1000000-0000-0000-0000-000000000002',
        'Go',
        'SdkClient',
        '// Auto-generated SDK Client for Settlements API v1.2.0
package settlements

import (
    "context"
    "encoding/json"
    "fmt"
    "net/http"
)

type Client struct {
    BaseURL    string
    HTTPClient *http.Client
}

func (c *Client) ListSettlements(ctx context.Context, from, to string) ([]Settlement, error) {
    url := fmt.Sprintf("%s/api/v1/settlements?from=%s&to=%s", c.BaseURL, from, to)
    req, err := http.NewRequestWithContext(ctx, http.MethodGet, url, nil)
    if err != nil {
        return nil, err
    }
    resp, err := c.HTTPClient.Do(req)
    if err != nil {
        return nil, err
    }
    defer resp.Body.Close()

    var result struct {
        Items []Settlement `json:"items"`
    }
    return result.Items, json.NewDecoder(resp.Body).Decode(&result)
}',
        false,
        'sdk-go-stdlib-v1',
        '2025-03-10T08:20:00Z'
    ),

    -- ── Registo 6: Exemplo de integração em CSharp para Reconciliation API — gerado por IA
    (
        'd3000000-0000-0000-0000-000000000006',
        'e2000000-0000-0000-0000-000000000005',
        'Reconciliation API',
        '1.0.0',
        'u1000000-0000-0000-0000-000000000001',
        'CSharp',
        'IntegrationExample',
        '// AI-generated integration example for Reconciliation API v1.0.0
using System.Net.Http.Json;

var client = new HttpClient { BaseAddress = new Uri("https://api.acme-corp.test") };

var batch = await client.GetFromJsonAsync<ReconciliationBatch>(
    "/api/v1/reconciliation/batch/2025-02");

Console.WriteLine($"Batch {batch.Id}: {batch.ItemCount} items, status={batch.Status}");

record ReconciliationBatch(string Id, int ItemCount, string Status, DateTimeOffset CreatedAt);',
        true,
        NULL,
        '2025-03-10T09:00:00Z'
    )
ON CONFLICT ("Id") DO NOTHING;
