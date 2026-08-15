#!/usr/bin/env bash
set -e

echo "[*] Compilando Heimdall em binário autossuficiente (Single File)..."
dotnet publish Heimdall.csproj -c Release -r linux-x64 /p:PublishAot=false /p:PublishSingleFile=true /p:SelfContained=true -o ./dist

# Renomeia o executável final para minúsculo
if [ -f "./dist/Heimdall" ]; then
    mv ./dist/Heimdall ./dist/heimdall
fi

chmod +x ./dist/heimdall
echo "[✔] Build concluído com sucesso! Binário autossuficiente gerado em: ./dist/heimdall"
