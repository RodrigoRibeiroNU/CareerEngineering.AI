using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI; // Necessário para configurar a Temperatura

namespace CareerEngineering.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CareerMentorController : ControllerBase
{
    private readonly IChatCompletionService _chatCompletionService;

    public CareerMentorController(Kernel kernel)
    {
        _chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
    }

    [HttpPost("evaluate-gap")]
    public async Task<IActionResult> EvaluateGap([FromBody] GapAnalysisRequest request)
    {
        // 🛡️ 1. O SEGREDO: Configurando Temperatura ZERO para acabar com alucinações
        var executionSettings = new OpenAIPromptExecutionSettings
        {
            Temperature = 0.0f, // Frio e calculista: apenas fatos.
            TopP = 0.0f
        };

        // 🧠 2. Prompt Restritivo (Engenharia de Prompt Avançada)
        var systemPrompt = @"Você é um Tech Recruiter Sênior extremamente analítico.
Sua tarefa é cruzar os Requisitos da Vaga com o Currículo do Candidato.
Regras ABSOLUTAS:
1. Baseie-se EXCLUSIVAMENTE nos textos fornecidos.
2. NÃO invente ou assuma tecnologias que não estão na descrição da vaga.
3. Liste APENAS as tecnologias/habilidades que a vaga exige e que estão FALTANDO no currículo.
4. Seja direto e objetivo, retorne apenas uma lista em tópicos (bullet points).";

        var chatHistory = new ChatHistory(systemPrompt);
        
        // Injetamos os dados dinamicamente
        var userMessage = $"VAGA:\n{request.JobDescription}\n\nCURRÍCULO:\n{request.ResumeText}";
        chatHistory.AddUserMessage(userMessage);

        // 🚀 3. Execução enviando as configurações de temperatura
        var response = await _chatCompletionService.GetChatMessageContentAsync(chatHistory, executionSettings);

        return Ok(new { MissingTechnologies = response.Content });
    }
}

// Classe para receber o JSON estruturado do Frontend
public class GapAnalysisRequest
{
    public string JobDescription { get; set; } = string.Empty;
    public string ResumeText { get; set; } = string.Empty;
}