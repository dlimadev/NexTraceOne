# Guia de Migração: Docker Compose → Kubernetes (Helm)

Este guia ajuda a migrar deployments do NexTraceOne de Docker Compose para Kubernetes usando Helm charts.

## 📋 Visão Geral das Diferenças

| Aspecto | Docker Compose | Kubernetes (Helm) |
|---------|----------------|-------------------|
| **Orquestração** | Single-host | Multi-node cluster |
| **Escalabilidade** | Manual (replicas) | Auto-scaling (HPA) |
| **Service Discovery** | DNS interno | Services + Ingress |
| **Storage** | Volumes locais | PersistentVolumeClaims |
| **Configuração** | .env files | ConfigMaps + Secrets |
| **Health Checks** | healthcheck | Probes (readiness/liveness) |
| **Rolling Updates** | Manual | Automático |
| **Self-healing** | Não | Sim (restart pods) |
| **Load Balancing** | Não nativo | Service + Ingress |

## 🔄 Processo de Migração

### Passo 1: Preparar o Cluster Kubernetes

```bash
# Verificar se kubectl está configurado
kubectl cluster-info

# Criar namespace
kubectl create namespace nextraceone

# Verificar storage class disponível
kubectl get storageclass
```

### Passo 2: Exportar Configurações do Docker Compose

Do seu `docker-compose.yml`, identifique:

```yaml
# Variáveis de ambiente críticas
environment:
  - ConnectionStrings__IdentityDatabase=Host=db;Database=nextraceone_identity
  - JWT__Secret=sua-chave-secreta
  - Smtp__Host=smtp.example.com
  
# Volumes persistentes
volumes:
  - postgres_data:/var/lib/postgresql/data
  - elasticsearch_data:/usr/share/elasticsearch/data
```

### Passo 3: Criar values-custom.yaml

```yaml
# values-custom.yaml - Suas configurações específicas

# Imagem da aplicação
image:
  repository: sua-registry/nextraceone/apihost
  tag: "1.0.0"
  pullPolicy: Always

# Escala
replicaCount: 3

# Ingress
ingress:
  enabled: true
  className: nginx
  hosts:
    - host: app.nextraceone.com
      paths:
        - path: /
          pathType: Prefix
  tls:
    - secretName: nextraceone-tls
      hosts:
        - app.nextraceone.com

# Database credentials (use secrets em produção!)
postgresql:
  auth:
    username: nextraceone
    password: "SuaSenhaForte123!"
    database: nextraceone

# Recursos ajustados para seu workload
resources:
  limits:
    cpu: 2000m
    memory: 4Gi
  requests:
    cpu: 500m
    memory: 1Gi

# SMTP configuration
app:
  smtp:
    enabled: true
    host: smtp.example.com
    port: 587
    username: noreply@example.com
    password: "SmtpPassword123"
    fromEmail: noreply@example.com
    fromName: NexTraceOne

# CORS
app:
  cors:
    allowedOrigins:
      - "https://app.nextraceone.com"
```

### Passo 4: Migrar Dados (PostgreSQL)

```bash
# 1. Backup do Docker Compose
docker-compose exec postgres pg_dump -U nextraceone > backup.sql

# 2. Instalar Helm chart (inicialmente sem dados)
helm install nextraceone ./deploy/kubernetes/helm/nextraceone \
  -f values-custom.yaml \
  --namespace nextraceone

# 3. Aguardar PostgreSQL ficar pronto
kubectl wait --for=condition=ready pod -l app.kubernetes.io/name=postgresql \
  --namespace nextraceone --timeout=300s

# 4. Restaurar backup
POSTGRES_POD=$(kubectl get pods -l app.kubernetes.io/name=postgresql \
  -o jsonpath="{.items[0].metadata.name}" --namespace nextraceone)

kubectl cp backup.sql $POSTGRES_POD:/tmp/backup.sql --namespace nextraceone

kubectl exec -it $POSTGRES_POD --namespace nextraceone -- \
  psql -U nextraceone -d nextraceone -f /tmp/backup.sql
```

### Passo 5: Migrar Elasticsearch Data

```bash
# Se você tem dados importantes no Elasticsearch:

# 1. Snapshot dos índices (via API Elasticsearch)
curl -X PUT "http://localhost:9200/_snapshot/my_backup/snapshot_1" \
  -H 'Content-Type: application/json' \
  -d '{
    "indices": "nextraceone-*",
    "ignore_unavailable": true,
    "include_global_state": false
  }'

# 2. Após deploy Kubernetes, restaurar snapshot
curl -X POST "http://elasticsearch-master:9200/_snapshot/my_backup/snapshot_1/_restore"
```

### Passo 6: Validar Migração

