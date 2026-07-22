using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.SemanticKernel;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using CareerEngineering.Api.Hubs;
using CareerEngineering.Api.Services;
using CareerEngineering.Api.Data;

#pragma warning disable SKEXP0010 // Suprime avisos de recursos experimentais da IA

var builder = WebApplication.CreateBuilder(args);

var auth0Domain = builder.Configuration["Auth0:Domain"]
    ?? throw new InvalidOperationException("Auth0:Domain não configurado.");
var auth0Audience = builder.Configuration["Auth0:Audience"]
    ?? throw new InvalidOperationException("Auth0:Audience não configurado.");

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? [];
if (allowedOrigins.Length == 0)
{
    throw new InvalidOperationException(
        "Cors:AllowedOrigins deve conter ao menos uma origem (appsettings ou variável Cors__AllowedOrigins__0).");
}

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection não configurada (appsettings ou ConnectionStrings__DefaultConnection).");
}

var ollamaEndpoint = builder.Configuration["Ollama:Endpoint"]
    ?? "http://localhost:11434/v1";
var ollamaModelId = builder.Configuration["Ollama:ModelId"]
    ?? "qwen2.5:14b";
var ollamaApiKey = builder.Configuration["Ollama:ApiKey"]
    ?? "ollama";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = auth0Domain;
        options.Audience = auth0Audience;

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/careerChatHub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

builder.Services.AddSingleton<Kernel>(sp =>
{
    var kernelBuilder = Kernel.CreateBuilder();
    kernelBuilder.AddOpenAIChatCompletion(
        modelId: ollamaModelId,
        apiKey: ollamaApiKey,
        endpoint: new Uri(ollamaEndpoint)
    );
    return kernelBuilder.Build();
});

builder.Services.AddScoped<IAnaliseService, AnaliseService>();
builder.Services.AddScoped<ICareerMentorService, CareerMentorService>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

var app = builder.Build();

// CORS antes de auth: cobre REST e WebSockets do SignalR com AllowCredentials.
app.UseCors("AllowAngular");
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapControllers();
app.MapHub<CareerChatHub>("/careerChatHub");

app.Run();
