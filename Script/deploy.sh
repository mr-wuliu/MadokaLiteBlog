#!/bin/bash

# 设置环境变量
export ASPNETCORE_ENVIRONMENT=Production

# 项目路径
API_DIR="/root/project/MadokaPublic/api"
CLIENT_DIR="/root/project/MadokaPublic/client"

# 检查 API dll 文件是否存在
if [ ! -f "$API_DIR/MadokaLiteBlog.Api.dll" ]; then
    echo "错误: 找不到 API dll 文件: $API_DIR/MadokaLiteBlog.Api.dll"
    exit 1
fi

# 检查 Blazor 静态文件是否存在
if [ ! -d "$CLIENT_DIR/wwwroot" ]; then
    echo "错误: 找不到 Blazor wwwroot 目录: $CLIENT_DIR/wwwroot"
    exit 1
fi

# 停止现有的进程
pkill -f "MadokaLiteBlog.Api"

# 等待进程完全停止
sleep 2

# 启动 API 服务
echo "Starting API service..."
cd $API_DIR
nohup dotnet MadokaLiteBlog.Api.dll > api.log 2>&1 &
API_PID=$!

# 检查 API 是否成功启动
sleep 3
if ! ps -p $API_PID > /dev/null; then
    echo "错误: API 服务启动失败，请检查 api.log"
    exit 1
fi

echo "部署完成！"
echo "API 服务已启动"
echo "Blazor 静态文件已部署到 $CLIENT_DIR"
echo ""
echo "查看 API 日志："
echo "tail -f $API_DIR/api.log" 