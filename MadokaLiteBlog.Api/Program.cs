using Npgsql;
using MadokaLiteBlog.Api.Service;
using MadokaLiteBlog.Api.Data;
using MadokaLiteBlog.Api.Extensions;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// 从配置文件中读取连接字符串
var connectionString = builder.Configuration.GetConnectionString("PostgresDb");

builder.Services.AddScoped<NpgsqlConnection>(sp => new NpgsqlConnection(connectionString));

builder.Services.AddScoped<PostMapper>();
builder.Services.AddScoped<PostServer>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 添加 CORS 配置
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// 自动注册所有 Jsonb 类型处理器
Assembly.GetExecutingAssembly().RegisterJsonbTypeHandlers();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();