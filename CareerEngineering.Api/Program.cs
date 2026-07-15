using Microsoft.SemanticKernel;
using Scalar.AspNetCore;
using CareerEngineering.Api.Hubs;

#pragma warning disable SKEXP0010 // Suprime avisos de recursos experimentais da IA

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. CONFIGURAÇÃO DE SERVIÇOS (DI Container)
// ==========================================

// APIs Rest e Documentação
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

// Configura a interface visual do Scalar no ambiente de desenvolvimento
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); 
}

// Aplicação de Middlewares (A ordem importa!)
app.UseCors("AllowAngular");
app.UseAuthorization();

// Mapeamento de Endpoints e Hubs
app.MapControllers();
app.MapHub<CareerChatHub>("/careerChatHub");

app.Run();