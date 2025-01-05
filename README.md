# MadokaLiteBlog

## 简介

MadokaLiteBlog 是一个基于 .NET 8 的博客系统，使用 PostgreSQL 作为数据库，Dapper 作为 ORM 框架，NLog 作为日志框架。

## 技术栈

- .NET 8
- PostgreSQL
- Dapper
- NLog

## 任务列表

- [x] s3上传
- [x] 自动建表
- [x] markdown 基本渲染
- [x] Post 增删改查
- [x] RSA加密
- [x] 用户认证
- [x] 更好的数据库查询支持
- [x] 用户信息显示
- [x] 文章扫描
- [x] 登录入口
- [x] 自动部署
- [ ] 后台管理
  - [ ] Markdown在线编辑
        - [x] 文章在线发布
        - [ ] 支持图片上传
        - [x] 文章正文编辑
        - [ ] 文章预览(最好就是所见即所得的形式)
  - [ ] 访客统计
  - [ ] 用户信息更新, 注册等
  - [ ] 标签, 分类
  - [ ] 文章管理
  - [ ] 机器状态监控
- [ ] 页面美化
  - [ ] 使用小圆主题色
  - [ ] Markdown公式渲染错误
  - [ ] Markdown渲染优化
  - [ ] 主页背景展示
- [ ] 后端优化
  - [ ] 日志
  - [ ] PG连接池
  - [ ] CDN
  - [ ] Mapper重构
  - [ ] Docker继承支持

# 未来可能要做的事情

- [ ] 升级到.NET 9
- [ ] 更多的数据库支持