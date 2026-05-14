# Load Testing Framework - NexTraceOne

Framework de testes de carga baseado em **k6** para validar performance, estabilidade e resiliência do NexTraceOne.

## 📋 Pré-requisitos

1. Instalar k6: https://k6.io/docs/getting-started/installation/
   ```bash
   # Windows (via Chocolatey)
   choco install k6

   # Linux
   sudo apt-get install k6

   # macOS
   brew install k6
   ```

2. Sistema NexTraceOne rodando (local ou staging)

## 🚀 Execução Rápida

### Smoke Test (validação básica - 30s)
```bash
k6 run tests/load-testing/scenarios/smoke-test.js
```

### Load Test (carga normal - 9min)
```bash
k6 run tests/load-testing/scenarios/load-test.js
```

### Stress Test (carga extrema - 24min)
```bash
k6 run tests/load-testing/scenarios/stress-test.js
```

### Spike Test (picos súbitos - 8min)
```bash
k6 run tests/load-testing/scenarios/spike-test.js
```

### Endurance Test (longa duração - 1h)
```bash
k6 run tests/load-testing/scenarios/endurance-test.js
```

## ⚙️ Configuração

### Variáveis de Ambiente

| Variável | Padrão | Descrição |
|----------|--------|-----------|
| `BASE_URL` | `http://localhost:5000` | URL base da API |
| `TEST_USER_EMAIL` | `loadtest@nextraceone.com` | Email de teste |
| `TEST_USER_PASSWORD` | `LoadTest@2026!` | Senha de teste |

Exemplo:
```bash
BASE_URL=https://staging.nextraceone.com k6 run tests/load-testing/scenarios/load-test.js
```

## 📊 Thresholds de Performance

### Padrão (THRESHOLDS)
- p95 response time < 500ms
- Error rate < 1%
- Throughput > 100 req/s

### Crítico (CRITICAL_ENDPOINT_THRESHOLDS)
- p95 response time < 300ms
- p99 response time < 500ms
- Error rate < 0.5%
- Throughput > 200 req/s

### Stress (STRESS_THRESHOLDS)
- p95 response time < 2000ms
- p99 response time < 5000ms
- Error rate < 5%
- Throughput > 50 req/s

### Endurance (ENDURANCE_THRESHOLDS)
- p95 response time < 800ms
- Average response time < 400ms
- Error rate < 2%
- Throughput > 80 req/s

## 📁 Estrutura

```
tests/load-testing/
├── scenarios/              # Scripts k6 por tipo de teste
│   ├── smoke-test.js      # Validação rápida (30s)
│   ├── load-test.js       # Carga normal (9min)
│   ├── stress-test.js     # Carga extrema (24min)
│   ├── spike-test.js      # Picos súbitos (8min)
│   └── endurance-test.js  # Longa duração (1h)
├── config/                 # Configurações reutilizáveis
│   ├── base-config.js     # URLs, headers, utilitários
│   └── thresholds.js      # Thresholds de performance
├── data/                   # Dados de teste
│   └── users.csv          # Usuários fictícios
├── reports/                # Relatórios gerados (gitignored)
└── README.md              # Esta documentação
```

## 🔧 Integração CI/CD

### GitHub Actions (smoke test em cada PR)
```yaml
name: Load Tests
on: [pull_request]

jobs:
  smoke-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Install k6
        run: sudo apt-get install k6
      - name: Run smoke test
        run: k6 run tests/load-testing/scenarios/smoke-test.js
```

### Execução Nightly (load test completo)
```yaml
name: Nightly Load Tests
on:
  schedule:
    - cron: '0 2 * * *'  # 2 AM UTC daily

jobs:
  load-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Install k6
        run: sudo apt-get install k6
      - name: Run load test
        run: k6 run tests/load-testing/scenarios/load-test.js --out json=report.json
      - name: Upload report
        uses: actions/upload-artifact@v3
        with:
          name: load-test-report
          path: report.json
```

## 📈 Análise de Resultados

### Saída Console
O k6 exibe métricas em tempo real:
```
✓ status is 200
✗ response time < 500ms
  ↳  95% — ✓ 190 / ✗ 10

checks.........................: 95.00% ✓ 190      ✗ 10
http_req_duration..............: avg=450ms  min=120ms  med=400ms  max=1200ms  p(90)=650ms  p(95)=750ms
http_req_failed................: 2.50%  ✓ 5        ✗ 195
http_reqs......................: 150.5/s
```

### Exportar Relatório JSON
```bash
k6 run tests/load-testing/scenarios/load-test.js --out json=report.json
```

### Dashboard Cloud (opcional)
```bash
k6 cloud tests/load-testing/scenarios/load-test.js
```

## 🎯 Cenários de Uso

### Antes de Deploy em Produção
1. Executar smoke test para validação básica
2. Executar load test para confirmar performance sob carga normal
3. Revisar métricas vs thresholds

### Após Mudanças Arquiteturais
1. Executar stress test para encontrar novo ponto de ruptura
2. Comparar com baseline anterior
3. Documentar melhorias/regressões

### Monitoramento Contínuo
1. Executar endurance test semanalmente
2. Monitorar degradação gradual (memory leaks, etc.)
3. Ajustar thresholds conforme evolução do sistema

## 🐛 Troubleshooting

### Erro: "Connection refused"
- Verificar se a API está rodando na URL correta
- Usar variável `BASE_URL` para apontar para o endpoint correto

### Thresholds falhando
- Reduzir número de VUs (virtual users)
- Aumentar duration dos stages
- Verificar recursos do servidor (CPU, memória, I/O)

### Timeouts frequentes
- Aumentar timeout no k6: `--http-debug`
- Verificar latência de rede
- Otimizar queries SQL lentas

## 📚 Recursos Adicionais

- [Documentação oficial k6](https://k6.io/docs/)
- [Best practices de load testing](https://k6.io/docs/test-types/introduction/)
- [Análise de resultados](https://k6.io/docs/results-visualization/)

## 🤝 Contribuição

Para adicionar novos cenários:
1. Criar arquivo em `scenarios/` seguindo padrão existente
2. Definir thresholds apropriados em `config/thresholds.js`
3. Atualizar este README
4. Testar localmente antes de commit

---

**Última atualização:** 2026-05-12  
**Versão k6 recomendada:** >= 0.45.0
