#!/bin/bash

# 设置环境变量
export ASPNETCORE_ENVIRONMENT=Production

# 项目路径
API_DIR="/root/project/MadokaPublic/api"
CLIENT_DIR="/root/project/MadokaPublic/client"

# 停止现有的进程
pkill -f "MadokaLiteBlog.Api"
pkill -f "MadokaLiteBlogClient"

# 启动 API 服务
echo "Starting API service..."
cd $API_DIR
nohup dotnet MadokaLiteBlog.Api.dll > api.log 2>&1 &

# 启动 Blazor 客户端
echo "Starting Blazor client..."
cd $CLIENT_DIR
nohup dotnet MadokaLiteBlogClient.dll > client.log 2>&1 &

echo "Deployment completed!"
echo "Check logs at:"
echo "$API_DIR/api.log"
echo "$CLIENT_DIR/client.log" 