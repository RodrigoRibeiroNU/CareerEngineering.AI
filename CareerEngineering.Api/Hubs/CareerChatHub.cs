using Microsoft.AspNetCore.SignalR;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace CareerEngineering.Api.Hubs;

public class CareerChatHub : Hub
{
    private readonly Kernel _kernel;

    public CareerChatHub(Kernel kernel)
    {
        _kernel = kernel;
    }

    public async Task StartAnalysis(string jobDescription, string resumeText)
    {
        var chatService = _kernel.GetRequiredService<IChatCompletionService>();
        
        // 🔥 FASE 3: O System Prompt Profissional
        string systemPrompt = @"Você é um Tech Recruiter Sênior e Engenheiro de Software especialista. 
Sua ÚNICA função é analisar o gap (lacunas) entre o currículo fornecido e a descrição da vaga.

REGRAS ESTRITAS DE COMPORTAMENTO:
1. VALIDAÇÃO DE DADOS: Se os textos enviados (vaga ou currículo) forem muito curtos, palavras isoladas (ex: 'teste'), jargões sem sentido ou não parecerem um currículo/vaga reais, RECUSE-SE a analisar. Responda EXATAMENTE com esta frase: '⚠️ Por favor, forneça uma descrição de vaga e um texto de currículo completos e válidos para que eu possa realizar a análise.'
2. SEM ALUCINAÇÃO: Não invente conselhos genéricos de carreira se não houver dados suficientes.
3. ANÁLISE DE GAP: Se os dados forem válidos, liste diretamente as tecnologias, ferramentas ou competências exigidas pela vaga que NÃO estão evidentes no currículo.
4. FORMATO: Use formatação Markdown (bullet points, negrito) para facilitar a leitura.
5. TOM: Seja objetivo, profissional e construtivo.";

        var chatHistory = new ChatHistory(systemPrompt);
        
        // Enviamos os dados do usuário de forma bem estruturada
        chatHistory.AddUserMessage($"--- VAGA ---\n{jobDescription}\n\n--- CURRÍCULO ---\n{resumeText}");

        var stream = chatService.GetStreamingChatMessageContentsAsync(chatHistory);

        await foreach (var content in stream)
        {
            if (!string.IsNullOrEmpty(content.Content))
            {
                await Clients.Caller.SendAsync("ReceiveToken", content.Content);
            }
        }
    }
}