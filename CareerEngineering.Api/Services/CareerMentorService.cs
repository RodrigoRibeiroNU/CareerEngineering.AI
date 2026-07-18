using CareerEngineering.Api.Entities;

namespace CareerEngineering.Api.Services;

/// <summary>
/// Adaptador legado: cria análise + primeira mensagem do assistente via IAnaliseService.
/// </summary>
public class CareerMentorService : ICareerMentorService
{
    private readonly IAnaliseService _analiseService;

    public CareerMentorService(IAnaliseService analiseService)
    {
        _analiseService = analiseService;
    }

    public async Task<Analise> SalvarAnaliseAsync(
        string usuarioId,
        string nome,
        string email,
        string vaga,
        string curriculo,
        string resultado)
    {
        await _analiseService.EnsureUsuarioAsync(usuarioId, nome, email);

        var titulo = InferirTitulo(vaga);
        var analise = await _analiseService.CriarAnaliseAsync(
            usuarioId,
            titulo,
            vaga,
            curriculo,
            "qwen2.5:14b");

        if (!string.IsNullOrWhiteSpace(resultado))
        {
            await _analiseService.AdicionarMensagemAsync(analise.Id, "assistant", resultado);
        }

        return analise;
    }

    private static string InferirTitulo(string vaga)
    {
        if (string.IsNullOrWhiteSpace(vaga)) return "Nova análise";

        var linha = vaga.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .FirstOrDefault(l => l.Length > 0);

        if (string.IsNullOrEmpty(linha)) return "Nova análise";

        const int maxTitulo = 150;
        if (linha.Length <= maxTitulo) return linha;
        return linha[..(maxTitulo - 1)].TrimEnd() + "…";
    }
}