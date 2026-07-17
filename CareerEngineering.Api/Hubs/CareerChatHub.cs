using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Security.Claims;
using System.Text;
using CareerEngineering.Api.Services;

namespace CareerEngineering.Api.Hubs;

[Authorize]
public class CareerChatHub : Hub
{
    private readonly Kernel _kernel;
    private readonly ICareerMentorService _mentorService;
    private readonly ILogger<CareerChatHub> _logger;

    public CareerChatHub(
        Kernel kernel,
        ICareerMentorService mentorService,
        ILogger<CareerChatHub> logger)
    {
        _kernel = kernel;
        _mentorService = mentorService;
        _logger = logger;
    }

    public async Task StartAnalysis(string jobDescription, string resumeText, string userName, string userEmail)
    {
        try
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return;

            var chatService = _kernel.GetRequiredService<IChatCompletionService>();

            const string promptExtrator = @"Compare a Vaga e o Currículo fornecidos. 
Liste APENAS os nomes das ferramentas, metodologias ou certificações que são exigidos na vaga mas estão TOTALMENTE ausentes no currículo.
Se o currículo não tiver NENHUMA ligação com a vaga, responda apenas: 'DIVERGENTE'.
Seja direto, use tópicos simples e não escreva nenhuma justificativa ou introdução.";

            var historicoEtapa1 = new ChatHistory(promptExtrator);
            historicoEtapa1.AddUserMessage($"--- VAGA ---\n{jobDescription}\n\n--- CURRÍCULO ---\n{resumeText}");

            string gapsExtraidos;

            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(45)))
            {
                try
                {
                    var respostaBruta = await chatService.GetChatMessageContentAsync(
                        historicoEtapa1,
                        cancellationToken: cts.Token);
                    gapsExtraidos = respostaBruta.Content ?? "Nenhum gap crítico encontrado.";
                }
                catch (OperationCanceledException)
                {
                    await Clients.Caller.SendAsync(
                        "ReceiveToken",
                        "⚠️ O modelo local demorou muito para responder devido ao limite de hardware. Por favor, tente novamente ou verifique se os textos são válidos.");
                    return;
                }
            }

            if (gapsExtraidos.Contains("DIVERGENTE"))
            {
                await Clients.Caller.SendAsync(
                    "ReceiveToken",
                    "⚠️ **Análise Inviável**: O currículo enviado não possui nenhuma aderência ou correlação com a área da vaga fornecida.");
                return;
            }

            const string promptRefinador = @"Você é um Mentor de Carreira Sênior focado em recolocação profissional.
Sua única função é ler a <LISTA_BRUTA> de gaps fornecida e transformá-la em um feedback consultivo amigável, distribuindo os itens estritamente dentro do gabarito abaixo.

REGRAS CRÍTICAS:
1. Use APENAS os 3 cabeçalhos do gabarito. Nunca crie outras seções.
2. Certificações (PMP, PMI-ACP) vão APENAS em Certificações. Ferramentas (Azure) vão APENAS em Ferramentas. Metodologias (PMBOK, Métricas) vão APENAS em Metodologias.
3. Para cada item, use o formato: * **[Nome]**: [Explicação de 1 frase sobre a importância para a vaga].
4. Escreva o relatório e pare imediatamente. É terminantemente proibido repetir seções.

<LISTA_BRUTA>
{{$listaGaps}}
</LISTA_BRUTA>

GABARITO OBRIGATÓRIO DE SAÍDA:
### 🛠️ Gaps de Ferramentas e Tecnologias
### 📚 Gaps de Metodologias e Processos
### 🎓 Certificações e Diferenciais Relevantes";

            var argumentos = new KernelArguments { ["listaGaps"] = gapsExtraidos };
            var promptTemplateFactory = new KernelPromptTemplateFactory();
            var templateRenderizado = await promptTemplateFactory
                .Create(new PromptTemplateConfig(promptRefinador))
                .RenderAsync(_kernel, argumentos);

            var historicoEtapa2 = new ChatHistory();
            historicoEtapa2.AddUserMessage(templateRenderizado);

            var settings = new OpenAIPromptExecutionSettings
            {
                PresencePenalty = 1.0,
                FrequencyPenalty = 1.0,
                MaxTokens = 450,
                StopSequences = new List<string> { "### 🛠️ Gaps de Ferramentas", "Por favor," }
            };

            var stream = chatService.GetStreamingChatMessageContentsAsync(historicoEtapa2, settings);
            var fullResponseBuilder = new StringBuilder();
            var jaPassouPelasCertificacoes = false;

            await foreach (var content in stream)
            {
                if (string.IsNullOrEmpty(content.Content)) continue;

                var textoChunk = content.Content;
                fullResponseBuilder.Append(textoChunk);
                var textoAcumulado = fullResponseBuilder.ToString();

                if (textoAcumulado.Contains("Certificações"))
                {
                    jaPassouPelasCertificacoes = true;
                }

                if (jaPassouPelasCertificacoes &&
                    (textoChunk.Contains("Observação") ||
                     textoChunk.Contains("Lembre-se") ||
                     textoChunk.Contains("Nota:") ||
                     textoChunk.Contains("Geração concluída")))
                {
                    break;
                }

                await Clients.Caller.SendAsync("ReceiveToken", textoChunk);
            }

            var resultadoCompleto = fullResponseBuilder.ToString();

            if (!string.IsNullOrEmpty(resultadoCompleto) &&
                !resultadoCompleto.Contains("⚠️ Por favor, forneça uma descrição de vaga"))
            {
                try
                {
                    await _mentorService.SalvarAnaliseAsync(
                        userId,
                        userName,
                        userEmail,
                        jobDescription,
                        resumeText,
                        resultadoCompleto);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falha ao salvar análise para o usuário {UserId}", userId);
                }
            }
        }
        finally
        {
            try
            {
                await Clients.Caller.SendAsync("AnalysisCompleted");
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Falha ao notificar AnalysisCompleted; conexão pode ter sido encerrada.");
            }
        }
    }
}
