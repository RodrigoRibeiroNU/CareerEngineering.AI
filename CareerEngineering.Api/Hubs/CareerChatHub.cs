using System.Security.Claims;
using System.Text;
using CareerEngineering.Api.Entities;
using CareerEngineering.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace CareerEngineering.Api.Hubs;

[Authorize]
public class CareerChatHub : Hub
{
    /// <summary>
    /// Sliding window defensivo: limita mensagens carregadas no contexto do Qwen local.
    /// </summary>
    private const int SlidingWindowSize = 12;

    private const string ModeloPadrao = "qwen2.5:14b";

    private readonly Kernel _kernel;
    private readonly IAnaliseService _analiseService;
    private readonly ILogger<CareerChatHub> _logger;

    public CareerChatHub(
        Kernel kernel,
        IAnaliseService analiseService,
        ILogger<CareerChatHub> logger)
    {
        _kernel = kernel;
        _analiseService = analiseService;
        _logger = logger;
    }

    /// <summary>
    /// Fluxo inicial (one-shot → sessão persistida): cria Analise, roda Generator-Refiner e grava a 1ª mensagem.
    /// </summary>
    public async Task StartAnalysis(string jobDescription, string resumeText, string userName, string userEmail)
    {
        Guid? analiseId = null;

        try
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return;

            if (string.IsNullOrWhiteSpace(jobDescription) || string.IsNullOrWhiteSpace(resumeText))
            {
                await Clients.Caller.SendAsync("ReceiveToken", "⚠️ Informe a descrição da vaga e o currículo.");
                return;
            }

            await _analiseService.EnsureUsuarioAsync(userId, userName, userEmail);

            var titulo = InferirTitulo(jobDescription);
            var analise = await _analiseService.CriarAnaliseAsync(
                userId,
                titulo,
                jobDescription.Trim(),
                resumeText.Trim(),
                ModeloPadrao);

            analiseId = analise.Id;

            // Notifica o cliente cedo para navegar para /analise/:id e atualizar a Sidebar.
            await Clients.Caller.SendAsync("AnalysisStarted", analise.Id.ToString(), analise.Titulo);

            var chatService = _kernel.GetRequiredService<IChatCompletionService>();
            var gapsExtraidos = await ExtrairGapsAsync(chatService, jobDescription, resumeText);
            if (gapsExtraidos is null)
            {
                // Timeout já foi enviado ao cliente; persiste para não deixar sessão órfã sem mensagens.
                const string timeoutMsg =
                    "⚠️ O modelo local demorou muito para responder devido ao limite de hardware. Por favor, tente novamente ou verifique se os textos são válidos.";
                await _analiseService.AdicionarMensagemAsync(analise.Id, "assistant", timeoutMsg);
                return;
            }

            if (gapsExtraidos.Contains("DIVERGENTE", StringComparison.OrdinalIgnoreCase))
            {
                const string aviso =
                    "⚠️ **Análise Inviável**: O currículo enviado não possui nenhuma aderência ou correlação com a área da vaga fornecida.";
                await Clients.Caller.SendAsync("ReceiveToken", aviso);
                await _analiseService.AdicionarMensagemAsync(analise.Id, "assistant", aviso);
                return;
            }

            var resultadoCompleto = await StreamRefinamentoAsync(chatService, gapsExtraidos);

            if (!string.IsNullOrWhiteSpace(resultadoCompleto) &&
                !resultadoCompleto.Contains("⚠️ Por favor, forneça uma descrição de vaga"))
            {
                await _analiseService.AdicionarMensagemAsync(analise.Id, "assistant", resultadoCompleto);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha em StartAnalysis. AnaliseId={AnaliseId}", analiseId);
            await Clients.Caller.SendAsync(
                "ReceiveToken",
                "\n\n⚠️ Ocorreu um erro ao processar a análise. Tente novamente.");
        }
        finally
        {
            await NotificarConclusaoAsync(analiseId);
        }
    }

