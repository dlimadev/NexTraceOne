# NexTraceOne — Pipeline de Build e Proteção de IP

## Pipeline oficial

```
1. dotnet build --configuration Release
2. dotnet-reactor --config obfuscate.xml       # Obfuscação IL
3. dotnet publish --runtime linux-x64 (AOT)    # Compilação nativa
4. sha256sum + gpg --sign                       # Assinatura de integridade
```

## Ambientes: linux-x64 (principal), win-x64 (IIS), linux-arm64 (futuro)
