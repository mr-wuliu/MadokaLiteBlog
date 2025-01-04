#!/bin/bash

echo "Stopping MadokaLiteBlog services..."

# 查找并停止 API 进程
API_PIDS=$(pgrep -f "MadokaLiteBlog.Api")
if [ ! -z "$API_PIDS" ]; then
    echo "Found API processes: $API_PIDS"
    echo "Stopping API processes..."
    kill $API_PIDS
    
    # 等待进程结束
    sleep 2
    
    # 检查是否还在运行，如果是则强制终止
    if pgrep -f "MadokaLiteBlog.Api" > /dev/null; then
        echo "Force stopping API processes..."
        pkill -9 -f "MadokaLiteBlog.Api"
    fi
    echo "API processes stopped"
else
    echo "No API processes found"
fi

echo "All services stopped" 