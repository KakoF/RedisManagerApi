using Domain.Interfaces;
using Infrastructure;
using Scalar.AspNetCore;
using StackExchange.Redis;
using WebApi.Middlewares;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    RedisConnectionFactory.Create(builder.Configuration.GetSection("Redis")["ConnectionString"]!));

// Injeta o repositório
builder.Services.AddScoped<IRedisRepository, RedisRepository>();


builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
/*if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}*/
app.MapOpenApi();

app.MapScalarApiReference();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.UseMiddleware(typeof(ErrorHandlingMiddleware));

app.Run();
