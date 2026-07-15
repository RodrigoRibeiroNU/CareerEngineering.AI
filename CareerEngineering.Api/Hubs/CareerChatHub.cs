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
    
    var chatHistory = new ChatHistory("Você é um mentor sênior.");
    chatHistory.AddUserMessage($"Vaga: {jobDescription}. Currículo: {resumeText}");

    // Esta chamada funciona passando os settings de forma explícita
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