    /// <summary>
    /// Continuação multi-turno: carrega âncoras (vaga/currículo) + sliding window e streama a resposta.
    /// </summary>
    public async Task SendChatMessage(string analiseId, string texto)
    {
        Guid? parsedId = null;

        try
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return;

            if (!Guid.TryParse(analiseId, out var id))
            {
                await Clients.Caller.SendAsync("ReceiveToken", "⚠️ Identificador de análise inválido.");
                return;
            }

            parsedId = id;

            if (string.IsNullOrWhiteSpace(texto))
            {
                await Clients.Caller.SendAsync("ReceiveToken", "⚠️ Digite uma mensagem para continuar a conversa.");
                return;
            }

            var analise = await _analiseService.ObterEntidadeDoUsuarioAsync(id, userId);
            if (analise is null)
            {
                await Clients.Caller.SendAsync("ReceiveToken", "⚠️ Análise não encontrada ou sem permissão.");
                return;
            }

            var pergunta = texto.Trim();
            await _analiseService.AdicionarMensagemAsync(analise.Id, "user", pergunta);

            var mensagens = await _analiseService.ObterUltimasMensagensAsync(analise.Id, SlidingWindowSize);
            var chatService = _kernel.GetRequiredService<IChatCompletionService>();

            var historico = MontarChatHistoryComAncoras(analise.DescricaoVaga, analise.TextoCurriculo, mensagens);

            var settings = new OpenAIPromptExecutionSettings
            {
                Temperature = 0.3f,
                MaxTokens = 800
            };

            var fullResponse = new StringBuilder();
            var stream = chatService.GetStreamingChatMessageContentsAsync(historico, settings);

            await foreach (var content in stream)
            {
                if (string.IsNullOrEmpty(content.Content)) continue;

                fullResponse.Append(content.Content);
                await Clients.Caller.SendAsync("ReceiveToken", content.Content);
            }

            var resposta = fullResponse.ToString();
            if (!string.IsNullOrWhiteSpace(resposta))
            {
                await _analiseService.AdicionarMensagemAsync(analise.Id, "assistant", resposta);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha em SendChatMessage. AnaliseId={AnaliseId}", analiseId);
            await Clients.Caller.SendAsync(
                "ReceiveToken",
                "\n\n⚠️ Ocorreu um erro ao gerar a resposta. Tente novamente.");
        }
        finally
        {
            await NotificarConclusaoAsync(parsedId);
        }
    }

    private async Task<string?> ExtrairGapsAsync(
        IChatCompletionService chatService,
        string jobDescription,
        string resumeText)
    {
        const string promptExtrator = @"Compare a Vaga e o Currículo fornecidos. 
Liste APENAS os nomes das ferramentas, metodologias ou certificações que são exigidos na vaga mas estão TOTALMENTE ausentes no currículo.
Se o currículo não tiver NENHUMA ligação com a vaga, responda apenas: 'DIVERGENTE'.
Seja direto, use tópicos simples e não escreva nenhuma justificativa ou introdução.";

        var historicoEtapa1 = new ChatHistory(promptExtrator);
        historicoEtapa1.AddUserMessage($"--- VAGA ---\n{jobDescription}\n\n--- CURRÍCULO ---\n{resumeText}");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        try
        {
            var respostaBruta = await chatService.GetChatMessageContentAsync(
                historicoEtapa1,
                cancellationToken: cts.Token);
            return respostaBruta.Content ?? "Nenhum gap crítico encontrado.";
        }
        catch (OperationCanceledException)
        {
            await Clients.Caller.SendAsync(
                "ReceiveToken",
                "⚠️ O modelo local demorou muito para responder devido ao limite de hardware. Por favor, tente novamente ou verifique se os textos são válidos.");
            return null;
        }
    }

    private async Task<string> StreamRefinamentoAsync(IChatCompletionService chatService, string gapsExtraidos)
    {
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

        return fullResponseBuilder.ToString();
    }

    /// <summary>
    /// Monta o ChatHistory com system prompt ancorado na vaga/currículo + janela deslizante de turnos.
    /// </summary>
    private static ChatHistory MontarChatHistoryComAncoras(
        string descricaoVaga,
        string textoCurriculo,
        IReadOnlyList<MensagemHistorico> mensagens)
    {
        var systemPrompt = $@"Você é um Mentor de Carreira Sênior do CareerEngineering.AI.
Continue a conversa com base na análise de gap entre a vaga e o currículo abaixo.
Seja objetivo, consultivo e responda em português.

--- VAGA (âncora) ---
{descricaoVaga}

--- CURRÍCULO (âncora) ---
{textoCurriculo}";

        var historico = new ChatHistory(systemPrompt);

        foreach (var msg in mensagens)
        {
            switch (msg.Role.ToLowerInvariant())
            {
                case "user":
                    historico.AddUserMessage(msg.Conteudo);
                    break;
                case "assistant":
                    historico.AddAssistantMessage(msg.Conteudo);
                    break;
                case "system":
                    historico.AddSystemMessage(msg.Conteudo);
                    break;
            }
        }

        return historico;
    }

    private async Task NotificarConclusaoAsync(Guid? analiseId)
    {
        try
        {
            await Clients.Caller.SendAsync("AnalysisCompleted", analiseId?.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Falha ao notificar AnalysisCompleted; conexão pode ter sido encerrada.");
        }
    }

    private string? GetUserId() =>
        Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? Context.User?.FindFirst("sub")?.Value;

    /// <summary>Título temporário inferido a partir do início da descrição da vaga.</summary>
    private static string InferirTitulo(string vaga)
    {
        if (string.IsNullOrWhiteSpace(vaga)) return "Nova análise";

        var linha = vaga.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .FirstOrDefault(l => l.Length > 0);

        if (string.IsNullOrEmpty(linha)) return "Nova análise";

        // Garante max 150 do mapeamento EF (ellipsis conta 1 char).
        const int maxTitulo = 150;
        if (linha.Length <= maxTitulo) return linha;
        return linha[..(maxTitulo - 1)].TrimEnd() + "…";
    }
}
