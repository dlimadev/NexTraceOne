# NexTraceOne Artifact Signing & SBOM Guide

Guia completo para implementação de **artifact signing** e **SBOM (Software Bill of Materials)** no NexTraceOne.

## 🎯 Visão Geral

Artifact signing garante a integridade e autenticidade dos artifacts produzidos pelo pipeline CI/CD, enquanto SBOM fornece inventário completo de dependências para compliance e security scanning.

### Benefícios:

✅ **Supply Chain Security** - Verificação de proveniência  
✅ **Integrity Assurance** - Detecção de tampering  
✅ **Compliance** - Atende requisitos enterprise (SOC2, ISO 27001)  
✅ **Transparency** - Audit trail completo via Rekor  
✅ **Dependency Tracking** - Visibilidade completa de componentes  

---

## 🛠️ Ferramentas Utilizadas

### 1. Cosign (Sigstore)
- **Propósito:** Assinatura de containers, binaries, packages
- **Vantagens:** Keyless signing, transparency log, OIDC integration
- **Documentação:** https://docs.sigstore.dev/cosign/overview/

### 2. SBOM Tools
- **syft** (Anchore) - Geração de SBOM em formatos SPDX/CycloneDX
- **grype** - Vulnerability scanning baseado em SBOM
- **Documentação:** https://github.com/anchore/syft

### 3. Rekor (Transparency Log)
- **Propósito:** Log imutável de signatures e attestations
- **Vantagens:** Audit trail público, verificação temporal
- **Documentação:** https://docs.sigstore.dev/rekor/overview/

---

## 📋 Pré-requisitos

### Instalação de Ferramentas

```bash
# Instalar cosign
curl -O -L "https://github.com/sigstore/cosign/releases/latest/download/cosign-linux-amd64"
sudo mv cosign-linux-amd64 /usr/local/bin/cosign
sudo chmod +x /usr/local/bin/cosign

# Instalar syft
curl -sSfL https://raw.githubusercontent.com/anchore/syft/main/install.sh | sh -s -- -b /usr/local/bin

# Instalar grype
curl -sSfL https://raw.githubusercontent.com/anchore/grype/main/install.sh | sh -s -- -b /usr/local/bin

# Verificar instalações
cosign version
syft version
grype version
```

---

## 🔑 Configuração de Chaves

### Opção 1: Keyless Signing (Recomendado para CI/CD)

Cosign suporta signing sem gerenciamento de chaves usando OIDC:

```bash
# Login com GitHub (para workflows)
export COSIGN_EXPERIMENTAL=1

# Assinar artifact (abre browser para auth)
cosign sign ghcr.io/nextraceone/apihost:1.0.0
```

### Opção 2: Chaves Locais (Para testing)

```bash
# Gerar par de chaves
cosign generate-key-pair

# Arquivos gerados:
# - cosign.key (chave privada - PROTEGER!)
# - cosign.pub (chave pública - distribuir)
```

**⚠️ Segurança:** Nunca commit `cosign.key` no git! Use secrets management.

---

## ✍️ Assinando Artifacts

### 1. Docker Images

```bash
# Assinar imagem no registry
cosign sign --key cosign.key \
  ghcr.io/nextraceone/apihost:1.0.0

# Verificar assinatura
cosign verify --key cosign.pub \
  ghcr.io/nextraceone/apihost:1.0.0
```

### 2. Binários/NuGet Packages

```bash
# Assinar arquivo
cosign sign-blob --key cosign.key \
  --output-signature apihost.sig \
  --output-certificate apihost.pem \
  NexTraceOne.ApiHost.dll

# Verificar
cosign verify-blob --key cosign.pub \
  --signature apihost.sig \
  --certificate apihost.pem \
  NexTraceOne.ApiHost.dll
```

### 3. Usando Script Automatizado

```bash
./scripts/artifact-signing/sign-artifact.sh \
  ./publish/NexTraceOne.ApiHost.dll \
  1.0.0
```

---

## 📦 Gerando SBOM

### Usando syft

