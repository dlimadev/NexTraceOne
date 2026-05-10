# Wave 6 — Gestão de Recursos, FinOps & Sustentabilidade

> **Prioridade:** Média
> **Esforço estimado:** L (Large)
> **Módulos impactados:** `operationalintelligence`, `governance`, `configuration`
> **Referência:** [INDEX.md](./INDEX.md)
> **Estado (Maio 2026):** W6-01 PARCIAL | W6-02 NAO IMPLEMENTADO | W6-03 IMPLEMENTADO | W6-04 NAO IMPLEMENTADO | W6-05 NAO IMPLEMENTADO

---

## Contexto

Em on-prem, o "cloud cost" é substituído por custo de hardware, energia e licenças.
O NexTraceOne já tem um módulo de Cost Intelligence. O que falta são:
1. **Capacidade de detectar desperdício computacional** nos serviços dos clientes
2. **Resource Budget por Tenant** para ambientes multi-tenant on-prem
3. **GreenOps** — correlação entre consumo e impacto ambiental

Benchmark de mercado (2026):
- 20-30% do gasto computacional é desperdiçado em recursos idle/subutilizados
- Green Software Foundation ratificou o SOFT Framework em finais de 2025
- Organizações que implementam rightsizing reduzem custos em 23.5% em média

---

## W6-01 — Waste Detection Engine

### Problema
Os clientes do NexTraceOne têm serviços que consomem recursos sem justificação
(idle, subutilizados, duplicados). A plataforma tem os dados de telemetria mas
não os analisa proactivamente para detectar desperdício.

### Solução
Job `WasteDetectionJob` que corre diariamente e produz recomendações:

**Tipos de desperdício detectados:**

| Tipo | Critério | Recomendação |
|---|---|---|
| **Serviço Idle** | 0 requests nas últimas 72h | Verificar se serviço está activo |
| **CPU Sobutilizado** | CPU < 5% em 95% do tempo (30 dias) | Redimensionar para instância menor |
| **Memória Sobutilizada** | Memória < 20% em 95% do tempo (30 dias) | Redimensionar heap/container |
| **Serviço Sem Owner** | `owner_team = null` ou owner inactivo | Atribuir owner ou deprecar |
| **Serviço Duplicado** | Sobreposição de domínio detectada pelo catalog | Consolidar serviços |
| **Ambiente Non-Prod activo fora de horas** | Non-prod com requests > 0 fora das 08h-20h | Agendar shutdown automático |

**Output:**
```json
{
  "analysis_date": "2026-04-15",
  "total_waste_score": 42,
  "estimated_monthly_savings": {
    "cpu_hours": 840,
    "memory_gb_hours": 12600,
    "energy_kwh": 180
  },
  "recommendations": [
    {
      "service_id": "svc_payment-api",
      "service_name": "payment-api",
      "waste_type": "CpuUnderutilized",
      "severity": "Medium",
      "evidence": "CPU médio: 2.3% (últimos 30 dias)",
      "recommendation": "Reduzir de 4 vCPUs para 1 vCPU",
      "estimated_saving_cpu_percent": 75
    }
  ]
}
```

### Estado de Implementação (Maio 2026): PARCIAL
Feature `GetWasteSignals` implementada com config key `finops.waste.detection_enabled`.
Os sinais de desperdício são consultados reactivamente mas não existe `WasteDetectionJob` Quartz
automático. A análise diária agendada e o tracking de poupanças realizadas estão pendentes.

### Critério de aceite
- [x] Job executável manualmente e em schedule configurável
- [x] Resultados visíveis na página de FinOps por serviço e equipa
- [x] Recomendações com evidência quantificada
- [x] Possibilidade de marcar recomendação como "Aceite", "Ignorada" ou "Em progresso"
- [ ] Histórico de recomendações e poupanças realizadas (job automático em falta)

---

## W6-02 — Non-Prod Shutdown Scheduler

### Problema
Ambientes de desenvolvimento e staging correm 24/7 desnecessariamente,
consumindo recursos fora do horário laboral.

### Solução
Scheduler configurável para cada ambiente não-produtivo:

```
Ambiente: staging-acme
├── Schedule activo: Segunda-Sexta 08:00-20:00 (GMT+1)
├── Fora de schedule: suspender ingestão de telemetria e alertas
├── Estimativa de poupança: 57% de redução em recursos de observabilidade
└── Override manual: "Manter activo até 23:59 hoje" (com justificação)
```

**Acções automáticas fora de schedule:**
- Parar ingestão de OTel (reduz volume de dados)
- Suspender alertas não-críticos
- Reduzir frequência de health checks
- Registar evento no audit trail

> Não desligar serviços dos clientes — apenas reduzir a carga do NexTraceOne
> no monitoramento de ambientes que não precisam de atenção.

### Estado de Implementação (Maio 2026): NAO IMPLEMENTADO
Não existe scheduler automático para ambientes não-produtivos. A suspensão de ingestão e alertas
fora de horário laboral não está implementada. Item pendente para iteração futura.

### Critério de aceite
- [ ] Schedule por ambiente configurável na UI
- [ ] Override manual com obrigatoriedade de justificação
- [ ] Estimativa de poupança visível antes de activar
- [ ] Acções automáticas auditadas

---

## W6-03 — Resource Budget por Tenant

### Problema
Em instalações on-prem com múltiplos tenants num servidor partilhado,
um tenant pode monopolizar disco, CPU ou requests, degradando os outros.

