using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Security.Claims;
using System.Text;
using CareerEngineering.Api.Services;

namespace CareerEngineering.Api.Hubs;

[Authorize]
public class CareerChatHub : Hub
{
    private readonly Kernel _kernel;
    private readonly ICareerMentorService _mentorService; // 🔥 Injetando nosso serviço de dados

    public CareerChatHub(Kernel kernel, ICareerMentorService mentorService)
    {
        _kernel = kernel;
        _mentorService = mentorService;
    }

    public async Task StartAnalysis(string jobDescription, string resumeText, string userName, string userEmail)
    {
        // 1. Extração segura das claims do usuário autenticado no Auth0
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            await Clients.Caller.SendAsync("ReceiveToken", "❌ Erro de autenticação: Usuário não identificado.");
            return;
        }

        var chatService = _kernel.GetRequiredService<IChatCompletionService>();
        
        string systemPrompt = @"Você é um Tech Recruiter Sênior e Engenheiro de Software especialista. 
Sua ÚNICA função é analisar o gap (lacunas) entre o currículo fornecido e a descrição da vaga.

REGRAS ESTRITAS DE COMPORTAMENTO:
1. VALIDAÇÃO DE DADOS: Se os textos enviados (vaga ou currículo) forem muito curtos, palavras isoladas (ex: 'teste'), jargões sem sentido ou não parecerem um currículo/vaga reais, RECUSE-SE a analisar. Responda EXATAMENTE com esta frase: '⚠️ Por favor, forneça uma descrição de vaga e um texto de currículo completos e válidos para que eu possa realizar a análise.'
2. SEM ALUCINAÇÃO: Não invente conselhos genéricos de carreira se não houver dados suficientes.
3. ANÁLISE DE GAP: Se os dados forem válidos, liste diretamente as tecnologias, ferramentas ou competências exigidas pela vaga que NÃO estão evidentes no currículo.
4. FORMATO: Use formatação Markdown (bullet points, negrito) para facilitar a leitura.
5. TOM: Seja objetivo, profissional e construtivo.";

        var chatHistory = new ChatHistory(systemPrompt);
        chatHistory.AddUserMessage($"--- VAGA ---\n{jobDescription}\n\n--- CURRÍCULO ---\n{resumeText}");

        var stream = chatService.GetStreamingChatMessageContentsAsync(chatHistory);
        var fullResponseBuilder = new StringBuilder(); // 🔥 Acumulador do resultado para o banco de dados

        await foreach (var content in stream)
        {
            if (!string.IsNullOrEmpty(content.Content))
            {
                // Envia o pedaço ao front-end em tempo real
                await Clients.Caller.SendAsync("ReceiveToken", content.Content);
                
                // Acumula para salvar depois
                fullResponseBuilder.Append(content.Content);
            }
        }

        // 2. Persistência inteligente pós-streaming
        var resultadoCompleto = fullResponseBuilder.ToString();

        if (!string.IsNullOrEmpty(resultadoCompleto) && 
        !resultadoCompleto.Contains("⚠️ Por favor, forneça uma descrição de vaga"))
        {
            try
            {
                await _mentorService.SalvarAnaliseAsync(
                    userId,
                    userName, // 🔥 Agora usa o nome correto vindo do Angular
                    userEmail, // 🔥 Agora usa o e-mail correto vindo do Angular
                    jobDescription,
                    resumeText,
                    resultadoCompleto
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB ERROR] Falha ao salvar análise: {ex.Message}");
            }
        }
    }
}