```bash
# SBOM de diretório do projeto
syft dir:. --output spdx-json > sbom.json

# SBOM de container image
syft ghcr.io/nextraceone/apihost:1.0.0 \
  --output spdx-json > image-sbom.json

# SBOM em formato CycloneDX
syft . --output cyclonedx-json > sbom-cyclonedx.json
```

### Exemplo de Output SBOM (SPDX):

```json
{
  "spdxVersion": "SPDX-2.3",
  "dataLicense": "CC0-1.0",
  "SPDXID": "SPDXRef-DOCUMENT",
  "name": "NexTraceOne",
  "documentNamespace": "https://nextraceone.com/spdx/uuid-123",
  "packages": [
    {
      "SPDXID": "SPDXRef-Package-1",
      "name": "Microsoft.Extensions.DependencyInjection",
      "versionInfo": "8.0.0",
      "downloadLocation": "https://nuget.org/packages/Microsoft.Extensions.DependencyInjection/8.0.0",
      "licenseConcluded": "MIT"
    }
  ],
  "relationships": [
    {
      "spdxElementId": "SPDXRef-Package",
      "relatedSpdxElement": "SPDXRef-Package-1",
      "relationshipType": "DEPENDS_ON"
    }
  ]
}
```

---

## 🔍 Scanning de Vulnerabilidades

### Usando grype com SBOM

```bash
# Scan direto de vulnerabilities
grype ghcr.io/nextraceone/apihost:1.0.0

# Scan baseado em SBOM
grype sbom:./sbom.json

# Output em JSON para CI/CD
grype sbom:./sbom.json --output json > vuln-report.json

# Fail on critical/high vulnerabilities
grype sbom:./sbom.json --fail-on high
```

### Exemplo de Report:

```
NAME                        INSTALLED  FIXED-IN  VULNERABILITY   SEVERITY
Newtonsoft.Json             13.0.2     13.0.3    CVE-2024-XXXXX  HIGH
System.Text.RegularExpressions  7.0.0  7.0.1     CVE-2024-YYYYY  CRITICAL
```

---

## 🚀 Integração CI/CD

### GitHub Actions Workflow

```yaml
name: Artifact Signing & SBOM

on:
  push:
    tags:
      - 'v*'

jobs:
  sign-and-sbom:
    runs-on: ubuntu-latest
    permissions:
      contents: read
      packages: write
      id-token: write  # Required for keyless signing

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Install cosign
        uses: sigstore/cosign-installer@v3

      - name: Install syft
        uses: anchore/sbom-action/download-syft@v0

      - name: Install grype
        run: |
          curl -sSfL https://raw.githubusercontent.com/anchore/grype/main/install.sh | sh -s -- -b /usr/local/bin

      - name: Build and push Docker image
        run: |
          docker build -t ghcr.io/nextraceone/apihost:${{ github.ref_name }} .
          docker push ghcr.io/nextraceone/apihost:${{ github.ref_name }}

      - name: Sign Docker image (keyless)
        env:
          COSIGN_EXPERIMENTAL: "true"
        run: |
          cosign sign ghcr.io/nextraceone/apihost:${{ github.ref_name }}

      - name: Generate SBOM
        run: |
          syft ghcr.io/nextraceone/apihost:${{ github.ref_name }} \
            --output spdx-json > sbom.json

      - name: Upload SBOM as artifact
        uses: actions/upload-artifact@v3
        with:
          name: sbom
          path: sbom.json

      - name: Vulnerability scan
        run: |
          grype sbom:./sbom.json --fail-on high

      - name: Verify signature
        run: |
          cosign verify ghcr.io/nextraceone/apihost:${{ github.ref_name }} \
            --certificate-identity-regexp "https://github.com/nextraceone" \
            --certificate-oidc-issuer "https://token.actions.githubusercontent.com"
```

---

## 📊 Policy Enforcement

### Criar Policy de Assinatura

```yaml
# policy.yaml
apiVersion: policy.sigstore.dev/v1beta1
kind: ClusterImagePolicy
metadata:
  name: nextraceone-policy
spec:
  images:
    - glob: "ghcr.io/nextraceone/**"
  authorities:
    - keyless:
        url: https://fulcio.sigstore.dev
        identities:
          - issuer: https://token.actions.githubusercontent.com
            subjectRegExp: "https://github.com/nextraceone/.*"
    - ctlog:
        url: https://rekor.sigstore.dev
```

