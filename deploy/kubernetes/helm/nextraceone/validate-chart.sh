#!/bin/bash
# Script para validar e testar o Helm chart do NexTraceOne
# Uso: ./validate-helm-chart.sh [lint|template|install|upgrade]

set -e

CHART_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CHART_NAME="nextraceone"
RELEASE_NAME="nextraceone-test"
NAMESPACE="nextraceone-test"

# Cores
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}╔═══════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║         NexTraceOne Helm Chart Validation                 ║${NC}"
echo -e "${BLUE}╚═══════════════════════════════════════════════════════════╝${NC}"
echo ""

# Verificar se Helm está instalado
if ! command -v helm &> /dev/null; then
    echo -e "${RED}❌ Helm não está instalado. Instale em: https://helm.sh/docs/intro/install/${NC}"
    exit 1
fi

HELM_VERSION=$(helm version --short)
echo -e "${GREEN}✅ Helm encontrado: ${HELM_VERSION}${NC}"
echo ""

ACTION=${1:-lint}

case $ACTION in
    lint)
        echo -e "${YELLOW}▶ Executando helm lint...${NC}"
        if helm lint "$CHART_DIR"; then
            echo -e "${GREEN}✅ Helm lint passou sem erros!${NC}"
        else
            echo -e "${RED}❌ Helm lint falhou. Corrija os erros acima.${NC}"
            exit 1
        fi
        ;;
    
    template)
        echo -e "${YELLOW}▶ Renderizando templates...${NC}"
        helm template "$RELEASE_NAME" "$CHART_DIR" --debug > /tmp/nextraceone-templates.yaml
        
        if [ $? -eq 0 ]; then
            echo -e "${GREEN}✅ Templates renderizados com sucesso!${NC}"
            echo -e "   Output salvo em: /tmp/nextraceone-templates.yaml"
            
            # Contar recursos gerados
            RESOURCE_COUNT=$(grep -c "^kind:" /tmp/nextraceone-templates.yaml || true)
            echo -e "   Recursos gerados: ${RESOURCE_COUNT}"
        else
            echo -e "${RED}❌ Falha ao renderizar templates.${NC}"
            exit 1
        fi
        ;;
    
    install)
        echo -e "${YELLOW}▶ Instalando chart em namespace ${NAMESPACE}...${NC}"
        
        # Criar namespace se não existir
        kubectl create namespace "$NAMESPACE" --dry-run=client -o yaml | kubectl apply -f -
        
        # Instalar chart
        if helm install "$RELEASE_NAME" "$CHART_DIR" \
            --namespace "$NAMESPACE" \
            --set postgresql.auth.password=testpassword123 \
            --set redis.auth.password=testpassword123 \
            --wait --timeout 10m; then
            
            echo -e "${GREEN}✅ Chart instalado com sucesso!${NC}"
            echo ""
            echo -e "${BLUE}Para verificar status:${NC}"
            echo "  helm status $RELEASE_NAME --namespace $NAMESPACE"
            echo ""
            echo -e "${BLUE}Para ver pods:${NC}"
            echo "  kubectl get pods --namespace $NAMESPACE"
            echo ""
            echo -e "${BLUE}Para acessar a aplicação (port-forward):${NC}"
            echo "  kubectl port-forward svc/$RELEASE_NAME-nextraceone 8080:80 --namespace $NAMESPACE"
        else
            echo -e "${RED}❌ Falha na instalação.${NC}"
            exit 1
        fi
        ;;
    
    upgrade)
        echo -e "${YELLOW}▶ Simulando upgrade...${NC}"
        helm upgrade --dry-run "$RELEASE_NAME" "$CHART_DIR" \
            --namespace "$NAMESPACE" \
            --set image.tag=1.1.0
        
        if [ $? -eq 0 ]; then
            echo -e "${GREEN}✅ Upgrade simulado com sucesso!${NC}"
        else
            echo -e "${RED}❌ Falha na simulação de upgrade.${NC}"
            exit 1
        fi
        ;;
    
    uninstall)
        echo -e "${YELLOW}▶ Desinstalando chart...${NC}"
        helm uninstall "$RELEASE_NAME" --namespace "$NAMESPACE"
        
        # Remover namespace
        kubectl delete namespace "$NAMESPACE"
        
        echo -e "${GREEN}✅ Chart desinstalado e namespace removido.${NC}"
        ;;
    
    all)
        echo -e "${BLUE}📋 Executando validação completa...${NC}"
        echo ""
        
        # 1. Lint
        echo -e "${YELLOW}[1/4] Helm lint...${NC}"
        bash "$0" lint
        
        echo ""
        
        # 2. Template
        echo -e "${YELLOW}[2/4] Renderização de templates...${NC}"
        bash "$0" template
        
        echo ""
        
        # 3. Dry-run install
        echo -e "${YELLOW}[3/4] Dry-run installation...${NC}"
        helm install --dry-run --debug "$RELEASE_NAME" "$CHART_DIR" \
            --namespace "$NAMESPACE" \
            --create-namespace \
            --set postgresql.auth.password=testpassword123 > /dev/null
        
        if [ $? -eq 0 ]; then
            echo -e "${GREEN}✅ Dry-run installation passed!${NC}"
        else
            echo -e "${RED}❌ Dry-run installation failed.${NC}"
            exit 1
        fi
        
        echo ""
        
        # 4. Validate values
        echo -e "${YELLOW}[4/4] Validação de valores...${NC}"
        helm show values "$CHART_DIR" > /dev/null
        
        if [ $? -eq 0 ]; then
            echo -e "${GREEN}✅ Values validation passed!${NC}"
        else
            echo -e "${RED}❌ Values validation failed.${NC}"
            exit 1
        fi
        
        echo ""
        echo -e "${GREEN}╔═══════════════════════════════════════════════════════════╗${NC}"
        echo -e "${GREEN}║              Validação Completa com Sucesso!               ║${NC}"
        echo -e "${GREEN}╚═══════════════════════════════════════════════════════════╝${NC}"
        ;;
    
    *)
        echo -e "${RED}❌ Ação desconhecida: ${ACTION}${NC}"
        echo ""
        echo "Uso: $0 [lint|template|install|upgrade|uninstall|all]"
        echo ""
        echo "Ações disponíveis:"
        echo "  lint      - Validar sintaxe do chart"
        echo "  template  - Renderizar templates Kubernetes"
        echo "  install   - Instalar em cluster (requer kubectl)"
        echo "  upgrade   - Simular upgrade"
        echo "  uninstall - Remover instalação"
        echo "  all       - Executar todas as validações"
        exit 1
        ;;
esac

echo ""
echo -e "${BLUE}Próximos passos:${NC}"
echo "  - Revisar valores em values.yaml"
echo "  - Customizar para seu ambiente"
echo "  - Executar: helm install my-release ./deploy/kubernetes/helm/nextraceone"
echo ""
