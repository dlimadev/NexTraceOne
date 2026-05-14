# NexTraceOne Helm Chart

Helm chart para deploy do **NexTraceOne** em clusters Kubernetes.

## 📋 Pré-requisitos

- Kubernetes 1.24+
- Helm 3.8+
- PV provisioner support no cluster
- (Opcional) cert-manager para TLS automático
- (Opcional) Prometheus Operator para monitoring

## 🚀 Instalação Rápida

### 1. Adicionar repositório (futuro)
```bash
helm repo add nextraceone https://charts.nextraceone.com
helm repo update
```

### 2. Instalar com valores padrão
```bash
helm install my-nextraceone ./deploy/kubernetes/helm/nextraceone
```

### 3. Instalar com configurações customizadas
```bash
helm install my-nextraceone ./deploy/kubernetes/helm/nextraceone \
  --set ingress.enabled=true \
  --set ingress.hosts[0].host=app.nextraceone.com \
  --set postgresql.auth.password=MySecurePassword123
```

### 4. Instalar em namespace específico
```bash
kubectl create namespace nextraceone
helm install my-nextraceone ./deploy/kubernetes/helm/nextraceone \
  --namespace nextraceone \
  --create-namespace
```

## ⚙️ Configuração

### Parâmetros Principais

| Parâmetro | Descrição | Padrão |
|-----------|-----------|--------|
| `replicaCount` | Número de réplicas da aplicação | `3` |
| `image.repository` | Imagem Docker da aplicação | `nextraceone/apihost` |
| `image.tag` | Tag da imagem | `1.0.0` |
| `service.type` | Tipo de service Kubernetes | `ClusterIP` |
| `service.port` | Porta do service | `80` |
| `ingress.enabled` | Habilitar Ingress | `false` |
| `resources.limits.cpu` | Limite de CPU | `2000m` |
| `resources.limits.memory` | Limite de memória | `4Gi` |
| `autoscaling.enabled` | Habilitar HPA | `true` |
| `autoscaling.minReplicas` | Mínimo de réplicas | `3` |
| `autoscaling.maxReplicas` | Máximo de réplicas | `10` |

### Banco de Dados (PostgreSQL)

| Parâmetro | Descrição | Padrão |
|-----------|-----------|--------|
| `postgresql.enabled` | Deploy PostgreSQL via Helm | `true` |
| `postgresql.auth.username` | Usuário do banco | `nextraceone` |
| `postgresql.auth.password` | Senha do banco | `""` (gerada) |
| `postgresql.primary.persistence.size` | Tamanho do PVC | `50Gi` |

### Redis (Cache)

| Parâmetro | Descrição | Padrão |
|-----------|-----------|--------|
| `redis.enabled` | Deploy Redis via Helm | `true` |
| `redis.auth.enabled` | Autenticação habilitada | `true` |
| `redis.auth.password` | Senha do Redis | `""` (gerada) |

### Elasticsearch (Observabilidade)

| Parâmetro | Descrição | Padrão |
|-----------|-----------|--------|
| `elasticsearch.enabled` | Deploy Elasticsearch | `true` |
| `elasticsearch.replicas` | Número de nós | `3` |
| `elasticsearch.volumeClaimTemplate.resources.requests.storage` | Storage por nó | `50Gi` |

### Kafka (Event Streaming - Opcional)

| Parâmetro | Descrição | Padrão |
|-----------|-----------|--------|
| `kafka.enabled` | Deploy Kafka | `false` |
| `kafka.replicas` | Número de brokers | `3` |
| `kafka.persistence.size` | Storage por broker | `100Gi` |

## 🔧 Exemplos de Deploy

### Ambiente de Desenvolvimento
```yaml
# values-dev.yaml
replicaCount: 1

postgresql:
  auth:
    password: dev-password

resources:
  limits:
    cpu: 500m
    memory: 1Gi
  requests:
    cpu: 100m
    memory: 256Mi

autoscaling:
  enabled: false

elasticsearch:
  replicas: 1
```

```bash
helm install nextraceone-dev ./deploy/kubernetes/helm/nextraceone -f values-dev.yaml
```

