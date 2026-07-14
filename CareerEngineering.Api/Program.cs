using Microsoft.SemanticKernel;
using Scalar.AspNetCore;

// 1. ESTA É A LINHA QUE ESTAVA FALTANDO! Ela cria o 'builder'
var builder = WebApplication.CreateBuilder(args);

// Adiciona os serviços essenciais de uma API
builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// Gera a documentação OpenAPI nativa do .NET
builder.Services.AddOpenApi(); 

// Configurando a IA Local (Ollama)
builder.Services.AddKernel().AddOpenAIChatCompletion(
    modelId: "llama3.1", 
    apiKey: "ignore", 
    endpoint: new Uri("http://localhost:11434/v1") 
);

// Constrói a aplicação com base no builder que configuramos acima
var app = builder.Build();

// Configura a interface visual do Scalar no ambiente de desenvolvimento
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); 
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();