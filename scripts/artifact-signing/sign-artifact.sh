#!/bin/bash
# Script para assinatura de artifacts usando cosign
# Uso: ./sign-artifact.sh <artifact-path> <version>

set -e

ARTIFACT_PATH=$1
VERSION=$2

if [ -z "$ARTIFACT_PATH" ] || [ -z "$VERSION" ]; then
    echo "❌ Uso: $0 <artifact-path> <version>"
    exit 1
fi

echo "╔═══════════════════════════════════════════════════════════╗"
echo "║         NexTraceOne Artifact Signing                      ║"
echo "╚═══════════════════════════════════════════════════════════╝"
echo ""

# Verificar se cosign está instalado
if ! command -v cosign &> /dev/null; then
    echo "❌ cosign não encontrado. Instale em: https://docs.sigstore.dev/cosign/installation/"
    exit 1
fi

COSIGN_VERSION=$(cosign version | head -1)
echo "✅ cosign encontrado: $COSIGN_VERSION"
echo ""

# Calcular checksum SHA256
echo "📊 Calculando checksum SHA256..."
CHECKSUM=$(sha256sum "$ARTIFACT_PATH" | awk '{print $1}')
echo "   Checksum: $CHECKSUM"
echo ""

# Gerar chave cosign (se não existir)
if [ ! -f cosign.key ]; then
    echo "🔑 Gerando par de chaves cosign..."
    cosign generate-key-pair
    echo "   Chaves geradas: cosign.key, cosign.pub"
    echo ""
else
    echo "✅ Chaves cosign existentes encontradas"
    echo ""
fi

# Assinar artifact
echo "✍️  Assinando artifact..."
cosign sign --key cosign.key "$ARTIFACT_PATH"

echo ""
echo "✅ Artifact assinado com sucesso!"
echo ""

# Verificar assinatura
echo "🔍 Verificando assinatura..."
cosign verify --key cosign.pub "$ARTIFACT_PATH"

echo ""
echo "✅ Verificação concluída!"
echo ""

# Gerar SBOM
echo "📦 Gerando SBOM (Software Bill of Materials)..."

# Em produção: usar syft ou spdx-tools
# syft "$ARTIFACT_PATH" -o spdx-json > sbom.json

echo "   ⚠ SBOM generation requires syft installation"
echo "   Install: curl -sSfL https://raw.githubusercontent.com/anchore/syft/main/install.sh | sh"
echo ""

# Salvar metadata
cat > artifact-metadata.json <<EOF
{
  "artifact": "$ARTIFACT_PATH",
  "version": "$VERSION",
  "checksum": "$CHECKSUM",
  "signed_at": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
  "signer": "NexTraceOne CI/CD Pipeline",
  "signature_tool": "cosign"
}
EOF

echo "📄 Metadata salva em: artifact-metadata.json"
echo ""
echo "🎉 Artifact signing completo!"
echo ""
echo "Próximos passos:"
echo "  1. Upload artifact + signature para registry"
echo "  2. Attestation no transparency log (Rekor)"
echo "  3. Policy enforcement no CI/CD"
echo ""
