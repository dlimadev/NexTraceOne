#!/bin/bash
# Script para build e instalação da NexTraceOne CLI

set -e

echo "╔═══════════════════════════════════════════════════════════╗"
echo "║         NexTraceOne CLI - Build & Install                 ║"
echo "╚═══════════════════════════════════════════════════════════╝"
echo ""

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CLI_DIR="$SCRIPT_DIR/../tools/sdk-cli"

cd "$CLI_DIR"

# Verificar .NET SDK
if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET SDK não encontrado. Instale em: https://dotnet.microsoft.com/download"
    exit 1
fi

DOTNET_VERSION=$(dotnet --version)
echo "✅ .NET SDK encontrado: $DOTNET_VERSION"
echo ""

# Restaurar pacotes
echo "📦 Restaurando pacotes NuGet..."
dotnet restore src/NexTraceOne.Cli/NexTraceOne.Cli.csproj
echo ""

# Build
echo "🔨 Building CLI..."
dotnet build src/NexTraceOne.Cli/NexTraceOne.Cli.csproj --configuration Release
echo ""

# Opções de instalação
echo "Escolha uma opção de instalação:"
echo "1. Instalar como tool global (recomendado)"
echo "2. Executar localmente (para testing)"
echo "3. Publicar como standalone"
echo ""

read -p "Opção [1-3]: " choice

case $choice in
    1)
        echo ""
        echo "📥 Instalando como tool global..."
        dotnet pack src/NexTraceOne.Cli/NexTraceOne.Cli.csproj --configuration Release -o ./nupkg
        
        if [ -f ./nupkg/*.nupkg ]; then
            PACKAGE=$(ls ./nupkg/*.nupkg | head -1)
            dotnet tool install -g --add-source ./nupkg NexTraceOne.Cli
            
            echo ""
            echo "✅ Instalação concluída!"
            echo ""
            echo "Para usar:"
            echo "  ntrace --help"
            echo ""
            echo "Para atualizar no futuro:"
            echo "  dotnet tool update -g NexTraceOne.Cli"
        else
            echo "❌ Erro ao criar package NuGet"
            exit 1
        fi
        ;;
    
    2)
        echo ""
        echo "🚀 Executando localmente..."
        dotnet run --project src/NexTraceOne.Cli/NexTraceOne.Cli.csproj -- --help
        ;;
    
    3)
        echo ""
        echo "📦 Publicando como standalone..."
        
        # Detectar OS
        OS=$(uname -s)
        case $OS in
            Linux*)
                dotnet publish src/NexTraceOne.Cli/NexTraceOne.Cli.csproj \
                    -c Release \
                    -r linux-x64 \
                    --self-contained false \
                    -o ./publish/linux-x64
                echo "✅ Binário publicado em: ./publish/linux-x64/"
                echo ""
                echo "Para instalar:"
                echo "  sudo cp ./publish/linux-x64/ntrace /usr/local/bin/"
                echo "  sudo chmod +x /usr/local/bin/ntrace"
                ;;
            Darwin*)
                dotnet publish src/NexTraceOne.Cli/NexTraceOne.Cli.csproj \
                    -c Release \
                    -r osx-x64 \
                    --self-contained false \
                    -o ./publish/osx-x64
                echo "✅ Binário publicado em: ./publish/osx-x64/"
                echo ""
                echo "Para instalar:"
                echo "  sudo cp ./publish/osx-x64/ntrace /usr/local/bin/"
                echo "  sudo chmod +x /usr/local/bin/ntrace"
                ;;
            *)
                echo "⚠ OS não suportado automaticamente. Use opção 1 ou 2."
                ;;
        esac
        ;;
    
    *)
        echo "❌ Opção inválida"
        exit 1
        ;;
esac

echo ""
echo "🎉 NexTraceOne CLI pronto para uso!"
echo ""
echo "Próximos passos:"
echo "  1. Configure o endpoint: ntrace config set endpoint https://api.nextraceone.com"
echo "  2. Faça login: ntrace auth login --email user@example.com"
echo "  3. Verifique saúde: ntrace health check"
echo ""
