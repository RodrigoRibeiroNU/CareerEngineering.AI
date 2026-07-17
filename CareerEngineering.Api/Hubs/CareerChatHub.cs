using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
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
    private readonly ICareerMentorService _mentorService; // 🔥 Injetando nosso serviço de dados

    public CareerChatHub(Kernel kernel, ICareerMentorService mentorService)
    {
        _kernel = kernel;
        _mentorService = mentorService;
    }

    public async Task StartAnalysis(string jobDescription, string resumeText, string userName, string userEmail)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return;

        var chatService = _kernel.GetRequiredService<IChatCompletionService>();

        // ----------------------------------------------------------------------------
        // PROMPT 1: Extração Bruta e Fria de Dados (Com trava de segurança contra travamentos)
        // ----------------------------------------------------------------------------
        string promptExtrator = @"Compare a Vaga e o Currículo fornecidos. 
Liste APENAS os nomes das ferramentas, metodologias ou certificações que são exigidos na vaga mas estão TOTALMENTE ausentes no currículo.
Se o currículo não tiver NENHUMA ligação com a vaga, responda apenas: 'DIVERGENTE'.
Seja direto, use tópicos simples e não escreva nenhuma justificativa ou introdução.";

        var historicoEtapa1 = new ChatHistory(promptExtrator);
        historicoEtapa1.AddUserMessage($"--- VAGA ---\n{jobDescription}\n\n--- CURRÍCULO ---\n{resumeText}");
        
        string gapsExtraidos = "";
        
        // 🔥 Criamos um token que cancela a requisição após 45 segundos se o hardware engasgar
        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(45)))
        {
            try
            {
                // Passamos o cts.Token como segundo argumento aqui
                var respostaBruta = await chatService.GetChatMessageContentAsync(historicoEtapa1, cancellationToken: cts.Token);
                gapsExtraidos = respostaBruta.Content ?? "Nenhum gap crítico encontrado.";
            }
            catch (OperationCanceledException)
            {
                // Se o hardware travar e estourar o tempo, enviamos um aviso limpo pro Angular e encerramos
                await Clients.Caller.SendAsync("ReceiveToken", "⚠️ O modelo local demorou muito para responder devido ao limite de hardware. Por favor, tente novamente ou verifique se os textos são válidos.");
                return;
            }
        }
    
        // 🔥 Validação inteligente: se forem perfis totalmente diferentes, o modelo vai responder "DIVERGENTE"
        if (gapsExtraidos.Contains("DIVERGENTE"))
        {
            await Clients.Caller.SendAsync("ReceiveToken", "⚠️ **Análise Inviável**: O currículo enviado não possui nenhuma aderência ou correlação com a área da vaga fornecida.");
            return; // Mata a execução aqui, poupando a sua memória de rodar o Prompt 2!
        }

        // ----------------------------------------------------------------------------
        // PROMPT 2: O Refinador Consultivo (Este sim faz o streaming para o Front-end)
        // ----------------------------------------------------------------------------
        string promptRefinador = @"Você é um Mentor de Carreira Sênior focado em recolocação profissional.
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

        // 1. Criamos os argumentos injetando a lista gerada no primeiro estágio
        var argumentos = new KernelArguments { ["listaGaps"] = gapsExtraidos };

        // 2. Renderizamos o template de string trocando a variável {{$listaGaps}} pelo valor real usando o Kernel
        var promptTemplateFactory = new KernelPromptTemplateFactory();
        var templateRenderizado = await promptTemplateFactory.Create(new PromptTemplateConfig(promptRefinador)).RenderAsync(_kernel, argumentos);

        // 3. Agora jogamos o texto já renderizado para dentro do ChatHistory
        var historicoEtapa2 = new ChatHistory();
        historicoEtapa2.AddUserMessage(templateRenderizado);

        // 🔥 CONFIGURAÇÃO CIRÚRGICA: O freio de mão físico para o modelo de 8B
        var settings = new OpenAIPromptExecutionSettings
        {
            PresencePenalty = 1.0,  // Alta penalidade impede repetição de blocos
            FrequencyPenalty = 1.0, // Impede reescrever as mesmas palavras
            MaxTokens = 450,        // Teto de tamanho para cortar duplicações compridas
            
            // Se ele terminar o relatório e tentar reescrever o cabeçalho inicial, o sinal cai na hora!
            StopSequences = new List<string> { "### 🛠️ Gaps de Ferramentas", "Por favor," }
        };

        // 4. Chamamos o streaming passando os tipos exatos: ChatHistory e PromptExecutionSettings
        var stream = chatService.GetStreamingChatMessageContentsAsync(historicoEtapa2, settings);
        
        var fullResponseBuilder = new StringBuilder();
        bool jaPassouPelasCertificacoes = false;

        await foreach (var content in stream)
        {
            if (!string.IsNullOrEmpty(content.Content))
            {
                string textoChunk = content.Content;
                fullResponseBuilder.Append(textoChunk);
                string textoAcumulado = fullResponseBuilder.ToString();

                // 1. Monitora se o modelo já imprimiu a última seção obrigatória
                if (textoAcumulado.Contains("Certificações"))
                {
                    jaPassouPelasCertificacoes = true;
                }

                // 2. GUILHOTINA INTELIGENTE: Se já passou pelas certificações e a IA tentar 
                // colocar textos corrido de despedida, observações ou notas, o C# corta na hora.
                if (jaPassouPelasCertificacoes)
                {
                    // Se após a seção de certificações ela tentar mandar textos comuns de encerramento:
                    if (textoChunk.Contains("Observação") || 
                        textoChunk.Contains("Lembre-se") || 
                        textoChunk.Contains("Nota:") ||
                        textoChunk.Contains("Geração concluída"))
                    {
                        break; // ✂️ Corta o stream no backend
                    }
                }

                // Transmite o token limpo pro Angular
                await Clients.Caller.SendAsync("ReceiveToken", textoChunk);
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
                    userName, 
                    userEmail, 
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