### Solução
Quotas por tenant configuráveis pelo `PlatformAdmin`:

```
Tenant: acme-corp
├── Storage (observabilidade): 50 GB
├── API requests/min: 500
├── AI tokens/mês: 100,000
├── Utilizadores activos: 50
└── Serviços no catalog: 200
```

**Comportamento ao atingir quota:**
- `Storage`: bloquear ingestão de novos dados, notificar admin do tenant
- `API requests`: rate limiting com resposta `429 Too Many Requests`
- `AI tokens`: bloquear chamadas AI, notificar utilizador com contexto
- Alertas a 80% da quota (aviso) e 100% (bloqueio)

### Estado de Implementação (Maio 2026): IMPLEMENTADO
Feature `GetResourceBudget` em `Governance.Application`. Quotas por tenant: CPU, Memory, Storage, API
requests/min e AI tokens/mês. Endpoints GET e UPDATE para `/resource-budget`.
Configuração via `Platform:ResourceBudget:*`. Alertas a 80% e bloqueio a 100%.

### Critério de aceite
- [x] Quotas configuráveis por `PlatformAdmin` por tenant
- [x] Dashboard de uso actual vs quota por tenant
- [x] Alertas a 80% e bloqueio a 100%
- [x] Override temporário com justificação e expiração

---

## W6-04 — GreenOps: Carbon Score por Serviço

### Problema
Empresas com metas ESG (Environmental, Social, Governance) precisam de medir
e reportar o impacto ambiental dos seus serviços de software. O NexTraceOne
tem os dados de CPU/memória/requests — falta correlacionar com emissões.

### Solução
**Carbon Score** por serviço, calculado a partir de telemetria real:

```
Carbon Score = CPU_hours × intensity_factor + Memory_GB_hours × 0.392 gCO₂/GB·h
               + Network_GB × 0.06 kgCO₂/GB

intensity_factor:
  → Configurável pelo admin (depende da localização do datacenter)
  → Default: 0.233 kgCO₂/kWh (média europeia 2026)
  → Exemplo PT: 0.18 kgCO₂/kWh (maior % renovável)
  → Exemplo PL: 0.68 kgCO₂/kWh (maior dependência de carvão)
```

**Dashboard GreenOps:**
```
Top 5 serviços por emissões (último mês)
├── payment-api       12.4 kgCO₂  ████████████  ↑ 8% vs mês anterior
├── order-service      8.1 kgCO₂  ████████
├── notification-svc   5.2 kgCO₂  █████
├── analytics-job      4.8 kgCO₂  ████▊
└── legacy-batch       3.1 kgCO₂  ███

Total da organização: 33.6 kgCO₂/mês
Equivalente a: 168 km de carro (Volkswagen Golf médio)
Meta ESG: 30 kgCO₂/mês → 12% acima do objectivo
```

### Estado de Implementação (Maio 2026): NAO IMPLEMENTADO
Não existe `CarbonScore` entity, cálculo de emissões CO₂ por serviço, nem dashboard GreenOps.
O módulo de Cost Intelligence existe mas sem correlação com emissões de carbono.
Item pendente para iteração futura (roadmap).

### Critério de aceite
- [ ] Carbon Score calculado diariamente por serviço
- [ ] Intensity factor configurável por datacenter/região
- [ ] Tendência mensal e comparação com mês anterior
- [ ] Exportável para relatórios ESG (CSV, PDF)
- [ ] Widget executivo na view de Governance

---

## W6-05 — Rightsizing Recommendations

### Problema
Mesmo que o desperdício seja detectado (W6-01), a equipa não sabe
qual o dimensionamento correcto para cada serviço.

### Solução
Recomendações de rightsizing baseadas em percentis reais:

```
Serviço: payment-api
Análise: últimos 30 dias

CPU Actual:    4 vCPUs
CPU p95:       0.8 vCPUs
CPU p99:       1.2 vCPUs
Recomendação:  2 vCPUs (margem de segurança 67% sobre p99)
Poupança:      50% de CPU

RAM Actual:    8 GB
RAM p95:       1.8 GB
RAM p99:       2.4 GB
Recomendação:  4 GB (margem de segurança 67% sobre p99)
Poupança:      50% de RAM

Impacto estimado na reliability: Baixo
  → Baseado em 0 OOM events nos últimos 30 dias
  → SLO actual: 99.95% (mantido com novo dimensionamento)
```

### Estado de Implementação (Maio 2026): NAO IMPLEMENTADO
Não existe `RightsizingRecommendation` entity nem análise de percentis p95/p99.
Os dados de CPU/RAM por serviço estão disponíveis via telemetria mas o motor de rightsizing
não foi implementado. Item pendente para iteração futura.

### Critério de aceite
- [ ] Análise baseada em percentis p95 e p99 reais (não médias)
- [ ] Margem de segurança configurável (padrão: 67% sobre p99)
- [ ] Correlação com SLO — não recomendar se mudar o SLO em risco
- [ ] Historial de recomendações aceites vs impacto real

---

## Referências de Mercado

- Sedai: AI-driven rightsizing com análise de percentis reais
- Kubecost 3.0: GPU monitoring + cloud cost integration + rightsizing
- Green Software Foundation SOFT Framework (ratificado Nov 2025)
- Dynatrace Sustainable Kubernetes: CO₂ + custo correlacionados
- FinOps Foundation: tagging, chargeback e waste elimination como pilares
