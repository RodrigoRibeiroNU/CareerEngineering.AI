using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.SemanticKernel;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using CareerEngineering.Api.Hubs;
using CareerEngineering.Api.Services;
using CareerEngineering.Api.Data;

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
            ValidAudience = builder.Configuration["Auth0:Audience"],
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Auth0:Domain"]
        };

        // 🔥 A MUDANÇA ENTRA AQUI:
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // 1. O .NET intercepta a requisição e verifica se há um "access_token" na URL (Query String)
                var accessToken = context.Request.Query["access_token"];

                // 2. Se houver um token E o destino for o nosso Hub do SignalR...
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/careerChatHub"))
                {
                    // 3. Nós pegamos manualmente esse token da URL e falamos para o .NET:
                    // "Ei, use este token aqui para autenticar o usuário!"
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
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

// Registrar o nosso serviço de mentoria de carreira
builder.Services.AddScoped<ICareerMentorService, CareerMentorService>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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