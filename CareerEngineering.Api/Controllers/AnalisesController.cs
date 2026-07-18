using System.Security.Claims;
using CareerEngineering.Api.DTOs;
using CareerEngineering.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareerEngineering.Api.Controllers;

/// <summary>
/// Endpoints HTTP de metadados/histórico — consultas estáticas fora do SignalR.
/// </summary>
[ApiController]
[Route("api/analises")]
[Authorize]
public class AnalisesController : ControllerBase
{
    private readonly IAnaliseService _analiseService;

    public AnalisesController(IAnaliseService analiseService)
    {
        _analiseService = analiseService;
    }

    /// <summary>Lista leve para a Sidebar (Id, Titulo, DataCriacao, ModeloLLM).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AnaliseListItemDto>>> Listar()
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var itens = await _analiseService.ListarPorUsuarioAsync(userId);
        return Ok(itens);
    }

    /// <summary>Detalhe completo + mensagens ordenadas por DataEnvio.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AnaliseDetailDto>> ObterPorId(Guid id)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var detalhe = await _analiseService.ObterDetalheAsync(id, userId);
        if (detalhe is null) return NotFound();

        return Ok(detalhe);
    }

    /// <summary>Renomeia o título da análise (otimismo visual no front).</summary>
    [HttpPatch("{id:guid}/title")]
    public async Task<IActionResult> AtualizarTitulo(Guid id, [FromBody] UpdateTituloDto dto)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(dto.Titulo))
            return BadRequest(new { message = "O título é obrigatório." });

        var ok = await _analiseService.AtualizarTituloAsync(id, userId, dto.Titulo);
        if (!ok) return NotFound();

        return NoContent();
    }

    /// <summary>Remove a análise e o histórico (cascade delete no EF).</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Excluir(Guid id)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var ok = await _analiseService.ExcluirAsync(id, userId);
        if (!ok) return NotFound();

        return NoContent();
    }

    private string? GetUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")?.Value;
}
