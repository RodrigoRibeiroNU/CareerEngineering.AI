using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.SemanticKernel;
using Scalar.AspNetCore;
using CareerEngineering.Api.Hubs;

#pragma warning disable SKEXP0010 // Suprime avisos de recursos experimentais da IA

var builder = WebApplication.CreateBuilder(args);

var auth0Domain = builder.Configuration["Auth0:Domain"]!;
var auth0Audience = builder.Configuration["Auth0:Audience"]!;

// ==========================================
// 1. CONFIGURAÇÃO DE SERVIÇOS (DI Container)
// ==========================================

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = auth0Domain;
        options.Audience = auth0Audience;

        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Auth0:Audience"], // O seu identifier
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Auth0:Domain"]
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddOpenApi(); 

// Comunicação em Tempo Real (WebSockets)
builder.Services.AddSignalR();

// Segurança e CORS (Permite que o Angular conecte no SignalR)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy => 
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

// Núcleo de Inteligência Artificial (Semantic Kernel + Ollama)
builder.Services.AddSingleton<Kernel>(sp => 
{
    var kernelBuilder = Kernel.CreateBuilder();
    kernelBuilder.AddOpenAIChatCompletion(
        modelId: "llama3.1", 
        apiKey: "ignore", 
        endpoint: new Uri("http://localhost:11434/v1")
    );
    return kernelBuilder.Build();
});

// ==========================================
// 2. CONFIGURAÇÃO DO PIPELINE (Middlewares)
// ==========================================

var app = builder.Build();

app.UseCors("AllowAngular");
app.UseAuthentication(); 
app.UseAuthorization();

// Configura a interface visual do Scalar no ambiente de desenvolvimento
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); 
}

// Aplicação de Middlewares (A ordem importa!)
app.UseAuthorization();

// Mapeamento de Endpoints e Hubs
app.MapControllers();
app.MapHub<CareerChatHub>("/careerChatHub");

app.Run();