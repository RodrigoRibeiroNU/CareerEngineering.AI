using CareerEngineering.Api.Entities;

namespace CareerEngineering.Api.Services;

/// <summary>
/// Mantido por compatibilidade com o fluxo legado de persistência one-shot.
/// O histórico multi-turno usa <see cref="IAnaliseService"/>.
/// </summary>
public interface ICareerMentorService
{
    Task<Analise> SalvarAnaliseAsync(
        string usuarioId,
        string nome,
        string email,
        string vaga,
        string curriculo,
        string resultado);
}