### Aplicar no Kubernetes (Kyverno/OPA)

```yaml
# kyverno policy
apiVersion: kyverno.io/v1
kind: ClusterPolicy
metadata:
  name: check-image-signatures
spec:
  validationFailureAction: Enforce
  rules:
    - name: check-signature
      match:
        resources:
          kinds:
            - Pod
      validate:
        message: "Images must be signed"
        pattern:
          spec:
            containers:
              - image: "ghcr.io/nextraceone/*"
                imageVerify:
                  - key: "$(COSIGN_PUBLIC_KEY)"
```

---

## 🔐 Best Practices de Segurança

### 1. Proteção de Chaves

```bash
# ❌ NUNCA fazer:
git add cosign.key

# ✅ Fazer:
# Armazenar em secrets manager (AWS Secrets Manager, Azure Key Vault, HashiCorp Vault)
aws secretsmanager create-secret \
  --name nextraceone/cosign-key \
  --secret-string file://cosign.key
```

### 2. Rotação de Chaves

```bash
# Rotacionar chaves a cada 90 dias
cosign generate-key-pair

# Revogar chaves antigas (via Rekor)
# TODO: Implementar quando suportado
```

### 3. Multi-Signature

```bash
# Requer múltiplas assinaturas para produção
cosign sign --key key1.key image:tag
cosign sign --key key2.key image:tag

# Verificar todas as assinaturas
cosign verify --key key1.pub --key key2.pub image:tag
```

---

## 📈 Monitoring & Alerting

### Métricas para Monitorar

```prometheus
# Número de artifacts assinados
artifacts_signed_total{type="docker-image"} 150
artifacts_signed_total{type="nuget-package"} 45

# Falhas de verificação
signature_verification_failures_total 2

# SBOM generation
sbom_generated_total 195
sbom_vulnerabilities_found{severity="critical"} 0
sbom_vulnerabilities_found{severity="high"} 3
```

### Alertas (Prometheus Rules)

```yaml
groups:
  - name: artifact-signing
    rules:
      - alert: SignatureVerificationFailed
        expr: rate(signature_verification_failures_total[5m]) > 0
        for: 5m
        labels:
          severity: critical
        annotations:
          summary: "Signature verification failures detected"
          
      - alert: CriticalVulnerabilitiesFound
        expr: sbom_vulnerabilities_found{severity="critical"} > 0
        for: 1m
        labels:
          severity: critical
        annotations:
          summary: "Critical vulnerabilities found in SBOM"
```

---

## 🐛 Troubleshooting

### Erro: "cosign: command not found"

```bash
# Verificar instalação
which cosign

# Reinstalar
curl -O -L "https://github.com/sigstore/cosign/releases/latest/download/cosign-linux-amd64"
sudo mv cosign-linux-amd64 /usr/local/bin/cosign
sudo chmod +x /usr/local/bin/cosign
```

### Erro: "No matching signatures found"

```bash
# Verificar se artifact foi realmente assinado
cosign triangulate ghcr.io/nextraceone/apihost:1.0.0

# Verificar transparency log entry
cosign verify ghcr.io/nextraceone/apihost:1.0.0 --output text
```

### Erro: "SBOM generation failed"

```bash
# Verificar permissões
ls -la project-directory/

# Executar com verbose
syft dir:. --output spdx-json -vv > sbom.json 2>&1
```

---

## 📚 Recursos Adicionais

- [Sigstore Documentation](https://docs.sigstore.dev/)
- [Cosign GitHub](https://github.com/sigstore/cosign)
- [Syft GitHub](https://github.com/anchore/syft)
- [Grype GitHub](https://github.com/anchore/grype)
- [SPDX Specification](https://spdx.github.io/spdx-spec/v2.3/)
- [CycloneDX Specification](https://cyclonedx.org/specification/overview/)

---

**Versão:** 1.0  
**Última atualização:** 2026-05-13  
**Manutenção:** NexTraceOne Security Team
