#!/bin/bash

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
SOLUTION_DIR="$( cd "$SCRIPT_DIR/.." && pwd )"

export ASPNETCORE_ENVIRONMENT=Production

API_DIR="/root/project/MadokaPublic/api"
CLIENT_DIR="/root/project/MadokaPublic/client"

mkdir -p $API_DIR
mkdir -p $CLIENT_DIR

echo "Building API project..."
cd "$SOLUTION_DIR/MadokaLiteBlog.Api"
if ! dotnet publish \
    -c Release \
    -o "$API_DIR" \
    --self-contained true \
    -r linux-x64 \
    /p:PublishReadyToRun=true \
    /p:DebugType=None \
    /p:DebugSymbols=false; then
    echo "错误: API 项目构建失败"
    exit 1
fi

echo "Building Blazor client..."
cd "$SOLUTION_DIR/MadokaLiteBlog.Client"
if ! dotnet publish \
    -c Release \
    -o "$CLIENT_DIR" \
    --self-contained true \
    -r linux-x64 \
    /p:DebugType=None \
    /p:DebugSymbols=false; then
    echo "错误: Blazor Client 项目构建失败"
    exit 1
fi

echo "Build completed and deployed to $API_DIR and $CLIENT_DIR"