```bash
# Verificar status do deployment
helm status nextraceone --namespace nextraceone

# Verificar pods
kubectl get pods --namespace nextraceone

# Verificar services
kubectl get svc --namespace nextraceone

# Testar health check
kubectl port-forward svc/nextraceone 8080:80 --namespace nextraceone &
curl http://localhost:8080/api/v1/platform/health

# Verificar logs
kubectl logs -l app.kubernetes.io/name=nextraceone --namespace nextraceone --tail=100
```

### Passo 7: Configurar Monitoring (Opcional)

```bash
# Se Prometheus Operator estiver instalado:
kubectl apply -f - <<EOF
apiVersion: monitoring.coreos.com/v1
kind: ServiceMonitor
metadata:
  name: nextraceone
  namespace: nextraceone
spec:
  selector:
    matchLabels:
      app.kubernetes.io/name: nextraceone
  endpoints:
    - port: http
      path: /metrics
      interval: 30s
EOF
```

### Passo 8: Configurar Backup Automatizado

```bash
# O Helm chart já inclui CronJob de backup
# Personalizar schedule em values.yaml:

backup:
  enabled: true
  schedule: "0 2 * * *"  # Daily at 2 AM
  retentionDays: 30
```

## 🔧 Ajustes Pós-Migração

### 1. Performance Tuning

```yaml
# Ajustar resources baseado em métricas reais
resources:
  limits:
    cpu: 4000m    # Aumentar se CPU throttling
    memory: 8Gi   # Aumentar se OOMKilled
  requests:
    cpu: 1000m
    memory: 2Gi
```

### 2. Auto-scaling

```yaml
autoscaling:
  enabled: true
  minReplicas: 3
  maxReplicas: 15  # Aumentar se necessário
  targetCPUUtilizationPercentage: 70
  targetMemoryUtilizationPercentage: 80
```

### 3. Network Policies (Segurança)

```yaml
# Adicionar network policies para restringir tráfego
networkPolicy:
  enabled: true
  ingress:
    - from:
        - namespaceSelector:
            matchLabels:
              name: ingress-nginx
  egress:
    - to:
        - podSelector:
            matchLabels:
              app.kubernetes.io/name: postgresql
```

## 🚨 Troubleshooting

### Problema: Pods não iniciam

```bash
# Verificar eventos
kubectl describe pod <pod-name> --namespace nextraceone

# Verificar logs de init containers
kubectl logs <pod-name> -c wait-for-database --namespace nextraceone
```

### Problema: Database connection failed

```bash
# Verificar se PostgreSQL está rodando
kubectl get pods -l app.kubernetes.io/name=postgresql --namespace nextraceone

# Testar conexão
kubectl run test-db --rm -i --tty --image=postgres:15 \
  --env="PGPASSWORD=<password>" \
  --command -- psql -h nextraceone-postgresql -U nextraceone -d nextraceone
```

### Problema: PVCs pendentes

```bash
# Verificar storage class
kubectl get storageclass

# Verificar eventos de PVC
kubectl describe pvc -l app.kubernetes.io/instance=nextraceone --namespace nextraceone
```

## 📊 Comparação de Custos

| Recurso | Docker Compose | Kubernetes |
|---------|----------------|------------|
| **Compute** | 1 VM grande | Múltiplos nós menores |
| **Storage** | Local disk | Cloud volumes (mais caro) |
| **Networking** | Bridge network | Load balancer (custo extra) |
| **Management** | Manual | Gerenciado (EKS/GKE/AKS) |
| **Total** | $50-100/mês | $200-500/mês |

**Benefício Kubernetes:** Alta disponibilidade, auto-scaling, self-healing justificam custo maior para produção.

## ✅ Checklist de Validação

- [ ] Todos os pods estão Running
- [ ] Health checks retornando 200 OK
- [ ] Database migrations executadas com sucesso
- [ ] Dados migrados corretamente
- [ ] Ingress roteando tráfego externo
- [ ] TLS/SSL configurado (se aplicável)
- [ ] Monitoring coletando métricas
- [ ] Logs centralizados funcionando
- [ ] Backup automatizado configurado
- [ ] Rollback testado e funcionando

## 🎯 Próximos Passos

Após migração bem-sucedida:

1. **Implementar GitOps** (ArgoCD/Flux) para deployments declarativos
2. **Configurar CI/CD** integrado com Helm upgrades
3. **Implementar service mesh** (Istio/Linkerd) para observabilidade avançada
4. **Configurar disaster recovery** com backups cross-region
5. **Otimizar custos** com spot instances e right-sizing

---

**Versão do Guia:** 1.0  
**Última atualização:** 2026-05-12  
**Compatível com:** Kubernetes 1.24+, Helm 3.8+
