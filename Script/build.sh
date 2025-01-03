#!/bin/bash

# 设置环境变量为生产环境
export ASPNETCORE_ENVIRONMENT=Production

# 项目目标路径
API_DIR="/root/project/MadokaPublic/api"
CLIENT_DIR="/root/project/MadokaPublic/client"

# 确保目标目录存在
mkdir -p $API_DIR
mkdir -p $CLIENT_DIR

# 构建后端 API 项目
echo "Building API project..."
cd MadokaLiteBlog.Api
dotnet publish \
    -c Release \
    -o $API_DIR \
    --self-contained true \
    -r linux-x64 \
    /p:PublishReadyToRun=true \
    /p:DebugType=None \
    /p:DebugSymbols=false

# 构建前端 Blazor 项目
echo "Building Blazor client..."
cd ../MadokaLiteBlogClient
dotnet publish \
    -c Release \
    -o $CLIENT_DIR \
    --self-contained true \
    -r linux-x64 \
    /p:PublishReadyToRun=true \
    /p:DebugType=None \
    /p:DebugSymbols=false

echo "Build completed and deployed to $API_DIR and $CLIENT_DIR"