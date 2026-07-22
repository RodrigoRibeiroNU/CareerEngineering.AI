using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace CareerEngineering.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SystemController : ControllerBase
{
    private readonly Kernel _kernel;

    public SystemController(Kernel kernel)
    {
        _kernel = kernel;
    }

    [HttpGet("active-model")]
    public IActionResult GetActiveModel()
    {
        try
        {
            var chatService = _kernel.GetRequiredService<IChatCompletionService>();

            string modeloAtivo = chatService.Attributes.ContainsKey("ModelId")
                ? chatService.Attributes["ModelId"]?.ToString() ?? "Qwen 2.5 14B"
                : "Qwen 2.5 14B";

            return Ok(new { model = modeloAtivo });
        }
        catch (Exception)
        {
            return Ok(new { model = "Local LLM" });
        }
    }
}