### Ambiente de Produção
```yaml
# values-prod.yaml
replicaCount: 5

image:
  tag: "1.0.0"
  pullPolicy: Always

ingress:
  enabled: true
  className: nginx
  annotations:
    cert-manager.io/cluster-issuer: letsencrypt-prod
  hosts:
    - host: app.nextraceone.com
      paths:
        - path: /
          pathType: Prefix
  tls:
    - secretName: nextraceone-tls
      hosts:
        - app.nextraceone.com

resources:
  limits:
    cpu: 4000m
    memory: 8Gi
  requests:
    cpu: 1000m
    memory: 2Gi

autoscaling:
  enabled: true
  minReplicas: 5
  maxReplicas: 20

postgresql:
  primary:
    persistence:
      size: 200Gi
    resources:
      limits:
        cpu: 4000m
        memory: 8Gi

elasticsearch:
  replicas: 5
  volumeClaimTemplate:
    resources:
      requests:
        storage: 200Gi
```

```bash
helm install nextraceone-prod ./deploy/kubernetes/helm/nextraceone -f values-prod.yaml
```

## 📊 Monitoring

### Prometheus Integration
O chart inclui ServiceMonitor para Prometheus Operator:

```yaml
monitoring:
  serviceMonitor:
    enabled: true
    interval: 30s
```

### Health Checks
- **Readiness Probe**: `/ready` - Verifica se a aplicação está pronta para receber tráfego
- **Liveness Probe**: `/live` - Verifica se a aplicação está saudável

## 🔐 Segurança

### Secrets Gerenciados
O chart cria automaticamente os seguintes secrets:
- Database connection strings
- JWT signing key
- SMTP credentials (se configurado)
- Kafka credentials (se configurado)

### Security Context
```yaml
podSecurityContext:
  runAsNonRoot: true
  runAsUser: 1000
  fsGroup: 2000

containerSecurityContext:
  allowPrivilegeEscalation: false
  readOnlyRootFilesystem: true
  capabilities:
    drop:
      - ALL
```

## 🔄 Upgrade

### Backup antes do upgrade
```bash
# Backup de dados
kubectl exec -it <postgres-pod> -- pg_dump -U nextraceone > backup.sql

# Exportar valores atuais
helm get values my-nextraceone > current-values.yaml
```

### Executar upgrade
```bash
helm upgrade my-nextraceone ./deploy/kubernetes/helm/nextraceone \
  -f current-values.yaml \
  --set image.tag=1.1.0
```

### Rollback (se necessário)
```bash
helm rollback my-nextraceone 1
```

## 🗑️ Desinstalação

```bash
helm uninstall my-nextraceone

# Remover PVCs manualmente (dados persistentes)
kubectl delete pvc -l app.kubernetes.io/instance=my-nextraceone
```

## 🏗️ Arquitetura do Deploy

```
┌─────────────────────────────────────────┐
│         Ingress Controller              │
│     (nginx / traefik / istio)           │
└──────────────┬──────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────┐
│      Service (ClusterIP)                │
└──────────────┬──────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────┐
│   Deployment (3-10 réplicas)            │
│  ┌─────────┐ ┌─────────┐ ┌─────────┐  │
│  │ Pod 1   │ │ Pod 2   │ │ Pod N   │  │
│  └─────────┘ └─────────┘ └─────────┘  │
└────┬──────────┬──────────┬─────────────┘
     │          │          │
     ▼          ▼          ▼
┌─────────┐ ┌────────┐ ┌──────────┐
│PostgreSQL│ │ Redis  │ │Elastic-  │
│ (Stateful│ │(State- │ │ search   │
│  Set)    │ │ fulSet)│ │(Stateful │
│          │ │        │ │ Set)     │
└─────────┘ └────────┘ └──────────┘
```

## 📚 Recursos Adicionais

- [Documentação oficial Helm](https://helm.sh/docs/)
- [Kubernetes Best Practices](https://kubernetes.io/docs/concepts/configuration/overview/)
- [NexTraceOne Documentation](https://docs.nextraceone.com)

## 🤝 Contribuição

Para contribuir com melhorias no chart:
1. Testar localmente com `helm lint`
2. Validar templates com `helm template .`
3. Testar instalação em cluster local (minikube/kind)
4. Submeter PR com descrição das mudanças

---

**Versão do Chart:** 1.0.0  
**Versão da Aplicação:** 1.0.0  
**Última atualização:** 2026-05-12
